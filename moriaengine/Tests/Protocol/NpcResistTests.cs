using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #606 - "Converter: map real tag.resist_X damage-resist tags onto NPCDef's existing
	/// ResistX fields". The engine's Character.ResistX properties (ResistFire/ResistPoison/
	/// etc, Character.ct's Defs: section) were already fully live and consumed in
	/// DamageManager.GetResistModifier - they just had zero .def content setting them
	/// anywhere. This proves the exact field names the converter now emits (ResistPoison/
	/// ResistFire/ResistElectric/ResistSlashing/ResistStabbing/ResistSharp) actually apply on a
	/// real NPC, and #860 that they are permille shares of the damage.
	/// </summary>
	[Collection(NpcResistCollection.Name)]
	public class NpcResistTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcResistTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ResistFieldsApplyOnARealNpc() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ResistChecker")) {
				client.SendSpeech(".TestCreateResistNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NPC_RESIST_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_RESIST_TEST_RESULT:");

				Assert.Contains("poison=850", line);
				Assert.Contains("fire=200", line);
				Assert.Contains("electric=100", line);
				Assert.Contains("slashing=-100", line);
				Assert.Contains("stabbing=-100", line);
				//#(resist-ranges) - ResistParalyse is "700 800" in the .def, a real per-spawn
				//roll, not a fixed value - assert it's in-range rather than an exact number.
				Assert.InRange(ExtractIntValue(line, "paralyse="), 700, 800);
				Assert.Contains("curse=-100", line);
				Assert.Contains("sharp=300", line);

				//#860 - Erebor's handleresists: subtypes averaged, then the group resist (500) halves
				string modifiers = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_RESIST_MODIFIER:");
				Assert.Contains("slashing=0.55", modifiers); //(1000 + 100) * 500
				Assert.Contains("sharp=0.35", modifiers);
				Assert.Contains("two=0.487", modifiers);     //(1100 + 850) / 2 = 975, * 500
				Assert.Contains("fire=0.8", modifiers);      //no magic group resist
				Assert.Contains("both=0.612", modifiers);    //(425 + 800) / 2
				Assert.Contains("plain=1", modifiers);       //no subtype: not reduced at all
			}
		}

		/// <summary>#872 - a titan resists bleed 300: 100 -> 70, the fade 99 -> 69, cap 300 + 200.</summary>
		[Fact]
		public void ThiefBleedFadesAndCaps() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "BleedChecker")) {
				client.SendSpeech(".TestThiefBleedReport");
				Assert.True(this.fixture.WaitForGameOutput("THIEF_BLEED:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "THIEF_BLEED:");

				Assert.Equal(70, ExtractIntValue(line, "firstTick="));
				Assert.Equal(69, ExtractIntValue(line, "secondTick="));
				Assert.Equal(500, ExtractIntValue(line, "capped="));
			}
		}

		/// <summary>#868 - the blow costs less and hurts the monster back.</summary>
		[Fact]
		public void ReactiveArmorAbsorbsAndReturnsBlows() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ReactiveChecker")) {
				client.SendSpeech(".TestReactiveArmorReport");
				Assert.True(this.fixture.WaitForGameOutput("REACTIVE_ARMOR:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "REACTIVE_ARMOR:");

				Assert.Contains("active=True", line);
				Assert.Equal(0, ExtractIntValue(line, "returnedWithout="));
				Assert.True(ExtractIntValue(line, "returned=") > 0, line);
				Assert.True(ExtractIntValue(line, "with=") <= ExtractIntValue(line, "without="), line); //the titan's roll is 0.85-1
			}
		}

		/// <summary>#860 - Sphere's armor coverage on players and monsters, PvP armor, dodge.</summary>
		[Fact]
		public void ArmorCoverageAndDodge() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ArmorChecker")) {
				client.SendSpeech(".TestArmorCoverageReport");
				Assert.True(this.fixture.WaitForGameOutput("ARMOR_COVERAGE:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ARMOR_COVERAGE:");

				Assert.Equal(ExtractIntValue(line, "bare=") + 23, ExtractIntValue(line, "chest=")); //65 * 36%
				Assert.Equal(ExtractIntValue(line, "bare=") + 32, ExtractIntValue(line, "helm="));  //65 * 50%
				Assert.Equal(ExtractIntValue(line, "npcBare=") + 23, ExtractIntValue(line, "npcWorn="));
				Assert.InRange(ExtractIntValue(line, "dodged="), 850, 1150); //20 points = 20% of 5000
				Assert.Contains("pvpChange=-32", line);
				Assert.Contains("playerDodged=False", line);
				Assert.Contains("spellDodged=False", line);

				//#860 - a monster's mind power is its Evaluate Intelligence, its defense Resist / 10
				string mind = LastLineContaining(this.fixture.GameOutputSnapshot(), "MIND_VALUES:");
				Assert.True(ExtractIntValue(mind, "titanMageEvalInt=") > 0, mind);
				Assert.Equal(ExtractIntValue(mind, "titanMageEvalInt="), ExtractIntValue(mind, "titanMagePower="));
				Assert.Equal(ExtractIntValue(mind, "resist=") / 10, ExtractIntValue(mind, "titanMageDefense="));
			}
		}

		[Fact]
		public void ResistRangeRollsDifferentValuesAcrossSpawns() {
			//proves ResistParalyse is actually ROLLED per spawn (not just clamped/validated
			//to be in-range by coincidence) - creates several NPCs and checks the roll isn't
			//the same single value every time. Statistically near-certain to see >1 distinct
			//value across 20 rolls of a 101-wide range (700-800) if rolling for real.
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ResistRangeChecker")) {
				var seenValues = new System.Collections.Generic.HashSet<int>();
				for (int i = 0; i < 20; i++) {
					int markerLength = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCreateResistNpcAndReport");
					Assert.True(this.fixture.WaitForGameOutput("NPC_RESIST_TEST_RESULT:", TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_RESIST_TEST_RESULT:");
					seenValues.Add(ExtractIntValue(line, "paralyse="));
				}
				Assert.True(seenValues.Count > 1,
					"Expected ResistParalyse to vary across spawns (range 700-800), but every roll produced the same value - rolling is not actually happening.");
			}
		}

		private static int ExtractIntValue(string line, string fieldPrefix) {
			int idx = line.IndexOf(fieldPrefix, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + fieldPrefix + "' in: " + line);
			int start = idx + fieldPrefix.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start));
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

	[CollectionDefinition(NpcResistCollection.Name, DisableParallelization = true)]
	public class NpcResistCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcResistPair";
	}
}
