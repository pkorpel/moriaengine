using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamEngine.LScript;

namespace SteamEngine.Tests {
	[TestClass]
	public class LScriptTests {
		[TestMethod]
		public void TrivialScript() {
			var result = LScriptMain.TryRunSnippet(null, @"return ""abc""");
			Assert.AreEqual("abc", result);
		}

		[TestMethod]
		public void RecursionRecoveryTest() {
			var expressions = new[] {
				"Srdcem Mordoru je temne mesto Barad -Dur z nehoz vladne Marghul.  Zde vladne absolutni disciplina a jakekoliv neuposechnuti rozkazu je odmeneno smrti..",
				"a odplula na zapad, ale stale jsou to bytosti, ktere budou ovlivnovat budoucnost.'."
			};

			foreach (var e in expressions) {
				var returnExpression = "return " + e;
				var result = LScriptMain.TryRunSnippet(null, returnExpression);
				Assert.AreEqual(null, result);
			}
		}
	}
}
