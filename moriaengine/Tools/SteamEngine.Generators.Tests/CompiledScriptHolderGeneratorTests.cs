using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.CompiledScriptHolders;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class CompiledScriptHolderGeneratorTests {
	private const string SharedStubs = "";

	private sealed class GeneratorRunSnapshot {
		internal IReadOnlyList<string> GeneratedSources { get; init; } = Array.Empty<string>();
		internal IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
	}

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
	public void DuplicateFunctionNames_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class CommandScriptsA {
	[SteamFunction("dupe")]
	public static int One(object self, int range) {
		return range;
	}
}

public static class CommandScriptsB {
	[SteamFunction("DUPE")]
	public static int Two(object self, int range) {
		return range;
	}
}
""";

		var run = RunGenerator(nameof(DuplicateFunctionNames_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN102");
		Assert.Contains("dupe", error.GetMessage(), System.StringComparison.OrdinalIgnoreCase);
		Assert.Empty(run.GeneratedSources);
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
	}

	[Fact]
	public void MissingSteamFunctionAttributeDeclaration_ReportsDiagnostic() {
		var run = RunGeneratorWithoutSharedStubs(
			nameof(MissingSteamFunctionAttributeDeclaration_ReportsDiagnostic),
			string.Empty);
		Assert.Equal(16, run.Diagnostics.Count(d => d.Id == "SEGEN900"));
		Assert.Contains(run.Diagnostics, d => d.GetMessage().Contains("SteamEngine.CompiledScripts.SteamFunctionAttribute", StringComparison.Ordinal));
		Assert.Equal(21, run.Diagnostics.Count(d => d.Id == "SEGEN901"));
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

		var compilation = CSharpCompilation.Create(
			"GeneratorTestAssembly",
			syntaxTrees,
			GetMetadataReferences(includeGeneratorAbstractionsReference),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new CompiledScriptHoldersGenerator());
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
		var run = driver.GetRunResult();
		var single = run.Results.Single();
		if (single.Exception != null) {
			throw single.Exception;
		}

		var snapshot = new GeneratorRunSnapshot {
			GeneratedSources = single.GeneratedSources.Select(gs => gs.SourceText.ToString()).ToList(),
			Diagnostics = single.Diagnostics.ToList()
		};
		DumpGeneratedSourcesIfRequested(scenarioName, single.GeneratedSources);
		return snapshot;
	}

	private static void DumpGeneratedSourcesIfRequested(string scenarioName, ImmutableArray<GeneratedSourceResult> sources) {
		var root = System.Environment.GetEnvironmentVariable("STEAMENGINE_GENERATOR_DUMP_DIR");
		if (string.IsNullOrWhiteSpace(root)) {
			return;
		}

		var scenarioDir = Path.Combine(root, scenarioName);
		Directory.CreateDirectory(scenarioDir);
		for (var i = 0; i < sources.Length; i++) {
			var hintName = sources[i].HintName.Replace('/', '_').Replace('\\', '_');
			var fileName = string.IsNullOrWhiteSpace(hintName) ? $"generated-{i}.g.cs" : hintName;
			File.WriteAllText(Path.Combine(scenarioDir, fileName), sources[i].SourceText.ToString());
		}
	}

	private static IEnumerable<MetadataReference> GetMetadataReferences(bool includeGeneratorAbstractionsReference) {
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly.Location);
		if (includeGeneratorAbstractionsReference) {
			yield return MetadataReference.CreateFromFile(typeof(SteamEngine.CompiledScripts.SteamFunctionAttribute).Assembly.Location);
		}
	}
}
