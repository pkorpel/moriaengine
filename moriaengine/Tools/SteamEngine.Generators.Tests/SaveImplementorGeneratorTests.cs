using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.SaveImplementors;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class SaveImplementorGeneratorTests {
	private const string SharedStubs = """
namespace SteamEngine.Common {
	public static class ConvertTools {
		public static bool ParseBoolean(string value) => false;
		public static byte ParseByte(string value) => 0;
		public static sbyte ParseSByte(string value) => 0;
		public static short ParseInt16(string value) => 0;
		public static ushort ParseUInt16(string value) => 0;
		public static int ParseInt32(string value) => 0;
		public static uint ParseUInt32(string value) => 0;
		public static long ParseInt64(string value) => 0L;
		public static ulong ParseUInt64(string value) => 0UL;
		public static float ParseSingle(string value) => 0f;
		public static double ParseDouble(string value) => 0d;
		public static decimal ParseDecimal(string value) => 0m;
	}
}
""";

	private sealed class GeneratorRunSnapshot {
		internal IReadOnlyList<string> GeneratedSources { get; init; } = Array.Empty<string>();
		internal IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
	}

	[Fact]
	public void ValidSaveableClass_ReportsNoErrorsAndGeneratesImplementor() {
		const string source = """
using SteamEngine;
using SteamEngine.Persistence;

namespace ScriptSandbox;

[SaveableClass]
public sealed class ArenaState {
	[SaveableData]
	public int Counter;

	[SaveableData]
	public object Payload { get; set; }

	[LoadingInitializer]
	public ArenaState() {
	}

	[LoadLine]
	public void LoadLine(string filename, int line, string name, string value) {
	}

	[LoadingFinalizer]
	public void LoadingFinished() {
	}

	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(ValidSaveableClass_ReportsNoErrorsAndGeneratesImplementor), source);
		Assert.Empty(run.Diagnostics);
		var generated = string.Join("\n\n", run.GeneratedSources);
		Assert.Contains("GeneratedSaveImplementor_ArenaState", generated);
		Assert.Contains("global::SteamEngine.Persistence.ObjectSaver.Load(propsLine_1.Value, this.DelayedLoad_Payload", generated);
		Assert.Contains("this.LoadSectionLines(input, loadedObject);", generated);
		Assert.Contains("loadedObject.LoadingFinished();", generated);
		Assert.Contains("castObjToSave.Save(writer);", generated);
	}

	[Fact]
	public void LoadSectionWithLoadingInitializer_ReportsDiagnostic() {
		const string source = """
using SteamEngine;
using SteamEngine.Persistence;

namespace ScriptSandbox;

[SaveableClass]
public sealed class ArenaState {
	[LoadingInitializer]
	public ArenaState() {
	}

	[LoadSection]
	public static ArenaState Load(PropsSection input) {
		return new ArenaState();
	}

	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(LoadSectionWithLoadingInitializer_ReportsDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN306");
		Assert.Contains("LoadSection", diagnostic.GetMessage());
	}

	[Fact]
	public void LoadSectionFactory_GeneratesLoadSectionFactoryInvocation() {
		const string source = """
using SteamEngine;
using SteamEngine.Persistence;

namespace ScriptSandbox;

public class ArenaBase {
	[LoadSection]
	public static object Load(PropsSection input) {
		return new ArenaState();
	}
}

[SaveableClass]
public sealed class ArenaState : ArenaBase {
	[SaveableData]
	public int Counter;

	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(LoadSectionFactory_GeneratesLoadSectionFactoryInvocation), source);
		Assert.Empty(run.Diagnostics);
		var generated = string.Join("\n\n", run.GeneratedSources);
		Assert.Contains("global::ScriptSandbox.ArenaState loadedObject = (global::ScriptSandbox.ArenaState) global::ScriptSandbox.ArenaBase.Load(input);", generated);
		Assert.DoesNotContain("TryPopPropsLine(\"Counter\")", generated);
		Assert.Contains("castObjToSave.Save(writer);", generated);
	}

	[Fact]
	public void MissingInitializerAndLoadSection_ReportsDiagnostic() {
		const string source = """
using SteamEngine.Persistence;

namespace ScriptSandbox;

[SaveableClass]
public sealed class ArenaState {
	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(MissingInitializerAndLoadSection_ReportsDiagnostic), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN305");
	}

	[Fact]
	public void SaveableDataPrivateField_ReportsDiagnostic() {
		const string source = """
using SteamEngine;
using SteamEngine.Persistence;

namespace ScriptSandbox;

[SaveableClass]
public sealed class ArenaState {
	[SaveableData]
	private int counter;

	[LoadingInitializer]
	public ArenaState() {
	}

	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(SaveableDataPrivateField_ReportsDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN905");
		Assert.Contains("counter", diagnostic.GetMessage());
	}

	[Fact]
	public void NoSaveableDataField_Supported() {
		const string source = """
using SteamEngine;
using SteamEngine.Persistence;

namespace ScriptSandbox;

[SaveableClass]
public sealed class ArenaState {

	[LoadingInitializer]
	public ArenaState() {
	}

	[Save]
	public void Save(SaveStream output) {
	}
}
""";

		var run = RunGenerator(nameof(NoSaveableDataField_Supported), source);
		Assert.Empty(run.Diagnostics);
		var generated = string.Join(Environment.NewLine, run.GeneratedSources);
		Assert.Contains("ArenaState()", generated);
		Assert.Contains(".Save(writer)", generated);
	}

	[Fact]
	public void DuplicateSaveableTypeNames_CaseInsensitive_ReportDiagnostic() {
		const string source = """
using SteamEngine.Persistence;

namespace FirstNamespace {
	[SaveableClass]
	public sealed class SharedName {
		[LoadingInitializer]
		public SharedName() {
		}

		[Save]
		public void Save(SaveStream output) {
		}
	}
}

namespace SecondNamespace {
	[SaveableClass]
	public sealed class sharedname {
		[LoadingInitializer]
		public sharedname() {
		}

		[Save]
		public void Save(SaveStream output) {
		}
	}
}
""";

		var run = RunGenerator(nameof(DuplicateSaveableTypeNames_CaseInsensitive_ReportDiagnostic), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN309");
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string scenarioName, string source) {
		var syntaxTrees = new List<SyntaxTree>();
		syntaxTrees.Add(CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes));
		if (!string.IsNullOrEmpty(SharedStubs)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(SharedStubs));
		}
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		var compilation = CSharpCompilation.Create(
			"GeneratorTestAssembly",
			syntaxTrees,
			GetMetadataReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new SaveImplementorsGenerator());
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
		var root = Environment.GetEnvironmentVariable("STEAMENGINE_GENERATOR_DUMP_DIR");
		if (string.IsNullOrWhiteSpace(root)) {
			return;
		}

		Directory.CreateDirectory(root);
		var safeName = scenarioName.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
		for (var i = 0; i < sources.Length; i++) {
			var path = Path.Combine(root, safeName + "." + i.ToString() + ".g.cs");
			File.WriteAllText(path, sources[i].SourceText.ToString());
		}
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
