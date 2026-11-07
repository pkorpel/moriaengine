using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1262 - barks, conversations and the crier: a client hears a resident's bark and gives the crier news with the GM's
	/// .vyvolat; then the bark rules, a conversation, the crier and a new hero's news on the server.
	/// Server side: scripts/Tests/AmbientLifeTests.cs.</summary>
	[Collection(AmbientLifeCollection.Name)]
	public class AmbientLifeTests : LivingWorldTestBase {
		public AmbientLifeTests(GameServerPairFixture servers)
			: base(servers, "ambientlifetests", "AMBIENT") {
		}

		[Fact]
		public void TheCityTalks() {
			using (TestGameClient game = this.Login("Vyvolavac")) {
				this.Run(game, ".TestAmbientSetup", "AMBIENT_SETUP_DONE");

				this.Run(game, ".TestAmbientBark", "AMBIENT_BARK_DONE");
				this.AssertChecks("aPassingPlayerHearsABark");
				string bark = this.LastLine("AMBIENT_BARK:").Substring("AMBIENT_BARK: text=".Length).Trim();
				bool heard = false;
				DateTime until = DateTime.UtcNow + TimeSpan.FromSeconds(20);
				while (!heard && (DateTime.UtcNow < until)) {
					heard = game.ReadSystemMessage(until - DateTime.UtcNow).Trim() == bark;
				}
				Assert.True(heard, "The client never heard the bark '" + bark + "'.");

				game.SendSpeech(".vyvolat \"Na trhu se prodava zlato\"");
				this.Run(game, ".TestAmbientRules", "AMBIENT_RULES_DONE");
				this.AssertChecks("oneBarkPerVenueOnlyNearAPlayer", "aResidentBarksOnceInAWhile", "barksArePlainAscii", "aConversationIsFourLinesAndAPairRests",
					"oneTalkAtATimeInAVenue", "theGmsNewsIsCalled", "theCrierCallsEveryFewMinutes", "oldNewsGivesWayToMarketCries", "aNewHeroIsNewsOnce");

				this.Run(game, ".TestAmbientLive", "AMBIENT_LIVE_DONE");
				string state = "";
				for (int i = 0; (i < 20) && !state.Contains("barked=1"); i++) { //the scheduler ticks every 20 s
					System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
					this.Run(game, ".TestAmbientLiveState", "AMBIENT_LIVE_STATE");
					state = this.LastLine("AMBIENT_LIVE:");
				}
				this.AssertChecks("theSchedulerBarksByItself");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class AmbientLifeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "AmbientLife";
	}
}
