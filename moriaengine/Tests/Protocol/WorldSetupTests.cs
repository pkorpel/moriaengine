using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#843 - .SetupWorld: runs every setup, can be run again and again, and only ever adds what's missing. A piece a
	/// GM moved stays, a piece a GM deleted comes back, and nothing else in the world is touched.
	/// Server-side helpers: scripts/Tests/WorldSetupTests.cs.</summary>
	[Collection(WorldSetupCollection.Name)]
	public class WorldSetupTests {
		private const string Account = "worldsetupplayer";
		private const string AdminAccount = "worldsetupadmin"; //the server's first account: a GM, can turn on auto-creation
		private const int ExpertPieces = 16, MasterPieces = 5, GrandmasterPieces = 17, EntrancePieces = 2;
		private const int TownPieces = 24, HouseSigns = 281, TravelStones = 30, StallAndFieldSigns = 99;
		private const int CampGuard = 1; //#1107
		private const int AllPieces = ExpertPieces + MasterPieces + GrandmasterPieces + EntrancePieces + TownPieces + HouseSigns + TravelStones + StallAndFieldSigns + CampGuard;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public WorldSetupTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SetupWorldOnlyEverAddsWhatIsMissing() {
			using (var admin = this.ConnectOnce(AdminAccount, "Setup Admin")) {
				this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST:");
				autoAccountCreationEnabled = true;

				//one quest's own command first: the master command then adds only the rest
				Assert.Contains("added=" + ExpertPieces + " kept=0", this.Ask(admin, ".SetupExpertQuest", "SETUP_DONE:"));
				Assert.Contains("added=" + (AllPieces - ExpertPieces) + " kept=" + ExpertPieces, this.Ask(admin, ".SetupWorld", "SETUP_DONE:"));
				Assert.Contains("added=0 kept=" + AllPieces, this.Ask(admin, ".SetupWorld", "SETUP_DONE:"));

				string counts = this.Ask(admin, ".TestWorldSetupCheck", "WORLD_CHECK:");
				Assert.Contains("stones=2 lenastirs=2 rinalds=1 elqiras=2", counts);
				Assert.Contains("loose=0", counts);

				//a GM's edits: the moved piece stays moved, the deleted one comes back, the unrelated item stays;
				//and a Lenastir from before pieces had ids is taken as that piece, not doubled
				this.Ask(admin, ".TestWorldSetupEdit", "WORLD_EDIT:");
				Assert.Contains("added=1 kept=" + (AllPieces - 1), this.Ask(admin, ".SetupWorld", "SETUP_DONE:"));
				counts = this.Ask(admin, ".TestWorldSetupCheck", "WORLD_CHECK:");
				Assert.Contains("elviras=1 elviraMoved=True", counts);
				Assert.Contains("loose=0 doors=1 strayThere=True", counts); //a sign placed loose by an older version is fixed too
				Assert.Contains("stones=2 lenastirs=2 rinalds=1 elqiras=2", counts);

				//the user's call: lone NPCs are spawns of one (.spawn's gems); the Lenastir gem with no id was taken, not doubled
				string spawns = this.Ask(admin, ".TestWorldSetupCheck", "WORLD_SPAWNS:");
				Assert.Contains("npcPieces=1 ", spawns); //Rinald, whose quest runs his life
				Assert.Contains(" bare=0 allInvulnerable=True shopsReady=4 ", spawns);
				Assert.Contains("gone=True timer=True", this.Ask(admin, ".TestWorldSetupRespawn", "WORLD_RESPAWN:"));
				Thread.Sleep(2000);
				Assert.Contains("elviras=1 fromGem=True invulnerable=True still=True", this.Ask(admin, ".TestWorldSetupRespawned", "WORLD_RESPAWNED:"));

				//a GM drags a fixed piece, and it stays fixed where it landed; with .gm off the same character can't
				string sign = this.Ask(admin, ".TestWorldSetupGoToSign", "WORLD_SIGN:");
				uint uid = uint.Parse(Field(sign, "uid="));
				string[] me = sign.Substring(sign.IndexOf("me=", StringComparison.Ordinal) + 3).Split(' ');
				admin.SendPickUpItem(uid, 1);
				Thread.Sleep(300);
				admin.SendDropItem(uid, ushort.Parse(me[0]), ushort.Parse(me[1]), sbyte.Parse(me[2]), -1);
				Thread.Sleep(500);
				string moved = this.Ask(admin, ".TestWorldSetupReportSign " + uid, "WORLD_SIGN:");
				Assert.Contains("at=" + me[0] + "," + me[1] + " ", moved);
				Assert.Contains("onGround=True fixed=True decaying=False", moved);

				admin.SendSpeech(".gm");
				Thread.Sleep(300);
				admin.SendPickUpItem(uid, 1);
				Thread.Sleep(300);
				Assert.Contains("onGround=True fixed=True", this.Ask(admin, ".TestWorldSetupReportSign " + uid, "WORLD_SIGN:"));
				admin.SendSpeech(".gm");
				Thread.Sleep(300);
			}
			using (var client = this.Connect(Account, "Setup Player")) {
				client.SendSpeech(".SetupWorld");
				Assert.NotNull(TryReadMessageContaining(client, "No permission", Wait));
			}
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
			using (var client = this.ConnectOnce(AdminAccount, "Setup Admin")) {
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
	public class WorldSetupCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "WorldSetupPair";
	}
}
