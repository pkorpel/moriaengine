using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-7 (#75): taming/mining/lumberjacking, over real skill-use (0x12) + target-cursor (0x6c)
	/// packets. Every scenario flips the test account to GM first (SkillDef.CheckSuccess always
	/// returns true for a GM) so skill success is deterministic without grinding real skill
	/// points - the real reachability/validity checks (IsValidTameTarget/IsValidVein/
	/// IsValidTree) still run unconditionally before that, so this doesn't skip testing whether
	/// the target is actually legal, only the RNG success roll itself.
	/// </summary>
	[Collection(GatheringCollection.Name)]
	public class GatheringTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltest2"; //#370 - protocoltest itself is already at its safe headroom (3 pre-existing Facts)
		private const string AccountC = "protocoltest3";
		private const string AccountD = "protocoltest4";
		private const string Password = "protocoltest";

		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public GatheringTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: taming a valid, unowned tameable creature sets its
		/// ControlMaster to the tamer.</summary>
		[Fact]
		public void TamingSetsControlMaster() {
			using (var client = this.ConnectLoginAndCreateCharacter("Tamer")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnTameable");
				Assert.True(this.fixture.WaitForGameOutputSince("TAME_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint npcUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TAME_SETUP: npc=#");

				client.SendSkillUse(35); // AnimalTaming
				Thread.Sleep(300);
				client.SendTargetUid(npcUid);
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckControlMaster " + npcUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TAME_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("controlMaster=Tamer", LastLineContaining(this.fixture.GameOutputSnapshot(), "TAME_CHECK:"));
			}
		}

		/// <summary>Real UO rule (this engine's own simplification: any impassable land tile
		/// stands in for a real ore vein, see MiningSkillDef's own doc comment): mining a valid
		/// vein yields a real ore item in the backpack.</summary>
		[Fact]
		public void MiningYieldsOre() {
			using (var client = this.ConnectLoginAndCreateCharacter("Miner")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				// #370 - Mining now requires a pickaxe equipped; without this the skill-select
				// below would be denied before ever reaching the real vein-mining logic.
				client.SendSpeech(".TestGrantAndEquipPickaxe");
				Assert.True(this.fixture.WaitForGameOutput("TOOL_SETUP:", TimeSpan.FromSeconds(10)));

				// #336 - Mining's real anti-macro delay is a 20-second base, far longer than
				// this test's own short wait below; speed it up for this fixture.
				client.SendSpeech(".TestSpeedUpGatheringDelayForTest");
				Assert.True(this.fixture.WaitForGameOutput("GATHERING_DELAY_SPED_UP:", TimeSpan.FromSeconds(10)));

				var (x, y, z) = client.ReadSpawnPosition(TimeSpan.FromSeconds(10));
				this.ForceCopperVein(client, x, y); //#813 - open rock is mostly empty now

				client.SendSkillUse(45); // Mining
				Thread.Sleep(300);
				client.SendTargetGround(x, y, z); // the exact spawn tile is impassable real map data (see V-6's identical finding)
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHasOre");
				Assert.True(this.fixture.WaitForGameOutputSince("BACKPACK_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1", LastLineContaining(this.fixture.GameOutputSnapshot(), "BACKPACK_CHECK:"));
			}
		}

		/// <summary>Real UO rule (this engine's own simplification: any world static flagged
		/// TileFlag.Foliage stands in for a real tree, see LumberjackingSkillDef's own doc
		/// comment): chopping a valid tree yields a real log item in the backpack.</summary>
		[Fact]
		public void LumberjackingYieldsWood() {
			using (var client = this.ConnectLoginAndCreateCharacter("Lumberjack")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				// #370 - Lumberjacking now requires a hatchet equipped, same consistency
				// decision as Mining's own pickaxe gate.
				client.SendSpeech(".TestGrantAndEquipHatchet");
				Assert.True(this.fixture.WaitForGameOutput("TOOL_SETUP:", TimeSpan.FromSeconds(10)));

				// #533 - same speed-up this test's MiningYieldsOre sibling already has (line 71)
				// but this test was missing - at the current 2.0s base delay (#533) the stroke
				// doesn't land within this test's 1.5s sleep below without it. Confirmed as a
				// real gap, not speculation: MiningYieldsOre has this call, LumberjackingYields-
				// Wood did not.
				client.SendSpeech(".TestSpeedUpGatheringDelayForTest");
				Assert.True(this.fixture.WaitForGameOutput("GATHERING_DELAY_SPED_UP:", TimeSpan.FromSeconds(10)));

				int markFind = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGoToNearbyFoliage");
				Assert.True(this.fixture.WaitForGameOutputSince("FOLIAGE_FOUND:", markFind, TimeSpan.FromSeconds(10)));
				string foliageLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "FOLIAGE_FOUND:");
				Assert.DoesNotContain("none", foliageLine);
				var (fx, fy, fz, model) = ExtractFoliage(foliageLine);
				//#813 - open-world trees roll from their region, about a third are empty
				int markForce = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceWoodVein " + fx + " " + fy + " \"spruce\" 5");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_VEIN_FORCED:", markForce, TimeSpan.FromSeconds(10)), "no WOOD_VEIN_FORCED");

				client.SendSkillUse(44); // Lumberjacking
				Thread.Sleep(300);
				client.SendTargetStatic(fx, fy, fz, model);
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHasLog");
				Assert.True(this.fixture.WaitForGameOutputSince("BACKPACK_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1", LastLineContaining(this.fixture.GameOutputSnapshot(), "BACKPACK_CHECK:"));
			}
		}

		/// <summary>#370 - mandatory-tool gate: selecting Mining with nothing equipped is denied
		/// with a real system message, before the real vein-targeting flow ever starts.</summary>
		[Fact]
		public void MiningDeniedWithoutPickaxeEquipped() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MinerNoTool")) {
				client.SendSkillUse(45); // Mining
				string message = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("You must be holding a pickaxe to do that.", message);
			}
		}

		/// <summary>#370 - the real gap this issue was filed over: dclicking a pickaxe sitting in
		/// the backpack (not yet equipped) must equip it AND immediately start Mining's own
		/// target-cursor flow in one step, not just equip and stop. Proven end-to-end (not just
		/// "a system message appeared") by actually completing the mine and checking for real
		/// ore afterward, reusing MiningYieldsOre's own verification shape.</summary>
		[Fact]
		public void DclickPickaxeInBackpackEquipsAndStartsMining() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MinerDclickPack")) {
				//#370 - AccountB is never the server's first-ever account, so .TestSetGmOn's own
				//"PLevel = MaxPLevel" is a no-op here (MaxPLevel only auto-elevates for account
				//#1) - .TestPromoteToGM's reflection-based maxPlevel override is the
				//account-order-independent fix, same precedent as AttrFlagTests.cs/this
				//session's own earlier findings.
				client.SendSpeech(".TestPromoteToGM");
				Assert.True(this.fixture.WaitForGameOutput("PROMOTE_TO_GM:", TimeSpan.FromSeconds(10)));
				client.SendSpeech(".TestSpeedUpGatheringDelayForTest");
				Assert.True(this.fixture.WaitForGameOutput("GATHERING_DELAY_SPED_UP:", TimeSpan.FromSeconds(10)));

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantPickaxeToBackpack");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_SETUP:");
				uint pickaxeUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TOOL_SETUP: uid=");
				uint backpackUid = ParseLabelledUInt(setupLine, "backpack=");

				var (x, y, z) = client.ReadSpawnPosition(TimeSpan.FromSeconds(10));
				this.ForceCopperVein(client, x, y); //#813 - open rock is mostly empty now

				//real client behavior: a container must be "open" before anything inside it can
				//be targeted/dclicked (OpenedContainers.HasContainerOpenFromAt) - a real client
				//auto-opens its own backpack on login, this minimal test client has to do it
				//explicitly (same precedent as EconomyTestsB.VendorSellGrantsGoldForItem).
				client.SendDoubleClick(backpackUid);
				Thread.Sleep(300);

				client.SendDoubleClick(pickaxeUid); //#370 - equip + start Mining in one dclick
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("Where do you want to mine?", prompt);

				int markEquip = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckEquipped " + pickaxeUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_EQUIP_CHECK:", markEquip, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_EQUIP_CHECK:"));

				client.SendTargetGround(x, y, z);
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHasOre");
				Assert.True(this.fixture.WaitForGameOutputSince("BACKPACK_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1", LastLineContaining(this.fixture.GameOutputSnapshot(), "BACKPACK_CHECK:"));
			}
		}

		/// <summary>#370 - the other real gap: dclicking a pickaxe that's ALREADY equipped must
		/// still start Mining - previously Equippable.On_DClick was a silent no-op once equipped,
		/// exactly the symptom originally reported (".usehand"/dclick on an in-hand tool does
		/// nothing).</summary>
		[Fact]
		public void DclickPickaxeWhileEquippedStartsMining() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MinerDclickWorn")) {
				//#370 - no GM/success-roll needed here at all; this only proves the dclick
				//itself reaches the skill-select prompt, it never completes a mine attempt.
				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipPickaxe");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint pickaxeUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TOOL_SETUP: uid=");

				client.SendDoubleClick(pickaxeUid); //#370 - already equipped; must still start Mining
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("Where do you want to mine?", prompt);
			}
		}

		/// <summary>#370 - same mandatory-tool gate as Mining, applied to Lumberjacking for
		/// consistency.</summary>
		[Fact]
		public void LumberjackingDeniedWithoutHatchetEquipped() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "LumberjackNoTool")) {
				client.SendSkillUse(44); // Lumberjacking
				string message = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("You must be holding a hatchet to do that.", message);
			}
		}

		/// <summary>#370 - same mandatory-tool gate as Mining, applied to Fishing for
		/// consistency. Lighter than Mining/Lumberjacking's own tests - no existing "yields a
		/// real fish" end-to-end scenario to extend (Fishing has none), so this only proves the
		/// gate itself denies/allows correctly, not a full catch.</summary>
		[Fact]
		public void FishingDeniedWithoutFishingPoleEquipped() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "FisherNoTool")) {
				client.SendSkillUse(18); // Fishing
				string message = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("You must be holding a fishing pole to do that.", message);
			}
		}

		[Fact]
		public void FishingAllowedWithFishingPoleEquipped() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "FisherWithTool")) {
				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipFishingPole");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markSetup, TimeSpan.FromSeconds(10)));

				client.SendSkillUse(18); // Fishing
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("Where do you want to fish?", prompt);
			}
		}

		/// <summary>#391 - the exact reported bug: targeting an invalid spot for Lumberjacking
		/// shows the "no wood here" message but must then STOP, not keep the target cursor
		/// active waiting for another attempt. Proven by counting real TargetCursorCommandsOutPacket
		/// (0x6c) packets - the skill-select itself sends exactly one to arm the cursor; before
		/// the #391 fix, an invalid target's own TargetResult.RestartTargetting re-armed a SECOND
		/// one (via AbstractTargetDef.On_Start -> GameState.Target -> SendTargettingCursor). A
		/// ground point is always invalid for Lumberjacking (valid targets are static foliage
		/// items only, never a bare ground point), so no terrain lookup is needed here.</summary>
		[Fact]
		public void InvalidLumberjackingTargetStopsCursorInsteadOfLooping() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "LumberjackMisses")) {
				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipHatchet");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markSetup, TimeSpan.FromSeconds(10)));

				var (x, y, z) = client.ReadSpawnPosition(TimeSpan.FromSeconds(10));

				client.SendSkillUse(44); // Lumberjacking
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("What do you want to chop?", prompt);
				client.ReadPacketPayload(0x6c, TimeSpan.FromSeconds(10)); // the real, first cursor arm

				client.SendTargetGround(x, y, z); // always invalid for Lumberjacking
				string invalidTargetMessage = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("There is no wood to be gathered there.", invalidTargetMessage);

				Assert.Throws<TimeoutException>(() => client.ReadPacketPayload(0x6c, TimeSpan.FromSeconds(2)));
			}
		}

		/// <summary>#392 - the exact reported design gap: dclicking a plain axe that has NEVER
		/// had any associatedskill=/tooltype= tag (i_axe_copper, unlike i_hatchet) must still
		/// start Lumberjacking, derived automatically from its own WeaponType.TwoHandAxe.</summary>
		[Fact]
		public void EquippingAnyAxeAutomaticallyStartsLumberjacking() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "PlainAxeWielder")) {
				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipPlainAxe");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint axeUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TOOL_SETUP: uid=");

				client.SendDoubleClick(axeUid); //already equipped; must still start Lumberjacking
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("What do you want to chop?", prompt);
			}
		}

		/// <summary>#390 - the exact reported bug: equipping a hatchet while already holding a
		/// one-handed weapon must displace the old weapon, not leave both equipped at once.
		/// Reproduces with i_axe_battle_copper (real, non-guessed layer=1/Hand1) + i_hatchet
		/// (corrected by #390 from layer=2 to layer=1, matching its own real TwoHanded=false) -
		/// before the fix both ended up simultaneously equipped since they targeted different,
		/// non-conflicting hand slots.</summary>
		[Fact]
		public void EquippingHatchetDisplacesMismatchedOneHandedWeapon() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "AxeSwapper")) {
				int markAxe = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipMismatchedAxe");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markAxe, TimeSpan.FromSeconds(10)));
				uint axeUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markAxe, "TOOL_SETUP: uid=");
				uint backpackUid = ParseLabelledUInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_SETUP:"), "backpack=");

				int markHatchet = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantHatchetToBackpack");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markHatchet, TimeSpan.FromSeconds(10)));
				uint hatchetUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markHatchet, "TOOL_SETUP: uid=");

				//real client behavior: the backpack must be "open" before anything inside it can
				//be dclicked, same precedent as DclickPickaxeInBackpackEquipsAndStartsMining.
				client.SendDoubleClick(backpackUid);
				Thread.Sleep(300);

				client.SendDoubleClick(hatchetUid); //equip + start Lumberjacking in one dclick
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("What do you want to chop?", prompt);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckEquipped " + axeUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_EQUIP_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_EQUIP_CHECK:"));

				int markCheck2 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckEquipped " + hatchetUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_EQUIP_CHECK:", markCheck2, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_EQUIP_CHECK:"));
			}
		}

		/// <summary>Real-user-reported: same hand-slot displacement bug as #390's hatchet, this
		/// time hit on the fishing pole - it has no TWOHANDS= line anywhere in the real archive
		/// source at all (confirmed), so it defaulted to one-handed and never displaced whatever
		/// weapon was already equipped. A real fishing pole is held with both hands - fixed by
		/// marking it TwoHanded in the converter, which (per the engine's own hand-slot
		/// displacement logic, ThingManipulationTriggers.InternalItemEnter) evicts BOTH hands
		/// unconditionally regardless of which specific layer the item itself sits at.</summary>
		[Fact]
		public void EquippingFishingPoleDisplacesMismatchedOneHandedWeapon() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "PoleSwapper")) {
				int markAxe = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipMismatchedAxe");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markAxe, TimeSpan.FromSeconds(10)));
				uint axeUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markAxe, "TOOL_SETUP: uid=");
				uint backpackUid = ParseLabelledUInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_SETUP:"), "backpack=");

				int markPole = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantFishingPoleToBackpack");
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_SETUP:", markPole, TimeSpan.FromSeconds(10)));
				uint poleUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markPole, "TOOL_SETUP: uid=");

				client.SendDoubleClick(backpackUid);
				Thread.Sleep(300);

				client.SendDoubleClick(poleUid); //equip + start Fishing in one dclick
				string prompt = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("Where do you want to fish?", prompt);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckEquipped " + axeUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_EQUIP_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_EQUIP_CHECK:"));

				int markCheck2 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckEquipped " + poleUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TOOL_EQUIP_CHECK:", markCheck2, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "TOOL_EQUIP_CHECK:"));
			}
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			return ParseLeadingUInt(rest);
		}

		private static (int X, int Y, int Z, int Model) ExtractFoliage(string line) {
			int x = ParseLabelledInt(line, "x=");
			int y = ParseLabelledInt(line, "y=");
			int z = ParseLabelledInt(line, "z=");
			int model = ParseLabelledInt(line, "model=");
			return (x, y, z, model);
		}

		private static uint ParseLabelledUInt(string line, string label) {
			return (uint) ParseLabelledInt(line, label);
		}

		/// <summary>#370 - same race as ProfessionTests.cs's identical helper: the
		/// DelayedResyncTimer's "Welcome to {shard}." broadcast can land ahead of this test's own
		/// expected message depending on exactly when its timer fires relative to this test's own
		/// commands - skip any such unrelated message rather than asserting on whichever one
		/// happens to arrive first.</summary>
		private static string ReadSystemMessageSkippingWelcome(TestGameClient client) {
			for (int attempt = 0; attempt < 3; attempt++) {
				string message = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				if (!message.StartsWith("Welcome to", StringComparison.Ordinal)) {
					return message;
				}
			}
			throw new TimeoutException("Only ever saw welcome-shard messages, never the expected message.");
		}

		private static int ParseLabelledInt(string line, string label) {
			int idx = line.IndexOf(label, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + label + "' in:\n" + line);
			string rest = line.Substring(idx + label.Length);
			int end = 0;
			bool negative = rest.Length > 0 && rest[0] == '-';
			int start = negative ? 1 : 0;
			end = start;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static uint ParseLeadingUInt(string text) {
			int end = 0;
			while (end < text.Length && char.IsDigit(text[end])) {
				end++;
			}
			return uint.Parse(text.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>#813 - what a rock spot holds comes from its region (r_default_rock: about 93%
		/// nothing), so a test that needs ore puts a copper vein on the spot first.</summary>
		private void ForceCopperVein(TestGameClient client, int x, int y) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestForceVein " + x + " " + y + " \"copper\" 5");
			Assert.True(this.fixture.WaitForGameOutputSince("VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)), "no VEIN_FORCED");
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		/// <summary>#370 - account-parameterized overload. The original 3 Facts in this file
		/// (TamingSetsControlMaster/MiningYieldsOre/LumberjackingYieldsWood) already used all 3
		/// of the shared "protocoltest" account's safe headroom under the 5-character-slot cap;
		/// #370's 6 new Facts need to spread across AccountB/AccountC instead of pushing
		/// "protocoltest" over the cap (the exact already-documented class of flakiness - see
		/// EnsureAutoAccountCreationEnabled below for the account that needs it first).</summary>
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

		/// <summary>#370 - AccountB/AccountC are never the server's literal first-ever account,
		/// so they need auto-account-creation explicitly enabled first (same precedent as
		/// AttrFlagTests.cs's identical helper) - guarded by a static bool so this only actually
		/// runs once per test process, not once per Fact (creating "AutoAccountEnabler" itself
		/// consumes one of protocoltest's own slots every call, not just the first).</summary>
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

		private int WaitForServerListingAndGetRedirectPort() {
			return this.WaitForServerListingAndGetRedirectPort(Account);
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GatheringCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatheringPair";
	}
}
