using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#902 - a group sharing a monster, and a player's death, on a real server.</summary>
	[Collection(SharedExperienceCollection.Name)]
	public class SharedExperienceTests {
		private const string AccountA = "sharedexpa", AccountB = "sharedexpb", AccountC = "sharedexpc";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public SharedExperienceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void FightersShareAndPartiesCountAndPlayersPay() {
			using (var a = this.Connect("Shared Exp One", AccountA)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var b = this.Connect("Shared Exp Two", AccountB);
				using var c = this.Connect("Shared Exp Three", AccountC);
				foreach (var client in new[] { a, b, c }) {
					this.Ask(client, ".TestExpReady G", "EXP_READY:");
				}

				//two fought: 1410 between them; the third, who didn't, gets nothing
				this.Ask(a, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(b, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(c, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(a, ".TestExpFight", "EXP_FOUGHT:");
				this.Ask(b, ".TestExpFight", "EXP_FOUGHT:");
				this.Ask(a, ".TestExpKill", "EXP_KILLED:");
				Assert.Equal(1705, this.Exp(a));
				Assert.Equal(1705, this.Exp(b));
				Assert.Equal(1000, this.Exp(c));

				//in a party with a fighter, the third shares without fighting
				this.Ask(a, ".TestExpParty", "EXP_PARTY:");
				this.Ask(c, ".TestExpParty", "EXP_PARTY:");
				this.Ask(a, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(b, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(c, ".TestExpMonster open", "EXP_MONSTER:");
				this.Ask(a, ".TestExpFight", "EXP_FOUGHT:");
				this.Ask(b, ".TestExpFight", "EXP_FOUGHT:");
				this.Ask(b, ".TestExpKill", "EXP_KILLED:");
				Assert.Equal(1000 + 576, this.Exp(c));

				//in a Gondor area, at most 100 each
				this.Ask(a, ".TestExpMonster cap", "EXP_MONSTER:");
				this.Ask(a, ".TestExpFight", "EXP_FOUGHT:");
				double before = this.Exp(a);
				this.Ask(a, ".TestExpKill", "EXP_KILLED:");
				Assert.Equal(before + 100, this.Exp(a));

				//a player's death: (5 + 1) * 330 / 20 = 99, shared by those of the other realm who fought
				this.Ask(a, ".TestExpReady G", "EXP_READY:");
				this.Ask(b, ".TestExpReady M", "EXP_READY:");
				this.Ask(c, ".TestExpReady M", "EXP_READY:");
				this.Ask(b, ".TestExpFightFirst", "EXP_FOUGHT:");
				this.Ask(c, ".TestExpFightFirst", "EXP_FOUGHT:");
				double victim = this.Exp(a), killer = this.Exp(b), helper = this.Exp(c);
				this.Ask(b, ".TestExpKillFirst", "EXP_KILLED:");
				Assert.Equal(victim - 99, this.Exp(a));
				Assert.Equal(killer + 49, this.Exp(b));
				Assert.Equal(helper + 49, this.Exp(c));
			}
		}

		private double Exp(TestGameClient client) {
			string report = this.Ask(client, ".TestExpReport", "EXP_REPORT:");
			int start = report.IndexOf("exp=", StringComparison.Ordinal) + 4;
			return double.Parse(report.Substring(start, report.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName, string account) {
			int redirectPort = TownTestsLogin.RedirectPort(AccountA);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SharedExperienceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SharedExperience";
	}
}
