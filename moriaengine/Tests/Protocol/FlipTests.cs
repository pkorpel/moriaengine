using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#938 - .flip as Sphere, and an item turning when dropped. Expected numbers are the real 0.99z8's,
	/// from EreborNext's FlipOracleTests.</summary>
	[Collection(FlipCollection.Name)]
	public class FlipTests {
		private const string Account = "fliptest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public FlipTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryKindOfThingFlipsAsInSphere() {
			using (var client = this.Connect("Flip Rules")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);

				string group = this.Ask(client, ".TestFlipWholeGroup", "FLIP_GROUP:");
				Match m = Regex.Match(group, @"steps=(\d+) models=(\d+) dupes=(\d+)");
				Assert.True(int.Parse(m.Groups[3].Value) >= 2, group);
				Assert.Equal(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[1].Value)); //each model once, then home
				Assert.Equal(int.Parse(m.Groups[3].Value) + 1, int.Parse(m.Groups[2].Value));

				Assert.Contains("FLIP_LIST: 610|609|608|", this.Ask(client, ".TestFlipList", "FLIP_LIST:"));
				//0x675 + next, ALIGN, HAND, SWING: Sphere's 1655, 1661, 1655, 1657
				Assert.Contains("FLIP_DOOR: 1655|1661|1655|1657|", this.Ask(client, ".TestFlipDoor", "FLIP_DOOR:"));
				string light = this.Ask(client, ".TestFlipLight", "FLIP_LIGHT:");
				Assert.Contains("FLIP_LIGHT: 55|0|model=2575", light);
				Assert.Contains("model=8198 dir=0", this.Ask(client, ".TestFlipCorpse", "FLIP_CORPSE:"));
			}
		}

		[Fact]
		public void TheCommandTurnsACharacterAndTakesADoorsWord() {
			using (var client = this.Connect("Flip Command")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);
				string setup = this.Ask(client, ".TestFlipSelfSetup", "FLIP_CMD:");
				uint self = Uid(setup, "self");
				uint door = Uid(setup, "door");
				string plainDoor = Regex.Match(setup, @"plainDoor=(\d+)").Groups[1].Value;

				client.SendSpeech(".flip");
				Thread.Sleep(300);
				client.SendTargetUid(self);
				Thread.Sleep(300);
				client.SendSpeech(".flip \"ALIGN\"");
				Thread.Sleep(300);
				client.SendTargetUid(door);
				Thread.Sleep(300);

				string report = this.Ask(client, ".TestFlipReport " + plainDoor, "FLIP_REPORT:");
				Assert.Contains("model=1661", report);
				Assert.Contains("dir=0", report); //NorthWest + one step clockwise

				//Sphere's own form: the method on the target, as a script's item.flip
				client.SendSpeech(".x flip");
				Thread.Sleep(300);
				client.SendTargetUid(door);
				Thread.Sleep(300);
				Assert.Contains("model=1663", this.Ask(client, ".TestFlipReport " + plainDoor, "FLIP_REPORT:"));
			}
		}

		[Fact]
		public void DroppingTurnsAnItemButNotAStack() {
			using (var client = this.Connect("Flip Dropper")) {
				string setup = this.Ask(client, ".TestFlipDropSetup", "FLIP_DROP:");
				uint probe = Uid(setup, "probe");
				uint gold = Uid(setup, "gold");
				int goldModel = Number(setup, "goldModel");
				ushort x = (ushort) (Number(setup, "x") + 1);
				ushort y = (ushort) Number(setup, "y");
				sbyte z = (sbyte) Number(setup, "z");

				client.SendPickUpItem(probe, 1);
				Thread.Sleep(300);
				client.SendDropItem(probe, x, y, z, -1);
				Thread.Sleep(500);
				client.SendPickUpItem(gold, 5);
				Thread.Sleep(300);
				client.SendDropItem(gold, x, y, z, -1);
				Thread.Sleep(500);

				Assert.Contains("model=610", this.Ask(client, ".TestCheckItemModel " + (probe & 0x7fffffff), "ITEM_CHECK:"));
				Assert.Contains("model=" + goldModel, this.Ask(client, ".TestCheckItemModel " + (gold & 0x7fffffff), "ITEM_CHECK:"));
			}
		}

		private static uint Uid(string line, string name) {
			return uint.Parse(Regex.Match(line, name + @"=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
		}

		private static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, name + @"=(-?\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
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
	public class FlipCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Flip";
	}
}
