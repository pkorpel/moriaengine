using Xunit;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// #768 - pet-command speech parsing (Sphere-style "&lt;addressee&gt; &lt;command phrase&gt;") and the
	/// follow catch-up cadence. The old parser took the LAST word as the verb, so "all follow me",
	/// "all guard me" and "all stay" were silently ignored.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class PetCommandParserTests {
		[Theory]
		[InlineData("all come", "all", PetCommand.Come)]
		[InlineData("all follow me", "all", PetCommand.Come)]
		[InlineData("all guard me", "all", PetCommand.GuardMe)]
		[InlineData("all defend me", "all", PetCommand.GuardMe)]
		[InlineData("all guard", "all", PetCommand.Guard)]
		[InlineData("all friend", "all", PetCommand.Friend)]
		[InlineData("Rex drop", "Rex", PetCommand.Drop)]
		[InlineData("all stay", "all", PetCommand.Stay)]
		[InlineData("all stay here", "all", PetCommand.Stay)]
		[InlineData("all stop", "all", PetCommand.Stay)]
		[InlineData("all follow", "all", PetCommand.Follow)]
		[InlineData("all go", "all", PetCommand.Go)]
		[InlineData("all kill", "all", PetCommand.Kill)]
		[InlineData("all attack", "all", PetCommand.Kill)]
		[InlineData("all transfer", "all", PetCommand.Transfer)]
		[InlineData("all release", "all", PetCommand.Release)]
		[InlineData("all dismiss", "all", PetCommand.Release)]
		[InlineData("pets status", "pets", PetCommand.Status)]
		[InlineData("Rex follow me", "Rex", PetCommand.Come)]
		[InlineData("big bad wolf guard me", "big bad wolf", PetCommand.GuardMe)]
		[InlineData("  ALL   Follow   Me  ", "ALL", PetCommand.Come)]
		public void ParsesAddresseeAndCommand(string speech, string addressee, PetCommand command) {
			string parsedAddressee;
			PetCommand parsedCommand;
			Assert.True(PetCommandParser.TryParse(speech, out parsedAddressee, out parsedCommand));
			Assert.Equal(addressee, parsedAddressee);
			Assert.Equal(command, parsedCommand);
		}

		[Theory]
		[InlineData("hello there")]
		[InlineData("come")]          //no addressee
		[InlineData("follow me")]     //"follow" + "me" would need an addressee of "follow" - nothing before the phrase
		[InlineData("welcome")]       //ends with "come" but not as a word
		[InlineData("all comes")]
		[InlineData("")]
		[InlineData(null)]
		public void IgnoresNormalSpeech(string speech) {
			string addressee;
			PetCommand command;
			Assert.False(PetCommandParser.TryParse(speech, out addressee, out command));
		}

		[Theory]
		[InlineData("all", true)]
		[InlineData("PETS", true)]
		[InlineData("allie", false)]
		public void RecognizesAllPets(string addressee, bool expected) {
			Assert.Equal(expected, PetCommandParser.IsAllPets(addressee));
		}

		[Theory]
		[InlineData("rex", "Rex", true)]
		[InlineData("horse", "a horse", true)]   //creature type names often carry an article
		[InlineData("the horse", "horse", true)]
		[InlineData("horse", "a horse brown", false)]
		[InlineData("re", "Rex", false)]
		public void MatchesNamesIgnoringCaseAndArticles(string addressee, string name, bool expected) {
			Assert.Equal(expected, PetCommandParser.NameMatches(addressee, name));
		}

		[Fact]
		public void CaughtUpPetKeepsItsNormalCadence() {
			Assert.Equal(1.5, NPCAiTimer.CatchUpInterval(1, 1.5));
			Assert.Equal(1.5, NPCAiTimer.CatchUpInterval(0, 1.5));
		}

		[Fact]
		public void PetBehindWalksThenRunsFaster() {
			Assert.Equal(NPCAiTimer.CatchUpWalkSeconds, NPCAiTimer.CatchUpInterval(2, 1.5));
			Assert.Equal(NPCAiTimer.CatchUpWalkSeconds, NPCAiTimer.CatchUpInterval(NPCAiTimer.RunWhenBehindBy, 1.5));
			Assert.Equal(NPCAiTimer.CatchUpRunSeconds, NPCAiTimer.CatchUpInterval(NPCAiTimer.RunWhenBehindBy + 1, 1.5));
		}

		[Fact]
		public void CatchUpNeverSlowsAFastMover() {
			Assert.True(NPCAiTimer.CatchUpInterval(10, 0.3) <= 0.3);
			Assert.Equal(0.3, NPCAiTimer.CatchUpInterval(2, 0.3)); //walking catch-up (0.45s) would be slower - keep the normal 0.3s
		}
	}
}
