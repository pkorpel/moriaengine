using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.CompiledScriptHolders;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class CompiledScriptHolderGeneratorTests {
	private const string SharedStubs = "";

	[Fact]
	public void PublicStaticSteamFunction_GeneratesCompiledHolder() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class CommandScripts {
	[SteamFunction]
	public static int Reveal(object self, int range) {
		return range;
	}
}
""";

		var run = RunGenerator(nameof(PublicStaticSteamFunction_GeneratesCompiledHolder), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"namespace SteamEngine.CompiledScripts",
			": base(\"Reveal\", null)",
			"return global::ScriptSandbox.CommandScripts.Reveal(self, global::System.Convert.ToInt32(argv[0]));");
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void RenamedSteamFunction_WithScriptArgsParam_GeneratesRun() {
		const string source = """
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class CommandScripts {
	[SteamFunction("cast_spell")]
	public static void Cast(object self, ScriptArgs args) {
	}
}
""";

		var run = RunGenerator(nameof(RenamedSteamFunction_WithScriptArgsParam_GeneratesRun), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			": base(\"cast_spell\", null)",
			"global::ScriptSandbox.CommandScripts.Cast(self, sa);",
			"return null;");
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void SteamFunctionBoolParameter_UsesBooleanConversion() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class CommandScripts {
	[SteamFunction]
	public static int Reveal(object self, bool enabled) {
		return enabled ? 1 : 0;
	}
}
""";

		var run = RunGenerator(nameof(SteamFunctionBoolParameter_UsesBooleanConversion), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated, "global::System.Convert.ToBoolean(argv[0])");
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void SteamFunctionEnumParameter_UsesItsUnderlyingType() {
		const string source = """
using SteamEngine.CompiledScripts;
namespace ScriptSandbox;
public enum Choice : ulong { None }
public static class Commands {
	[SteamFunction] public static void Choose(object self, Choice choice, System.DateTime time) { }
}
""";
		var run = RunGenerator(nameof(SteamFunctionEnumParameter_UsesItsUnderlyingType), source);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("(global::ScriptSandbox.Choice)global::System.Convert.ToUInt64(argv[0])", generated);
		Assert.Contains("global::System.Convert.ToDateTime(argv[1])", generated);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		Assert.DoesNotContain(run.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void NonStaticSteamFunction_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public sealed class CommandScripts {
	[SteamFunction]
	public int Reveal(object self, int range) {
		return range;
	}
}
""";

		var run = RunGenerator(nameof(NonStaticSteamFunction_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN101");
		Assert.Contains("must be public static", error.GetMessage());
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void InvalidMethodsAndNames_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts;
public class Commands {
	const string Alias = "a";
	[SteamFunction] public int First(object self) => 0; // Invalid: not static.
	[SteamFunction(Alias)] public int Second(object self) => 0; // Invalid: not static and name is not a literal.
	[SteamFunction] private static int Fourth(object self) => 0; // Invalid: not public.
	[SteamFunction("dupe")] public static int One(object self) => 1; // Invalid: name conflicts with Two.
	[SteamFunction("DUPE")] public static int Two(object self) => 2; // Invalid: name conflicts case-insensitively with One.
	[SteamFunction("foo-bar")] public static int Three(object self) => 3; // Invalid: hyphen.
	[SteamFunction("9start")] public static int Digit(object self) => 0; // Invalid: starts with a digit.
	[SteamFunction("")] public static int Empty(object self) => 0; // Invalid: empty name.
	[SteamFunction("has space")] public static int Space(object self) => 0; // Invalid: whitespace.
	[SteamFunction("na\u00efve")] public static int Unicode(object self) => 0; // Invalid: non-ASCII letter.
	[SteamFunction] public static int \u00c9cho(object self) => 0; // Invalid: default name is non-ASCII.
	[SteamFunction] public static int _Valid123(object self) => 0; // Valid: underscore, letters, and digits.
	[SteamFunction("foo_bar")] public static int Five(object self) => 5; // Valid: letters and underscore.
	[SteamFunction("__ENCODED_foo_002dbar")] public static int Six(object self) => 6; // Valid: the encoding prefix is escaped.
}
""";
		var run = RunGenerator(nameof(InvalidMethodsAndNames_ReportDiagnostics), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN101" && d.GetMessage().Contains("First"));
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN101" && d.GetMessage().Contains("Second"));
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN101" && d.GetMessage().Contains("Fourth"));
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN103");
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN102");
		foreach (var invalidName in new[] { "foo-bar", "9start", "", "has space", "na\u00efve", "\u00c9cho" }) {
			Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN104" && d.GetMessage().Contains("'" + invalidName + "'"));
		}
		Assert.Equal(3, run.GeneratedSources.Count);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated, ": base(\"_Valid123\",", ": base(\"foo_bar\",", ": base(\"__ENCODED_foo_002dbar\",");
		Assert.DoesNotContain(run.GeneratedSources, source => source.Contains(": base(\"dupe\",", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(run.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void NonLiteralSteamFunctionName_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class CommandScripts {
	private const string Alias = "cast_spell";

	[SteamFunction(Alias)]
	public static int Cast(object self, int range) {
		return range;
	}
}
""";

		var run = RunGenerator(nameof(NonLiteralSteamFunctionName_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN103");
		Assert.Contains("Alias", error.GetMessage(), StringComparison.Ordinal);
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void ShadowedSteamFunction_ReportsWarning() {
		const string source = """
namespace Shadow {
	public sealed class SteamFunctionAttribute : System.Attribute { }
	public static class Commands {
		[SteamFunction]
		public static int Reveal(object self) => 1;
	}
}
""";
		var run = RunGenerator(nameof(ShadowedSteamFunction_ReportsWarning), source);
		var warning = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN902");
		Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
		Assert.Contains("global::Shadow.SteamFunctionAttribute", warning.GetMessage());
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void MissingSteamFunctionAttributeDeclaration_ReportsDiagnostic() {
		var run = RunGeneratorWithoutSharedStubs(
			nameof(MissingSteamFunctionAttributeDeclaration_ReportsDiagnostic),
			string.Empty);
		Assert.Equal(16, run.Diagnostics.Count(d => d.Id == "SEGEN900"));
		Assert.Contains(run.Diagnostics, d => d.GetMessage().Contains("SteamEngine.CompiledScripts.SteamFunctionAttribute", StringComparison.Ordinal));
		Assert.Equal(22, run.Diagnostics.Count(d => d.Id == "SEGEN901"));
		Assert.Empty(run.GeneratedSources);
	}

	private static string JoinGeneratedSources(GeneratorRunSnapshot run) {
		return string.Join("\n\n", run.GeneratedSources);
	}

	private static void AssertGeneratedContains(string generated, params string[] expectedSnippets) {
		foreach (var snippet in expectedSnippets) {
			Assert.Contains(snippet, generated);
		}
	}

	private static GeneratorRunSnapshot RunGenerator(string scenarioName, string source) {
		return RunGeneratorInternal(
			scenarioName,
			source,
			includeSharedStubs: true,
			includeGeneratorAbstractionsReference: true);
	}

	private static GeneratorRunSnapshot RunGeneratorWithoutSharedStubs(string scenarioName, string source) {
		return RunGeneratorInternal(
			scenarioName,
			source,
			includeSharedStubs: false,
			includeGeneratorAbstractionsReference: false);
	}

	private static GeneratorRunSnapshot RunGeneratorInternal(
		string scenarioName,
		string source,
		bool includeSharedStubs,
		bool includeGeneratorAbstractionsReference) {
		var syntaxTrees = new List<SyntaxTree>();
		if (includeSharedStubs) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes));
			if (!string.IsNullOrEmpty(SharedStubs)) {
				syntaxTrees.Add(CSharpSyntaxTree.ParseText(SharedStubs));
			}
		}
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return GeneratorTestRunner.Run(new CompiledScriptHoldersGenerator(), syntaxTrees,
			GetMetadataReferences(includeGeneratorAbstractionsReference), scenarioName);
	}

	private static IEnumerable<MetadataReference> GetMetadataReferences(bool includeGeneratorAbstractionsReference) {
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly.Location);
		yield return MetadataReference.CreateFromFile(System.IO.Path.Combine(
			System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll"));
		yield return MetadataReference.CreateFromFile(System.IO.Path.Combine(
			System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"));
		if (includeGeneratorAbstractionsReference) {
			yield return MetadataReference.CreateFromFile(typeof(SteamEngine.CompiledScripts.SteamFunctionAttribute).Assembly.Location);
		}
	}
}
