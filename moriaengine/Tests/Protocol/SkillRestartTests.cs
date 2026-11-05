using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1172 - a skill under way when the world is saved goes on after the restart, as Sphere's does: a smith making two
	/// daggers, a second smith between two items (on a second account), and a held monster in the middle of a swing at a sheep.
	/// Server side: scripts/Tests/SkillRestartTests.cs.</summary>
	[Collection(SkillRestartCollection.Name)]
	public class SkillRestartTests {
		private const string Account = "skillrestart";
		private const string SecondAccount = "skillrestart2";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static readonly Regex ProblemRE = new Regex(@"\b(ERROR|WARNING|CRITICAL|FATAL):");

		[Fact]
		public void ASkillUnderWayGoesOnAfterARestart() {
			string setup, between, presave;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(Account, "Restart Smith")) {
					Ask(fixture1, client, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
					setup = Ask(fixture1, client, ".TestSkillRestart \"setup\"", "SKILL_RESTART: setup");
					using (var second = Connect(SecondAccount, "Restart Smithy")) {
						between = Ask(fixture1, second, ".TestSkillRestart \"between\"", "SKILL_RESTART: between");
					}
					presave = Ask(fixture1, client, ".TestSkillRestart \"presave\"", "SKILL_RESTART: presave");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			Assert.Contains("smithSkill=Blacksmith", setup);
			Assert.DoesNotContain("orcSkill=None", setup);
			Assert.Equal("0", Value(setup, "daggers"));
			Assert.Contains("smithySkill=None plugin=True", between);
			Assert.Equal("0", Value(between, "daggers"));
			//what the save catches: both smiths short of their daggers, the smith and the orc mid-stroke, the other between items
			Assert.Contains("found=True,True,True,True", presave);
			Assert.Contains("smithSkill=Blacksmith", presave);
			Assert.DoesNotContain("orcSkill=None", presave);
			Assert.Equal("0", Value(presave, "daggers"));
			Assert.Equal("0", Value(presave, "smithyDaggers"));
			Assert.Equal("True", Value(presave, "smithyPlugin"));
			Assert.Equal("Swords", Value(presave, "lostOrcSkill")); //its target already deleted, the swing still under way

			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				List<string> problems = ProblemsAtBoot(fixture2);
				//the strokes were due 5 s after the save and the second smith's next item 15 s after it, in server time, which goes
				//on from the save: by now all have come due, the orc's swing has landed, and both smiths wait as they're offline
				//(as in Sphere, which doesn't tick them)
				Thread.Sleep(20000);
				string after = "";
				using (var client = ConnectAs(Account, "Restart Smith"))
				using (var second = ConnectAs(SecondAccount, "Restart Smithy")) {
					//the stroke goes on at the login, then the rest of it and the second dagger; the second smith starts the next item
					DateTime until = DateTime.UtcNow.AddSeconds(60);
					do {
						Thread.Sleep(3000);
						after = Ask(fixture2, client, ".TestSkillRestart \"after\"", "SKILL_RESTART: after");
					} while ((DateTime.UtcNow < until) && !(after.Contains(" daggers=2 ") && after.Contains(" smithyDaggers=1 ")));
				}
				Assert.True(problems.Count == 0, problems.Count + " at boot:\n" + string.Join("\n", problems));
				//the orc whose target died before the save: its swing ends, no error from it or anything else since the boot
				Assert.Equal("None", Value(after, "lostOrcSkill"));
				List<string> later = ProblemsSinceBoot(fixture2);
				Assert.True(later.Count == 0, later.Count + " after boot:\n" + string.Join("\n", later));
				Assert.Contains("found=True,True,True,True", after);
				//both of the order's daggers: the queue itself came back from the save, not only the stroke in hand
				Assert.True(Value(after, "daggers") == "2", "The smith's order didn't finish after the restart: " + after);
				Assert.True(Value(after, "smithyDaggers") == "1", "The smith between two items didn't go on after the login: " + after);
				Assert.True(int.Parse(Value(after, "sheepHits")) < int.Parse(Value(presave, "sheepHits")), "The orc's swing didn't land: " + after);
			}
		}

		private static string Value(string line, string key) {
			Match m = Regex.Match(line, @"\b" + key + @"=(\S+)");
			Assert.True(m.Success, "No " + key + "= in: " + line);
			return m.Groups[1].Value;
		}

		private static List<string> ProblemsSinceBoot(GameServerPairFixture fixture) {
			string output = fixture.GameOutputSnapshot();
			int start = output.IndexOf("Init done.", StringComparison.Ordinal);
			List<string> problems = new List<string>();
			foreach (string line in output.Substring(start).Split('\n')) {
				if (ProblemRE.IsMatch(line)) {
					problems.Add(line.Trim());
				}
			}
			return problems;
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

		private static TestGameClient Connect(string account, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(account, account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		/// <summary>Logs in as a character the account already has.</summary>
		private static TestGameClient ConnectAs(string account, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(account, account);
			int index = Array.IndexOf(client.ReadCharacterList(), charName);
			Assert.True(index >= 0, "No character " + charName + " on " + account);
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

		private static int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
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
