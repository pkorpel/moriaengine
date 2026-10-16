using SteamEngine;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Pure logic coverage for the GM ".go" multi-word-argument bug reported directly by the
	/// user: ".go edoras" (one word) worked, but ".go jamy smrti" (two words) failed with
	/// "jamy is not a function". Root cause: GM-plevel commands run through the full LScript
	/// parser (GameCore/Commands.cs InvokeCommand), whose grammar lets a bare word followed by
	/// whitespace start its own function-call-without-parens (LScript_strict.grammar's
	/// SimpleExpression -> WhiteSpaceAssigner -> ArgsList). That binds the second word to the
	/// FIRST word's own argument list rather than leaving it as a sibling argument to "go" -
	/// "go jamy smrti" parses as go(jamy(smrti)), not go(jamy, smrti) or go("jamy smrti").
	///
	/// Fix: Commands.InvokeCommand now always routes command names in
	/// SimpleCommandParser.alwaysRawArgCommands (currently just "go") through the regex-based
	/// SimpleCommandParser, regardless of plevel - the same raw-rest-of-line parsing already
	/// used for below-GM-plevel accounts, which takes everything after the command name as one
	/// untouched string with no further splitting at all.
	/// </summary>
	public class CommandDispatchTests {
		[Fact]
		public void GoWithTwoWordArgumentIsRoutedToRawArgParser() {
			Assert.True(SimpleCommandParser.IsAlwaysRawArgCommand("go jamy smrti"));
		}

		[Fact]
		public void GoWithOneWordArgumentIsRoutedToRawArgParser() {
			Assert.True(SimpleCommandParser.IsAlwaysRawArgCommand("go edoras"));
		}

		[Fact]
		public void GoCommandNameMatchIsCaseInsensitive() {
			Assert.True(SimpleCommandParser.IsAlwaysRawArgCommand("GO Edoras"));
		}

		[Fact]
		public void UnrelatedCommandIsNotRoutedToRawArgParser() {
			Assert.False(SimpleCommandParser.IsAlwaysRawArgCommand("kill"));
		}
	}
}
