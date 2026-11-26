using System;
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

		[Fact]
		public void FrameworkOverloadsResolveAsBefore() {
			//#1551 - LScript picks .NET overloads by name at run time, and .NET 9/10 added some for the calls scripts make:
			//TimeSpan.FromSeconds(long) and FromMilliseconds(long), and params ReadOnlySpan<T> forms of Format, Concat, Combine.
			//The expected types are what .NET 8 returned: a new overload would change the type or the value.
			string[,] cases = {
				{ "TimeSpan:00:00:05", "System.TimeSpan.FromSeconds(5)" },
				{ "TimeSpan:00:00:02.5000000", "System.TimeSpan.FromSeconds(2.5)" },
				{ "TimeSpan:00:00:01.5000000", "System.TimeSpan.FromMilliseconds(1500)" },
				{ "TimeSpan:00:00:00.0005000", "System.TimeSpan.FromMilliseconds(0.5)" },
				{ "Double:2", "System.Math.Round(2.5)" },
				{ "Int32:7", "System.Math.Max(3, 7)" },
				{ "Double:1.5", "System.Math.Max(1.5, 0.0)" },
				{ "Int32:2", "System.Math.Min(4, 2)" },
				{ "Double:3", "System.math.abs(-3)" },
				{ "Double:4", "System.Math.Sqrt(16)" },
				{ "String:1-2", "System.String.Format(\"{0}-{1}\", 1, 2)" },
				{ "String:a", "System.String.Format(\"{0}\", \"a\")" },
				{ "String:ab", "System.String.Concat(\"a\", \"b\")" },
				{ "String:" + System.IO.Path.Combine("a", "b"), "System.IO.Path.Combine(\"a\", \"b\")" },
			};
			var expected = new System.Text.StringBuilder();
			var actual = new System.Text.StringBuilder();
			for (int i = 0; i < cases.GetLength(0); i++) {
				expected.Append(cases[i, 1]).Append(" -> ").AppendLine(cases[i, 0]);
				actual.Append(cases[i, 1]).Append(" -> ").AppendLine(Run(cases[i, 1]));
			}
			Assert.Equal(expected.ToString(), actual.ToString());
		}

		private static string Run(string expression) {
			object result = LScriptMain.TryRunSnippet(null, "return " + expression);
			return (result == null) ? "null" : result.GetType().Name + ":" + Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
