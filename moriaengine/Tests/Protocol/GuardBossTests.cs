using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1234 - the guard boss system: B guards A (linked through the .spawn dialog's "Guarded by" and its cursor),
	/// C guards B; nothing hurts a guarded spawn's monster while a guard lives; loops and bad links are refused; a deleted
	/// guard gem guards nothing. Server side: scripts/Tests/GuardBossTests.cs.</summary>
	[Collection(GuardBossCollection.Name)]
	public class GuardBossTests {
		private const string Account = "guardboss";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public GuardBossTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void AGuardedSpawnTakesNoDamageWhileItsGuardsLive() {
			using (var gm = this.Connect("Boss Keeper")) {
				this.Ask(gm, ".TestPromoteToGM", "PROMOTE_TO_GM:"); //before the gems: they're invisible to all but GMs
				gm.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);
				string setup = this.Ask(gm, ".TestGuardBossSetup", "GUARD_BOSS_SETUP:");
				uint a = Number(setup, "a="), b = Number(setup, "b=");

				//the .spawn dialog of a placed gem has "Guarded by"; its cursor links B as A's guard
				gm.SendDoubleClick(a);
				var spawn = gm.ReadGumpDialogWithStrings(Wait);
				Assert.True(Regex.IsMatch(spawn.LayoutText, @"\s1\s+0\s+3\}"), "no Guarded by button: " + spawn.LayoutText);
				Assert.Contains("Guarded by (0)", string.Join(" | ", spawn.Strings));
				gm.SendGumpResponse(spawn.GumpUid, 3);
				var links = gm.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("No guards", string.Join(" | ", links.Strings));
				Assert.True(Regex.IsMatch(links.LayoutText, @"4005\s+4007\s+1\s+0\s+2\}"), "no + Guarded by button: " + links.LayoutText);
				gm.SendGumpResponse(links.GumpUid, 2);
				Thread.Sleep(300);
				gm.SendTargetUid(b);
				links = gm.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("alive 1", string.Join(" | ", links.Strings));
				gm.SendGumpResponse(links.GumpUid, 0);

				//C guards B; a spawn can't guard itself, twice, in a loop or be anything but a spawn
				Assert.Contains("cGuardsB=True self=True duplicate=True twoLoop=True threeLoop=True notGem=True guardsOfA=1 guardsOfB=1 guardsOfC=0",
					this.Ask(gm, ".TestGuardBossLinks", "GUARD_BOSS_LINKS:"));

				//A and B are guarded, C isn't
				string hit = this.Ask(gm, ".TestGuardBossHit", "GUARD_BOSS_HIT:");
				Assert.Contains(" a=0 b=0 ", hit);
				Assert.True(Number(hit, "c=") > 0, hit);

				//C's gem is gone: B can be hurt, A still not, B's monster lives
				Assert.Contains("guardsOfB=1", this.Ask(gm, ".TestGuardBossDeleteGem", "GUARD_BOSS_GEM_DELETED:"));
				hit = this.Ask(gm, ".TestGuardBossHit", "GUARD_BOSS_HIT:");
				Assert.Contains(" a=0 ", hit);
				Assert.True(Number(hit, "b=") > 0, hit);

				//B's monster dies: A can be hurt
				int killed = this.fixture.GameOutputSnapshot().Length;
				Assert.Contains("GUARD_BOSS_KILL: done", this.Ask(gm, ".TestGuardBossKill \"b\"", "GUARD_BOSS_KILL:"));
				Assert.True(this.fixture.WaitForGameOutputSince("SPAWN_KILL_TEST: killed", killed, Wait));
				hit = this.Ask(gm, ".TestGuardBossHit", "GUARD_BOSS_HIT:");
				Assert.Contains(" b=dead ", hit);
				Assert.True(Number(hit, "a=") > 0, hit);
			}
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

		private TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Account);
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
	public class GuardBossCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GuardBoss";
	}
}
