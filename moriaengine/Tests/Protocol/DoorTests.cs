using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - doors, as Sphere's Use_Door: real double clicks and the real macro.</summary>
	[Collection(DoorCollection.Name)]
	public class DoorTests {
		private const string Account = "doortest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public DoorTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryOffsetSwingsAsInSphere() {
			using (var client = this.Connect("Door Table")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);
				//new offset and step per offset, from CItem.Use_Door
				Assert.Contains("DOOR_TABLE: 1:-1,1 0:1,-1 3:1,1 2:-1,-1 5:-1,0 4:1,0 7:1,-1 6:-1,1 9:1,1 8:-1,-1 11:1,-1 10:-1,1 13:0,0 12:0,0 15:0,-1 14:0,1",
					this.Ask(client, ".TestDoorTable", "DOOR_TABLE:"));
				Assert.Contains("type=t_door offset=2", this.Ask(client, ".TestDoorDupe", "DOOR_DUPE:"));
			}
		}

		[Fact]
		public void AClickOpensItAndItClosesByItself() {
			using (var client = this.Connect("Door Clicker")) {
				uint door = Uid(this.Ask(client, ".TestDoorSetup \"plain\"", "DOOR_SETUP:"));

				client.SendDoubleClick(door);
				Thread.Sleep(400);
				Assert.Contains("open=True model=1654 timer=True", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));

				this.Ask(client, ".TestDoorHurry", "DOOR_HURRY:");
				Thread.Sleep(2500);
				Assert.Contains("open=False model=1653 timer=False", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));

				client.SendOpenDoorMacro();
				Thread.Sleep(400);
				Assert.Contains("open=True", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));
				client.SendOpenDoorMacro(); //Sphere's macro takes an open door too
				Thread.Sleep(400);
				Assert.Contains("open=False", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));
			}
		}

		[Fact]
		public void ALockedDoorWantsItsKey() {
			using (var client = this.Connect("Door Locked")) {
				uint door = Uid(this.Ask(client, ".TestDoorSetup \"locked\"", "DOOR_SETUP:"));

				client.SendDoubleClick(door);
				Thread.Sleep(400);
				Assert.Contains("open=False", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));

				this.Ask(client, ".TestDoorKey \"5\"", "DOOR_KEY:");
				client.SendDoubleClick(door);
				Thread.Sleep(400);
				Assert.Contains("open=False", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));

				this.Ask(client, ".TestDoorKey \"77\"", "DOOR_KEY:");
				client.SendDoubleClick(door);
				Thread.Sleep(400);
				Assert.Contains("open=True", this.Ask(client, ".TestDoorState", "DOOR_STATE:"));
			}
		}

		[Fact]
		public void AClosedDoorStopsWalking() {
			using (var client = this.Connect("Door Walker")) {
				this.Ask(client, ".TestTeleportToOpenGround", "OPEN_GROUND");
				Assert.Contains("free=True moved=False", this.Ask(client, ".TestDoorBlocks", "DOOR_BLOCK:"));
			}
		}

		private static uint Uid(string line) {
			return uint.Parse(Regex.Match(line, @"uid=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
		}

		private TestGameClient Connect(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);
						if (login.ReadServersList() < 1) {
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class DoorCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Door";
	}
}
