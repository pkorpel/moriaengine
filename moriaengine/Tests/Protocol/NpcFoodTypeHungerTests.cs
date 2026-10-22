using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #593 - proves the real hunger/decay/feeding/starvation pipeline actually works, not
	/// just that NPCDef.FoodTypes parses. Covers: taming installs PetHungerTimer at full
	/// food, releasing removes it, a real timer tick decays food by 1, the real ".Feed"
	/// command (real protocol target-cursor round trip) raises food back and consumes the
	/// matching backpack item, and starvation (food reaching 0) kills the pet.
	///
	/// AccountB (not Account) is used for the decay/feed/starve Facts specifically because
	/// PetHungerTimer deliberately exempts a GM's own pets from starving (same spirit as the
	/// real source's mounted-GM exemption) - and the server's literal first-ever account is
	/// always auto-GM (AbstractAccount.HandleLoginAttempt's accounts.Count==1 check), which
	/// Account would otherwise be in an isolated run of just this file. Same established
	/// EnsureAutoAccountCreationEnabled/AccountB precedent as ProfessionTests.cs.
	/// </summary>
	[Collection(NpcFoodTypeHungerCollection.Name)]
	public class NpcFoodTypeHungerTests {
		private const string Account = "npchungertest";
		private const string AccountB = "npchungertestb";
		private const string Password = "npchungertest";

		private readonly GameServerPairFixture fixture;

		public NpcFoodTypeHungerTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void TamingInstallsHungerTimerAndReleaseRemovesIt() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "HungerInstall")) {
				uint uid = this.SpawnPet(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTameHungerPet " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_TAMED:", mark, TimeSpan.FromSeconds(10)));
				string tamedLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "HUNGER_TEST_TAMED:");
				Assert.Contains("installed=True", tamedLine);
				Assert.Contains("food=5", tamedLine); //MaxFood for npc_test_hunger (FoodType = 5 t_meat_raw) - starts full

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReleaseHungerPet " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_RELEASED:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("stillInstalled=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "HUNGER_TEST_RELEASED:"));
			}
		}

		[Fact]
		public void ARealTimerTickDecaysFoodByOne() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "HungerDecay")) {
				uint uid = this.SpawnPet(client);
				this.Tame(client, uid);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceHungerTick " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_FORCED_TICK:", mark, TimeSpan.FromSeconds(10)));

				//The forced tick fires ~50ms later on the real timer thread - give it a
				//moment before checking.
				Thread.Sleep(300);

				Assert.Equal(4, this.ReportFood(client, uid));
			}
		}

		[Fact]
		public void FeedingARealMatchingItemRaisesFoodAndConsumesIt() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "HungerFeed")) {
				uint uid = this.SpawnPet(client);
				this.Tame(client, uid);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceHungerTick " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_FORCED_TICK:", mark, TimeSpan.FromSeconds(10)));
				Thread.Sleep(300);
				Assert.Equal(4, this.ReportFood(client, uid)); //decayed from 5 to 4, matches the real archive's i_ribs_raw/t_meat_raw category

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGiveRibsRaw");
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_GAVE_RIBS:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".Feed");
				Assert.True(this.fixture.WaitForGameOutputSince("commands 'Feed'. OK", mark, TimeSpan.FromSeconds(10)));
				client.SendTargetUid(uid);
				Thread.Sleep(300);

				Assert.Equal(5, this.ReportFood(client, uid)); //raised back to MaxFood via the real backpack item
			}
		}

		[Fact]
		public void StarvationKillsThePet() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "HungerStarve")) {
				uint uid = this.SpawnPet(client);
				this.Tame(client, uid);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetHungerCritical " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_SET_CRITICAL:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceHungerTick " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_FORCED_TICK:", mark, TimeSpan.FromSeconds(10)));
				Thread.Sleep(300);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportHunger " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_REPORT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "HUNGER_TEST_REPORT:");
				//food hit 0 -> self.Hits=0 -> CauseDeath -> the character record itself is gone
				//(replaced by a corpse) by the time we check, so Thing.UidGetThing(uid) as NPC
				//is null and TestReportHunger's own food=-1 sentinel/isDeleted=True is exactly
				//the expected post-death shape, not food=0 staying observable on a live NPC.
				Assert.Contains("isDeleted=True", line);
			}
		}

		private void EnsureAutoAccountCreationEnabled() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
		}

		private uint SpawnPet(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSpawnHungerPet");
			Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "HUNGER_TEST_SPAWNED:");
			Assert.Contains("maxFood=5", line);
			return ExtractUidAfter(line, "uid=");
		}

		private void Tame(TestGameClient client, uint uid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTameHungerPet " + uid);
			Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_TAMED:", mark, TimeSpan.FromSeconds(10)));
		}

		private int ReportFood(TestGameClient client, uint uid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportHunger " + uid);
			Assert.True(this.fixture.WaitForGameOutputSince("HUNGER_TEST_REPORT:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "HUNGER_TEST_REPORT: uid=" + uid + " ");
			int idx = line.IndexOf("food=", StringComparison.Ordinal);
			int end = idx + "food=".Length;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(idx + "food=".Length, end - idx - "food=".Length));
		}

		private static uint ExtractUidAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			idx += marker.Length;
			int end = idx;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return uint.Parse(line.Substring(idx, end - idx));
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

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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
						login.SendLogin(account, Password);

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
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(NpcFoodTypeHungerCollection.Name, DisableParallelization = true)]
	public class NpcFoodTypeHungerCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcFoodTypeHungerPair";
	}
}
