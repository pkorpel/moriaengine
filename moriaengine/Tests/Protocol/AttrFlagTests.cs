using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Coverage for the item ATTR flag core infrastructure (#215/#217) - the
	/// ItemAttributes enum/storage, ".def" "ATTR = x|y" parsing, and the dedicated D_AttrList
	/// checkbox dialog (reachable via the ".AttrList" target-cursor command, matching ".flip"'s
	/// own shape, per explicit user request for a dedicated dialog rather than scattered fields
	/// in the generic .info view).</summary>
	[Collection(AttrFlagTestCollection.Name)]
	public class AttrFlagTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string AccountC = "protocoltest3";
		private const string AccountD = "protocoltest4";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public AttrFlagTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real ".def" "ATTR = attr_static|attr_magic" line (AttrFlagTest.def) parses
		/// correctly into ItemDef.Attributes, and a freshly created instance's own
		/// Attributes gets seeded from it at creation time (Item.On_Create).</summary>
		[Fact]
		public void DefAttrLineParsesAndSeedsNewInstances() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDefTester")) {
				client.SendSpeech(".TestAttrDefParsing");
				// ItemAttributes.ToString() lists flags in enum declaration order (attr_magic
				// before attr_static), not the order they appeared in the ".def" line.
				this.AssertSeen("ATTR_DEF_TEST: defDefault=attr_magic, attr_static instanceAttributes=attr_magic, attr_static hasStatic=True hasMagic=True hasNewbie=False", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>SetAttribute/HasAttribute (the programmatic API #218-225's real behaviors
		/// will consume) correctly read/write the same underlying bitmask. Runs on AccountB (not
		/// the shared Account, which is already at its own 5-character cap from the other Facts
		/// in this file) - also doubles as the thing that ensures auto-account-creation is on,
		/// since every AccountB-using Fact (including #220's Theory below) needs it.</summary>
		[Fact]
		public void ProgrammaticToggleWorks() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "AttrBoolTester")) {
				client.SendSpeech(".TestAttrProgrammaticToggle");
				this.AssertSeen("ATTR_BOOL_TEST: beforeAny=False afterToggle=True afterUntoggle=True", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>#218: attr_newbie/attr_blessed items stay on the body (no fall), attr_cursed
		/// forces a fall even though it co-exists with nothing else protective here, attr_insured
		/// also protects but is consumed by the one death that used it, and a plain item with no
		/// flags falls normally.</summary>
		[Fact]
		public void DeathDropRespectsAttrFlags() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDeathTester")) {
				client.SendSpeech(".TestDeathDropAttrItems");
				this.AssertSeen(
					"ATTR_DEATH_RESULT: newbieStayed=True blessedStayed=True cursedStayed=False insuredStayed=True plainStayed=False insuredConsumed=True",
					TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>#371: a plain ground item with NO attributes set at all now decays away by
		/// default (the behavior #371 flipped - previously it would have needed an explicit
		/// attr_decay to be eligible), while attr_questitem, attr_lockeddown, and attr_secure
		/// (all exempt) and a plain contained item (exempt while contained regardless of the
		/// flag default) all survive the same wait. TestSetupDecayItems shrinks
		/// DecaySettings.itemDecayMinutes to 0.6 real seconds so this doesn't need to wait out
		/// the real 5-minute production default - each item's own per-item timer reads that
		/// setting the moment it's scheduled (#219, redesigned away from a global sweep).</summary>
		[Fact]
		public void OnlyEligibleGroundItemDecaysAway() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDecayTester")) {
				client.SendSpeech(".TestSetupDecayItems");
				this.AssertSeen("ATTR_DECAY_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_DECAY_SETUP:");
				uint plainNoFlags = ExtractUidAfter(setupLine, "plainNoFlags=");
				uint decayQuestItem = ExtractUidAfter(setupLine, "decayQuestItem=");
				uint lockeddownItem = ExtractUidAfter(setupLine, "lockeddownItem=");
				uint secureItem = ExtractUidAfter(setupLine, "secureItem=");
				uint decayButContained = ExtractUidAfter(setupLine, "decayButContained=");

				//#219 (redesigned) - each item's own real-time timer fires at exactly 0.6s now
				//(no sweep cadence to wait out), a short margin is plenty.
				Thread.Sleep(1500);

				Assert.False(this.ReportDecayItemExists(client, plainNoFlags), "plainNoFlags should have decayed away by default (#371).");
				Assert.True(this.ReportDecayItemExists(client, decayQuestItem), "decayQuestItem (exempt) should still exist.");
				Assert.True(this.ReportDecayItemExists(client, lockeddownItem), "lockeddownItem (exempt, #371) should still exist.");
				Assert.True(this.ReportDecayItemExists(client, secureItem), "secureItem (exempt, #371) should still exist.");
				Assert.True(this.ReportDecayItemExists(client, decayButContained), "decayButContained (paused while contained) should still exist.");
			}
		}

		private bool ReportDecayItemExists(TestGameClient client, uint uid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportDecayItemExists " + uid);
			this.AssertSeenSince("ATTR_DECAY_EXISTS: uid=" + uid, mark, TimeSpan.FromSeconds(10));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_DECAY_EXISTS: uid=" + uid);
			return line.EndsWith("exists=True", StringComparison.Ordinal);
		}

		/// <summary>#219 (redesigned, per explicit user request): a player's own corpse and an
		/// NPC's corpse must decay at two independently-configurable rates (Corpse.Owner is
		/// non-null only for a player's death - see Corpse.InitFromChar). Uses deliberately
		/// different tiny rates (NPC 0.6s, player 1.8s) and checks existence at an intermediate
		/// checkpoint (after the NPC rate elapsed, before the player rate did) to prove the
		/// RIGHT rate was picked for each, not just that corpses decay at all.</summary>
		[Fact]
		public void PlayerAndNpcCorpsesDecayAtDifferentRates() {
			using (var killer = this.ConnectLoginAndCreateCharacter(AccountC, "CorpseRateKiller")) {
				//no .TestSetGmOn needed here, unlike #224's .Reveal test - every command used
				//below is "Test"-prefixed, which bypasses CommandsAuthorization entirely
				//regardless of plevel (CauseDeath itself needs no special authority either).
				int markRates = this.fixture.GameOutputSnapshot().Length;
				killer.SendSpeech(".TestSetupCorpseDecayRates 0.01,0.03"); //npc=0.6s, player=1.8s
				this.AssertSeenSince("ATTR_CORPSE_RATES_SET", markRates, TimeSpan.FromSeconds(10));

				int markNpc = this.fixture.GameOutputSnapshot().Length;
				killer.SendSpeech(".TestKillNpcAndReportCorpse");
				this.AssertSeenSince("ATTR_CORPSE_SETUP:", markNpc, TimeSpan.FromSeconds(10));
				uint npcCorpseUid = ExtractUidAfter(LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_CORPSE_SETUP:"), "uid=");

				using (var victim = this.ConnectLoginAndCreateCharacter(AccountD, "CorpseRateVictim")) {
					uint victimUid = this.ReportMyUid(victim);

					int markPlayer = this.fixture.GameOutputSnapshot().Length;
					killer.SendSpeech(".TestKillPlayerAndReportCorpse " + victimUid);
					this.AssertSeenSince("ATTR_CORPSE_SETUP:", markPlayer, TimeSpan.FromSeconds(10));
					uint playerCorpseUid = ExtractUidAfter(LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_CORPSE_SETUP:"), "uid=");

					//past the NPC's 0.6s rate, well before the player's 1.8s rate
					Thread.Sleep(1100);
					Assert.False(this.ReportThingExists(killer, npcCorpseUid), "NPC corpse should have decayed by its own (shorter) rate.");
					Assert.True(this.ReportThingExists(killer, playerCorpseUid), "Player corpse should still be here - not yet past its own (longer) rate.");

					//now past the player's 1.8s rate too
					Thread.Sleep(1200);
					Assert.False(this.ReportThingExists(killer, playerCorpseUid), "Player corpse should have decayed by now.");
				}
			}
		}

		private bool ReportThingExists(TestGameClient client, uint uid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportThingExists " + uid);
			this.AssertSeenSince("ATTR_THING_EXISTS: uid=" + uid, mark, TimeSpan.FromSeconds(10));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_THING_EXISTS: uid=" + uid);
			return line.EndsWith("exists=True", StringComparison.Ordinal);
		}

		/// <summary>#220: attr_static and attr_move_never both deny a real pickup attempt
		/// (0x07) via the newly-added On_DenyPickup -> Flag_NonMovable path; attr_move_always
		/// overrides attr_static on the same item (pickup succeeds); a plain item with neither
		/// flag also picks up normally (control case, confirms the new deny path doesn't
		/// over-trigger).</summary>
		[Theory]
		[InlineData("static", true)]
		[InlineData("movenever", true)]
		[InlineData("movealwaysoverride", false)]
		[InlineData("plain", false)]
		public void PickupRespectsMovementAttrFlags(string mode, bool expectDenied) {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "AttrMove" + mode)) {
				//AccountB is a plain (non-elevated) account, routed through SimpleCommandParser,
				//which does NOT strip quotes - quoting here would arrive as the literal string
				//"\"static\"" and never match the switch below. Unquoted is correct for this
				//account; only elevated/first-account commands need the opposite workaround.
				//AssertSeen (not AssertSeenSince) would be satisfied instantly by a PREVIOUS
				//Theory case's own "ATTR_MOVE_SETUP:" line already sitting in the shared log
				//buffer, racing ahead of this iteration's actual command - mark first.
				int setupMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupMovementAttrItem " + mode);
				this.AssertSeenSince("ATTR_MOVE_SETUP:", setupMark, TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_MOVE_SETUP:");
				uint uid = ExtractUidAfter(setupLine, "uid=");

				client.SendPickUpItem(uid, 1);
				Thread.Sleep(300);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportItemOnGround " + uid);
				this.AssertSeenSince("ATTR_MOVE_RESULT: uid=" + uid, mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_MOVE_RESULT: uid=" + uid);
				bool stillOnGround = line.EndsWith("stillOnGround=True", StringComparison.Ordinal);
				Assert.True(expectDenied == stillOnGround, "setup=" + setupLine + " result=" + line); //includes setupLine for a quick nonMovable= sanity check on failure
			}
		}

		/// <summary>Real end-to-end flow: ".AttrList" target cursor opens D_AttrList, submitting
		/// it with attr_newbie's and attr_static's checkboxes selected (and nothing else) must
		/// leave exactly those two set on the targeted item, no others.</summary>
		[Fact]
		public void AttrListDialogTogglesSelectedFlags() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrListTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestCreateAttrTestItem");
				this.AssertSeen("ATTR_ITEM_SETUP:", TimeSpan.FromSeconds(10));
				uint itemUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), "ATTR_ITEM_SETUP: uid=");

				client.SendSpeech(".AttrList");
				Thread.Sleep(300);
				client.SendTargetUid(itemUid);
				var (gumpUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				// D_AttrList's allFlags order: 0=identified,1=decay,2=newbie,...,10=static - ids
				// are 10+index, so attr_newbie=12, attr_static=20.
				client.SendGumpResponse(gumpUid, 1, new[] { 12, 20 }, null);
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportAttrTestItem " + itemUid);
				this.AssertSeenSince("ATTR_ITEM_REPORT:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_ITEM_REPORT:");
				Assert.Equal("ATTR_ITEM_REPORT: attributes=attr_newbie, attr_static", line);
			}
		}

		/// <summary>#222: each magic/crafting-quality flag prepends its own name - flags+display
		/// only, no mechanical effect (confirmed scope, see CLAUDE.md). Also confirms the
		/// precedence rule (reforged beats magic) on one item carrying both at once. Compares
		/// against the plain control item's own reported base name rather than a hardcoded
		/// literal, since i_rock_plain's real display name comes from client tiledata, not a
		/// "Name=" line in its own .def.</summary>
		[Fact]
		public void QualityFlagsPrependNamePrefix() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "QualityTester")) {
				client.SendSpeech(".TestSetupQualityDisplayItems");
				this.AssertSeen("ATTR_QUALITY_TEST:", TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_QUALITY_TEST:");

				string plainName = ExtractValueAfter(line, "plain=");
				Assert.Equal("magic " + plainName, ExtractValueAfter(line, "magic="));
				Assert.Equal("exceptional " + plainName, ExtractValueAfter(line, "exceptional="));
				Assert.Equal("enchanted " + plainName, ExtractValueAfter(line, "enchanted="));
				Assert.Equal("imbued " + plainName, ExtractValueAfter(line, "imbued="));
				Assert.Equal("reforged " + plainName, ExtractValueAfter(line, "reforgedAndMagic="));
				//#224 - same attr_magic flag as the "magic=" item above, but NOT identified -
				//its prefix must stay hidden, showing the plain name instead.
				Assert.Equal(plainName, ExtractValueAfter(line, "unidentifiedMagic="));
			}
		}

		/// <summary>#224: an attr_invis item is invisible to an unrelated character, but a GM
		/// always sees it, and an explicitly-granted non-GM character sees it too (real
		/// Sphere's own "TAG.SeenBy_[CharUID]" exception list, via the new .Reveal command's
		/// item-then-character target chain). Teleports the stranger to the item first - same
		/// CanReach-after-deny-chain-style trap as #221/#223, except here it's about targeting
		/// range for the .Reveal command itself, not pickup.</summary>
		[Fact]
		public void InvisItemHiddenUnlessExplicitlyRevealed() {
			this.EnsureAutoAccountCreationEnabled();
			using (var owner = this.ConnectLoginAndCreateCharacter(AccountC, "InvisOwnerTester")) {
				//GMs see every invisible item unconditionally - needed here so the owner can
				//even TARGET their own invis item for the .Reveal flow below. HandleTarget
				//checks self.CanSeeForUpdate(thing) before invoking the target handler at all,
				//so a non-GM owner couldn't target their own invisible creation either.
				//TestSetGmOn's "PLevel = MaxPLevel" only reaches real GM level on the server's
				//first-ever account (the only one with MaxPLevel auto-elevated) - AccountC
				//isn't that account, so this needs TestPromoteToGM's own maxPlevel override
				//instead (same account-order-independent promotion SocialTests.cs itself uses).
				owner.SendSpeech(".TestPromoteToGM");
				Thread.Sleep(500);

				owner.SendSpeech(".TestSetupInvisItem");
				this.AssertSeen("ATTR_INVIS_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_INVIS_SETUP:");
				uint itemUid = ExtractUidAfter(setupLine, "uid=");
				int x = ExtractSignedIntAfter(setupLine, "x=");
				int y = ExtractSignedIntAfter(setupLine, "y=");
				int z = ExtractSignedIntAfter(setupLine, "z=");

				//must be a different account than owner - only one connection per account at a time
				using (var stranger = this.ConnectLoginAndCreateCharacter(AccountD, "InvisStrangerTester")) {
					this.TeleportTo(stranger, x, y, z);
					uint strangerUid = this.ReportMyUid(stranger);

					Assert.False(this.CheckCanSee(stranger, itemUid), "Stranger should not see the invisible item yet.");

					//owner (acting as the revealer) targets the item, then the stranger
					owner.SendSpeech(".Reveal");
					Thread.Sleep(300);
					owner.SendTargetUid(itemUid);
					Thread.Sleep(300);
					owner.SendTargetUid(strangerUid);
					Thread.Sleep(300);

					Assert.True(this.CheckCanSee(stranger, itemUid), "Stranger should see the item after being explicitly revealed to.");
				}
			}
		}

		private bool CheckCanSee(TestGameClient client, uint itemUid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestCheckCanSeeItem " + itemUid);
			this.AssertSeenSince("ATTR_INVIS_CHECK:", mark, TimeSpan.FromSeconds(10));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_INVIS_CHECK:");
			return line.EndsWith("canSee=True", StringComparison.Ordinal);
		}

		/// <summary>Extracts the value for "key=" up to the next " <word>=" field or end of
		/// line - needed since the values themselves (item display names) can contain spaces,
		/// unlike every other marker line in this file.</summary>
		private static string ExtractValueAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			string rest = line.Substring(idx + marker.Length);
			System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(rest, @"^.*?(?=\s+\w+=|$)");
			return m.Value.TrimEnd('\r');
		}

		/// <summary>#223: an attr_owned item (claimed by its creator) can only be picked up by
		/// its owner or a GM - a plain, unrelated character is denied. Teleports the stranger
		/// to the item's own position first - same CanReach-after-deny-chain trap already
		/// documented for #221 (a too-far-away denial and an attr_owned denial look identical).</summary>
		[Fact]
		public void OwnedItemOnlyPickupableByOwnerOrGM() {
			this.EnsureAutoAccountCreationEnabled();
			using (var owner = this.ConnectLoginAndCreateCharacter(AccountC, "OwnedOwnerTester")) {
				owner.SendSpeech(".TestSetupOwnedItem");
				this.AssertSeen("ATTR_OWNED_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_OWNED_SETUP:");
				uint itemUid = ExtractUidAfter(setupLine, "uid=");
				int x = ExtractSignedIntAfter(setupLine, "x=");
				int y = ExtractSignedIntAfter(setupLine, "y=");
				int z = ExtractSignedIntAfter(setupLine, "z=");

				//must be a different account than owner - only one connection per account at a time
				using (var stranger = this.ConnectLoginAndCreateCharacter(AccountD, "OwnedStrangerTester")) {
					this.TeleportTo(stranger, x, y, z);
					Assert.True(this.AttemptPickupAndReportStillOnGround(stranger, itemUid), "Stranger should NOT be able to pick up another player's owned item.");
				}

				//owner is already standing at the item's own creation point
				Assert.False(this.AttemptPickupAndReportStillOnGround(owner, itemUid), "Owner should be able to pick up their own owned item.");
			}
		}

		/// <summary>#223: attr_nodroptrade denies dropping the item back onto the ground -
		/// picks it up first (that part is unrestricted), then confirms the drop attempt
		/// leaves it still held (Cont stays the character, never becomes the ground/null).</summary>
		[Fact]
		public void NoDropTradeItemCannotBeDroppedOnGround() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "NoDropGroundTester")) {
				client.SendSpeech(".TestSetupNoDropTradeItem");
				this.AssertSeen("ATTR_NODROPTRADE_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_NODROPTRADE_SETUP:");
				uint itemUid = ExtractUidAfter(setupLine, "uid=");
				int x = ExtractSignedIntAfter(setupLine, "x=");
				int y = ExtractSignedIntAfter(setupLine, "y=");
				int z = ExtractSignedIntAfter(setupLine, "z=");

				client.SendPickUpItem(itemUid, 1);
				Thread.Sleep(300);
				client.SendDropItem(itemUid, (ushort) x, (ushort) y, (sbyte) z, -1);
				Thread.Sleep(300);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportItemOnGround " + itemUid);
				this.AssertSeenSince("ATTR_MOVE_RESULT: uid=" + itemUid, mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_MOVE_RESULT: uid=" + itemUid);
				Assert.False(line.EndsWith("stillOnGround=True", StringComparison.Ordinal), "Item should still be held, not back on the ground.");
			}
		}

		/// <summary>#223: attr_nodroptrade also denies adding the item to a secure-trade
		/// window - that path (SecureTrade.AddItem) deliberately bypasses the normal
		/// deny-trigger chain, so this specifically verifies the explicit check added there,
		/// not just the On_DenyPutOnGround path the other Fact above already covers.</summary>
		[Fact]
		public void NoDropTradeItemCannotBeAddedToSecureTrade() {
			this.EnsureAutoAccountCreationEnabled();
			using (var initiator = this.ConnectLoginAndCreateCharacter(AccountC, "NoDropTradeInitiator")) {
				initiator.SendSpeech(".TestSetupNoDropTradeItem");
				this.AssertSeen("ATTR_NODROPTRADE_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_NODROPTRADE_SETUP:");
				uint itemUid = ExtractUidAfter(setupLine, "uid=");
				int x = ExtractSignedIntAfter(setupLine, "x=");
				int y = ExtractSignedIntAfter(setupLine, "y=");
				int z = ExtractSignedIntAfter(setupLine, "z=");

				//must be a different account than initiator - only one connection per account at a time
				using (var target = this.ConnectLoginAndCreateCharacter(AccountD, "NoDropTradeTarget")) {
					this.TeleportTo(target, x, y, z);
					uint targetUid = this.ReportMyUid(target);

					initiator.SendPickUpItem(itemUid, 1);
					Thread.Sleep(300);
					//classic UO gesture: dropping a held item onto another player starts/adds to a secure trade
					initiator.SendDropItem(itemUid, 0xFFFF, 0xFFFF, 0, (int) targetUid);
					Thread.Sleep(500);

					int mark = this.fixture.GameOutputSnapshot().Length;
					initiator.SendSpeech(".TestReportItemOnGround " + itemUid);
					this.AssertSeenSince("ATTR_MOVE_RESULT: uid=" + itemUid, mark, TimeSpan.FromSeconds(10));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_MOVE_RESULT: uid=" + itemUid);
					//denied - the item never left the initiator's own dragging layer, so it's
					//neither on the ground nor (obviously) inside the trade box.
					Assert.False(line.EndsWith("stillOnGround=True", StringComparison.Ordinal), "Item should not have been added to the trade (and not dropped either).");
				}
			}
		}

		/// <summary>Shared by the attr_owned Fact above - sends a real pickup packet and reports
		/// whether the item is still sitting loose on the ground afterward.</summary>
		private bool AttemptPickupAndReportStillOnGround(TestGameClient client, uint itemUid) {
			client.SendPickUpItem(itemUid, 1);
			Thread.Sleep(300);
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportItemOnGround " + itemUid);
			this.AssertSeenSince("ATTR_MOVE_RESULT: uid=" + itemUid, mark, TimeSpan.FromSeconds(10));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_MOVE_RESULT: uid=" + itemUid);
			return line.EndsWith("stillOnGround=True", StringComparison.Ordinal);
		}

		private uint ReportMyUid(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			this.AssertSeenSince("MY_UID:", mark, TimeSpan.FromSeconds(10));
			return ExtractUidAfter(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");
		}

		/// <summary>Same CanReach-after-deny-chain trap already documented for #221 - a fresh
		/// character spawns far from any item a previous connection set up, so tests that need
		/// a second character to interact with an already-placed item must move it there first.</summary>
		private void TeleportTo(TestGameClient client, int x, int y, int z) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTeleportTo " + x + "," + y + "," + z);
			this.AssertSeenSince("HOUSE_LOCKDOWN_SETUP: teleported", mark, TimeSpan.FromSeconds(10));
		}

		/// <summary>z coordinates can be negative - unlike every other marker this file parses.</summary>
		private static int ExtractSignedIntAfter(string text, string marker) {
			int idx = text.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + text);
			string rest = text.Substring(idx + marker.Length);
			int end = (rest.Length > 0 && rest[0] == '-') ? 1 : 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutput(marker, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private void AssertSeenSince(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		private static uint ExtractUidAfter(string output, string marker) {
			int idx = output.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		/// <summary>#220 - AccountB (needs EnsureAutoAccountCreationEnabled called once first,
		/// same pattern as ProfessionTests.cs/SocialTests.cs/LevelingTests.cs) - the shared
		/// Account above already hits AbstractAccount.maxCharactersPerGameAccount's 5-slot cap
		/// from the Facts above, so PickupRespectsMovementAttrFlags' 4 Theory cases need their
		/// own account rather than overflowing it.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private static bool autoAccountCreationEnabled;

		/// <summary>Flips Globals.AutoAccountCreation on (server-wide, via the already-existing
		/// first account) so AccountB's first-ever login can succeed - same pattern as
		/// ProfessionTests.cs. Guarded by a static flag so repeated calls (one per AccountB-using
		/// Fact/Theory case) create the "AutoAccountEnabler" character on Account only once -
		/// ConnectLoginAndCreateCharacter always creates a NEW character slot per call, so
		/// without this guard every call would burn another of Account's 5 slots (safe under
		/// DisableParallelization=true, Facts in this collection never run concurrently).</summary>
		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
		}

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);
						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(AttrFlagTestCollection.Name, DisableParallelization = true)]
	public class AttrFlagTestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "AttrFlagTestPair";
	}
}
