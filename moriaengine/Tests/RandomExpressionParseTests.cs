using System.IO;
using Xunit;
using SteamEngine.LScript;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// #763 - c_sheep_woolly crashed the first time it was created because its converted
	/// "Color = { colors_white 9 color_gray_lt 1 }" never parsed. In LScript "name value" is an
	/// assignment (WhiteSpaceAssigner), so a whitespace-separated random list that starts with an
	/// identifier collapses into a single element and the RandomExpression rule fails. The
	/// converter now writes these lists comma-separated (ConvertedTemplateDef.FixRandomExpression
	/// applied to Color and model). FieldValue resolves them as "return(value)", same as here.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class RandomExpressionParseTests {
		private static System.Exception CompileFieldValue(string value) {
			var holder = new LScriptHolder();
			return Record.Exception(() => LScriptMain.Compile(holder, new StringReader("return(" + value + ")\n"), 0));
		}

		[Fact]
		public void WhitespaceListStartingWithAnIdentifierDoesNotParse() {
			Assert.NotNull(CompileFieldValue("{ colors_white 9 color_gray_lt 1 }"));
		}

		[Fact]
		public void CommaSeparatedWeightedIdentifierListParses() {
			Assert.Null(CompileFieldValue("{ colors_white, 9, color_gray_lt, 1 }"));
		}

		[Fact]
		public void CommaSeparatedModelListParses() {
			Assert.Null(CompileFieldValue("{ i_chest_metal, 1, i_chest_metal_brass, 1, I_CHEST_WOODEN_BRASS, 1}"));
		}

		[Fact]
		public void CommaSeparatedNumericRangeStillParses() {
			Assert.Null(CompileFieldValue("{0539, 054a}"));
		}
	}
}
