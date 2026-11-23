using System;
using System.Collections.Generic;
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

	private static GeneratorRunSnapshot RunGenerator(string source) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes),
			CSharpSyntaxTree.ParseText(PluginStubs)
		};
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return GeneratorTestRunner.Run(new PluginTriggerDispatchGenerator(), syntaxTrees, GeneratorTestRunner.GetRuntimeReferences());
	}
}
