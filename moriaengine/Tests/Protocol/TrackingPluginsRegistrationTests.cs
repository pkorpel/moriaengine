using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #444: PlayerTrackingPlugin/NPCTrackingPlugin never called
	/// .Register() on their PluginDef instances at all, so "p_tracking"/"p_NPCtracking" were
	/// never resolvable via the registry under any timing. Fixed alongside #436's
	/// RegenerationPlugin fix (added the missing .Register() plus the same early-touch static
	/// cctor pattern #434 established). This checks resolution as the very first command sent,
	/// with no prior action that could have incidentally touched TrackingPlugins first.
	/// </summary>
	[Collection(TrackingPluginsRegistrationCollection.Name)]
	public class TrackingPluginsRegistrationTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public TrackingPluginsRegistrationTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BothTrackingPluginDefsAreRegisteredRightAfterBoot() {
			using (var client = this.ConnectLoginAndCreateCharacter("TrackingDefsChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckTrackingDefsRegistered");
				Assert.True(this.fixture.WaitForGameOutputSince("TRACKING_DEFS_REGISTERED:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "TRACKING_DEFS_REGISTERED:");
				Assert.Contains("playerTracking=True", line);
				Assert.Contains("npcTracking=True", line);
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TrackingPluginsRegistrationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TrackingPluginsRegistrationPair";
	}
}
