using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.CompiledTriggerGroups;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class CompiledTriggerGroupGeneratorTests {
	[Fact]
	public void ValidCompiledTriggerGroup_GeneratesDispatch() {
		const string source = """
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public enum Choice : long { None }

public abstract class BaseTriggerGroup : CompiledTriggerGroup {
	public object on_Login(object self, ScriptArgs args) {
		return null;
	}
}

public partial class ArenaTriggerGroup : BaseTriggerGroup {
	public void On_DClick(object self, int value, bool enabled) {
	}

	public void On_Choose(object self, Choice choice) {
	}
}
""";

		var run = RunGenerator(source);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("partial class ArenaTriggerGroup", generated);
		Assert.Contains("switch (tk.LowercaseName)", generated);
		Assert.Contains("case \"dclick\":", generated);
		Assert.Contains("this.On_DClick(self, global::System.Convert.ToInt32(sa.Argv[0]), global::System.Convert.ToBoolean(sa.Argv[1]));", generated);
		Assert.Contains("this.On_Choose(self, (global::ScriptSandbox.Choice)global::System.Convert.ToInt32(sa.Argv[0]));", generated);
		Assert.Contains("case \"login\":", generated);
		Assert.Contains("return this.on_Login(self, sa);", generated);
		Assert.DoesNotContain("InternalFirstGetDefname", generated);
		Assert.DoesNotContain(run.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
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

	[Fact]
	public void NestedCompiledTriggerGroup_WithPartialContainingType_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static partial class Outer {
	public partial class NestedTriggerGroup : CompiledTriggerGroup {
		public void on_DClick(object self) {
		}
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN906" && d.Severity == DiagnosticSeverity.Warning);
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void NestedCompiledTriggerGroup_WithNonPartialContainingType_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox;

public static class Outer {
	public partial class NestedTriggerGroup : CompiledTriggerGroup {
		public void on_DClick(object self) {
		}
	}
}
""";

		var run = RunGenerator(source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN906" && d.Severity == DiagnosticSeverity.Warning);
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void DuplicateShortTypeNames_ReportDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts;

namespace ScriptSandbox.A {
	public partial class SharedNameTriggerGroup : CompiledTriggerGroup {
		public void on_DClick(object self) {
		}
	}
}

namespace ScriptSandbox.B {
	public partial class SharedNameTriggerGroup : CompiledTriggerGroup {
		public void on_DClick(object self) {
		}
	}
}
""";

		var run = RunGenerator(source);
		Assert.Single(run.Diagnostics, d => d.Id == "SEGEN405");
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void PartialDeclarations_IncludeMethodsFromEveryPart() {
		const string source = """
using SteamEngine.CompiledScripts;
public partial class ArenaTriggerGroup : CompiledTriggerGroup { }
public partial class ArenaTriggerGroup {
	public void on_Click(object self) { }
}
""";
		var run = RunGenerator(source);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("this.on_Click(self);", generated);
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void MissingRequiredTypes_ReportDiagnosticsWithoutGenerating() {
		var run = GeneratorTestRunner.Run(new CompiledTriggerGroupsGenerator(),
			new[] { CSharpSyntaxTree.ParseText("public class Unrelated { }") },
			GeneratorTestRunner.GetRuntimeReferences());
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN901");
		Assert.Empty(run.GeneratedSources);
	}

	private static GeneratorRunSnapshot RunGenerator(string source) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes)
		};
		if (!string.IsNullOrEmpty(source)) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return GeneratorTestRunner.Run(new CompiledTriggerGroupsGenerator(), syntaxTrees, GeneratorTestRunner.GetRuntimeReferences());
	}
}
