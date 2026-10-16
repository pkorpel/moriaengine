using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Regression for a real user-reported bug: ".info" (via ".x info" + targeting a
	/// corpse) crashed outright with "ERROR: Light property is not valid for corpses" -
	/// Corpse.Light's getter unconditionally threw, and the generic reflection-based .info
	/// field renderer (InfoDialogHandler.WriteDataField/GetFieldName) called every viewable
	/// property's getter with no exception handling, so one throwing property aborted the
	/// whole dialog build. Fixed two ways: Corpse.Light now returns LightType.Empty (the real
	/// "no light shape" sentinel) instead of throwing, and InfoDialogHandler now catches a
	/// per-field exception and renders "&lt;error&gt;" for just that row instead of crashing
	/// the entire dialog - so any future similarly-specialized property override can't
	/// reproduce this failure mode either.</summary>
	[Collection(InfoDialogCollection.Name)]
	public class InfoDialogTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public InfoDialogTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void InfoDialogOnCorpseDoesNotCrash() {
			using (var client = this.ConnectLoginAndCreateCharacter("InfoCorpseTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupCorpseForInfo");
				this.AssertSeen("INFO_CORPSE_SETUP:", mark, TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "INFO_CORPSE_SETUP:");
				Assert.DoesNotContain("none", setupLine);
				uint corpseUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), mark, "INFO_CORPSE_SETUP: uid=");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".x info");
				Thread.Sleep(300);
				client.SendTargetUid(corpseUid);

				// D_Info.Construct builds the headline via GUTAText.Builder.TextHeadline(string),
				// which embeds only a numeric textId in the layout grammar (the {gumpptext ...}
				// entries) - the literal text lives in the packet's separate dynamic strings
				// list, hence ReadGumpDialogWithStrings rather than the plain layout-only read.
				var (_, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				Assert.Contains(strings, s => s.StartsWith("Info dialog", StringComparison.Ordinal));

				string outputSinceCommand = this.fixture.GameOutputSnapshot().Substring(mark);
				Assert.DoesNotContain("Light property is not valid for corpses", outputSinceCommand);
			}
		}

		private void AssertSeen(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
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

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

	[CollectionDefinition(InfoDialogCollection.Name, DisableParallelization = true)]
	public class InfoDialogCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "InfoDialogPair";
	}
}
