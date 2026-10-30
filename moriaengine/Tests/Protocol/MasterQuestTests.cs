using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#833 - Erebor's Master quest end to end: a GM's .SetupMasterQuest places Lenastir, the teleport and the snake
	/// queen's spawn; an Expert of level 6 gets the task and the key from Lenastir, goes through the teleport to the Snake
	/// Islands, lifts the egg from the real queen's corpse and is a Master after bringing it back. Being too low, a plain
	/// egg, someone else's egg and a spent key are covered too. Server-side helpers: scripts/Tests/MasterQuestTests.cs.</summary>
	[Collection(MasterQuestCollection.Name)]
	public class MasterQuestTests {
		private const string Account = "masterquestplayer";
		private const string AdminAccount = "masterquestadmin"; //the server's first account: a GM, can turn on auto-creation
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public MasterQuestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ExpertBringsTheQueensEggAndBecomesAMaster() {
			using (var admin = this.ConnectOnce(AdminAccount, "Quest Admin")) {
				this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST:");
				autoAccountCreationEnabled = true;
				Assert.Contains("added=5 kept=0", this.Ask(admin, ".SetupMasterQuest", "SETUP_DONE:"));
				Thread.Sleep(2500); //the gems' first spawns
				string world = this.Ask(admin, ".TestMasterQuestWorld", "MASTER_WORLD:");
				Assert.Contains("queens=1 ", world);
				Assert.DoesNotContain("serpents=0", world);
				Assert.Contains("queenReachable=True teleportReachable=True lenastirInvulnerable=True teleportNonMovable=True", world);
			}
			using (var client = this.Connect(Account, "Quest Hero")) {
				client.SendSpeech(".SetupMasterQuest");
				Assert.NotNull(TryReadMessageContaining(client, "No permission", Wait));

				//not ready yet
				this.Prepare(client, "0 6 0");
				client.SendSpeech("ahoj");
				Assert.NotNull(TryReadMessageContaining(client, "Nejprve se musis stat expertem", Wait));
				this.Prepare(client, "1 5 0");
				client.SendSpeech("ahoj");
				Assert.NotNull(TryReadMessageContaining(client, "Nejprve se musis stat expertem", Wait));

				//the task and the key, once
				this.Prepare(client, "1 6 0");
				client.SendSpeech("dobry den");
				Assert.NotNull(TryReadMessageContaining(client, "Mam pro tebe ukol", Wait));
				client.SendSpeech("ano");
				Assert.NotNull(TryReadMessageContaining(client, "Poslouchej tedy", Wait));
				Assert.Contains("key=True keyNewbie=True", this.State(client));
				Assert.Contains("Mistr Lenastir po tobe chce", this.JournalPage(client));
				client.SendSpeech("ano");
				Assert.NotNull(TryReadMessageContaining(client, "Musis mi prinest", Wait));
				client.SendSpeech("kde najdu klic");
				Assert.NotNull(TryReadMessageContaining(client, "pobliz hadich oltaru", Wait));

				//an egg without ever having been there
				this.Ask(client, ".TestMasterQuestGiveEgg \"none\"", "MASTER_EGG:");
				client.SendSpeech("ahoj");
				Assert.NotNull(TryReadMessageContaining(client, "koupene obycejne vejce", Wait));
				string state = this.State(client);
				Assert.Contains("rank=Expert", state);
				Assert.Contains("egg=True", state);

				//the teleport takes the key and leads to the islands, which marks the player
				this.Ask(client, ".TestMasterQuestStepOnTeleport", "MASTER_STEPPED:");
				state = this.State(client);
				Assert.Contains("region=a_Hadi_ostrovy rank=Expert reached=True key=False", state);

				//the real queen's egg, lifted out of her corpse by the real client
				string queen = this.Ask(client, ".TestMasterQuestKillQueen", "MASTER_QUEEN:");
				Assert.DoesNotContain("egg=none", queen);
				Assert.Contains("owner= ", queen);
				uint egg = Uid(queen, "egg=");
				client.SendDoubleClick(Uid(queen, "pack=")); //a client can only reach into what it has open
				client.SendDoubleClick(Uid(queen, "corpse="));
				Thread.Sleep(500);
				client.SendPickUpItem(egg, 1);
				Thread.Sleep(300);
				client.SendDropItem(egg, 0xFFFF, 0xFFFF, 0, (int) Uid(queen, "pack="));
				Assert.Contains("egg=True eggMine=True", this.State(client));
				Assert.Contains("Odnes ho Mistru Lenastirovi", this.JournalPage(client));

				//back to Lenastir
				this.Ask(client, ".TestMasterQuestBackToLenastir", "MASTER_BACK:");
				client.SendSpeech("zdravim");
				Assert.NotNull(TryReadMessageContaining(client, "povysuji na mistra", Wait));
				Assert.NotNull(TryReadMessageContaining(client, "You have gained 500 experience", Wait));
				state = this.State(client);
				Assert.Contains("rank=Master reached=False key=False", state);
				Assert.Contains("egg=False", state);
				Assert.Contains("na mistra", this.JournalPage(client));
				client.SendSpeech("ahoj");
				Assert.NotNull(TryReadMessageContaining(client, "Jiz jsi muj ukol splnil", Wait));

				//a Master's key is wasted on the teleport
				this.Ask(client, ".TestMasterQuestGiveKey", "MASTER_KEY:");
				this.Ask(client, ".TestMasterQuestStepOnTeleport", "MASTER_STEPPED:");
				Assert.NotNull(TryReadMessageContaining(client, "can't go through here", Wait));
				state = this.State(client);
				Assert.Contains("region=a_Beridor_Level_3", state);
				Assert.Contains("key=False", state);

				//someone else's egg: sent to the bug area, still an Expert
				this.Prepare(client, "1 6 1");
				this.Ask(client, ".TestMasterQuestGiveEgg \"other\"", "MASTER_EGG:");
				client.SendSpeech("ahoj");
				Assert.NotNull(TryReadMessageContaining(client, "otisk jineho hrdiny", Wait));
				state = this.State(client);
				Assert.Contains("region=a_bug rank=Expert", state);
				Assert.Contains("egg=True", state);
			}
		}

		private void Prepare(TestGameClient client, string args) {
			Assert.Contains("los=True", this.Ask(client, ".TestMasterQuestPrepare \"" + args + "\"", "MASTER_PREPARE:"));
		}

		//the Master quest's page in the journal, with the menu closed again
		private string JournalPage(TestGameClient client) {
			client.SendSpeech(".questy");
			client.SendGumpResponse(client.ReadGumpSkippingQuestScroll(Wait).GumpUid, 1001);
			string page = string.Join("|", client.ReadGumpSkippingQuestScroll(Wait).Strings);
			client.SendGumpResponse(client.ReadGumpSkippingQuestScroll(Wait).GumpUid, 0);
			return page;
		}

		private string State(TestGameClient client) {
			Thread.Sleep(500);
			return this.Ask(client, ".TestMasterQuestState", "MASTER_STATE:");
		}

		private static uint Uid(string line, string key) {
			return uint.Parse(Field(line, key), System.Globalization.CultureInfo.InvariantCulture);
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
	public class MasterQuestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MasterQuestPair";
	}
}
