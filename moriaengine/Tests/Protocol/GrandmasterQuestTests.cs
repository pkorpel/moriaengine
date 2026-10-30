using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#833 - Erebor's Grandmaster quest end to end, stage by stage with real gumps and clicks: a GM's
	/// .SetupGrandmasterQuest places everything, walkably; a Master of level 12 takes it from Elqira to the Gondor envoy.
	/// Wrong choices, an upset Elqira, guards, a closed cave and Rinald's unspoken name are covered on the way.
	/// Server-side helpers: scripts/Tests/GrandmasterQuestTests.cs.</summary>
	[Collection(GrandmasterQuestCollection.Name)]
	public class GrandmasterQuestTests {
		private const string Account = "grandmasterquestplayer";
		private const string AdminAccount = "grandmasterquestadmin"; //the server's first account: a GM, can turn on auto-creation
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public GrandmasterQuestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MasterGoesThroughTheWholeStoryAndBecomesAGrandmaster() {
			using (var admin = this.ConnectOnce(AdminAccount, "Quest Admin")) {
				this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST:");
				autoAccountCreationEnabled = true;
				this.Ask(admin, ".SetupGrandmasterQuest", "SETUP_DONE:");
				Assert.Contains("added=2 kept=0", this.Ask(admin, ".SetupDungeonEntrances", "SETUP_DONE:"));
				Thread.Sleep(3000); //the gems' first spawns
				string world = this.Ask(admin, ".TestQ000World", "Q000_WORLD:");
				Assert.True(world.Contains("elvira=True elmira=True entrance=True rinald=True exit=True root=True rootGuarded=True rinaldGuarded=True entranceInvul=True envoy=True", StringComparison.Ordinal), world);
				Assert.Contains("gateFromSurface=True", world);
				Assert.True(int.Parse(Field(world, "rootFromGate="), CultureInfo.InvariantCulture) > 50, "the root should be deep in the cave: " + world);
				int apart = int.Parse(Field(world, "sistersApart="), CultureInfo.InvariantCulture);
				Assert.True((apart > 40) && (apart < 150), "the forest should be a little way from the cave: " + world);
			}
			using (var client = this.Connect(Account, "Quest Hero")) {
				client.SendSpeech(".SetupGrandmasterQuest");
				Assert.NotNull(TryReadMessageContaining(client, "No permission", Wait));
				this.Ask(client, ".TestQ000Prepare \"0 2 12 Gondor\"", "Q000_PREPARED:");

				//Elqira: a wrong choice upsets her, the right one starts the quest
				uint elqira = this.GoTo(client, "elqira");
				client.SendSpeech("dobry den");
				this.Press(client, "elqira q000_wrongChoice");
				Assert.NotNull(TryReadMessageContaining(client, "Nechci se ted s tebou bavit", Wait));
				Assert.Contains("stage=0", this.State(client));
				Assert.Contains("upset=True", this.State(client));
				this.Ask(client, ".TestQ000Calm", "Q000_CALM:");
				client.SendDoubleClick(elqira);
				var assignment = client.ReadGumpSkippingQuestScroll(Wait);
				string texts = assignment.LayoutText + string.Join("|", assignment.Strings);
				Assert.True(texts.Contains("Mam dve dcery", StringComparison.Ordinal) && texts.Contains("Vydam se na Ledove plane.", StringComparison.Ordinal), texts); //the 12th of 12 places, on a further page of responses
				this.Answer(client, assignment.GumpUid, "elqira q000_questAssignment");
				Assert.Contains("stage=1 ", this.State(client));

				//Elvira first, spellbound
				client.SendDoubleClick(this.GoTo(client, "elvira"));
				Assert.NotNull(TryReadMessageContaining(client, "tezce nemocna", Wait));
				Assert.Contains("stage=2 ", this.State(client));

				//the other order: Elmira first, then Elvira
				this.Ask(client, ".TestQ000Prepare \"1 2 12 Gondor\"", "Q000_PREPARED:");
				uint elmira = this.GoTo(client, "elmira");
				client.SendSpeech("zdravim");
				this.Press(client, "elmira q000_goFindElvira");
				Assert.Contains("stage=3 ", this.State(client));
				client.SendDoubleClick(this.GoTo(client, "elvira"));
				Assert.NotNull(TryReadMessageContaining(client, "tezce nemocna", Wait));
				Assert.Contains("stage=4 ", this.State(client));

				//Elmira sends the player for the root; the cave is still closed to them
				this.GoTo(client, "elmira");
				client.SendSpeech("zdravim");
				this.Press(client, "elmira q000_goForHerb");
				Assert.NotNull(TryReadMessageContaining(client, "Pospes si", Wait));
				Assert.Contains("stage=5 ", this.State(client));
				this.Ask(client, ".TestQ000StepOn \"entrance\"", "Q000_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "tam nejdes", Wait));
				Assert.Contains("region=a_Severni_Spoust", this.State(client));

				//the dark cave's gate works both ways, on foot
				Assert.Contains("m=2 region=a_Ered_Mithrinska_Jeskyne", this.Ask(client, ".TestQ000WalkOnto \"gateIn\"", "Q000_WALKED:"));
				Assert.Contains("m=0 region=a_Ered_Mithrin_vstup", this.Ask(client, ".TestQ000WalkOnto \"gateOut\"", "Q000_WALKED:"));

				//the root is guarded until the guards are dead
				uint root = this.GoTo(client, "root");
				client.SendDoubleClick(root);
				Assert.NotNull(TryReadMessageContaining(client, "stale nekdo hlida", Wait));
				this.Ask(client, ".TestQ000ClearGuards \"root\"", "Q000_CLEARED:");
				client.SendDoubleClick(root);
				Assert.NotNull(TryReadMessageContaining(client, "vytrhnout koren Este", Wait));
				Assert.Contains("stage=6 ", this.State(client));

				//Elvira takes it by hand, not by talking
				uint elvira = this.GoTo(client, "elvira");
				client.SendSpeech("dobry den");
				Assert.Contains("stage=6 ", this.State(client));
				client.SendDoubleClick(elvira);
				Assert.NotNull(TryReadMessageContaining(client, "vypada to, ze zabira", Wait));
				Assert.Contains("stage=7 ", this.State(client));
				client.SendDoubleClick(elvira);
				this.Press(client, "elvira q000_goKillRinald");
				Assert.Contains("stage=8 ", this.State(client));

				//Rinald's cave: no way out while he lives, a wrong name throws the player out
				this.EnterCave(client);
				this.Ask(client, ".TestQ000StepOn \"exit\"", "Q000_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "neotocis", Wait));
				uint rinald = this.GoTo(client, "rinald");
				client.SendDoubleClick(rinald);
				Assert.NotNull(TryReadMessageContaining(client, "bestie kolem", Wait));
				this.Ask(client, ".TestQ000ClearGuards \"rinald\"", "Q000_CLEARED:");
				client.SendDoubleClick(rinald);
				this.Press(client, "rinald q000_wrongName");
				Assert.NotNull(TryReadMessageContaining(client, "te vyhnal z jeskyne", Wait));
				Assert.Contains("stage=8 ", this.State(client));
				Assert.Contains("region=a_Severni_Spoust", this.State(client));

				//his name: he turns, and can be killed
				this.EnterCave(client);
				client.SendDoubleClick(this.GoTo(client, "rinald"));
				Assert.Contains("rinaldInvulnerable=True", this.State(client));
				this.Press(client, "rinald q000_tellHisName");
				Assert.NotNull(TryReadMessageContaining(client, "bidnou smrti", Wait));
				Thread.Sleep(3500);
				Assert.Contains("rinald=Rinald rinaldInvulnerable=False", this.State(client));
				this.Ask(client, ".TestQ000KillRinald", "Q000_RINALD_DEAD:");
				this.Ask(client, ".TestQ000StepOn \"exit\"", "Q000_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "jeste sebral klic", Wait));
				string state = this.State(client);
				Assert.Contains("stage=9 ", state);
				Assert.Contains("region=a_Severni_Spoust", state);
				this.Ask(client, ".TestQ000StepOn \"entrance\"", "Q000_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "Tady uz jsi", Wait));

				//the cage, Elqira's reward, the envoy
				client.SendDoubleClick(this.GoTo(client, "door"));
				Assert.NotNull(TryReadMessageContaining(client, "vrata klece", Wait));
				Assert.Contains("stage=10 ", this.State(client));
				client.SendDoubleClick(this.GoTo(client, "elqira"));
				this.Press(client, "elqira q000_questSolved");
				state = this.State(client);
				Assert.Contains("stage=11 ", state);
				Assert.Contains("gold=2500 bag=True", state);

				this.GoTo(client, "envoy");
				client.SendSpeech("zdravim");
				var envoy = client.ReadGumpSkippingQuestScroll(Wait);
				Assert.Contains(envoy.Strings, s => s.Contains("Nejsi ty ten, kdo pomohl Elqire", StringComparison.Ordinal));
				this.Answer(client, envoy.GumpUid, "envoy q000_getVM");
				Assert.Contains("stage=13 rank=Grandmaster", this.State(client));

				//an envoy deals with his own realm only
				this.Ask(client, ".TestQ000Prepare \"11 2 12 Mordor\"", "Q000_PREPARED:");
				client.SendSpeech("zdravim");
				Assert.NotNull(TryReadMessageContaining(client, "prisluhovaci Mordoru", Wait));

				//the next hero's Rinald keeps the way out shut too, though he isn't the one the setup placed
				this.Ask(client, ".TestQ000Prepare \"8 2 12 Gondor\"", "Q000_PREPARED:");
				this.EnterCave(client);
				this.Ask(client, ".TestQ000StepOn \"exit\"", "Q000_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "neotocis", Wait));
			}
		}

		private void EnterCave(TestGameClient client) {
			this.Ask(client, ".TestQ000StepOn \"entrance\"", "Q000_STEPPED:");
			Assert.NotNull(TryReadMessageContaining(client, "Musis spechat", Wait));
			Assert.NotNull(TryReadMessageContaining(client, "Zbyva 60 minut", Wait));
			Assert.Contains("region=a_Slunecni_jeskyne", this.State(client));
		}

		private uint GoTo(TestGameClient client, string target) {
			string line = this.Ask(client, ".TestQ000GoTo \"" + target + "\"", "Q000_AT:");
			Assert.Contains("los=True", line);
			return uint.Parse(Field(line, "uid="), CultureInfo.InvariantCulture);
		}

		//answers the gump that's coming with the named exit button
		private void Press(TestGameClient client, string dialogAndButton) {
			this.Answer(client, client.ReadGumpSkippingQuestScroll(Wait).GumpUid, dialogAndButton);
		}

		private void Answer(TestGameClient client, int gumpUid, string dialogAndButton) {
			string line = this.Ask(client, ".TestQ000Button \"" + dialogAndButton + "\"", "Q000_BUTTON:");
			client.SendGumpResponse(gumpUid, int.Parse(Field(line, "id="), CultureInfo.InvariantCulture));
		}

		private string State(TestGameClient client) {
			Thread.Sleep(500);
			return this.Ask(client, ".TestQ000State", "Q000_STATE:");
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
			using (var client = this.ConnectOnce(AdminAccount, "Quest Admin")) {
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
	public class GrandmasterQuestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GrandmasterQuestPair";
	}
}
