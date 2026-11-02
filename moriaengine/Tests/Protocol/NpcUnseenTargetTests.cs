using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1038 - server side in scripts/Tests/NpcUnseenTargetTests.cs, driven by the real AI.</summary>
	[Collection(NpcUnseenTargetCollection.Name)]
	public class NpcUnseenTargetTests {
		private const string TestAccount = "protocoltest";
		private readonly GameServerPairFixture fixture;

		public NpcUnseenTargetTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcForgetsATargetItCanNoLongerSee() {
			int redirectPort = this.RedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestAccount);
				game.ReadCharacterList();
				game.SendCreateCharacter("Vanisher");
				Thread.Sleep(500);
				this.Do(game, ".TestUnseenStart", "UNSEEN_STARTED");
				Assert.Contains("targetIsMe=True", this.ReportAfter(game, 2500), StringComparison.Ordinal);

				this.Do(game, ".TestUnseenGoInvisible", "UNSEEN_INVISIBLE");
				string gone = this.ReportAfter(game, 4000);
				Assert.Contains("targetIsMe=False", gone, StringComparison.Ordinal);
				Assert.Contains("war=False", gone, StringComparison.Ordinal);
				Assert.DoesNotContain("state=Fighting", gone, StringComparison.Ordinal);

				this.Do(game, ".TestUnseenBeSeen", "UNSEEN_VISIBLE");
				Assert.Contains("targetIsMe=True", this.ReportAfter(game, 4000), StringComparison.Ordinal);

				this.Do(game, ".TestUnseenHide", "UNSEEN_HIDDEN");
				Assert.Contains("targetIsMe=False", this.ReportAfter(game, 4000), StringComparison.Ordinal);

				//a detector stays on the target, and its reveal ends the hiding
				this.Do(game, ".TestUnseenBeSeen", "UNSEEN_VISIBLE");
				Assert.Contains("targetIsMe=True", this.ReportAfter(game, 4000), StringComparison.Ordinal);
				this.Do(game, ".TestUnseenDetector", "hasDetect=True");
				this.Do(game, ".TestUnseenGoInvisible", "UNSEEN_INVISIBLE");
				string detected = this.ReportAfter(game, 8000);
				Assert.Contains("targetIsMe=True", detected, StringComparison.Ordinal);
				Assert.Contains("invisible=False", detected, StringComparison.Ordinal);
			}
		}

		private void Do(TestGameClient game, string speech, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			game.SendSpeech(speech);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(15)), "no " + marker + ":\n" + this.fixture.GameOutputSnapshot());
		}

		private string ReportAfter(TestGameClient game, int milliseconds) {
			Thread.Sleep(milliseconds);
			int mark = this.fixture.GameOutputSnapshot().Length;
			game.SendSpeech(".TestUnseenReport");
			Assert.True(this.fixture.WaitForGameOutputSince(" end", mark, TimeSpan.FromSeconds(15)), this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int at = output.LastIndexOf("UNSEEN_REPORT:", StringComparison.Ordinal);
			return output.Substring(at, output.IndexOf(" end", at, StringComparison.Ordinal) - at);
		}

		private int RedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestAccount);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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
	public class NpcUnseenTargetCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcUnseenTarget";
	}
}
