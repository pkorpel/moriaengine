using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #1201 - NPCs sleep where no player is near, and wake when one comes. Server side in
	/// scripts/Tests/NpcSleepTests.cs (the awake time cut to 3 s): A beside the player, B wild three areas away,
	/// C a pet following its owner, D a follower, E wild beyond its leash, F NeverSleeps, G a pet told to stay and H a held NPC, both far from their homes.
	/// </summary>
	[Collection(NpcSleepCollection.Name)]
	public class NpcSleepTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcSleepTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcsSleepWhereNoPlayerIsNearAndWakeWhenOneComes() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("Sleepwatcher");
				try {
					this.Command(game, ".TestNpcSleepSetup", "NPC_SLEEP_SETUP");
					Thread.Sleep(5000); //each thinks at least once
					string first = this.Report(game, "first");
					Assert.True(Sleeping(first, "first", "B"), "a wild NPC with no player near should sleep:\n" + first);
					Assert.True(Sleeping(first, "first", "E"), first);
					Assert.Matches(@"at=first npc=E sleeping=True paused=True pos=\S+ atHome=False", first); //#1204 - sleeps where it stands, not put home
					Assert.Matches(@"at=first npc=B sleeping=True paused=True", first); //its timer is paused, not just skipping
					foreach (string awake in new[] { "A", "C", "D", "F" }) {
						Assert.False(Sleeping(first, "first", awake), awake + " should stay awake:\n" + first);
					}
					Assert.Matches(@"at=first npc=C sleeping=False paused=False pos=\S+ atHome=\S+ state=Following", first); //thinking, not just unpaused
					Assert.Matches(@"at=first npc=D sleeping=False paused=False pos=\S+ atHome=\S+ state=Following", first);
					Assert.Matches(@"at=first npc=G sleeping=True paused=True pos=\S+ atHome=False", first); //a pet told to stay isn't sent home
					Assert.Matches(@"at=first npc=H sleeping=True paused=True pos=\S+ atHome=False", first); //nor a held NPC

					string summon = this.Command(game, ".TestNpcSleepSummon", "NPC_SLEEP_SUMMON");
					Assert.Contains("NPC_SLEEP_SUMMON: before=True after=False", summon);

					string visit = this.Command(game, ".TestNpcSleepVisit", "NPC_SLEEP_VISIT");
					Assert.Contains("NPC_SLEEP_VISIT: before=True after=False", visit);

					this.Command(game, ".TestNpcSleepGoBack", "NPC_SLEEP_BACK");
					Thread.Sleep(8000); //3 s awake, then its next think
					string again = this.Report(game, "again");
					Assert.True(Sleeping(again, "again", "B"), "B should fall asleep again once the player has gone:\n" + again);
				} finally {
					this.Command(game, ".TestNpcSleepCleanup", "NPC_SLEEP_CLEANED");
				}
			}
		}

		private static bool Sleeping(string output, string label, string npc) {
			Match m = Regex.Match(output, "NPC_SLEEP: at=" + label + " npc=" + npc + " sleeping=(True|False)");
			Assert.True(m.Success, "no report for " + npc + " at " + label + ":\n" + output);
			return m.Groups[1].Value == "True";
		}

		private string Report(TestGameClient game, string label) {
			return this.Command(game, ".TestNpcSleepReport \"" + label + "\"", "NPC_SLEEP_REPORTED: " + label);
		}

		private string Command(TestGameClient game, string command, string marker) {
			game.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutput(marker, TimeSpan.FromSeconds(30)), this.fixture.GameOutputSnapshot());
			return this.fixture.GameOutputSnapshot();
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NpcSleepCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcSleep";
	}
}
