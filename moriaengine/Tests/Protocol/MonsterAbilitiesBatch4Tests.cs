using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #797 - monster abilities batch 4 (phoenix fire/rebirth, explode, dragon regeneration,
	/// life leech, reactive armor, stamina drain, lightnings, summon devourer, mana gain,
	/// mana/stamina curses, inexhaustible, push all, magic shield, auto reflect). See
	/// scripts/Tests/MonsterAbilitiesBatch4Tests.cs for the server-side helpers. Magic shield's
	/// formula is covered in the fast tier (MagicShieldFormulaTests).
	/// </summary>
	[Collection(MonsterAbilitiesBatch4Collection.Name)]
	public class MonsterAbilitiesBatch4Tests {
		private const string Account = "monsterabilitiesbatch4test";
		private const string Password = "monsterabilitiesbatch4test";

		private readonly GameServerPairFixture fixture;

		public MonsterAbilitiesBatch4Tests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SwingReactionAbilitiesWork() {
			string line = this.Run("BatchFourSwing", ".TestBatch4SwingReactions", "B4_SWING_TEST:");
			Has(line, "reflected=True");
			Has(line, "selfHurt=True");
			Has(line, "drained=10");
			Has(line, "stamAfter=40");
			Has(line, "gain1=0");
			Has(line, "gain2=100");
			Has(line, "reflectArmed=True");
			Has(line, "hasReflection=True");
			Has(line, "reflectArmedAgain=False");
			Has(line, "leeched=25");
			Has(line, "eaten=True");
			Has(line, "summonDeleted=True");
			Has(line, "strOk=True");
		}

		[Fact]
		public void HazardTilesLightningsAndExplodeHurtPlayers() {
			string line = this.Run("BatchFourHazard", ".TestBatch4HazardsAndExplode", "B4_HAZARD_TEST:");
			Has(line, "standerHit=True");
			Has(line, "liveTiles=1");
			Has(line, "stepperHit=True");
			Has(line, "liveTilesAfterStep=0");
			Has(line, "lightningHit=True");
			Has(line, "explodeOk=True");
		}

		[Fact]
		public void PushAllAndTheAreaCursesFollowTheArchiveRules() {
			string line = this.Run("BatchFourCurse", ".TestBatch4PushAndCurses", "B4_CURSE_TEST:");
			//not an exact count: this account's earlier test characters stay in the world at the
			//same open-ground spot, and they're players in range too
			Assert.DoesNotContain("pushed=0 ", line);
			Has(line, "pushedAway=True");
			Has(line, "selfManaOk=True");
			Has(line, "petManaLost=150"); //5x vs pets
			Has(line, "selfStamLost=30");
			Has(line, "targetSpared=True");
			Has(line, "targetHitWithRuleOff=True");
		}

		[Fact]
		public void PhoenixRisesUntilItsMaxHitsRunOutAndRegenerationAndInexhaustibleHeal() {
			string line = this.Run("BatchFourPhoenix", ".TestBatch4PhoenixRegenInexhaustible", "B4_PHOENIX_TEST:");
			Has(line, "rose1=True");
			Has(line, "max1=500");
			Has(line, "hits1=500");
			Has(line, "rose2=True");
			Has(line, "max2=250");
			Has(line, "rose3=False");
			Has(line, "regen1=0");
			Has(line, "regen2=40");
			Has(line, "stamGained=3");
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private string Run(string charName, string command, string marker) {
			using (var client = this.ConnectLoginAndCreateCharacter(charName)) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTeleportToOpenGround");
				Thread.Sleep(500);
				client.SendSpeech(command);
				Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(10)),
					"no " + marker + " line - server output:\n" + this.fixture.GameOutputSnapshot());
				return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
			}
		}

		//the whole result line in the failure message - Assert.Contains truncates it
		private static void Has(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "expected '" + expected + "' in: " + line);
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(MonsterAbilitiesBatch4Collection.Name, DisableParallelization = true)]
	public class MonsterAbilitiesBatch4Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterAbilitiesBatch4Pair";
	}
}
