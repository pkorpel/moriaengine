using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.PluginTriggerDispatch;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class PluginTriggerDispatchGeneratorTests {
	private const string PluginStubs = """
namespace SteamEngine {
	public class Plugin {
		public virtual object Run(TriggerKey tk, ScriptArgs sa) {
			return null;
		}
	}
}
""";

	private sealed class GeneratorRunSnapshot {
		internal IReadOnlyList<string> GeneratedSources { get; init; } = Array.Empty<string>();
		internal IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
	}

	[Fact]
	public void ValidPlugin_GeneratesRunOverride() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public partial class ArenaPlugin : Plugin {
	public void On_Assign() {
	}

	public object On_DenyMove(bool denied) {
		return denied;
	}

	public object On_Login(ScriptArgs args) {
		return args;
	}
}
""";

		var run = RunGenerator(source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("public override object Run(global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa)", generated);
		Assert.Contains("switch (tk.LowercaseName)", generated);
		Assert.Contains("case \"assign\":", generated);
		Assert.Contains("this.On_Assign();", generated);
		Assert.Contains("case \"denymove\":", generated);
		Assert.Contains("this.On_DenyMove(global::System.Convert.ToBoolean(sa.Argv[0]));", generated);
		Assert.Contains("case \"login\":", generated);
		Assert.Contains("return this.On_Login(sa);", generated);
	}

	[Fact]
	public void NonPartialPlugin_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public class ArenaPlugin : Plugin {
	public void On_Assign() {
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN406");
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void NestedPlugin_WithNonPartialContainingType_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public static class Outer {
	public partial class ArenaPlugin : Plugin {
		public void On_Assign() {
		}
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN407");
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void DuplicateTriggerNames_ReportDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public partial class ArenaPlugin : Plugin {
	public void On_Assign() {
	}

	public void on_assign() {
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN408");
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string source) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes),
			CSharpSyntaxTree.ParseText(PluginStubs)
		};
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		var compilation = CSharpCompilation.Create(
			"GeneratorTestAssembly",
			syntaxTrees,
			GetMetadataReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new PluginTriggerDispatchGenerator());
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
