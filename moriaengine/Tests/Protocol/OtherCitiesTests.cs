using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1304 - the living world in the other cities: a client meets a merchant of Dol Amroth, who greets a stranger as
	/// Dol Amroth does; then names, news, factions, events and festivals on the server. Server side: scripts/Tests/OtherCitiesTests.cs.</summary>
	[Collection(OtherCitiesCollection.Name)]
	public class OtherCitiesTests : LivingWorldTestBase {
		private static readonly string[] dolAmrothGreetings = {
			"Vitej v Dol Amrothu, pod labutim praporem.", "More je dnes klidne. Co te k nam privadi?", "Knize ma rad hosty, kteri vedi, jak se chovat."
		};

		public OtherCitiesTests(GameServerPairFixture servers)
			: base(servers, "othercitiestests", "OTHERCITIES") {
		}

		[Fact]
		public void TheOtherCitiesHaveTheirOwnFolkAndTalk() {
			using (TestGameClient game = this.Login("Poutnik k mori")) {
				this.Run(game, ".TestOtherCitiesSetup", "OTHERCITIES_SETUP:");
				uint merchant = (uint) Number(this.LastLine("OTHERCITIES_SETUP:"), "merchant");

				game.SendDoubleClick(merchant);
				var talk = game.ReadGumpDialogWithStrings(Wait);
				string texts = string.Join("|", talk.Strings);
				Assert.True(System.Array.Exists(dolAmrothGreetings, g => texts.Contains(g)), texts);
				game.SendGumpResponse(talk.GumpUid, 0);

				this.Run(game, ".TestOtherCitiesChain", "OTHERCITIES_CHAIN_DONE");
				this.AssertChecks("namesByFolk", "greetingsAndNewsOfTheirOwn", "factionsPerCity", "eventsInTheOtherCities", "festivalsWhereTheyFit", "aFestivalHostCanBeReached");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class OtherCitiesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "OtherCities";
	}
}
