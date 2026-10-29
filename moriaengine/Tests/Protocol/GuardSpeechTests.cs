using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#826 - Erebor's spk_guard: a player who says "guard" near a real converted guard sends it after
	/// whoever that player fought with, unless the guard protects that one (Gondor guards: innocent Gondor players,
	/// Mordor guards: every Mordor player). Two real clients, real speech, real hostile acts (Notoriety makes the
	/// attacker criminal). Server-side helpers: scripts/Tests/GuardSpeechTests.cs.</summary>
	[Collection(GuardSpeechCollection.Name)]
	public class GuardSpeechTests {
		private const string AccountA = "guardcalla";
		private const string AccountB = "guardcallb";
		private const string AdminAccount = "guardcalladmin"; //the server's first account - can turn on auto-creation
		private const string GondorGuard = "c_vguard_rohan_hala_jezdec";
		private const string MordorGuard = "c_vguard_mordor_hala_jezdec";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public GuardSpeechTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void GuardsGoAfterTheCallersOpponentUnlessTheyProtectThem() {
			using (var a = this.Connect(AccountA, "Guard Caller"))
			using (var b = this.Connect(AccountB, "Guard Suspect")) {
				uint bUid = this.MyUid(b);

				//a Gondor player attacked me: criminal, so not protected by Gondor guards
				AssertHas(this.Setup(a, bUid, GondorGuard, "Gondor Gondor other"), "realm=Gondor otherCriminal=True");
				Assert.Equal("Guard Suspect", this.CallGuards(a));

				//I attacked an innocent Gondor player: the guard protects them
				AssertHas(this.Setup(a, bUid, GondorGuard, "Gondor Gondor me"), "otherCriminal=False meCriminal=True");
				Assert.Equal("none", this.CallGuards(a));

				//a criminal I never fought: nobody to send the guard after
				AssertHas(this.Setup(a, bUid, GondorGuard, "Gondor Gondor none"), "otherCriminal=True");
				Assert.Equal("none", this.CallGuards(a));

				//Mordor guards protect every Mordor player, criminal or not...
				AssertHas(this.Setup(a, bUid, MordorGuard, "Mordor Mordor other"), "realm=Mordor otherCriminal=True");
				Assert.Equal("none", this.CallGuards(a));

				//...but nobody else
				this.Setup(a, bUid, MordorGuard, "Mordor None other");
				Assert.Equal("Guard Suspect", this.CallGuards(a));
			}
		}

		private string Setup(TestGameClient client, uint other, string guardDef, string realmsAndFight) {
			string[] parts = realmsAndFight.Split(' ');
			string line = this.Ask(client, ".TestGuardCallSetup \"" + other + " " + guardDef + " " + parts[0] + " " + parts[1] + " " + parts[2] + "\"", "GUARD_CALL_SETUP:");
			AssertHas(line, "guardSeesMe=True");
			return line;
		}

		private string CallGuards(TestGameClient client) {
			client.SendSpeech("guard!");
			Thread.Sleep(700);
			string line = this.Ask(client, ".TestGuardCallReport", "GUARD_CALL_TARGET:");
			return line.Substring("GUARD_CALL_TARGET: ".Length).Trim();
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

		private uint MyUid(TestGameClient client) {
			string line = this.Ask(client, ".TestReportMyUid", "MY_UID:");
			string rest = line.Substring("MY_UID:".Length).TrimStart(' ', '#');
			int end = 0;
			while ((end < rest.Length) && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
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
	public class GuardSpeechCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GuardSpeechPair";
	}
}
