using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #600 - proves a real NPC equipped with a real bow (c_titan_ranger, real archive
	/// content, Equip = i_bow) actually gets a live ranged combat range and a resolved
	/// ammo projectile from CombatCalculator, instead of being stuck at NPCDef.WeaponRange's
	/// unconfigured melee default of 1.
	/// </summary>
	[Collection(NpcRangedCombatCollection.Name)]
	public class NpcRangedCombatTests {
		private const string Account = "npcrangedcombattest";
		private const string Password = "npcrangedcombattest";

		private readonly GameServerPairFixture fixture;

		public NpcRangedCombatTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RangerWithARealBowGetsARealRangedCombatRangeAndAmmo() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangedNpcChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnRangerAndReportCombatValues");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGED_NPC_TEST_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RANGED_NPC_TEST_SPAWNED:");

				Assert.DoesNotContain("rangeVsM=1 ", line); //the bug: was stuck at NPCDef.WeaponRange's unconfigured melee default
				Assert.DoesNotContain("rangeVsP=1", line);
				Assert.Contains("hasProjectile=True", line); //a real matching i_arrow resolved from its own starting-equipment backpack stock
			}
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

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

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

	[CollectionDefinition(NpcRangedCombatCollection.Name, DisableParallelization = true)]
	public class NpcRangedCombatCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcRangedCombatPair";
	}
}
