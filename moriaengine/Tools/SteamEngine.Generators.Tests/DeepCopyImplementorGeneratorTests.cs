using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.DeepCopyImplementors;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class DeepCopyImplementorGeneratorTests {
	private const string SharedStubs = "";

	[Fact]
	public void ValidDeepCopyableClass_ReportsNoErrors() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	public int Charges;

	[CopyableData]
	public object[] Payload { get; set; }

	[DeepCopyImplementation]
	public SpellState(SpellState copyFrom) {
	}
}
""";

		var run = RunGenerator(nameof(ValidDeepCopyableClass_ReportsNoErrors), source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = string.Join("\n\n", run.GeneratedSources);
		Assert.Contains("GeneratedDeepCopyImplementor_SpellState", generated);
		Assert.Contains("global::SteamEngine.DeepCopyFactory.GetCopyDelayed(copyFromConverted.Payload, this.DelayedCopy_Payload, copy)", generated);
		Assert.Contains("copy.Charges = copyFromConverted.Charges;", generated);
		Assert.Contains("new global::ScriptSandbox.SpellState(copyFromConverted)", generated);
	}


	[Fact]
	public void ValidDeepCopyableClass_NoData() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[DeepCopyImplementation]
	public SpellState(SpellState copyFrom) {
	}
}
""";

		var run = RunGenerator(nameof(ValidDeepCopyableClass_NoData), source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = string.Join("\n\n", run.GeneratedSources);
		Assert.Contains("GeneratedDeepCopyImplementor_SpellState", generated);
		Assert.Contains("new global::ScriptSandbox.SpellState(copyFromConverted)", generated);
	}

	[Fact]
	public void EmptyClass_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	
}
""";

		var run = RunGenerator(nameof(EmptyClass_ReportsDiagnostic), source);
		Assert.Single(run.Diagnostics, d => d.Id == "SEGEN205");
	}

	[Fact]
	public void CopyableDataPrivateField_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	private int charges;

	[DeepCopyImplementation]
	public SpellState() {
	}
}
""";

		var run = RunGenerator(nameof(CopyableDataPrivateField_ReportsDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN905");
		Assert.Contains("charges", diagnostic.GetMessage());
	}

	[Fact]
	public void MissingDeepCopyImplementation_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	public int Charges;
}
""";

		var run = RunGenerator(nameof(MissingDeepCopyImplementation_ReportsDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN205");
		Assert.Contains("SpellState", diagnostic.GetMessage());
	}

	[Fact]
	public void MultipleDeepCopyImplementations_ReportDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[DeepCopyImplementation]
	public SpellState() {
	}

	[DeepCopyImplementation]
	public static SpellState Create(SpellState original) {
		return original;
	}
}
""";

		var run = RunGenerator(nameof(MultipleDeepCopyImplementations_ReportDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN904");
		Assert.Contains("SpellState", diagnostic.GetMessage());
	}

	[Fact]
	public void CopyableDataPropertyWithoutPublicSetter_ReportsDiagnostic() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	public object[] Payload { get; }

	[DeepCopyImplementation]
	public SpellState() {
	}
}
""";

		var run = RunGenerator(nameof(CopyableDataPropertyWithoutPublicSetter_ReportsDiagnostic), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN905");
		Assert.Contains("Payload", diagnostic.GetMessage());
	}

	[Fact]
	public void ShadowedDeepCopyableAttribute_ReportsWarning() {
		const string source = """
using Shadowing;

namespace Shadowing {
	public sealed class DeepCopyableClassAttribute : System.Attribute { }
}

namespace ScriptSandbox;

[DeepCopyableClass]
public sealed class SpellState {
}
""";

		var run = RunGenerator(nameof(ShadowedDeepCopyableAttribute_ReportsWarning), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN902");
		Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
	}

	[Fact]
	public void MissingCanonicalEngineTypes_ReportDiagnostics() {
		const string source = """
namespace ScriptSandbox;
public sealed class Placeholder {
}
""";

		var run = RunGeneratorWithoutSharedStubs(nameof(MissingCanonicalEngineTypes_ReportDiagnostics), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN901");
	}

	[Fact]
	public void DuplicateDeepCopyableTypeNames_CaseInsensitive_ReportSourceNameCollision() {
		const string source = """
using SteamEngine;

namespace FirstNamespace {
	[DeepCopyableClass]
	public sealed class SharedName {
		[DeepCopyImplementation]
		public SharedName() {
		}
	}
}

namespace SecondNamespace {
	[DeepCopyableClass]
	public sealed class sharedname {
		[DeepCopyImplementation]
		public sharedname() {
		}
	}
}
""";

		var run = RunGenerator(nameof(DuplicateDeepCopyableTypeNames_CaseInsensitive_ReportSourceNameCollision), source);
		var diagnostic = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN208");
		Assert.Contains("SharedName", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string scenarioName, string source) {
		return RunGeneratorInternal(scenarioName, source, includeSharedStubs: true);
	}

	private static GeneratorRunSnapshot RunGeneratorWithoutSharedStubs(string scenarioName, string source) {
		return RunGeneratorInternal(scenarioName, source, includeSharedStubs: false);
	}

	private static GeneratorRunSnapshot RunGeneratorInternal(string scenarioName, string source, bool includeSharedStubs) {
		var syntaxTrees = new List<SyntaxTree>();
		if (includeSharedStubs) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes));
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(SharedStubs));
		}
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return GeneratorTestRunner.Run(new DeepCopyImplementorsGenerator(), syntaxTrees, GetMetadataReferences(), scenarioName);
	}

	private static IEnumerable<MetadataReference> GetMetadataReferences() {
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var assemblies = new[] {
			typeof(object).Assembly,
			typeof(Enumerable).Assembly,
			typeof(System.Runtime.GCSettings).Assembly,
			typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly,
			typeof(SteamEngine.DeepCopyableClassAttribute).Assembly
		};

		foreach (var assembly in assemblies) {
			var location = assembly.Location;
			if (string.IsNullOrWhiteSpace(location) || !seen.Add(location)) {
				continue;
			}
			yield return MetadataReference.CreateFromFile(location);
		}
	}
}
