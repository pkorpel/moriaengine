using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.CompiledTriggerGroups;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class CompiledTriggerGroupGeneratorTests {
	private sealed class GeneratorRunSnapshot {
		internal IReadOnlyList<string> GeneratedSources { get; init; } = Array.Empty<string>();
		internal IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
	}

	[Fact]
	public void ValidCompiledTriggerGroup_GeneratesDispatch() {
		const string source = """
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public abstract class BaseTriggerGroup : CompiledTriggerGroup {
	public object on_Login(object self, ScriptArgs args) {
		return null;
	}
}

public partial class ArenaTriggerGroup : BaseTriggerGroup {
	public void On_DClick(object self, int value) {
	}
}
""";

		var run = RunGenerator(source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("partial class ArenaTriggerGroup", generated);
		Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase.Equals(tk.Name, \"DClick\")", generated);
		Assert.Contains("this.On_DClick(self, global::System.Convert.ToInt32(sa.Argv[0]));", generated);
		Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase.Equals(tk.Name, \"Login\")", generated);
		Assert.Contains("return this.on_Login(self, sa);", generated);
		Assert.DoesNotContain("InternalFirstGetDefname", generated);
	}

	[Fact]
	public void SealedAndAbstractTypes_AreNotGenerated() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public abstract class AbstractTriggerGroup : CompiledTriggerGroup {
}

public sealed class SealedTriggerGroup : CompiledTriggerGroup {
}
""";

		var run = RunGenerator(source);
		Assert.Empty(run.GeneratedSources);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void DuplicateTriggerNames_ReportDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public partial class ArenaTriggerGroup : CompiledTriggerGroup {
	public void on_DClick() {
	}

	public void On_DClick(int value) {
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN402");
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void NonPartialCompiledTriggerGroup_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public class ArenaTriggerGroup : CompiledTriggerGroup {
	public void on_DClick() {
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN403");
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string source) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes)
		};
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		var compilation = CSharpCompilation.Create(
			"GeneratorTestAssembly",
			syntaxTrees,
			GetMetadataReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new CompiledTriggerGroupsGenerator());
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
		var run = driver.GetRunResult();
		var single = run.Results.Single();
		if (single.Exception is not null) {
			throw single.Exception;
		}

		return new GeneratorRunSnapshot {
			GeneratedSources = single.GeneratedSources.Select(gs => gs.SourceText.ToString()).ToList(),
			Diagnostics = single.Diagnostics.ToList()
		};
	}

	private static IReadOnlyList<MetadataReference> GetMetadataReferences() {
		var refs = new List<MetadataReference>();
		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		var needed = new[] {
			"System.Private.CoreLib.dll",
			"System.Runtime.dll",
			"netstandard.dll",
			"System.Collections.dll",
			"System.Linq.dll",
			"System.Console.dll",
			"System.Text.RegularExpressions.dll"
		};
		foreach (var file in needed) {
			var full = Path.Combine(runtimeDir, file);
			if (File.Exists(full)) {
				refs.Add(MetadataReference.CreateFromFile(full));
			}
		}

		refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
		refs.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
		refs.Add(MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location));
		refs.Add(MetadataReference.CreateFromFile(typeof(SteamEngine.Persistence.SaveableClassAttribute).Assembly.Location));
		return refs;
	}
}
