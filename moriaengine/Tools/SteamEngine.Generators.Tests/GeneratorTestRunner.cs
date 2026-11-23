using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SteamEngine.Generators.Tests;

internal sealed class GeneratorRunSnapshot {
	internal IReadOnlyList<string> GeneratedSources { get; init; } = Array.Empty<string>();
	internal IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
	internal IReadOnlyList<Diagnostic> DriverDiagnostics { get; init; } = Array.Empty<Diagnostic>();
	internal required Compilation OutputCompilation { get; init; }
}

internal static class GeneratorTestRunner {
	internal static GeneratorRunSnapshot Run(
		IIncrementalGenerator generator,
		IEnumerable<SyntaxTree> syntaxTrees,
		IEnumerable<MetadataReference> references,
		string? scenarioName = null,
		bool flatDump = false) {
		var compilation = CSharpCompilation.Create(
			"GeneratorTestAssembly",
			syntaxTrees,
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
		var run = driver.GetRunResult();
		var single = run.Results.Single();
		if (single.Exception is not null) {
			throw single.Exception;
		}

		if (scenarioName is not null) {
			var root = Environment.GetEnvironmentVariable("STEAMENGINE_GENERATOR_DUMP_DIR");
			if (!string.IsNullOrWhiteSpace(root)) {
				if (flatDump) {
					Directory.CreateDirectory(root);
					var safeName = scenarioName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
					for (var i = 0; i < single.GeneratedSources.Length; i++) {
						File.WriteAllText(Path.Combine(root, safeName + "." + i.ToString() + ".g.cs"), single.GeneratedSources[i].SourceText.ToString());
					}
				} else {
					var scenarioDir = Path.Combine(root, scenarioName);
					Directory.CreateDirectory(scenarioDir);
					for (var i = 0; i < single.GeneratedSources.Length; i++) {
						var source = single.GeneratedSources[i];
						var hintName = source.HintName.Replace('/', '_').Replace('\\', '_');
						var fileName = string.IsNullOrWhiteSpace(hintName) ? $"generated-{i}.g.cs" : hintName;
						File.WriteAllText(Path.Combine(scenarioDir, fileName), source.SourceText.ToString());
					}
				}
			}
		}

		return new GeneratorRunSnapshot {
			GeneratedSources = single.GeneratedSources.Select(gs => gs.SourceText.ToString()).ToList(),
			Diagnostics = single.Diagnostics.ToList(),
			DriverDiagnostics = run.Diagnostics.ToList(),
			OutputCompilation = outputCompilation
		};
	}

	internal static IReadOnlyList<MetadataReference> GetRuntimeReferences() {
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
