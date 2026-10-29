using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#827 - Erebor's healers (real converted C_m_HEALER): a ghost that single-clicks one from close by is
	/// resurrected (e_healer), a realm outcast is told they're cursed instead; the healer's spk_healer "res" speech
	/// tells a living outcast the same, but can't hear a ghost (as in Erebor). Real client clicks and speech.
	/// Server-side helpers: scripts/Tests/HealerTests.cs.</summary>
	[Collection(HealerCollection.Name)]
	public class HealerTests {
		private const string Account = "healertestplayer";
		private const string AdminAccount = "healertestadmin"; //the server's first account - can turn on auto-creation
		private const string Cursed = "cursed by the gods";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public HealerTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void HealersResurrectClickingGhostsButNotOutcasts() {
			using (var client = this.Connect(Account, "Healer Patient")) {
				uint healer = this.Setup(client, "Gondor 2 1");
				client.SendSingleClick(healer);
				Assert.Contains("dead=False", this.Report(client));

				healer = this.Setup(client, "GondorOutcast 2 1");
				client.SendSingleClick(healer);
				Assert.NotNull(TryReadMessageContaining(client, Cursed, Wait));
				Assert.Contains("dead=True", this.Report(client));

				healer = this.Setup(client, "Gondor 6 1");
				client.SendSingleClick(healer);
				Assert.Contains("dead=True", this.Report(client));

				//a ghost's "res" isn't heard - the healer doesn't see ghosts (Erebor: doesn't understand them)
				this.Setup(client, "Gondor 2 1");
				client.SendSpeech("resurrect me");
				Assert.Contains("dead=True", this.Report(client));

				this.Setup(client, "GondorOutcast 2 0");
				client.SendSpeech("res");
				Assert.NotNull(TryReadMessageContaining(client, Cursed, Wait));
			}
		}

		private uint Setup(TestGameClient client, string args) {
			string line = this.Ask(client, ".TestHealerSetup \"" + args + "\"", "HEALER_SETUP:");
			Assert.Contains("seesMe=True", line);
			int idx = line.IndexOf("healer=", StringComparison.Ordinal) + "healer=".Length;
			int end = idx;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return uint.Parse(line.Substring(idx, end - idx), System.Globalization.CultureInfo.InvariantCulture);
		}

		private string Report(TestGameClient client) {
			Thread.Sleep(500);
			return this.Ask(client, ".TestHealerReport", "HEALER_REPORT:");
		}

		private static string TryReadMessageContaining(TestGameClient client, string text, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (true) {
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					return null;
				}
				string message;
				try {
					message = client.ReadSystemMessage(remaining);
				} catch (TimeoutException) {
					return null;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return message;
				}
			}
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			if (!this.fixture.WaitForGameOutputSince(marker, mark, Wait)) {
				string output = this.fixture.GameOutputSnapshot();
				Assert.Fail("no '" + marker + "' after " + speech + ", server said:\n" + output.Substring(Math.Min(mark, output.Length)));
			}
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
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

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectOnce(AdminAccount, "Trade Admin")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, Wait));
			}
			autoAccountCreationEnabled = true;
		}

		private TestGameClient Connect(string account, string charName) {
			this.EnsureAutoAccountCreationEnabled();
			return this.ConnectOnce(account, charName);
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private TestGameClient ConnectOnce(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
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
						login.SendLogin(account, account);
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HealerCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HealerPair";
	}
}
