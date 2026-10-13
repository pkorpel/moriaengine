using SteamEngine.Common;
using Xunit;

namespace SteamEngine.Tests {
	public enum ConvertToolsTestEnum {
		Alpha,
		Beta,
	}

	/// <summary>
	/// Pure logic test for ConvertTools.ConvertTo's enum branch (issue #104, Part B PR3) - no
	/// engine boot needed. Covers a real bug found while adding the `defaultLanguage` ini
	/// setting: the enum branch's first attempt (`Enum.ToObject(type, ToInt64(obj))`) only
	/// caught `InvalidCastException` as the signal to fall back to `Enum.Parse` on the string.
	/// For a symbolic string value like "Beta" (not a number), `ConvertTools.ToInt64` throws
	/// `FormatException` instead, which went uncaught and crashed. This matters for ANY
	/// enum-typed ini setting once a value has round-tripped through the ini file at least
	/// once: on first boot a missing key is written out verbatim as its default's
	/// `ToString()` (a symbolic name, not a number) via `IniFileSection.GetValue`, so every
	/// boot after that re-parses a symbolic string, not a number.
	/// </summary>
	public class ConvertToolsTests {
		[Fact]
		public void ConvertsSymbolicEnumStringName() {
			var result = ConvertTools.ConvertTo<ConvertToolsTestEnum>("Beta");
			Assert.Equal(ConvertToolsTestEnum.Beta, result);
		}

		[Fact]
		public void ConvertsNumericEnumString() {
			var result = ConvertTools.ConvertTo<ConvertToolsTestEnum>("1");
			Assert.Equal(ConvertToolsTestEnum.Beta, result);
		}
	}
}
