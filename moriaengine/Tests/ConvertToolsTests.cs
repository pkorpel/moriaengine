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

	/// <summary>
	/// Pure logic test for issue #11's hex-header widening fix - no engine boot needed.
	/// Real Sphere resource/item ids are naturally unsigned 32-bit and can exceed
	/// Int32.MaxValue (e.g. events.def's 0xa2000eb0, confirmed a genuine Sphere id via a
	/// real cross-reference in the Erebor archive, not corruption). ThingDef/AbstractDef's
	/// numeric-defname parsing used to call ConvertTools.TryParseInt32 directly on such a
	/// value, which throws OverflowException and aborted loading the whole def entry. The
	/// fix parses via TryParseUInt32 instead, then reinterprets the same 32 bits via
	/// unchecked((int)...) rather than letting Convert.ToInt32 throw - this test proves
	/// that reinterpretation round-trips through ToString("x") with identical hex digits,
	/// which is the property the fix depends on (same bit pattern, only the sign changes).
	/// </summary>
	public class HexHeaderWideningTests {
		[Fact]
		public void OutOfInt32RangeHexParsesAsUInt32() {
			bool ok = ConvertTools.TryParseUInt32("0a2000eb0", out uint value);
			Assert.True(ok);
			Assert.Equal(0xa2000eb0u, value);
		}

		[Fact]
		public void UncheckedInt32ReinterpretationPreservesHexDigits() {
			ConvertTools.TryParseUInt32("0a2000eb0", out uint value);
			int reinterpreted = unchecked((int) value);
			Assert.True(reinterpreted < 0); // confirms this value genuinely exceeds Int32.MaxValue
			Assert.Equal("a2000eb0", reinterpreted.ToString("x"));
			Assert.Equal(value.ToString("x"), reinterpreted.ToString("x"));
		}

		[Fact]
		public void InRangeHexStillParsesNormally() {
			// a normal, small tiledata-range id should be completely unaffected by the fix
			bool ok = ConvertTools.TryParseUInt32("0x1f2d", out uint value);
			Assert.True(ok);
			Assert.Equal(0x1f2du, value);
			Assert.Equal(0x1f2d, unchecked((int) value));
		}
	}
}
