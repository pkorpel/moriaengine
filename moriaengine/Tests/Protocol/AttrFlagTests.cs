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
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public AttrFlagTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real ".def" "ATTR = attr_static|attr_magic" line (AttrFlagTest.def) parses
		/// correctly into ItemDef.AttributesDefault, and a freshly created instance's own
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

		/// <summary>#219: a plain attr_decay ground item actually decays away after the
		/// configured interval elapses, while attr_questitem (exempt), no-flag (never eligible),
		/// and attr_decay-but-contained (paused while in a backpack) items all survive the same
		/// wait. Uses TestSetupDecayItems' own speed-up (0.6s decay interval, 1s sweep cadence)
		/// so this doesn't need to wait out the real 60-minute production defaults.</summary>
		[Fact]
		public void OnlyEligibleGroundItemDecaysAway() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDecayTester")) {
				client.SendSpeech(".TestSetupDecayItems");
				this.AssertSeen("ATTR_DECAY_SETUP:", TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_DECAY_SETUP:");
				uint decayPlain = ExtractUidAfter(setupLine, "decayPlain=");
				uint decayQuestItem = ExtractUidAfter(setupLine, "decayQuestItem=");
				uint noDecayFlag = ExtractUidAfter(setupLine, "noDecayFlag=");
				uint decayButContained = ExtractUidAfter(setupLine, "decayButContained=");

				//2 sweep ticks (1s cadence) after the 0.6s decay interval - comfortably enough
				//for the sweep to both notice the ground items and then delete the eligible one.
				Thread.Sleep(3000);

				Assert.False(this.ReportDecayItemExists(client, decayPlain), "decayPlain should have decayed away.");
				Assert.True(this.ReportDecayItemExists(client, decayQuestItem), "decayQuestItem (exempt) should still exist.");
				Assert.True(this.ReportDecayItemExists(client, noDecayFlag), "noDecayFlag (never eligible) should still exist.");
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
			}
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
