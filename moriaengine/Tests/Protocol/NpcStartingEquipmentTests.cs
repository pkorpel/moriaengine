using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #563 - proves NPCDef.StartingEquipment (scripts/Basic/CharClasses/NPC.cs) actually
	/// applies on NPC.On_Create, over the real client protocol. npc_test_starting_equipment
	/// (scripts/Tests/NpcStartingEquipmentTest.def) has two `Equip =` lines (a real equippable
	/// weapon, then real ammo with a {min,max} amount range) - proving both that multiple
	/// consecutive ITEMNEWBIE-derived lines all survive (the converter bug #563 also fixes)
	/// and that ThingDef.Create(this) correctly routes an equippable item to a layer and a
	/// non-equippable one into the backpack.
	/// </summary>
	[Collection(NpcStartingEquipmentCollection.Name)]
	public class NpcStartingEquipmentTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcStartingEquipmentTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StartingEquipmentAppliesEquippedAndBackpackItemsOnCreate() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "StartingEquipChecker")) {
				client.SendSpeech(".TestCreateStartingEquipmentNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NPC_STARTING_EQUIPMENT_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_STARTING_EQUIPMENT_TEST_RESULT:");

				Assert.Contains("equipped=i_bow_copper", line);

				int arrowAmount = ParseIntField(line, "arrowAmount=");
				Assert.InRange(arrowAmount, 50, 75);

				//#648 - a monster's starting-equipment grants (a mage's spellbook, a ranger's
				//bow) were lootable off the corpse; the real archive's own field name for this
				//mechanism is literally ITEMNEWBIE= - every grant should carry attr_newbie so
				//it disappears with the monster instead of becoming farmable loot.
				Assert.Contains("equippedIsNewbie=True", line);
				Assert.Contains("arrowsAreNewbie=True", line);
			}
		}

		private static int ParseIntField(string line, string prefix) {
			int start = line.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
			int end = start;
			while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-')) {
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

	[CollectionDefinition(NpcStartingEquipmentCollection.Name, DisableParallelization = true)]
	public class NpcStartingEquipmentCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcStartingEquipmentPair";
	}
}
