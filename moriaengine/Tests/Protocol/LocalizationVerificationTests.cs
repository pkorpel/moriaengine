using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Permanent regression guard for #104 (Part B): asserts every live localization
	/// entry (both languages) resolves to a real value from the resx files, not the
	/// "missing" sentinel - catches the resx migration silently dropping/mis-keying an
	/// entry (e.g. a field rename that doesn't get mirrored into the resx key).
	/// </summary>
	[Collection(LocalizationVerificationCollection.Name)]
	public class LocalizationVerificationTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LocalizationVerificationTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryLiveLocalizationEntryResolvesForBothLanguages() {
			using (var client = this.ConnectLoginAndCreateCharacter("LocVerifier")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestVerifyAllLocEntries");
				Assert.True(this.fixture.WaitForGameOutputSince("LOC_VERIFY:", mark, TimeSpan.FromSeconds(30)));
				string output = this.fixture.GameOutputSnapshot();
				string summary = LastLineContaining(output, "LOC_VERIFY:");
				//#1020 - Assert.Contains cuts its message short; list the missing keys themselves
				System.Text.StringBuilder missing = new System.Text.StringBuilder();
				foreach (string line in output.Substring(mark).Split('\n')) {
					if (line.Contains("LOC_VERIFY_MISSING:")) {
						missing.AppendLine(line.Trim());
					}
				}
				Assert.True(summary.Contains("missing=0"), summary + "\n" + missing);
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
					System.Threading.Thread.Sleep(300);
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
					System.Threading.Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class LocalizationVerificationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LocalizationVerificationPair";
	}
}
