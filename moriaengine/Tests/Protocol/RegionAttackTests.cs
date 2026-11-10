using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1019 - where an attack may start, by real attack requests: a guarded town allows it (its guards punish, as
	/// on Erebor), a no-PvP area allows fighting monsters but not players, a safe zone allows nothing, an arena everything.
	/// Server side: scripts/Tests/RegionAttackTests.cs.</summary>
	[Collection(RegionAttackCollection.Name)]
	public class RegionAttackTests {
		private const string AccountA = "regionattacka", AccountB = "regionattackb";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public RegionAttackTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RegionsDecideWhoMayBeAttacked() {
			using (var a = this.Connect("Region Attacker", AccountA)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using (var b = this.Connect("Region Victim", AccountB)) {
					this.Check(a, b, "guarded", "guarded=True safe=False", true, true);
					this.Ask(a, ".TestRegionAttackRestore", "REGION_RESTORED:");
					this.Check(a, b, "a_Goblin_Labyrint_1", "nopvp=True arena=False", true, false);
					this.Check(a, b, "a_Arena_Lamy", "safe=True nopvp=False arena=False", false, false); //safe only, despite its name
					this.Check(a, b, "a_Arena_Gondor", "arena=True", true, true);
				}
			}
		}

		/// <summary>Both into the region, then the attacker asks to attack the rabbit, then the other player.</summary>
		private void Check(TestGameClient a, TestGameClient b, string region, string flags, bool monster, bool player) {
			string go = this.Ask(a, ".TestRegionAttackGo \"" + region + "\"", "REGION_GO:");
			Assert.Contains("inside=True", go);
			Assert.Contains(flags, go);
			uint rabbit = Number(go, "rabbit=");
			uint other = Number(this.Ask(b, ".TestRegionAttackJoin " + Number(go, "me="), "REGION_JOIN:"), "uid=");
			a.SendRequestAttack(rabbit);
			Thread.Sleep(300);
			string onRabbit = Field(this.Ask(a, ".TestRegionAttackReport", "REGION_ATTACK:"));
			Assert.True(onRabbit == (monster ? "target=" + rabbit : "target=none"), "the rabbit in " + region + ": " + onRabbit);
			a.SendRequestAttack(other);
			Thread.Sleep(300);
			string onPlayer = Field(this.Ask(a, ".TestRegionAttackReport", "REGION_ATTACK:"));
			Assert.True(onPlayer == (player ? "target=" + other : "target=none"), "the player in " + region + ": " + onPlayer);
		}

		private static string Field(string line) {
			int start = line.IndexOf("target=", StringComparison.Ordinal);
			return line.Substring(start, line.IndexOf(' ', start) - start);
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(key, StringComparison.Ordinal) + key.Length;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot().Substring(mark));
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName, string account) {
			int redirectPort = TownTestsLogin.RedirectPort(AccountA);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is IOException)) {
					game.Dispose();
					lastFailure = lastFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as '" + charName + "' after retrying.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RegionAttackCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RegionAttack";
	}
}
