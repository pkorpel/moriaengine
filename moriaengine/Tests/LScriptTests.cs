using Xunit;
using SteamEngine.LScript;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	[Collection(EngineCollection.Name)]
	public class LScriptTests {
		[Fact]
		public void TrivialScript() {
			var result = LScriptMain.TryRunSnippet(null, @"return ""abc""");
			Assert.Equal("abc", result);
		}

		[Fact]
		public void RecursionRecoveryTest() {
			var expressions = new[] {
				"Srdcem Mordoru je temne mesto Barad -Dur z nehoz vladne Marghul.  Zde vladne absolutni disciplina a jakekoliv neuposechnuti rozkazu je odmeneno smrti..",
				"a odplula na zapad, ale stale jsou to bytosti, ktere budou ovlivnovat budoucnost.'."
			};

			foreach (var e in expressions) {
				var returnExpression = "return " + e;
				var result = LScriptMain.TryRunSnippet(null, returnExpression);
				Assert.Null(result);
			}
		}
	}
}
