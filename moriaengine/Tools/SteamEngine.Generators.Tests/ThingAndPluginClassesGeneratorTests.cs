using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Generators.ThingAndPluginClasses;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class ThingAndPluginClassesGeneratorTests {
	private const string SharedStubs = """
global using SteamEngine.Generators.Abstractions;
global using SteamEngine;

public sealed class FieldValue { }

public class Plugin { }
public class PluginDef {
	protected FieldValue InitTypedField(string name, object value, System.Type type) => new FieldValue();
	public static void RegisterPluginDef(System.Type defType, System.Type instanceType) { }
}

public class MagicThing : global::SteamEngine.Thing { }
public class MagicThingDef : global::SteamEngine.ThingDef { }
public class EffectPlugin : Plugin {
	[global::SteamEngine.Persistence.LoadingFinalizer]
	public void FinishLoadingFromBasePlugin() { }
}
public class EffectPluginDef : PluginDef { }

public delegate void LoadObject(object resolvedObject, string filename, int line);

public static class DeepCopyFactory {
	public static object GetCopyDelayed(object input, System.Action<object> callback) => input;
}

public static class ObjectSaver {
	public static void Load(string value, LoadObject callback, string filename, int line) { }
	public static object OptimizedLoad_String(string value) => value;
	public static object OptimizedLoad_SimpleType(string value, System.Type type) => value;
	public static object Load(string value) => value;
}

namespace SteamEngine.Common {
	public static class ConvertTools {
		public static int ParseInt32(string value) => 0;
		public static bool ParseBoolean(string value) => false;
		public static long ParseInt64(string value) => 0L;
		public static uint ParseUInt32(string value) => 0u;
		public static ulong ParseUInt64(string value) => 0UL;
		public static short ParseInt16(string value) => 0;
		public static ushort ParseUInt16(string value) => 0;
		public static byte ParseByte(string value) => 0;
		public static sbyte ParseSByte(string value) => 0;
		public static float ParseSingle(string value) => 0f;
		public static double ParseDouble(string value) => 0d;
		public static decimal ParseDecimal(string value) => 0m;
	}
}

public enum EnergyMode {
	Low,
	High
}

public sealed class ComplexPayload {
}
""";

	[Fact]
	public void FullThingPair_GeneratesThingSpecificFeatures() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class ArcaneBeacon : MagicThing {
	[SaveableData, CopyableData]
	private int charge = 3;
	protected static decimal rechargeRate = 1.25m;
}

[ThingDefTemplate]
public partial class ArcaneBeaconDef : MagicThingDef {
	[DefField(typeof(int), DefaultExpression = "9")]
	private FieldValue maxCharge;

	[DefField(typeof(string), DefaultExpression = "\"Arcane\"")]
	private FieldValue school;
}
""";

		var run = RunGenerator(nameof(FullThingPair_GeneratesThingSpecificFeatures), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"partial class ArcaneBeacon : MagicThing",
			"partial class ArcaneBeaconDef : MagicThingDef",
			"GeneratedDeepCopyImplementor_ArcaneBeacon",
			"GeneratedSaveImplementor_ArcaneBeacon",
			"class GeneratedSaveImplementor_ArcaneBeacon",
			"class GeneratedDeepCopyImplementor_ArcaneBeacon",
			"public sealed class GeneratedSaveImplementor_ArcaneBeacon : global::SteamEngine.Persistence.DecoratedClassesSaveImplementor",
			"public sealed class GeneratedDeepCopyImplementor_ArcaneBeacon : global::SteamEngine.IDeepCopyImplementor",
			"global::ScriptSandbox.ArcaneBeacon loadedObject = (global::ScriptSandbox.ArcaneBeacon) global::SteamEngine.Thing.Load(input);",
			"castObjToSave.Save(writer);",
			"public object DeepCopy(object copyFrom)",
			"global::ScriptSandbox.ArcaneBeacon copy = new global::ScriptSandbox.ArcaneBeacon(copyFromConverted);",
			"global::ScriptSandbox.ArcaneBeacon loadedObject = (global::ScriptSandbox.ArcaneBeacon) global::SteamEngine.Thing.Load(input);",
			"public new ArcaneBeaconDef TypeDef",
			"public override void Save(",
			"public override void LoadLine(",
			"public ArcaneBeacon(ArcaneBeaconDef myDef)",
			"public ArcaneBeacon(ArcaneBeacon copyFrom)",
			"public int MaxCharge",
			"public string School",
			"InitTypedField(\"maxCharge\", 9, typeof(int))",
			"InitTypedField(\"school\", \"Arcane\", typeof(string))",
			"static ArcaneBeaconDef() {",
			"public static new void Bootstrap() {",
			"ThingDef.RegisterThingDef(typeof(ArcaneBeaconDef), typeof(ArcaneBeacon))");
		AssertGeneratedOmits(generated, "RegisterPluginDef(");
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void FullPluginPair_GeneratesPluginSpecificFeatures() {
		const string source = """
namespace ScriptSandbox;

[PluginTemplate]
public partial class EchoWard : EffectPlugin {
	[SaveableData, CopyableData]
	private int pulseSeconds = 5;
	[SaveableData, CopyableData]
	private string label = null;
}

[PluginDefTemplate]
public partial class EchoWardDef : EffectPluginDef {
	[DefField(typeof(int), DefaultExpression = "5")]
	private FieldValue pulseSeconds;
}
""";

		var run = RunGenerator(nameof(FullPluginPair_GeneratesPluginSpecificFeatures), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"partial class EchoWard : EffectPlugin",
			"partial class EchoWardDef : EffectPluginDef",
			"GeneratedDeepCopyImplementor_EchoWard",
			"GeneratedSaveImplementor_EchoWard",
			"class GeneratedSaveImplementor_EchoWard",
			"class GeneratedDeepCopyImplementor_EchoWard",
			"public sealed class GeneratedSaveImplementor_EchoWard : global::SteamEngine.Persistence.DecoratedClassesSaveImplementor",
			"public sealed class GeneratedDeepCopyImplementor_EchoWard : global::SteamEngine.IDeepCopyImplementor",
			"global::ScriptSandbox.EchoWard loadedObject = new global::ScriptSandbox.EchoWard();",
			"this.LoadSectionLines(input, loadedObject);",
			"loadedObject.FinishLoadingFromBasePlugin();",
			"castObjToSave.Save(writer);",
			"public object DeepCopy(object copyFrom)",
			"global::ScriptSandbox.EchoWard copy = new global::ScriptSandbox.EchoWard(copyFromConverted);",
			"public new EchoWardDef TypeDef",
			"public override void Save(",
			"public override void LoadLine(",
			"public EchoWard()",
			"public int PulseSeconds",
			"this.pulseSeconds = SteamEngine.Common.ConvertTools.ParseInt32(valueString);",
			"this.label = (string) ObjectSaver.OptimizedLoad_String(valueString);",
			"InitTypedField(\"pulseSeconds\", 5, typeof(int))",
			"static EchoWardDef() {",
			"public static new void Bootstrap() {",
			"PluginDef.RegisterPluginDef(typeof(EchoWardDef), typeof(EchoWard))");
		AssertGeneratedOmits(generated, "RegisterThingDef(");
		AssertGeneratedOmits(generated, "Convert.ChangeType(");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void ThingDefOnly_GeneratesMinimalThingInstanceCompanion() {
		const string source = """
namespace ScriptSandbox;

[ThingDefTemplate]
public partial class WhisperNodeDef : MagicThingDef {
	[DefField(typeof(int), DefaultExpression = "1")]
	private FieldValue pulse;
}
""";

		var run = RunGenerator(nameof(ThingDefOnly_GeneratesMinimalThingInstanceCompanion), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"partial class WhisperNode : MagicThing",
			"partial class WhisperNodeDef : MagicThingDef",
			"class GeneratedSaveImplementor_WhisperNode",
			"public WhisperNode(WhisperNodeDef myDef)",
			"public new WhisperNodeDef TypeDef",
			"public int Pulse",
			"InitTypedField(\"pulse\", 1, typeof(int))",
			"static WhisperNodeDef() {",
			"public static new void Bootstrap() {",
			"ThingDef.RegisterThingDef(typeof(WhisperNodeDef), typeof(WhisperNode))");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void PluginInstanceOnly_GeneratesMinimalPluginDefCompanion() {
		const string source = """
namespace ScriptSandbox;

[PluginTemplate]
public partial class EmberGlyph : EffectPlugin {
	[SaveableData, CopyableData]
	private int energy = 5;
}
""";

		var run = RunGenerator(nameof(PluginInstanceOnly_GeneratesMinimalPluginDefCompanion), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"partial class EmberGlyph : EffectPlugin",
			"partial class EmberGlyphDef : EffectPluginDef",
			"static EmberGlyphDef() {",
			"public static new void Bootstrap() {",
			"PluginDef.RegisterPluginDef(typeof(EmberGlyphDef), typeof(EmberGlyph))",
			"public new EmberGlyphDef TypeDef",
			"public EmberGlyph()");
		AssertGeneratedOmits(generated, "RegisterThingDef(");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void ThingFields_UseDelayedLoadAndDeepCopyForNonSimpleTypes() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class DelayedRelicBeacon : MagicThing {
	[SaveableData, CopyableData]
	private int charges = 1;
	[SaveableData, CopyableData]
	private EnergyMode mode = EnergyMode.Low;
	[SaveableData, CopyableData]
	private ComplexPayload payload = null;
}

[ThingDefTemplate]
public partial class DelayedRelicBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(ThingFields_UseDelayedLoadAndDeepCopyForNonSimpleTypes), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"private void DelayedCopy_Payload(object copiedValue)",
			"this.mode = copyFrom.mode;",
			"DeepCopyFactory.GetCopyDelayed(copyFrom.payload, this.DelayedCopy_Payload)",
			"private void DelayedLoad_Mode(object resolvedObject, string filename, int line)",
			"private void DelayedLoad_Payload(object resolvedObject, string filename, int line)",
			"ObjectSaver.Load(valueString, new LoadObject(this.DelayedLoad_Mode), filename, line)",
			"ObjectSaver.Load(valueString, new LoadObject(this.DelayedLoad_Payload), filename, line)",
			"this.charges = SteamEngine.Common.ConvertTools.ParseInt32(valueString);");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void MissingBase_DoesNotGeneratePair() {
		const string source = """
namespace ScriptSandbox;

[ThingDefTemplate]
public partial class BrokenDef : DefLikeWithoutSuffix {
}
""";

		var run = RunGenerator(nameof(MissingBase_DoesNotGeneratePair), source);
		Assert.Empty(run.GeneratedSources);
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void DefFieldDefaultExpression_NonLiteral_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[ThingDefTemplate]
public partial class ArcaneBeaconDef : MagicThingDef {
	private const string DefaultExpr = "5";

	[DefField(typeof(int), DefaultExpression = DefaultExpr)]
	private FieldValue maxCharge;
}
""";

		var run = RunGenerator(nameof(DefFieldDefaultExpression_NonLiteral_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN011");
		Assert.Contains("DefaultExpr", error.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void ThingTypedField_UsesDirectCopyButDelayedLoad() {
		const string source = """
namespace ScriptSandbox;

public class Item : global::SteamEngine.Thing {
}

public class Shrine : Item {
}

[ThingTemplate]
public partial class LinkedShrineBeacon : MagicThing {
	[SaveableData, CopyableData]
	private Shrine linked = null;
}

[ThingDefTemplate]
public partial class LinkedShrineBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(ThingTypedField_UsesDirectCopyButDelayedLoad), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"this.linked = copyFrom.linked;",
			"ObjectSaver.Load(valueString, new LoadObject(this.DelayedLoad_Linked), filename, line)");
		AssertGeneratedOmits(generated,
			"DeepCopyFactory.GetCopyDelayed(copyFrom.linked, this.DelayedCopy_Linked)");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void TemplateCopyFalse_OmitsCopyConstructor() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate(Copy = false)]
public partial class NoCopyBeacon : MagicThing {
	[SaveableData, CopyableData]
	private ComplexPayload payload = null;
	[SaveableData, CopyableData]
	private int charges = 1;
}

[ThingDefTemplate]
public partial class NoCopyBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(TemplateCopyFalse_OmitsCopyConstructor), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"if (this.payload != null) output.WriteValue(\"payload\", this.payload);",
			"ObjectSaver.Load(valueString, new LoadObject(this.DelayedLoad_Payload), filename, line)");
		AssertGeneratedOmits(generated,
			"public NoCopyBeacon(NoCopyBeacon copyFrom)",
			"[DeepCopyImplementation]",
			"GeneratedDeepCopyImplementor_NoCopyBeacon",
			"this.charges = copyFrom.charges;",
			"this.payload = copyFrom.payload;",
			"private void DelayedCopy_Payload(object copiedValue)");
		Assert.DoesNotContain(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void TemplateCopyNonLiteral_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate(Copy = !false)]
public partial class CopyExpressionBeacon : MagicThing {
	[SaveableData, CopyableData]
	private int charges = 1;
}

[ThingDefTemplate]
public partial class CopyExpressionBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(TemplateCopyNonLiteral_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN006");
		Assert.Contains("!false", error.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void TemplateDeclaringLoadSectionAttributes_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[PluginTemplate]
public partial class BadLoadFactoryBeacon : EffectPlugin {
	[SteamEngine.Persistence.LoadSection]
	public static object Load(global::SteamEngine.PropsSection input) {
		return null;
	}
}
""";

		var run = RunGenerator(nameof(TemplateDeclaringLoadSectionAttributes_ReportsDiagnostic), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN012" && d.GetMessage().Contains("LoadSection", StringComparison.Ordinal));
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void TemplateDeclaringLoadingInitializerAttributes_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[PluginTemplate]
public partial class BadLoadFactoryBeacon : EffectPlugin {
	[SteamEngine.Persistence.LoadingInitializer]
	public BadLoadFactoryBeacon() {
	}
}
""";

		var run = RunGenerator(nameof(TemplateDeclaringLoadingInitializerAttributes_ReportsDiagnostic), source);
		Assert.Contains(run.Diagnostics, d => d.Id == "SEGEN012" && d.GetMessage().Contains("LoadingInitializer", StringComparison.Ordinal));
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void UnannotatedMembers_AreIgnoredByTemplateGeneration() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class OptInBeacon : MagicThing {
	private int ignoredCharge = 7;

	[SaveableData, CopyableData]
	private int trackedCharge = 5;
}

[ThingDefTemplate]
public partial class OptInBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(UnannotatedMembers_AreIgnoredByTemplateGeneration), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"if (this.trackedCharge != 5) output.WriteValue(\"trackedCharge\", this.trackedCharge);",
			"this.trackedCharge = copyFrom.trackedCharge;",
			"case \"trackedcharge\":");
		AssertGeneratedOmits(generated,
			"ignoredCharge",
			"case \"ignoredcharge\":");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void AttributedProperty_WithGetterAndSetter_IsGenerated() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class PropertyBeacon : MagicThing {
	[SaveableData, CopyableData]
	private int Charges { get; set; } = 6;
}

[ThingDefTemplate]
public partial class PropertyBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(AttributedProperty_WithGetterAndSetter_IsGenerated), source);
		var generated = JoinGeneratedSources(run);
		AssertGeneratedContains(generated,
			"if (this.Charges != 6) output.WriteValue(\"Charges\", this.Charges);",
			"this.Charges = copyFrom.Charges;",
			"case \"charges\":",
			"this.Charges = SteamEngine.Common.ConvertTools.ParseInt32(valueString);");
		Assert.Empty(run.Diagnostics);
	}

	[Fact]
	public void AttributedProperty_WithoutSetter_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class BadPropertyBeacon : MagicThing {
	[SaveableData]
	private int Charges { get; } = 6;
}

[ThingDefTemplate]
public partial class BadPropertyBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(AttributedProperty_WithoutSetter_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		Assert.Equal("SEGEN001", error.Id);
		Assert.Contains("without a setter", error.GetMessage());
	}

	[Fact]
	public void AttributedIndexer_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class BadIndexerBeacon : MagicThing {
	[SaveableData]
	private int this[int key] {
		get => key;
		set { }
	}
}

[ThingDefTemplate]
public partial class BadIndexerBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(AttributedIndexer_ReportsDiagnostic), source);
		var error = Assert.Single(run.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
		Assert.Equal("SEGEN001", error.Id);
		Assert.Contains("indexers are not supported", error.GetMessage());
	}

	[Fact]
	public void SaveableMembersDifferingOnlyByCase_ReportConflict() {
		const string source = """
namespace ScriptSandbox;

[ThingTemplate]
public partial class DuplicateLoadKeyBeacon : MagicThing {
	[SaveableData]
	private int value = 1;

	[SaveableData]
	private int Value = 2;
}

[ThingDefTemplate]
public partial class DuplicateLoadKeyBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(SaveableMembersDifferingOnlyByCase_ReportConflict), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN003");
		Assert.Contains("value", error.GetMessage(), StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ShadowedTemplateAttribute_ReportsDiagnostic() {
		const string source = """
namespace Other {
	public sealed class ThingTemplateAttribute : System.Attribute {
	}
}

namespace ScriptSandbox;

[Other.ThingTemplate]
public partial class FakeTemplateBeacon : MagicThing {
	[SaveableData]
	private int charge = 3;
}
""";

		var run = RunGenerator(nameof(ShadowedTemplateAttribute_ReportsDiagnostic), source);
		var warning = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN902");
		Assert.Contains("ThingTemplate", warning.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void MissingTemplateAttributeDeclaration_ReportsDiagnostic() {
		var run = RunGeneratorWithoutGeneratorAbstractions(
			nameof(MissingTemplateAttributeDeclaration_ReportsDiagnostic));
		Assert.Equal(16, run.Diagnostics.Count(d => d.Id == "SEGEN900"));
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void TypeNameConflictAgainstCanonicalThing_ReportsDiagnostic() {
		const string source = """
namespace ScriptSandbox {
	public class Thing {
	}
}

namespace ScriptSandbox;

[ThingTemplate]
public partial class TypeConflictBeacon : MagicThing {
	[CopyableData]
	private Thing localThing = null;
}

[ThingDefTemplate]
public partial class TypeConflictBeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(TypeNameConflictAgainstCanonicalThing_ReportsDiagnostic), source);
		var warning = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN005");
		Assert.Contains("Thing", warning.GetMessage(), StringComparison.Ordinal);
	}

	[Fact]
	public void MissingCanonicalEngineTypes_ReportErrors() {
		var run = RunGeneratorWithoutCanonicalEngineTypes(
			nameof(MissingCanonicalEngineTypes_ReportErrors));
		var errors = run.Diagnostics.Where(d => d.Id == "SEGEN901").ToList();
		Assert.NotEmpty(errors);
		Assert.Contains(errors, d => d.GetMessage().Contains("SteamEngine.Thing", StringComparison.Ordinal));
		Assert.Contains(errors, d => d.GetMessage().Contains("SteamEngine.AbstractAccount", StringComparison.Ordinal));
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void SameTemplateNameInDifferentFamilies_ReportsConflict() {
		const string source = """
namespace A {
	[ThingTemplate]
	public partial class Shared : MagicThing { }
}
namespace B {
	[PluginTemplate]
	public partial class shared : EffectPlugin { }
}
""";
		var run = RunGenerator(nameof(SameTemplateNameInDifferentFamilies_ReportsConflict), source);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN010");
		Assert.Contains("Shared", error.GetMessage(), StringComparison.OrdinalIgnoreCase);
		Assert.Empty(run.GeneratedSources);
	}

	[Fact]
	public void DuplicateTemplateNamesAcrossNamespaces_ReportConflict() {
		const string sourceA = """
namespace A;

[ThingTemplate]
public partial class SharedBeacon : MagicThing {
}

[ThingDefTemplate]
public partial class SharedBeaconDef : MagicThingDef {
}
""";

		const string sourceB = """
namespace B;

[ThingTemplate]
public partial class sharedbeacon : MagicThing {
}

[ThingDefTemplate]
public partial class sharedbeaconDef : MagicThingDef {
}
""";

		var run = RunGenerator(nameof(DuplicateTemplateNamesAcrossNamespaces_ReportConflict), sourceA, sourceB);
		var error = Assert.Single(run.Diagnostics, d => d.Id == "SEGEN010");
		Assert.Contains("SharedBeacon", error.GetMessage(), StringComparison.OrdinalIgnoreCase);
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

	private static void AssertGeneratedOmits(string generated, params string[] unexpectedSnippets) {
		foreach (var snippet in unexpectedSnippets) {
			Assert.DoesNotContain(snippet, generated);
		}
	}

	private static GeneratorRunSnapshot RunGenerator(string scenarioName, params string[] sources) {
		return RunGeneratorInternal(scenarioName, true, sources);
	}

	private static GeneratorRunSnapshot RunGeneratorWithoutGeneratorAbstractions(string scenarioName, params string[] sources) {
		return RunGeneratorInternal(scenarioName, false, sources);
	}

	private static GeneratorRunSnapshot RunGeneratorWithoutCanonicalEngineTypes(string scenarioName) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(SharedStubs)
		};

		return GeneratorTestRunner.Run(new ThingAndPluginClassesGenerator(), syntaxTrees, GetMetadataReferences(true), scenarioName);
	}

	private static GeneratorRunSnapshot RunGeneratorInternal(
		string scenarioName,
		bool includeGeneratorAbstractionsReference,
		params string[] sources) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(SharedStubs)
		};
		if (includeGeneratorAbstractionsReference) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes));
		}
		syntaxTrees.AddRange(sources.Select(s => CSharpSyntaxTree.ParseText(s)));

		return GeneratorTestRunner.Run(new ThingAndPluginClassesGenerator(), syntaxTrees,
			GetMetadataReferences(includeGeneratorAbstractionsReference), scenarioName);
	}

	private static IEnumerable<MetadataReference> GetMetadataReferences(bool includeGeneratorAbstractionsReference) {
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location);
		if (includeGeneratorAbstractionsReference) {
			yield return MetadataReference.CreateFromFile(typeof(ThingTemplateAttribute).Assembly.Location);
		}
	}
}
