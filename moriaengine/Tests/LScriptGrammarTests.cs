using System.IO;
using Xunit;
using SteamEngine.LScript;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// Regression coverage for a real LScript_strict.grammar bug: a bare newline with no
	/// leading whitespace/comment (COMEOL's second alternative, matching exactly 1 character)
	/// used to tie in match length against OTHERSYMBOLS (also exactly 1 character, since its
	/// character class didn't exclude \n/\r), and lost the tie because OTHERSYMBOLS was
	/// declared earlier in the grammar file. This broke any statement whose line ending wasn't
	/// followed by at least one more real newline - which in practice meant the LAST statement
	/// of the LAST block in a [function]/[dialog] section whenever the source had no trailing
	/// blank line before the next section header. Confirmed via git diff against the original
	/// pre-port commit that the affected real content (e.g. scripts/Basic/administration/Pages/
	/// pages.scp's [function pageMan]) was already broken before this port - not a regression,
	/// but real content that silently failed to load (dialogs rendering with no layout, etc.)
	/// for as long as this code has existed.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class LScriptGrammarTests {
		[Fact]
		public void IfBlockWithNoTrailingBlankLineCompiles() {
			var holder = new LScriptHolder();
			var ex = Record.Exception(() => LScriptMain.Compile(holder, new StringReader("if(1 == 0)\nendif\n"), 0));
			Assert.Null(ex);
		}

		[Fact]
		public void WhileBlockWithNoTrailingBlankLineCompiles() {
			var holder = new LScriptHolder();
			var ex = Record.Exception(() => LScriptMain.Compile(holder, new StringReader("while(1 == 0)\narg.x = 1\nendwhile\n"), 0));
			Assert.Null(ex);
		}

		[Fact]
		public void BareStatementWithExactlyOneTrailingNewlineCompiles() {
			// The minimal repro: a single real line, a single \n, then EOF - no second
			// newline to let COMEOL's [\n\r]+ win outright on length against OTHERSYMBOLS.
			var holder = new LScriptHolder();
			var ex = Record.Exception(() => LScriptMain.Compile(holder, new StringReader("arg.x = 1\n"), 0));
			Assert.Null(ex);
		}
	}
}
