using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#951 - who can do what, with real accounts at each plevel.</summary>
	[Collection(CommandPermissionCollection.Name)]
	public class CommandPermissionTests {
		private const string AdminAccount = "permissionadmin", PlayerAccount = "permissionplayer";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		private readonly GameServerPairFixture fixture;

		public CommandPermissionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private void TryToCheat(TestGameClient player, string quote) {
			foreach (string command in new[] { ".level 200", ".skillrank 3", ".experience 99999", ".name " + quote + "Cheater" + quote, ".set_Level 200", ".plevel 7" }) {
				player.SendSpeech(command);
				Thread.Sleep(150);
			}
		}

		[Fact]
		public void EachPlevelGetsItsCommands() {
			using var admin = Connect(AdminAccount, "Perm Admin");
			this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var player = Connect(PlayerAccount, "Perm Player");
			Assert.Contains(": 1", this.Ask(player, ".TestPermissionMe", "PERMISSION_ME:"));
			const string Untouched = "level=0 rank=Novice name=Perm Player plevel=1 exp=0";

			//a player sets nothing, and .explevel's dialog still works for them
			this.TryToCheat(player, "");
			Assert.Contains(Untouched, this.Ask(admin, ".TestPermissionReport", "PERMISSION_REPORT:"));
			player.SendSpeech(".explevel");
			player.ReadGumpDialog(Wait);

			//the two levels below GM get the full parser, and so were the ones who could
			this.Ask(admin, ".TestPermissionPlevel 3", "PERMISSION_PLEVEL: 3");
			this.TryToCheat(player, "\"");
			Assert.Contains("level=0 rank=Novice name=Perm Player plevel=3 exp=0", this.Ask(admin, ".TestPermissionReport", "PERMISSION_REPORT:"));

			//a GM can, but setups are for plevel 5
			this.Ask(admin, ".TestPermissionPlevel 4", "PERMISSION_PLEVEL: 4");
			player.SendSpeech(".level 7");
			Thread.Sleep(300);
			Assert.Contains("level=7", this.Ask(admin, ".TestPermissionReport", "PERMISSION_REPORT:"));
			int mark = this.fixture.GameOutputSnapshot().Length;
			foreach (string setup in new[] { ".SetupWorld", ".SetupHouses", ".SetupTowns", ".SetupDungeonEntrances", ".SetupExpertQuest", ".SetupMasterQuest", ".SetupGrandmasterQuest" }) {
				player.SendSpeech(setup);
				Thread.Sleep(150);
			}
			Thread.Sleep(1000);
			Assert.DoesNotContain("SETUP_DONE", this.fixture.GameOutputSnapshot().Substring(mark));

			//plevel 5 runs one, told how far it got, and a second has to wait
			this.Ask(admin, ".TestPermissionPlevel 5", "PERMISSION_PLEVEL: 5");
			mark = this.fixture.GameOutputSnapshot().Length;
			player.SendSpeech(".SetupHouses");
			player.SendSpeech(".SetupHouses");
			SeeMessage(player, "A setup is already running");
			SeeMessage(player, "Setup: Houses 20/281");
			SeeMessage(player, "Setup: Houses 280/281");
			Assert.True(this.fixture.WaitForGameOutputSince("SETUP_DONE: Houses added=281 kept=0", mark, Wait), "The run didn't finish.");

			//under the harness a player reaches a Test command, on a real server not
			this.Ask(admin, ".TestPermissionPlevel 1", "PERMISSION_PLEVEL: 1");
			this.Ask(player, ".TestPermissionProbe", "PERMISSION_PROBE: Perm Player");
			this.Ask(admin, ".TestPermissionCloseTestCommands", "PERMISSION_CLOSED:");
			mark = this.fixture.GameOutputSnapshot().Length;
			player.SendSpeech(".TestPermissionProbe");
			player.SendSpeech(".TestPromoteToGM");
			Thread.Sleep(1500);
			Assert.DoesNotContain("PERMISSION_PROBE: Perm Player", this.fixture.GameOutputSnapshot().Substring(mark));
			Assert.Contains("plevel=1", this.Ask(admin, ".TestPermissionReport", "PERMISSION_REPORT:"));
		}

		private static void SeeMessage(TestGameClient client, string text) {
			DateTime deadline = DateTime.UtcNow + Wait;
			string seen = "";
			while (DateTime.UtcNow < deadline) {
				string message = client.ReadSystemMessage(Wait);
				if (message.Contains(text, StringComparison.Ordinal)) {
					return;
				}
				seen += message + " | ";
			}
			Assert.Fail("No '" + text + "', saw: " + seen);
		}

		internal static TestGameClient Connect(string account, string charName) {
			for (int attempt = 0; ; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
						client.SendLogin(account, account);
						client.ReadCharacterList();
						client.SendCreateCharacter(charName);
						Thread.Sleep(500);
						return client;
					}
				} catch (InvalidOperationException) when (attempt < 40) {
					Thread.Sleep(300);
				}
			}
		}

		internal static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return Ask(this.fixture, client, speech, marker);
		}
	}

	/// <summary>A rank on the plugin through a real save and restart.</summary>
	[Collection(RankRestartCollection.Name)]
	public class RankRestartTests {
		private const string Account = "rankrestart";

		[Fact]
		public void ARankSurvivesARestart() {
			using (var first = new GameServerPairFixture()) {
				using (var client = CommandPermissionTests.Connect(Account, "Rank Saver")) {
					//a rank from an old save moves over, and stays when the profession changes
					Assert.Contains("read=Master plugin=Master tagLeft=False", CommandPermissionTests.Ask(first, client, ".TestRankOldSave", "RANK_OLD:"));
					string changed = CommandPermissionTests.Ask(first, client, ".TestRankChangeProfession", "RANK_CHANGED:");
					Assert.Contains("changed=Master/Mag none=Master back=Master", changed);
					Assert.Contains("Master", changed.Substring(changed.IndexOf("title=", StringComparison.Ordinal)));
					Assert.Contains("Grandmaster", CommandPermissionTests.Ask(first, client, ".TestRankSave", "RANK_SAVE:"));
					CommandPermissionTests.Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = CommandPermissionTests.Connect(Account, "Rank Loader")) {
					Assert.Contains("Rank Saver Grandmaster", CommandPermissionTests.Ask(second, client, ".TestRankLoaded", "RANK_LOADED:"));
				}
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CommandPermissionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CommandPermission";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RankRestartCollection {
		public const string Name = "RankRestart";
	}
}
