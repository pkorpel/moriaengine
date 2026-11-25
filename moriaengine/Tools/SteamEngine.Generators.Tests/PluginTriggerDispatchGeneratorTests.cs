using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.PluginTriggerDispatch;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class PluginTriggerDispatchGeneratorTests {
	[Fact]
	public void ValidPlugin_GeneratesRunOverride() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public enum Choice : long { None }

public partial class ArenaPlugin : Plugin {
	public void On_Assign() {
	}

	public object On_DenyMove(bool denied, int reason) {
		return denied;
	}

	public void On_Choose(Choice choice) {
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
		Assert.Contains("this.On_DenyMove(global::System.Convert.ToBoolean(sa.Argv[0]), global::System.Convert.ToInt32(sa.Argv[1]));", generated);
		Assert.Contains("this.On_Choose((global::ScriptSandbox.Choice)global::System.Convert.ToInt32(sa.Argv[0]));", generated);
		Assert.Contains("case \"login\":", generated);
		Assert.Contains("return this.On_Login(sa);", generated);
		Assert.DoesNotContain(run.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
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
	public void NestedPlugin_ReportsDiagnostic() {
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
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN906" && d.Severity == DiagnosticSeverity.Warning);
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

	[Fact]
	public void NonPublicMethods_AreDiscoveredExceptPrivateBase() {
		const string source = """
using SteamEngine;

namespace ScriptSandbox;

public abstract class BasePlugin : Plugin {
	private void On_HiddenBase() {
	}

	protected void On_BaseVisible() {
	}
}

public partial class ArenaPlugin : BasePlugin {
	private void On_LocalPrivate() {
	}
}
""";

		var run = RunGenerator(source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("case \"basevisible\":", generated);
		Assert.Contains("this.On_BaseVisible();", generated);
		Assert.Contains("case \"localprivate\":", generated);
		Assert.Contains("this.On_LocalPrivate();", generated);
		Assert.DoesNotContain("hiddenbase", generated, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void DistinctPluginTypes_WithCollidingSanitizedNames_GenerateSeparately() {
		const string source = """
using SteamEngine;
namespace A_B { public partial class C : Plugin { public void On_First() { } } }
namespace A { public partial class B_C : Plugin { public void On_Second() { } } }
namespace A { public partial class b_C : Plugin { public void On_Third() { } } }
""";
		var run = RunGenerator(source);
		Assert.Equal(3, run.GeneratedSources.Count);
		Assert.Contains(run.GeneratedSources, source => source.Contains("this.On_First()"));
		Assert.Contains(run.GeneratedSources, source => source.Contains("this.On_Second()"));
		Assert.Contains(run.GeneratedSources, source => source.Contains("this.On_Third()"));
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void MissingSharedRequiredTypes_ReportDiagnosticsWithoutGenerating() {
		const string source = """
namespace SteamEngine {
	public class Plugin { }
	public class ScriptArgs { }
	public partial class Example : Plugin { public void On_Test() { } }
}
""";
		var run = GeneratorTestRunner.Run(new PluginTriggerDispatchGenerator(),
			new[] { CSharpSyntaxTree.ParseText(source) }, GeneratorTestRunner.GetRuntimeReferences());
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN901" && d.GetMessage().Contains("SteamEngine.Thing"));
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string source) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes)
		};
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return GeneratorTestRunner.Run(new PluginTriggerDispatchGenerator(), syntaxTrees, GeneratorTestRunner.GetRuntimeReferences());
	}
}
