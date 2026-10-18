using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #416 - GMs (plevel >= Globals.PlevelOfGM) skip the classic
	/// logout-lingering delay (#261, Player.On_LogOut/CharLingeringTimer) and disappear
	/// immediately on disconnect, instead of staying IsLingering for
	/// LogoutSettings.instance.logoutDelaySeconds (default 60s). A disconnected character
	/// can't observe its own post-disconnect state (its own connection is closed), so a
	/// separate observer character (still connected) checks via Thing.UidGetCharacter -
	/// same pattern as LevelingTests.cs's TestKillPlayerAndReportResult.
	/// </summary>
	[Collection(LogoutLingeringCollection.Name)]
	public class LogoutLingeringTests {
		private const string ObserverAccount = "logoutlingertest";
		private const string PlayerAccount = "logoutlingertest2";
		private const string GmAccount = "logoutlingertest3";
		private const string Password = "logoutlingertest";

		private readonly GameServerPairFixture fixture;

		public LogoutLingeringTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void GmDisconnectsImmediatelyButOrdinaryPlayerLingers() {
			using (var observer = this.ConnectLoginAndCreateCharacter(ObserverAccount, "LingerObserver")) {
				observer.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST: ENABLED", TimeSpan.FromSeconds(10)));

				uint playerUid;
				using (var player = this.ConnectLoginAndCreateCharacter(PlayerAccount, "LingerPlayer")) {
					int markP = this.fixture.GameOutputSnapshot().Length;
					player.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markP, TimeSpan.FromSeconds(10)));
					playerUid = ExtractUid(this.fixture.GameOutputSnapshot(), markP);
				} //connection closed here -> triggers On_LogOut server-side

				uint gmUid;
				using (var gm = this.ConnectLoginAndCreateCharacter(GmAccount, "LingerGM")) {
					int markGmPromote = this.fixture.GameOutputSnapshot().Length;
					gm.SendSpeech(".TestPromoteToGM");
					Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", markGmPromote, TimeSpan.FromSeconds(10)));

					int markGm = this.fixture.GameOutputSnapshot().Length;
					gm.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markGm, TimeSpan.FromSeconds(10)));
					gmUid = ExtractUid(this.fixture.GameOutputSnapshot(), markGm);
				} //connection closed here -> triggers On_LogOut server-side

				Thread.Sleep(1500); //give the server time to process both disconnects

				int markPlayerCheck = this.fixture.GameOutputSnapshot().Length;
				observer.SendSpeech(".TestReportLingerState " + playerUid);
				Assert.True(this.fixture.WaitForGameOutputSince("LINGER_STATE: uid=" + playerUid, markPlayerCheck, TimeSpan.FromSeconds(10)),
					"playerUid=" + playerUid + " gmUid pending. Output:\n" + this.fixture.GameOutputSnapshot());
				string playerLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LINGER_STATE: uid=" + playerUid);
				Assert.Contains("isLingering=True", playerLine);
				Assert.Contains("flagDisconnected=False", playerLine);

				int markGmCheck = this.fixture.GameOutputSnapshot().Length;
				observer.SendSpeech(".TestReportLingerState " + gmUid);
				Assert.True(this.fixture.WaitForGameOutputSince("LINGER_STATE: uid=" + gmUid, markGmCheck, TimeSpan.FromSeconds(10)));
				string gmLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LINGER_STATE: uid=" + gmUid);
				Assert.Contains("isLingering=False", gmLine);
				Assert.Contains("flagDisconnected=True", gmLine);
			}
		}

		private static uint ExtractUid(string output, int since) {
			int idx = output.IndexOf("MY_UID: ", since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find MY_UID after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + "MY_UID: ".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
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
	public class LogoutLingeringCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LogoutLingeringPair";
	}
}
