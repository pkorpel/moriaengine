using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#833 - Erebor's Expert quest end to end: a GM's .SetupExpertQuest places the start stones and the quest
	/// sites with their clue signs in the a_expert cave; a level-3 Novice double-clicks the Gondor stone, lands in the
	/// cave, reads a sign, greets the quest master and answers its three questions through the real gump - and is an
	/// Expert back in Gondor. A wrong answer ends the quest instead. Server-side helpers: scripts/Tests/ExpertQuestTests.cs.</summary>
	[Collection(ExpertQuestCollection.Name)]
	public class ExpertQuestTests {
		private const string Account = "expertquestplayer";
		private const string AdminAccount = "expertquestadmin"; //the server's first account: a GM, can turn on auto-creation
		private static readonly string[] Names = { "Burin", "Tilar", "Armel", "Koren", "Sarod" };
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public ExpertQuestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NoviceBecomesAnExpertByAnsweringTheMastersQuestions() {
			using (var admin = this.ConnectOnce(AdminAccount, "Quest Admin")) {
				this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST:");
				autoAccountCreationEnabled = true;
				Assert.Contains("added=16 kept=0", this.Ask(admin, ".SetupExpertQuest", "SETUP_DONE:"));

				//Erebor's GM linking: double-click a new site and target the start stone, a new sign and target the site
				string pieces = this.Ask(admin, ".TestExpertQuestNewPieces", "EXPERT_PIECES:");
				Assert.Contains("invisibleBefore=False", pieces);
				admin.SendDoubleClick(Uid(pieces, "site="));
				Assert.NotNull(TryReadMessageContaining(admin, "Target the Expert quest start stone", Wait));
				admin.SendTargetUid(Uid(pieces, "stone="));
				Assert.NotNull(TryReadMessageContaining(admin, "OK", Wait));
				admin.SendDoubleClick(Uid(pieces, "sign="));
				Assert.NotNull(TryReadMessageContaining(admin, "Target the Expert quest site", Wait));
				admin.SendTargetUid(Uid(pieces, "site="));
				Assert.NotNull(TryReadMessageContaining(admin, "OK", Wait));
				string linked = this.Ask(admin, ".TestExpertQuestLinked", "EXPERT_LINKED:");
				Assert.Contains("siteLinked=True signLinked=True siteInvisible=True signName=Cedule_s_napisem", linked);
			}
			using (var client = this.Connect(Account, "Quest Novice")) {
				client.SendSpeech(".SetupExpertQuest");
				Assert.NotNull(TryReadMessageContaining(client, "No permission", Wait));

				uint stone = this.Prepare(client);
				client.SendDoubleClick(stone);
				string state = this.State(client);
				Assert.Contains("region=a_expert rank=Novice onQuest=True signs=6", state);
				Assert.Contains("signName=Cedule_s_napisem siteInvisible=True", state);
				Assert.Contains("masters=1 masterInvulnerable=True", state);
				string[] answers = Field(state, "answers=").Split(',');

				//a clue sign: its name in Erebor's spaced capitals on top
				client.SendDoubleClick(Uid(state, "sign="));
				var sign = client.ReadGumpSkippingQuestScroll(Wait);
				string spaced = string.Join(" ", Field(state, "clueName=").ToUpperInvariant().ToCharArray());
				Assert.True(sign.LayoutText.Contains(spaced, StringComparison.Ordinal) || sign.Strings.Any(s => s.Contains(spaced, StringComparison.Ordinal)),
					"sign gump without '" + spaced + "': " + sign.LayoutText);

				//the journal knows, and the master's text must not be black on its dark gump (no scrollbar = black in the client)
				client.SendSpeech(".questy");
				var journal = client.ReadGumpSkippingQuestScroll(Wait);
				client.SendGumpResponse(journal.GumpUid, 1000);
				var journalPage = client.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(journalPage.Strings, s => s.Contains("Jsi v jeskyni zkousky", StringComparison.Ordinal));
				client.SendGumpResponse(client.ReadGumpSkippingQuestScroll(Wait).GumpUid, 0);

				client.SendSpeech("ahoj");
				for (int question = 0; question < 3; question++) {
					var gump = client.ReadGumpSkippingQuestScroll(Wait);
					Assert.Matches("htmlgump 170 230 440 100 \\d+ 0 1", gump.LayoutText);
					client.SendGumpResponse(gump.GumpUid, Array.IndexOf(Names, answers[question]) + 1);
				}
				//the celebration, Manwe's blessing and the 16 sparkles around the new Expert, where they ended up
				for (int effect = 0; effect < 17; effect++) {
					Assert.NotNull(client.ReadPacketPayload(0xC0, Wait));
				}
				state = this.State(client);
				Assert.Contains("rank=Expert", state);
				Assert.DoesNotContain("region=a_expert", state);
				Assert.Contains("busySites=0 masters=0", state);

				this.Ask(client, ".TestExpertQuestGoToStone", "EXPERT_AT_STONE:");
				client.SendDoubleClick(stone);
				Assert.NotNull(TryReadMessageContaining(client, "already walked this path", Wait));

				//again as a Novice, with a wrong first answer
				stone = this.Prepare(client);
				client.SendDoubleClick(stone);
				state = this.State(client);
				Assert.Contains("region=a_expert rank=Novice onQuest=True", state);
				string right = Field(state, "answers=").Split(',')[0];
				client.SendDoubleClick(Uid(state, "master=")); //the same as greeting it
				var first = client.ReadGumpSkippingQuestScroll(Wait);
				client.SendGumpResponse(first.GumpUid, Array.IndexOf(Names, Names.First(n => n != right)) + 1);
				state = this.State(client);
				Assert.Contains("rank=Novice", state);
				Assert.DoesNotContain("region=a_expert", state);
				Assert.Contains("busySites=0 masters=0", state);

				//the countdown, on the real timer: each minute inside the cave tells the time left, leaving the cave ends the quest
				stone = this.Prepare(client);
				client.SendDoubleClick(stone);
				Assert.Contains("onQuest=True", this.State(client));
				this.Ask(client, ".TestExpertQuestTick \"stay\"", "EXPERT_TICK:");
				Assert.NotNull(TryReadMessageContaining(client, "59 more minutes", Wait));
				this.Ask(client, ".TestExpertQuestTick \"stay\"", "EXPERT_TICK:"); //only there if the first tick armed the next one
				Assert.NotNull(TryReadMessageContaining(client, "58 more minutes", Wait));
				this.Ask(client, ".TestExpertQuestTick \"leave\"", "EXPERT_TICK:");
				state = this.State(client);
				Assert.Contains("busySites=0 masters=0", state);
				Assert.Contains("region=a_Anorien", state); //left where they walked out to, not sent home

				//running the setup again in the middle of a quest changes nothing
				stone = this.Prepare(client);
				client.SendDoubleClick(stone);
				Assert.Contains("masters=1", this.State(client));
				Assert.Contains("added=0 kept=16", this.Ask(client, ".TestExpertQuestResetup", "SETUP_DONE:"));
				state = this.State(client);
				Assert.Contains("region=a_expert rank=Novice onQuest=True", state);
				Assert.Contains("busySites=1 masters=1", state);
			}
		}

		private uint Prepare(TestGameClient client) {
			string line = this.Ask(client, ".TestExpertQuestPrepare", "EXPERT_PREPARE:");
			Assert.Contains("sites=2", line);
			return Uid(line, "stone=");
		}

		private static uint Uid(string line, string key) {
			return uint.Parse(Field(line, key), System.Globalization.CultureInfo.InvariantCulture);
		}

		private string State(TestGameClient client) {
			Thread.Sleep(500);
			return this.Ask(client, ".TestExpertQuestState", "EXPERT_STATE:");
		}

		private static string Field(string line, string key) {
			int idx = line.IndexOf(key, StringComparison.Ordinal);
			Assert.True(idx >= 0, "no '" + key + "' in: " + line);
			int start = idx + key.Length;
			int end = line.IndexOf(' ', start);
			return (end < 0) ? line.Substring(start).Trim() : line.Substring(start, end - start);
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
			Exception firstFailure = null; //later attempts only say the account is in use, by the first one
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", firstFailure);
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
	public class ExpertQuestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ExpertQuestPair";
	}
}
