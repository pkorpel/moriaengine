using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#828 - Erebor's skill teachers (spk_mistr): the real converted anatomy teacher sells the jump from exactly
	/// 25.0/50.0/75.0 to 30.0/55.0/80.0 for gold, when the student's level (and, with the requireRank switch, rank)
	/// allows it, and says why not otherwise. A mage (Anatomy 0-75) and a priest (30-100) student. Real client speech. Server-side helpers: scripts/Tests/SkillTeacherTests.cs.</summary>
	[Collection(SkillTeacherCollection.Name)]
	public class SkillTeacherTests {
		private const string Account = "teacherteststudent";
		private const string AdminAccount = "teachertestadmin"; //the server's first account - can turn on auto-creation
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan Short = TimeSpan.FromSeconds(3);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public SkillTeacherTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void TeacherSellsTheNextRankAtExactlyTheThreshold() {
			using (var client = this.Connect(Account, "Skill Student")) {
				//Expert lesson: 25.0 -> 30.0 for 5000, no rank needed with the switch off
				this.Setup(client, "profession_mage 250 3 Novice 0 6000");
				client.SendSpeech("dobry den");
				Assert.NotNull(TryReadMessageContaining(client, "stati se EXPERTEM Anatomy", Wait));
				client.SendSpeech("ano");
				Assert.NotNull(TryReadMessageContaining(client, "Byl jsi vytrenovan EXPERTEM Anatomy", Wait));
				Assert.Contains("anatomy=300 gold=1000", this.Report(client));

				//the rank switch on: a Novice isn't ready
				this.Setup(client, "profession_mage 250 3 Novice 1 6000");
				client.SendSpeech("hello");
				Assert.NotNull(TryReadMessageContaining(client, "not ready for the next lesson", Wait));
				client.SendSpeech("ano");
				Assert.Contains("anatomy=250 gold=6000", this.Report(client));

				//greeting rounds the skill to 5.0 (Erebor's roundskill) - 30.0 is past the Expert lesson, short of the Master one
				this.Setup(client, "profession_mage 276 3 Novice 0 6000");
				client.SendSpeech("zdravim mistre");
				Assert.NotNull(TryReadMessageContaining(client, "train more before the next lesson", Wait));
				Assert.Contains("anatomy=300", this.Report(client));

				//too little gold for the Master lesson
				this.Setup(client, "profession_mage 500 6 Novice 0 100");
				client.SendSpeech("ano");
				Assert.NotNull(TryReadMessageContaining(client, "Nemas patricnou sumu", Wait));
				Assert.Contains("anatomy=500 gold=100", this.Report(client));

				//Grandmaster lesson with the rank switch on and the rank: 75.0 -> 80.0 for 20000
				this.Setup(client, "profession_priest 750 12 Grandmaster 1 25000");
				client.SendSpeech("yes");
				Assert.NotNull(TryReadMessageContaining(client, "Byl jsi vytrenovan VELMISTREM Anatomy", Wait));
				Assert.Contains("anatomy=800 gold=5000", this.Report(client));

				//a mage's Anatomy stops at 75.0: no Grandmaster lesson
				this.Setup(client, "profession_mage 750 12 Grandmaster 1 25000");
				client.SendSpeech("hello");
				Assert.NotNull(TryReadMessageContaining(client, "reached your maximum in Anatomy", Wait));

				//level too low for the Expert lesson
				this.Setup(client, "profession_mage 250 2 Novice 0 6000");
				client.SendSpeech("hi");
				Assert.NotNull(TryReadMessageContaining(client, "level is too low", Wait));
				Assert.Contains("anatomy=250 gold=6000", this.Report(client));
			}
		}

		private void Setup(TestGameClient client, string args) {
			string line = this.Ask(client, ".TestTeacherSetup \"" + args + "\"", "TEACHER_SETUP:");
			Assert.Contains("seesMe=True teaches=Anatomy", line);
		}

		private string Report(TestGameClient client) {
			Thread.Sleep(300);
			return this.Ask(client, ".TestTeacherReport", "TEACHER_REPORT:");
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
	public class SkillTeacherCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SkillTeacherPair";
	}
}
