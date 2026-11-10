using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1358 - a boot with no script change reuses the last build of both script assemblies (scripts and Generated)
	/// instead of running dotnet build, and the reused assemblies work: a script command answers and a world save, which
	/// goes through the Generated save implementors, completes.</summary>
	[Collection(BuildReuseCollection.Name)]
	public class BuildReuseBootTests {
		private const string Account = "buildreuse";
		private const string Password = "buildreuse";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void ASecondBootReusesBothScriptAssemblies() {
			using (var first = new GameServerPairFixture()) {
				// the first boot builds or reuses, whichever the last run left; it leaves both stamps for the second
				Assert.Contains("C# scripts", first.GameOutputSnapshot(), StringComparison.Ordinal);
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				string output = second.GameOutputSnapshot();
				Assert.True(output.Contains("Reused compiled C# scripts, no source changed.", StringComparison.Ordinal), output);
				Assert.True(output.Contains("Reused compiled Generated C# scripts, no generated code changed.", StringComparison.Ordinal), output);
				Assert.DoesNotContain("Done compiling", output, StringComparison.Ordinal);

				using (var client = Connect(second, "Build Reuser")) {
					int mark = second.GameOutputSnapshot().Length;
					client.SendSpeech(".TestTriggerGlobalSave");
					Assert.True(second.WaitForGameOutputSince("TEST_SAVE: done", mark, Wait), second.GameOutputSnapshot());
				}
			}
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture, Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Password);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture, string account) {
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class BuildReuseCollection {
		public const string Name = "BuildReuse";
	}
}
