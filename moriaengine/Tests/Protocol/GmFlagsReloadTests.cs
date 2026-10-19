using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #438: the user reported that after toggling the ".invul"/".invis"
	/// GM flags, saving, and restarting the server, both flags came back off - and that they
	/// couldn't find either flag represented in the save file at all, suspecting the whole
	/// persistence path might be missing.
	///
	/// Both flags are packed bits in AbstractCharacter.directionAndFlags (Flag_Invulnerable =
	/// DirFlag8, Flag_Insubst = DirFlag2 - ".invis" toggles Flag_Insubst, not a separate
	/// "invisible" flag), saved/loaded as a single "directionAndFlags" line
	/// (AbstractCharacter.Save/LoadLine) - same real save -> restart -> reload gap
	/// RaceNationRealmReloadTests (#434) closed for Race/Nation/Realm: no existing test ever
	/// confirmed this round-trips for a REAL restart, only within a single live session.
	/// </summary>
	[Collection(GmFlagsReloadCollection.Name)]
	public class GmFlagsReloadTests {
		private const string Account = "gmflagsreloadtest";
		private const string Password = "gmflagsreloadtest";
		private const string CharName = "FlagsReloadChecker";

		[Fact]
		public void InvulAndInsubstSurviveARealServerRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture1, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(CharName);

					int mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestPromoteToGM");
					Assert.True(fixture1.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));

					mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".Invul");
					client.SendSpeech(".Invis");
					client.SendSpeech(".TestCheckGmFlags");
					Assert.True(fixture1.WaitForGameOutputSince("GM_FLAGS_STATE:", mark, TimeSpan.FromSeconds(10)));
					// Confirm the live, same-session read-after-toggle works BEFORE the restart -
					// isolates whether a failure below is really about reload.
					string setLine = LastLineContaining(fixture1.GameOutputSnapshot(), "GM_FLAGS_STATE:");
					Assert.Contains("invul=True", setLine);
					Assert.Contains("insubst=True", setLine);

					mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestTriggerGlobalSave");
					Assert.True(fixture1.WaitForGameOutputSince("TEST_SAVE: done", mark, TimeSpan.FromSeconds(30)));
				}
			} // fixture1 disposed here - kills the process pair, saves/ is left on disk.

			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture2, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					string[] names = client.ReadCharacterList();
					int slot = Array.IndexOf(names, CharName);
					Assert.True(slot >= 0, "Expected '" + CharName + "' to still exist after restart.");
					client.SendSelectCharacter(slot);

					int mark = fixture2.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCheckGmFlags");
					Assert.True(fixture2.WaitForGameOutputSince("GM_FLAGS_STATE:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(fixture2.GameOutputSnapshot(), "GM_FLAGS_STATE:");

					Assert.Contains("invul=True", line);
					Assert.Contains("insubst=True", line);
				}
			}
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GmFlagsReloadCollection {
		public const string Name = "GmFlagsReloadPair";
	}
}
