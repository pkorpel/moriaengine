using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-8 (#76): day/night and weather, confirming GlobalLightLevelOutPacket (0x4f) and
	/// SeasonalInformationOutPacket (0xBC) actually change value over an observed window, driven
	/// by the real production code paths (LightAndWeather.IsNight/GetLightAt and
	/// LightAndWeather.CurrentSeason's setter) rather than asserted against in isolation.
	///
	/// The real autonomous clock only checks for a day/night flip every 60 real seconds and only
	/// has a 1% chance per check of rolling a new season - both too slow/non-deterministic to
	/// wait out directly in a test. The day/night scenario shortens and re-anchors the day cycle
	/// and speeds up the clock's check interval (see LightAndWeather.ResetDayCycleForTest/
	/// SpeedUpClockForTest) so the real flip happens within a known short window instead of at
	/// an unpredictable point in an unshortened cycle. The season scenario instead calls
	/// CurrentSeason's real setter directly - the same one the clock's probabilistic roll itself
	/// calls - which already pushes synchronously, so no timing hook is needed there.
	/// </summary>
	[Collection(DayNightWeatherCollection.Name)]
	public class DayNightWeatherTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public DayNightWeatherTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule (Enhancements #23's own simplification: first half of the day
		/// cycle is day, second half is night): once the day cycle flips to night, players
		/// receive a GlobalLightLevelOutPacket carrying the night light value.</summary>
		[Fact]
		public void ShortenedDayCycleFlipsLightLevelToNight() {
			using (var client = this.ConnectLoginAndCreateCharacter("Chronomancer")) {
				byte initialLight = client.ReadOverallLightLevel(TimeSpan.FromSeconds(10));
				Assert.Equal(0, initialLight); // fresh server, default dayLight

				client.SendSpeech(".TestSetDayLengthSeconds 2");
				client.SendSpeech(".TestResetDayCycle");
				client.SendSpeech(".TestSpeedUpWorldClock 0.5 0.5");

				byte nightLight = client.ReadOverallLightLevel(TimeSpan.FromSeconds(10));
				Assert.Equal(12, nightLight); // default nightLight - confirms the clock-driven transition pushed a changed value
			}
		}

		/// <summary>Real UO rule: classic UO has no separate rain/snow packet - the client
		/// renders season-appropriate weather purely from the season byte, so changing season
		/// *is* the weather system here (see LightAndWeather's own doc comment).</summary>
		[Fact]
		public void SeasonChangeIsPushedToClient() {
			using (var client = this.ConnectLoginAndCreateCharacter("Druid")) {
				byte initialSeason = client.ReadSeason(TimeSpan.FromSeconds(10));
				Assert.Equal(0, initialSeason); // Season.Spring, fresh server default

				client.SendSpeech(".TestSetSeason 3"); // Season.Winter

				byte newSeason = client.ReadSeason(TimeSpan.FromSeconds(10));
				Assert.Equal(3, newSeason);
			}
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class DayNightWeatherCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DayNightWeatherPair";
	}
}
