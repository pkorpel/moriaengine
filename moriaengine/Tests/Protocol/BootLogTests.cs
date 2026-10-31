using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#937 - a boot logs no error or warning at all, on a new world or a saved one.</summary>
	[Collection(BootLogCollection.Name)]
	public class BootLogTests {
		private const string Account = "bootlogtest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static readonly Regex ProblemRE = new Regex(@"\b(ERROR|WARNING|CRITICAL|FATAL):");

		[Fact]
		public void BootsWithoutAProblemAndLosesNothing() {
			using (var fixture1 = new GameServerPairFixture()) {
				AssertNoProblems(fixture1);
				using (var client = Connect("Boot Saver")) {
					string defs = Ask(fixture1, client, ".TestBootDefs", "BOOT_DEFS:");
					Assert.Contains("missing=[]", defs);
					//the engine's bits, not defs.def's Sphere 08000 and 020
					Assert.Contains("attrStatic=64 statfWar=64", defs);
					Assert.Contains("sewingKit=True", defs);
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				AssertNoProblems(fixture2);
			}
		}

		private static void AssertNoProblems(GameServerPairFixture fixture) {
			List<string> problems = ProblemsAtBoot(fixture);
			Assert.True(problems.Count == 0, problems.Count + " at boot:\n" + string.Join("\n", problems));
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
	public class BootLogCollection {
		public const string Name = "BootLog";
	}
}
