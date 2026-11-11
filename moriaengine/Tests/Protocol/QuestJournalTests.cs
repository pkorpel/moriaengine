using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#839 - the quest journal: a plain player's .questy, the placeholder sections, the quest list with its colors,
	/// a quest's page and the scroll on a new stage. The quests' own steps are in the three rank quests' tests.
	/// Server-side helpers: scripts/Tests/QuestJournalTests.cs.</summary>
	[Collection(QuestJournalCollection.Name)]
	public class QuestJournalTests {
		private const string Account = "questjournalplayer";
		private const string AdminAccount = "questjournaladmin"; //the server's first account: a GM, can turn on auto-creation
		private const int ExpertButton = 1000, GrandmasterButton = 1002;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public QuestJournalTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void PlayersFollowTheirQuestsInTheJournal() {
			using (var client = this.Connect(Account, "Quest Reader")) {
				client.SendSpeech(".questy");
				var menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(menu.Strings, s => s.Contains("You have no quests yet", StringComparison.Ordinal));

				//the section that isn't ported says so; the PvP league's (#1116) shows its season
				AssertWindowIsSolid(menu.LayoutText);
				client.SendGumpResponse(menu.GumpUid, 2);
				menu = client.ReadGumpDialogWithStrings(Wait);
				AssertWindowIsSolid(menu.LayoutText);
				Assert.Contains("Otevrene Dungeony", menu.Strings);
				Assert.Contains(menu.Strings, s => s.Contains("isn't available yet", StringComparison.Ordinal));
				client.SendGumpResponse(menu.GumpUid, 21);
				menu = client.ReadGumpDialogWithStrings(Wait);
				AssertWindowIsSolid(menu.LayoutText);
				Assert.Contains(menu.Strings, s => s.StartsWith("PvP Sezona ", StringComparison.Ordinal));
				Assert.Contains("Konec Sezony", menu.Strings);
				//#1090 - the weekly section: below level 10, when the tasks open
				client.SendGumpResponse(menu.GumpUid, 11);
				menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Tydenni Questy", menu.Strings);
				Assert.Contains(menu.Strings, s => s.Contains("Plneni se otevre na 10 urovni.", StringComparison.Ordinal));
				client.SendGumpResponse(menu.GumpUid, 1);
				menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Questy", menu.Strings);

				//#1090 - the boosters: the stored ones, one started by its button, none other while it runs
				client.SendGumpResponse(menu.GumpUid, 3);
				menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Zadny Booster neni aktivni.", menu.Strings);
				client.SendGumpResponse(menu.GumpUid, 0);
				this.Ask(client, ".TestBoostersGive", "BOOSTERS_GIVEN:");
				client.SendSpeech(".questy");
				menu = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(menu.GumpUid, 3);
				menu = client.ReadGumpDialogWithStrings(Wait);
				string boosters = string.Join("|", menu.Strings);
				Assert.Contains("+25% zkusenosti na 1h", boosters);
				Assert.Contains("+50% zkusenosti na 1h", boosters);
				Assert.Contains("2x", boosters);
				client.SendGumpResponse(menu.GumpUid, 500);
				menu = client.ReadGumpDialogWithStrings(Wait);
				boosters = string.Join("|", menu.Strings);
				Assert.Contains("Aktivni booster", boosters);
				Assert.DoesNotContain("Zadny Booster neni aktivni.", boosters);
				Assert.Contains("kind=0 due=True bonus=True noSecond=True left=0x1,3x1 paused=True resumed=True",
					this.Ask(client, ".TestBoostersReport", "BOOSTERS_REPORT:"));
				client.SendGumpResponse(menu.GumpUid, 0);

				//a finished quest in green, the active one in blue and first
				this.Ask(client, ".TestJournalSet \"1 5\"", "JOURNAL_SET:");
				client.SendSpeech(".questy");
				menu = client.ReadGumpDialogWithStrings(Wait);
				string texts = string.Join("|", menu.Strings);
				int active = texts.IndexOf("#00F0FA>Ztracene Dcery", StringComparison.Ordinal);
				int done = texts.IndexOf("#00FA55>Povyseni na Experta", StringComparison.Ordinal);
				Assert.True((active >= 0) && (done > active), texts);
				Assert.DoesNotContain("Povyseni na Mistra", texts);

				//a quest's page: this stage's text, its markup expanded
				client.SendGumpResponse(menu.GumpUid, GrandmasterButton);
				var page = client.ReadGumpDialogWithStrings(Wait);
				string pageText = string.Join("|", page.Strings);
				Assert.Contains("jsi Elmire, jak je na tom jeji sestra", pageText);
				Assert.DoesNotContain("src.sex", pageText);
				client.SendGumpResponse(page.GumpUid, 0);
				menu = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(menu.GumpUid, ExpertButton);
				page = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("ses expertem", string.Join("|", page.Strings));
				client.SendGumpResponse(page.GumpUid, 0);
				client.SendGumpResponse(client.ReadGumpDialogWithStrings(Wait).GumpUid, 0);

				//the scroll on a new stage opens the page of that stage; a newer one replaces it
				this.Ask(client, ".TestJournalAdvance \"6\"", "JOURNAL_ADVANCED:");
				var scroll = client.ReadGumpDialog(Wait);
				Assert.Contains(TestGameClient.QuestScrollLayout, scroll.LayoutText, StringComparison.OrdinalIgnoreCase);
				Assert.Contains("nomove", scroll.LayoutText, StringComparison.OrdinalIgnoreCase);
				this.Ask(client, ".TestJournalAdvance \"7\"", "JOURNAL_ADVANCED:");
				scroll = client.ReadGumpDialog(Wait);
				client.SendGumpResponse(scroll.GumpUid, 1);
				page = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("jsi Elvire koren Este", string.Join("|", page.Strings));
				client.SendGumpResponse(page.GumpUid, 0);

				//#1245 - the living world's side: its four tabs, the city page with my standing, its factions and a friend
				string city = this.Ask(client, ".TestJournalCity", "JOURNAL_CITY:");
				Assert.Contains("here=Edoras", city);
				string friend = Between(city, "friend=", " place=");
				string place = Between(city, " place=", " stranger=");
				string stranger = Between(city, " stranger=", " here=");
				client.SendSpeech(".questy");
				menu = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(menu.GumpUid, 31);
				menu = client.ReadGumpDialogWithStrings(Wait);
				AssertWindowIsSolid(menu.LayoutText);
				texts = string.Join("|", menu.Strings);
				Assert.Contains("Ukoly", menu.Strings);
				Assert.Contains("Ukoly ti daji lide ve meste.", texts);
				client.SendGumpResponse(menu.GumpUid, 32);
				menu = client.ReadGumpDialogWithStrings(Wait);
				AssertWindowIsSolid(menu.LayoutText);
				texts = string.Join("|", menu.Strings);
				int edoras = menu.Strings.IndexOf("Edoras");
				Assert.True(edoras >= 0, texts);
				Assert.Equal("Vazeny", menu.Strings[edoras + 1]);
				Assert.True(edoras < menu.Strings.IndexOf("Minas Tirith"), texts); //where I stand comes first, before .povest's order
				Assert.Contains("Povest 250", menu.Strings);
				Assert.Contains("Kraluv dvur", menu.Strings);
				Assert.Contains(friend, menu.Strings);
				Assert.Contains(place, menu.Strings); //where they work
				Assert.Contains("7/10", menu.Strings);
				if ((stranger.Length > 0) && (stranger != friend)) {
					Assert.DoesNotContain(stranger, menu.Strings); //only friends are listed
				}
				client.SendGumpResponse(menu.GumpUid, 33);
				menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Ve svete se ted nic zvlastniho nedeje.", string.Join("|", menu.Strings));
				client.SendGumpResponse(menu.GumpUid, 34);
				menu = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vitany host", menu.Strings); //#1296 - the deeds: Vazeny in Edoras is a friend of one city, its first title
				//the city page's button: .povest, and the journal stays open on the city
				client.SendGumpResponse(menu.GumpUid, 32);
				menu = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(menu.GumpUid, 5000);
				ReadUntil(client, "Povest ve mestech");
			}
		}

		private static string Between(string text, string start, string end) {
			int from = text.IndexOf(start, StringComparison.Ordinal) + start.Length;
			return text.Substring(from, text.IndexOf(end, from, StringComparison.Ordinal) - from);
		}

		//a checkertrans turns everything it overlaps see-through in the client: only the side buttons, left of the window, may use one
		private static void AssertWindowIsSolid(string layout) {
			foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(layout, "checkertrans (\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
				Assert.Equal("10", m.Groups[1].Value);
			}
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

		/// <summary>#1090 - Erebor's weekly quests: the sets, every trigger and the rewards server side, then the journal's page, a
		/// task's detail and the popup of a task done in the client. Server side: scripts/Tests/WeeklyQuestTests.cs.</summary>
		[Fact]
		public void WeeklyQuestsFollowErebor() {
			using (var client = this.Connect(Account, "Weekly Runner")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				this.Ask(client, ".TestWeeklySets", "WEEKLY_SETS_DONE");
				this.Ask(client, ".TestWeeklyTriggers", "WEEKLY_TRIGGERS_DONE");
				this.Ask(client, ".TestWeeklyRoutes", "WEEKLY_ROUTES_DONE"); //#1426
				string output = this.fixture.GameOutputSnapshot().Substring(mark);
				foreach (string check in new[] { "underTenNothing", "countsByLevel", "blockedNeverDrawn", "noTaskTwice", "slotsByPool", "ereborsWorkedExample", "noSetForACraftsman",
						"aWeekLong", "expiredIsRenewed", "eatingCountsWhileHungry", "theTeacherGreetsBack", "aWebCountsOnce", "aMonstersGoldCountsSpawnedOrNot", "whatCameLaterDoesnt",
						"damageDealtCounts", "onlyASpawnedFoesBlowCounts", "aStreakOfTwoCounts", "aTaskAndTheSetPayIntoTheBank",
						"theSetIsThePlugins", "aDoubleClickReachesIt", "aStepReachesIt", "aPickUpReachesIt", "aKillCreditReachesIt", "aLoginRenewsAnExpiredSet" }) {
					string marker = "WEEKLY: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}

				string fresh = this.Ask(client, ".TestWeeklyFreshSet", "WEEKLY_FRESH:");
				int at = fresh.IndexOf("first=", StringComparison.Ordinal) + "first=".Length;
				string first = fresh.Substring(at, fresh.IndexOf(" |", at, StringComparison.Ordinal) - at);
				string firstLine = first.Contains(':') ? first.Substring(0, first.IndexOf(':')) : first.Split(' ')[0];
				//the triggers above finished tasks, each with its popup still queued for the client
				client.SendSpeech(".questy");
				var menu = ReadUntil(client, "Questy");
				client.SendGumpResponse(menu.GumpUid, 11);
				menu = ReadUntil(client, "Odmena za splneni vsech Questu");
				string page = string.Join("|", menu.Strings);
				Assert.Contains("Odmena za splneni vsech Questu", page);
				Assert.Contains(firstLine, page);
				Assert.Contains("Zbyva: 6 dni", page);
				client.SendGumpResponse(menu.GumpUid, 100);
				var detail = ReadUntil(client, "zlatych");
				Assert.Contains(firstLine, string.Join("|", detail.Strings));

				this.Ask(client, ".TestWeeklyFinishFirst", "WEEKLY_FINISHED: True");
				var done = ReadUntil(client, "Splnen Weekly Quest");
				client.SendGumpResponse(done.GumpUid, 1);
				ReadUntil(client, "S P L N E N O");
			}
		}

		private static (int GumpUid, string LayoutText, System.Collections.Generic.List<string> Strings) ReadUntil(TestGameClient client, string text) {
			string seen = "";
			for (int i = 0; i < 20; i++) {
				var gump = client.ReadGumpDialogWithStrings(Wait);
				string all = string.Join("|", gump.Strings);
				if (all.Contains(text, StringComparison.Ordinal)) {
					return gump;
				}
				seen += all.Substring(0, Math.Min(60, all.Length)) + " ## ";
			}
			throw new Xunit.Sdk.XunitException("No gump with '" + text + "', saw: " + seen);
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
			using (var client = this.ConnectOnce(AdminAccount, "Journal Admin")) {
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
	public class QuestJournalCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "QuestJournalPair";
	}
}
