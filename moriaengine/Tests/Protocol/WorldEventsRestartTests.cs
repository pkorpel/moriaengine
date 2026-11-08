using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1307 - world events survive a real save and restart: running events with their NPCs, heroes, guests and rounds, a
	/// city's price outcome, the realm's bonus, a landmark and the criers' news. Server side: scripts/Tests/WorldEventsRestartTests.cs.</summary>
	[Collection(WorldEventsRestartCollection.Name)]
	public class WorldEventsRestartTests {
		private const string Account = "wevrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void WorldEventsSurviveARealRestart() {
			string ready;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Events Saver")) {
					string round = Ask(fixture1, client, ".TestWorldEventsRecordRoundTrip", "WEVR_ROUNDTRIP:");
					Assert.Contains("WEVR_ROUNDTRIP: karavana=True najezd=True majaky=True pozar=True end", round);
					ready = Ask(fixture1, client, ".TestWorldEventsRestartPrepare", "WEVR_READY:");
					Assert.DoesNotContain("troll=none", ready);
					Assert.DoesNotContain("feast=none", ready);
					Assert.Contains("price=-10 ", ready);
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
					System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wevr-boot1.txt"), fixture1.GameOutputSnapshot());
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Events Loader")) {
					string check = Ask(fixture2, client, ".TestWorldEventsRestartCheck", "WEVR_CHECK:");
					System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wevr-boot2.txt"), fixture2.GameOutputSnapshot());
					Assert.Contains("running=2 ", check);
					foreach (string key in new[] { "troll=", "beast=", "feast=", "host=", "animal=", "landmark=" }) {
						Assert.Equal(Field(ready, key), Field(check, key));
					}
					Assert.Contains("heroKills=2 ", check);
					Assert.Contains("guest=Events_Saver catches=1 rounds=2 ", check);
					Assert.Contains("price=-10 bonus=True news=True ", check);
					Assert.Contains("swept=0 ", check);
					Assert.True(int.Parse(Field(check, "serial=")) >= int.Parse(Field(ready, "serial=")), check);
					Assert.Contains("timer=True ticks=True ", check);
				}
			}
		}

		private static string Field(string line, string key) {
			int idx = line.IndexOf(" " + key, StringComparison.Ordinal);
			Assert.True(idx >= 0, "no '" + key + "' in: " + line);
			int start = idx + key.Length + 1;
			int end = line.IndexOf(' ', start);
			return (end < 0) ? line.Substring(start).Trim() : line.Substring(start, end - start);
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = RedirectPort();
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static int RedirectPort() {
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

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech + "\n" + fixture.GameOutputSnapshot());
			string output = fixture.GameOutputSnapshot();
			int at = output.LastIndexOf(marker, StringComparison.Ordinal);
			int lineEnd = output.IndexOf('\n', at);
			return output.Substring(at, ((lineEnd < 0) ? output.Length : lineEnd) - at);
		}
	}

	[CollectionDefinition(Name)]
	public class WorldEventsRestartCollection {
		public const string Name = "WorldEventsRestart";
	}
}
