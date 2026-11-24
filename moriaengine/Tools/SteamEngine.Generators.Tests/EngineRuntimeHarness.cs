using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Shared by the *RuntimeTests classes: runs a generator on sandbox source compiled against the
// real GameCore and Scripts assemblies, then emits and loads the result so the generated code
// can be called through the engine's own interfaces.
internal static class EngineRuntimeHarness {
	internal static Assembly Compile(IIncrementalGenerator generator, string source, string? scenarioName = null) {
		return Emit(Run(generator, source, scenarioName));
	}

	// scenarioName: dumps the generated sources when STEAMENGINE_GENERATOR_DUMP_DIR is set.
	internal static GeneratorRunSnapshot Run(IIncrementalGenerator generator, string source, string? scenarioName = null) {
		var run = GeneratorTestRunner.Run(
			generator,
			new[] { CSharpSyntaxTree.ParseText(source) },
			GetEngineReferences(),
			scenarioName);
		var errors = run.Diagnostics.Concat(run.DriverDiagnostics).Concat(run.OutputCompilation.GetDiagnostics())
			.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		Assert.Empty(errors);
		return run;
	}

	internal static Assembly Emit(GeneratorRunSnapshot run) {
		using var assemblyStream = new MemoryStream();
		var emit = run.OutputCompilation.Emit(assemblyStream);
		Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
		return Assembly.Load(assemblyStream.ToArray());
	}

	internal static object Create(Assembly assembly, string typeName) {
		return Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
	}

	// One instance of every concrete T in the emitted assembly (the generated implementors).
	internal static T[] CreateAll<T>(Assembly assembly) {
		return assembly.GetTypes()
			.Where(static type => !type.IsAbstract && typeof(T).IsAssignableFrom(type))
			.Select(static type => (T)Activator.CreateInstance(type)!)
			.ToArray();
	}

	// The framework plus every assembly next to the test (GameCore, Common, Scripts, ...).
	private static IEnumerable<MetadataReference> GetEngineReferences() {
		var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		var local = Directory.GetFiles(AppContext.BaseDirectory, "*.dll");
		return trusted.Concat(local)
			.Where(static path => path.Length > 0)
			.GroupBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
			.Select(static group => MetadataReference.CreateFromFile(group.First()));
	}
}
