using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1172 - a skill under way when the world is saved goes on after the restart, as Sphere's does: a smith making two
	/// daggers, and a held monster in the middle of a swing at a sheep. Server side: scripts/Tests/SkillRestartTests.cs.</summary>
	[Collection(SkillRestartCollection.Name)]
	public class SkillRestartTests {
		private const string Account = "skillrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static readonly Regex ProblemRE = new Regex(@"\b(ERROR|WARNING|CRITICAL|FATAL):");

		[Fact]
		public void ASkillUnderWayGoesOnAfterARestart() {
			string setup;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect("Restart Smith")) {
					setup = Ask(fixture1, client, ".TestSkillRestart \"setup\"", "SKILL_RESTART: setup");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			Assert.Contains("smithSkill=Blacksmith", setup);
			Assert.DoesNotContain("orcSkill=None", setup);
			Assert.Equal("0", Value(setup, "daggers"));

			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				List<string> problems = ProblemsAtBoot(fixture2);
				//the strokes were due 5 s after the save, in server time, which goes on from the save: by now both have come due,
				//the orc's has landed, and the smith's waits because the smith is offline (as in Sphere, which doesn't tick them)
				Thread.Sleep(10000);
				string after;
				using (var client = ConnectAs("Restart Smith")) {
					Thread.Sleep(20000); //the stroke goes on at the login, then the rest of it and the second dagger
					after = Ask(fixture2, client, ".TestSkillRestart \"after\"", "SKILL_RESTART: after");
				}
				Assert.True(problems.Count == 0, problems.Count + " at boot:\n" + string.Join("\n", problems));
				Assert.Contains("found=True,True,True", after);
				Assert.True(int.Parse(Value(after, "daggers")) > 0, "No dagger made after the restart: " + after);
				Assert.True(int.Parse(Value(after, "sheepHits")) < int.Parse(Value(setup, "sheepHits")), "The orc's swing didn't land: " + after);
			}
		}

		private static string Value(string line, string key) {
			Match m = Regex.Match(line, @"\b" + key + @"=(\S+)");
			Assert.True(m.Success, "No " + key + "= in: " + line);
			return m.Groups[1].Value;
		}

		private static List<string> ProblemsAtBoot(GameServerPairFixture fixture) {
			string output = fixture.GameOutputSnapshot();
			int end = output.IndexOf("Init done.", StringComparison.Ordinal);
			List<string> problems = new List<string>();
			foreach (string line in output.Substring(0, end).Split('\n')) {
				if (ProblemRE.IsMatch(line)) {
					problems.Add(line.Trim());
				}
			}
			return problems;
		}

		private static TestGameClient Connect(string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort();
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		/// <summary>Logs in as a character the account already has.</summary>
		private static TestGameClient ConnectAs(string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort();
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			int index = Array.IndexOf(client.ReadCharacterList(), charName);
			Assert.True(index >= 0, "No character " + charName + " on " + Account);
			client.SendSelectCharacter(index);
			Thread.Sleep(1500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);
						if (login.ReadServersList() < 1) {
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
	public class SkillRestartCollection {
		public const string Name = "SkillRestart";
	}
}
