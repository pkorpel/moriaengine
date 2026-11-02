using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1035 - server side in scripts/Tests/TravelStoneTests.cs.</summary>
	[Collection(TravelStoneCollection.Name)]
	public class TravelStoneTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const int StopButtonBase = 10, FornostIndex = 7, NextPage = 2, FirstPage = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		private readonly GameServerPairFixture fixture;

		public TravelStoneTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryStopIsUsableAndEverySetupRunAddsThirtyStonesOnce() {
			using (TestGameClient game = this.Login("Surveyor")) {
				game.SendSpeech(".TestTravelStops");
				Assert.True(this.fixture.WaitForGameOutput("TRAVEL_STOPS_DONE", Wait), this.fixture.GameOutputSnapshot());
				this.AssertChecks("everyStopCanBeStoodOn", "everyStopHasAStoneSpot", "bothSidesHaveStops");

				//30 in all: a stone a GM placed near a stop is adopted, not doubled
				game.SendSpeech(".TestTravelSetup");
				Thread.Sleep(3000);
				Assert.Equal(30, this.StoneCount(game));
				game.SendSpeech(".TestTravelSetup");
				Thread.Sleep(3000);
				Assert.Equal(30, this.StoneCount(game));
			}
		}

		[Fact]
		public void SidesAndPricesFollowErebor() {
			using (TestGameClient game = this.Login("Pricer")) {
				game.SendSpeech(".TestTravelSides");
				Assert.True(this.fixture.WaitForGameOutput("TRAVEL_SIDES_DONE", Wait), this.fixture.GameOutputSnapshot());
				this.AssertChecks("edorasIsGondor", "baradDurIsMordor", "gondorIsNotOfferedMordor", "murdererCountsAsMurderer", "gondorMurdererKeepsGondorsStops", "mordorIsNotOfferedGondor",
					"noRealmIsNotOfferedMordor", "priceIsDistanceBy3Point5", "leavingEnemyLandCostsFiveTimes", "mordorPaysPlainAtHome",
					"gmIsOfferedEverything");
			}
		}

		[Fact]
		public void ARideThroughTheDialog() {
			using (TestGameClient game = this.Login("Passenger")) {
				game.SendSpeech(".TestTravelPrepare");
				Assert.True(this.fixture.WaitForGameOutput("TRAVEL_READY", Wait), this.fixture.GameOutputSnapshot());
				string ready = this.LastLine("TRAVEL_READY");
				uint stone = uint.Parse(Regex.Match(ready, @"stone=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
				int gold = Number(ready, "gold"), price = Number(ready, "price");
				Assert.True(price > 0);

				//the first page has 14 stops, no way back, and a way on
				game.SendDoubleClick(stone);
				var gump = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Minas Tirith", gump.Strings);
				Assert.Contains(price.ToString(CultureInfo.InvariantCulture) + " gp", gump.Strings);
				Assert.DoesNotContain("Barad-Dur", gump.Strings);
				Assert.DoesNotContain("Na zacatek seznamu", gump.Strings);
				Assert.Contains("Dalsi trasy", gump.Strings);

				game.SendGumpResponse(gump.GumpUid, NextPage);
				gump = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Na zacatek seznamu", gump.Strings);
				Assert.DoesNotContain("Minas Tirith", gump.Strings);
				game.SendGumpResponse(gump.GumpUid, FirstPage);
				gump = game.ReadGumpDialogWithStrings(Wait);

				//a step after opening: the button does nothing
				game.SendSpeech(".TestTravelStep");
				Thread.Sleep(300);
				game.SendGumpResponse(gump.GumpUid, StopButtonBase + FornostIndex);
				string report = this.Report(game);
				Assert.Equal(gold, Number(report, "gold"));
				Assert.Contains("riding=False", report, StringComparison.Ordinal);

				//straying off during the count loses the ride and the money
				game.SendSpeech(".TestTravelBackToStone");
				Thread.Sleep(300);
				game.SendDoubleClick(stone);
				gump = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(gump.GumpUid, StopButtonBase + FornostIndex);
				report = this.Report(game);
				Assert.Equal(gold - price, Number(report, "gold"));
				Assert.Contains("riding=True", report, StringComparison.Ordinal);
				game.SendSpeech(".TestTravelStray");
				Thread.Sleep(4500);
				report = this.Report(game);
				Assert.Contains("riding=False", report, StringComparison.Ordinal);
				Assert.Contains("atFornost=False", report, StringComparison.Ordinal);
				Assert.Equal(gold - price, Number(report, "gold"));

				//a whole ride
				game.SendSpeech(".TestTravelBackToStone");
				Thread.Sleep(300);
				game.SendDoubleClick(stone);
				gump = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(gump.GumpUid, StopButtonBase + FornostIndex);
				Thread.Sleep(4500);
				report = this.Report(game);
				Assert.Contains("atFornost=True", report, StringComparison.Ordinal);
				Assert.Equal(gold - (2 * price), Number(report, "gold"));

				//an outcast is sent away
				game.SendSpeech(".TestTravelBackToStone");
				game.SendSpeech(".TestTravelBecomeOutcast");
				Assert.True(this.fixture.WaitForGameOutput("TRAVEL_OUTCAST", Wait), this.fixture.GameOutputSnapshot());
				Thread.Sleep(300);
				game.SendDoubleClick(stone);
				Assert.ThrowsAny<Exception>(() => game.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(3)));
			}
		}

		private TestGameClient Login(string name) {
			Exception firstFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				int redirectPort = this.WaitForServerListingAndGetRedirectPort();
				var game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(TestAccount, TestPassword);
					game.ReadCharacterList();
					game.SendCreateCharacter(name);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose(); //the last test's character can still be leaving
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as " + name, firstFailure);
		}

		private int StoneCount(TestGameClient game) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			game.SendSpeech(".TestTravelSetupCount");
			for (int i = 0; i < 100; i++) {
				string output = this.fixture.GameOutputSnapshot();
				Match match = Regex.Match(output.Substring(mark), @"TRAVEL_SETUP_STONES=(\d+)");
				if (match.Success) {
					return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				}
				Thread.Sleep(100);
			}
			throw new TimeoutException("No stone count. Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private string Report(TestGameClient game) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			Thread.Sleep(300);
			game.SendSpeech(".TestTravelReport");
			for (int i = 0; i < 100; i++) {
				string output = this.fixture.GameOutputSnapshot();
				int at = output.IndexOf("TRAVEL_REPORT:", mark, StringComparison.Ordinal);
				if ((at >= 0) && output.IndexOf('\n', at) > 0) {
					return output.Substring(at, output.IndexOf('\n', at) - at);
				}
				Thread.Sleep(100);
			}
			throw new TimeoutException("No report. Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private string LastLine(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int at = output.LastIndexOf(marker, StringComparison.Ordinal);
			return output.Substring(at, output.IndexOf('\n', at) - at);
		}

		private static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, name + @"=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
		}

		private void AssertChecks(params string[] checks) {
			string output = this.fixture.GameOutputSnapshot();
			foreach (string check in checks) {
				string marker = "TRAVEL_CHECK: " + check + "=";
				int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
				Assert.True(idx >= 0, "Check '" + check + "' never reported. Game output:\n" + output);
				Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, "Check '" + check + "' failed. Game output:\n" + output);
			}
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

	/// <summary>A ride in progress goes into the world save, and an unsaveable timer aborts all of it.</summary>
	[Collection(TravelRideRestartCollection.Name)]
	public class TravelRideRestartTests {
		private const string Account = "travelriderestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void ARideSurvivesARestart() {
			using (var first = new GameServerPairFixture()) {
				using (var client = Connect(first, "Ride Saver")) {
					client.SendSpeech(".TestTravelBookRide");
					Assert.True(first.WaitForGameOutput("TRAVEL_BOOKED: riding=True", Wait), first.GameOutputSnapshot());
					client.SendSpeech(".TestTriggerGlobalSave");
					Assert.True(first.WaitForGameOutput("TEST_SAVE: done", Wait), first.GameOutputSnapshot());
				}
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(second, "Ride Loader")) {
					client.SendSpeech(".TestTravelCountRides");
					Assert.True(second.WaitForGameOutput("TRAVEL_RIDES=1", Wait), second.GameOutputSnapshot());
				}
			}
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string name) {
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
						var client = new TestGameClient("127.0.0.1", redirectPort);
						client.SendLogin(Account, Account);
						client.ReadCharacterList();
						client.SendCreateCharacter(name);
						Thread.Sleep(500);
						return client;
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
	public class TravelRideRestartCollection {
		public const string Name = "TravelRideRestart";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TravelStoneCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TravelStone";
	}
}
