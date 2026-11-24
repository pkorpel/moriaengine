using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SteamEngine.Generators.ViewableClasses;
using Xunit;

namespace SteamEngine.Generators.Tests;

public sealed class ViewableClassGeneratorTests {
	private const string ViewableContracts = """
namespace SteamEngine.Common {
	public static class ConvertTools {
		public static bool ParseBoolean(string value) => bool.Parse(value);
		public static byte ParseByte(string value) => byte.Parse(value);
		public static sbyte ParseSByte(string value) => sbyte.Parse(value);
		public static int ParseInt32(string value) => int.Parse(value);
		public static short ParseInt16(string value) => short.Parse(value);
		public static ushort ParseUInt16(string value) => ushort.Parse(value);
		public static uint ParseUInt32(string value) => uint.Parse(value);
		public static long ParseInt64(string value) => long.Parse(value);
		public static ulong ParseUInt64(string value) => ulong.Parse(value);
		public static float ParseSingle(string value) => float.Parse(value);
		public static double ParseDouble(string value) => double.Parse(value);
		public static decimal ParseDecimal(string value) => decimal.Parse(value);
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {
	public interface IDataFieldView {
		string GetName(object target);
		bool ReadOnly { get; }
		bool IsButtonEnabled { get; }
		System.Type FieldType { get; }
		object GetValue(object target);
		void SetValue(object target, object value);
		string GetStringValue(object target);
		void SetStringValue(object target, string value);
		void OnButton(object target);
	}

	public abstract class ViewableClassDataView {
		public abstract System.Type HandledType { get; }
		public bool HandleSubclasses => false;
		public abstract string GetName(object instance);
		public abstract int GetActionButtonsCount(object instance);
		public abstract int GetFieldsCount(object instance);
		public abstract System.Collections.Generic.IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target);
		public abstract System.Collections.Generic.IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target);
	}

	public abstract class ReadOnlyDataFieldView : IDataFieldView {
		public bool ReadOnly => true;
		public bool IsButtonEnabled => false;
		public void SetValue(object target, object value) { }
		public void SetStringValue(object target, string value) { }
		public void OnButton(object target) { }
		public abstract string GetName(object target);
		public abstract System.Type FieldType { get; }
		public abstract object GetValue(object target);
		public abstract string GetStringValue(object target);
	}

	public abstract class ReadWriteDataFieldView : IDataFieldView {
		public bool ReadOnly => false;
		public bool IsButtonEnabled => false;
		public void OnButton(object target) { }
		public abstract string GetName(object target);
		public abstract System.Type FieldType { get; }
		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract string GetStringValue(object target);
		public abstract void SetStringValue(object target, string value);
	}

	public abstract class ButtonDataFieldView : IDataFieldView {
		public bool ReadOnly => true;
		public bool IsButtonEnabled => true;
		public System.Type FieldType => typeof(object);
		public object GetValue(object target) => null;
		public void SetValue(object target, object value) { }
		public string GetStringValue(object target) => null;
		public void SetStringValue(object target, string value) { }
		public abstract string GetName(object target);
		public abstract void OnButton(object target);
	}
}

""";

	[Fact]
	public void ViewableClass_GeneratesLabeledFieldsButtonsAndHidesNoShowMembers() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public enum CreatureMode : short {
	Idle
}

[ViewableClass("Creature view")]
public sealed class Creature {
	public int Health { get; set; }
	public CreatureMode Mode { get; set; }

	[NoShow]
	public int Secret { get; set; }

	[InfoField("Creature name")]
	public string Name { get; private set; }

	[Button("Heal")]
	public void Heal() { }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("Creature view", generated, StringComparison.Ordinal);
		Assert.Contains("Creature name", generated, StringComparison.Ordinal);
		Assert.Contains("ParseInt32(value)", generated, StringComparison.Ordinal);
		Assert.Contains("(global::Sandbox.CreatureMode) SteamEngine.Common.ConvertTools.ParseInt16(value)", generated, StringComparison.Ordinal);
		Assert.Contains(": ReadOnlyDataFieldView", generated, StringComparison.Ordinal);
		Assert.Contains("class GeneratedViewableButtonView_Sandbox_Creature_Heal ", generated, StringComparison.Ordinal);
		Assert.Contains("class GeneratedViewableFieldView_Sandbox_Creature_Health ", generated, StringComparison.Ordinal);
		Assert.Contains("class GeneratedDataView_Sandbox_Creature ", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".Secret", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void Descriptor_ContributesInheritedFieldsAndButtonsAndSuppressesNamedFields() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class BaseType {
	public int Hidden { get; set; }
	public int Visible { get; set; }
}

[ViewableClass]
public sealed class DerivedType : BaseType {
	public string Own { get; set; }
}

[ViewDescriptor(typeof(BaseType), "Base type", new[] { "Hidden" })]
public static class BaseTypeDescriptor {
	[GetMethod("Computed", typeof(string))]
	public static object GetComputed(object target) => "value";

	[SetMethod("Computed", typeof(string))]
	public static void SetComputed(object target, object value) { }

	[Button("Refresh")]
	public static void Refresh(object target) { }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("BaseTypeDescriptor.GetComputed(target)", generated, StringComparison.Ordinal);
		Assert.Contains("BaseTypeDescriptor.SetComputed(target, value)", generated, StringComparison.Ordinal);
		Assert.Contains("BaseTypeDescriptor.SetComputed(target, (string) ObjectSaver.OptimizedLoad_String(value))", generated, StringComparison.Ordinal);
		Assert.Contains("BaseTypeDescriptor.Refresh(target)", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".Hidden", generated, StringComparison.Ordinal);
		Assert.Contains(".Visible", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void PluginSubclass_IsViewableWithoutAttributeButGameCoreGetsNoOutput() {
		const string source = """
namespace Sandbox;

public class BasePlugin : SteamEngine.Plugin { }

public sealed class ScriptPlugin : BasePlugin {
	public int State { get; set; }
}
""";

		var scriptsRun = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(scriptsRun.GeneratedSources);
		Assert.Contains("typeof(global::Sandbox.BasePlugin)", generated, StringComparison.Ordinal);
		Assert.Contains("typeof(global::Sandbox.ScriptPlugin)", generated, StringComparison.Ordinal);
		AssertNoErrors(scriptsRun);

		var gameCoreRun = Run(source, includeViewableContracts: false);
		Assert.Empty(gameCoreRun.GeneratedSources);
		AssertNoErrors(gameCoreRun);
	}

	[Fact]
	public void EmptyViewableClass_GeneratesRegisteredEmptyView() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass("Empty view")]
public sealed class EmptyType { }
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("typeof(global::Sandbox.EmptyType)", generated, StringComparison.Ordinal);
		Assert.Contains("GetFieldsCount(object instance) => dataFields.Length", generated, StringComparison.Ordinal);
		Assert.Contains("GetActionButtonsCount(object instance) => actionButtons.Length", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void OverriddenButtons_GenerateSafeHelpersForBaseAndDerivedViews() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass]
public class BaseType {
	public int Calls;

	[Button("Run")]
	public virtual void Run() { Calls++; }
}

[ViewableClass]
public sealed class DerivedType : BaseType {
	[Button("Run")]
	public override void Run() { Calls++; }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("((global::Sandbox.BaseType)target).Run();", generated, StringComparison.Ordinal);
		Assert.Contains("((global::Sandbox.DerivedType)target).Run();", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void DescriptorFieldWithoutGetter_ReportsDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewDescriptor(typeof(Target), "Target")]
public static class TargetDescriptor {
	[SetMethod("Missing", typeof(string))]
	public static void SetMissing(object target, object value) { }
}

public class Target { }
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == "SEGEN951");
		AssertNoErrors(run, "SEGEN951");
	}

	[Fact]
	public void DescriptorMethodsWithByRefParameters_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewDescriptor(typeof(Target))]
public static class TargetDescriptor {
	[Button("Refresh")]
	public static void Refresh(ref object target) { }

	[GetMethod("Computed", typeof(string))]
	public static object GetComputed(ref object target) => "value";

	[SetMethod("Computed", typeof(string))]
	public static void SetComputed(object target, ref object value) { }
}

public class Target { }
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Equal(3, run.Diagnostics.Count(diagnostic => diagnostic.Id == "SEGEN951"));
		AssertNoErrors(run, "SEGEN951");
	}

	[Fact]
	public void RefLikeMembersAndOpenGenericDescriptorFields_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass]
public sealed class Target {
	public System.Span<int> Span => default;
}

[ViewDescriptor(typeof(Target))]
public static class TargetDescriptor {
	[GetMethod("Open generic", typeof(System.Collections.Generic.List<>))]
	public static object GetOpenGeneric(object target) => null;
}
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == "SEGEN950");
		Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == "SEGEN951");
		AssertNoErrors(run, "SEGEN950", "SEGEN951");
	}

	[Fact]
	public void PointerAndFunctionPointerMembers_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass]
public unsafe class Target {
	public int* Pointer { get; set; }
	public delegate*<void> FunctionPointer { get; set; }
}
""";

		var run = RunUnsafe(source);
		Assert.Equal(2, run.Diagnostics.Count(diagnostic => diagnostic.Id == "SEGEN950"));
		AssertNoErrors(run, "SEGEN950");
	}

	[Fact]
	public void InvalidViewableTypesPropertiesAndButtons_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass]
public sealed class GenericViewable<T> { }

[ViewableClass]
public sealed class InvalidMembers {
	private int refTarget;

	public int this[int index] => index;
	public int PrivateGetter { private get; set; }

	[Button]
	public void NeedsArgument(int value) { }

	[Button]
	public void GenericButton<T>() { }

	[Button]
	public ref int RefButton() => ref refTarget;

	[Button]
	public void OptionalButton(int value = 0) { }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var invalidMembers = run.Diagnostics.Where(diagnostic => diagnostic.Id == "SEGEN950").ToArray();
		Assert.Equal(6, invalidMembers.Length);
		Assert.Contains(invalidMembers, diagnostic => diagnostic.GetMessage().Contains("generic viewable types are not supported", StringComparison.Ordinal));
		Assert.Contains(invalidMembers, diagnostic => diagnostic.GetMessage().Contains("indexers are not supported", StringComparison.Ordinal));
		Assert.Contains(invalidMembers, diagnostic => diagnostic.GetMessage().Contains("a public getter is required", StringComparison.Ordinal));
		Assert.Equal(3, invalidMembers.Count(diagnostic =>
			diagnostic.GetMessage().Contains("buttons must be instance methods callable without arguments", StringComparison.Ordinal)));
		AssertNoErrors(run, "SEGEN950");
	}

	[Fact]
	public void InvalidDescriptorTypesAndMembers_ReportDiagnostics() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;
using System;

namespace Sandbox;

public interface ITargetContract { }
public class Target { }

[ViewDescriptor(typeof(ITargetContract))]
public static class InterfaceTargetDescriptor { }

[ViewDescriptor(typeof(System.Collections.Generic.List<>))]
public static class OpenGenericTargetDescriptor { }

[ViewDescriptor(typeof(Target))]
public struct StructDescriptor { }

[ViewDescriptor(typeof(Target))]
public static class GenericDescriptor<T> { }

[ViewDescriptor(typeof(Target))]
public static class InvalidMembers {
	[Button]
	public static void WrongButton(string target) { }

	[Button]
	public static void GenericButton<T>(object target) { }

	[GetMethod("Void getter", typeof(int))]
	public static void VoidGetter(object target) { }

	[GetMethod("Wrong getter", typeof(int))]
	public static object WrongGetter(string target) => null;

	[SetMethod("Wrong setter", typeof(int))]
	public static void WrongSetter(object target, int value) { }

	[GetMethod("Duplicate getter", typeof(int))]
	public static object FirstGetter(object target) => 0;

	[GetMethod("Duplicate getter", typeof(int))]
	public static object SecondGetter(object target) => 0;

	[GetMethod("Duplicate setter", typeof(int))]
	public static object GetDuplicateSetter(object target) => 0;

	[SetMethod("Duplicate setter", typeof(int))]
	public static void FirstSetter(object target, object value) { }

	[SetMethod("Duplicate setter", typeof(int))]
	public static void SecondSetter(object target, object value) { }

	[GetMethod("Mismatched type", typeof(int))]
	public static object GetMismatched(object target) => 0;

	[SetMethod("Mismatched type", typeof(string))]
	public static void SetMismatched(object target, object value) { }

	[GetMethod("Ref-like field", typeof(Span<int>))]
	public static object GetRefLike(object target) => null;
}
""";

		var run = Run(source, includeViewableContracts: true);
		var messages = run.Diagnostics
			.Where(diagnostic => diagnostic.Id == "SEGEN951")
			.Select(static diagnostic => diagnostic.GetMessage())
			.ToArray();
		Assert.Contains(messages, message => message.Contains("the handled type must be a closed class or struct", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("descriptors must be non-generic classes", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("descriptor buttons must be public static methods taking one object argument", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("get methods must be public static methods taking one object argument", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("set methods must be public static methods taking two object arguments", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("the descriptor field has more than one getter", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("the descriptor field has more than one setter", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("the getter and setter field types do not match", StringComparison.Ordinal));
		Assert.Contains(messages, message => message.Contains("a field name and representable field type are required", StringComparison.Ordinal));
		AssertNoErrors(run, "SEGEN951");
	}

	[Fact]
	public void ViewableAndPluginDescriptorConflicts_ReportDiagnostic() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewableClass]
[ViewDescriptor(typeof(ViewableTarget))]
public class ViewableTarget { }

[ViewDescriptor(typeof(PluginTarget))]
public class PluginTarget : SteamEngine.Plugin { }
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Equal(2, run.Diagnostics.Count(diagnostic => diagnostic.Id == "SEGEN952"));
		AssertNoErrors(run, "SEGEN952");
	}

	[Fact]
	public void NonCanonicalTypeWithEngineName_ReportsWarning() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public sealed class Thing { }

[ViewableClass]
public sealed class Target {
	public Thing Value { get; set; }
}
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == "SEGEN953");
		AssertNoErrors(run);
	}

	[Fact]
	public void ShadowedViewableAttribute_ReportsWarning() {
		const string source = """
namespace Sandbox;

public sealed class ViewableClassAttribute : System.Attribute { }

[ViewableClass]
public sealed class Target { }
""";

		var run = Run(source, includeViewableContracts: true);
		Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == "SEGEN902");
		Assert.DoesNotContain("typeof(global::Sandbox.Target)", Assert.Single(run.GeneratedSources), StringComparison.Ordinal);
		AssertNoErrors(run, "SEGEN902");
	}

	[Fact]
	public void PartialViewableClass_GeneratesOneViewFromAllParts() {
		const string source = """
namespace Sandbox;

public partial class Target {
	public int First { get; set; }
}

[SteamEngine.CompiledScripts.Dialogs.ViewableClass("Partial target")]
public partial class Target {
	public int Second { get; set; }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("Partial target", generated, StringComparison.Ordinal);
		Assert.Contains(".First", generated, StringComparison.Ordinal);
		Assert.Contains(".Second", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void DescriptorCanSnapshotAHandledTypeFromAnExternalAssembly() {
		const string externalSource = """
namespace External;

public class Target {
	public int Value { get; set; }

	public int this[int index] => index;

	public int WriteOnly { set { } }

	public System.Span<int> Buffer => default;
}
""";
		const string descriptorSource = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

[ViewDescriptor(typeof(External.Target), "External target")]
public static class ExternalTargetDescriptor {
	[GetMethod("Computed", typeof(int))]
	public static object GetComputed(object target) => ((External.Target)target).Value;
}
""";

		var externalCompilation = CSharpCompilation.Create(
			"ExternalTargetAssembly",
			new[] { CSharpSyntaxTree.ParseText(externalSource) },
			GeneratorTestRunner.GetRuntimeReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using var externalStream = new System.IO.MemoryStream();
		var externalEmit = externalCompilation.Emit(externalStream);
		Assert.True(externalEmit.Success, string.Join(Environment.NewLine, externalEmit.Diagnostics));

		var externalReference = MetadataReference.CreateFromImage(externalStream.ToArray());
		var run = Run(descriptorSource, includeViewableContracts: true, externalReference);
		AssertNoErrors(run);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("typeof(global::External.Target)", generated, StringComparison.Ordinal);
		Assert.Contains("((global::External.Target)target).Value", generated, StringComparison.Ordinal);
		Assert.Contains("External target", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".WriteOnly", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".Buffer", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void DescriptorHiddenFieldsAreSkippedBeforeValidation() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class Target {
	public int Value { get; set; }

	public System.Span<int> Buffer => default;
}

[ViewDescriptor(typeof(Target), "Target", new[] { "Buffer" })]
public static class TargetDescriptor { }
""";

		var run = Run(source, includeViewableContracts: true);
		AssertNoErrors(run);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains(".Value", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".Buffer", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void NoShowOnOverride_HidesTheBaseMember() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class BaseType {
	public virtual int Secret { get; set; }

	public int Visible { get; set; }
}

[ViewableClass]
public sealed class DerivedType : BaseType {
	[NoShow]
	public override int Secret { get; set; }
}
""";

		var run = Run(source, includeViewableContracts: true);
		AssertNoErrors(run);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains(".Visible", generated, StringComparison.Ordinal);
		Assert.DoesNotContain(".Secret", generated, StringComparison.Ordinal);
	}

	[Fact]
	public void ValueKindConversionsGenerateCompilableLoadExpressions() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public enum Mode : uint { Idle }

[ViewableClass]
public sealed class Values {
	public string Text { get; set; }
	public bool Boolean { get; set; }
	public byte Byte { get; set; }
	public sbyte SByte { get; set; }
	public short Int16 { get; set; }
	public ushort UInt16 { get; set; }
	public int Int32 { get; set; }
	public uint UInt32 { get; set; }
	public long Int64 { get; set; }
	public ulong UInt64 { get; set; }
	public float Single { get; set; }
	public double Double { get; set; }
	public decimal Decimal { get; set; }
	public char Character { get; set; }
	public System.DateTime DateTime { get; set; }
	public System.TimeSpan TimeSpan { get; set; }
	public object Object { get; set; }
	public Mode Enum { get; set; }
	public System.Guid Fallback { get; set; }
	public SteamEngine.Thing Thing { get; set; }
	public SteamEngine.Globals Globals { get; set; }
	public SteamEngine.AbstractScript Script { get; set; }
	public SteamEngine.Regions.Region Region { get; set; }
	public SteamEngine.IAbstractKey Key { get; set; }
	public SteamEngine.AbstractAccount Account { get; set; }
}
""";

		var run = Run(source, includeViewableContracts: true);
		var generated = Assert.Single(run.GeneratedSources);
		Assert.Contains("OptimizedLoad_String(value)", generated, StringComparison.Ordinal);
		foreach (var parser in new[] {
			"ParseBoolean(value)", "ParseByte(value)", "ParseSByte(value)", "ParseInt16(value)",
			"ParseUInt16(value)", "ParseInt32(value)", "ParseUInt32(value)", "ParseInt64(value)",
			"ParseUInt64(value)", "ParseSingle(value)", "ParseDouble(value)", "ParseDecimal(value)"
		}) {
			Assert.Contains(parser, generated, StringComparison.Ordinal);
		}
		Assert.Contains("Convert.ToChar(ObjectSaver.OptimizedLoad_SimpleType(value", generated, StringComparison.Ordinal);
		Assert.Contains("Convert.ToDateTime(global::SteamEngine.Persistence.ObjectSaver.OptimizedLoad_SimpleType(value", generated, StringComparison.Ordinal);
		Assert.Contains("ObjectSaver.Load(value)", generated, StringComparison.Ordinal);
		Assert.Contains("(global::Sandbox.Mode) SteamEngine.Common.ConvertTools.ParseUInt32(value)", generated, StringComparison.Ordinal);
		Assert.Contains("OptimizedLoad_SimpleType(value", generated, StringComparison.Ordinal);
		AssertNoErrors(run);
	}

	[Fact]
	public void GeneratedSource_IsStableAcrossTypeDeclarationOrder() {
		const string first = """
using SteamEngine.CompiledScripts.Dialogs;
namespace Sandbox;
[ViewableClass] public class Beta { public int Value { get; set; } }
[ViewableClass] public class Alpha { public int Count { get; set; } }
""";
		const string second = """
using SteamEngine.CompiledScripts.Dialogs;
namespace Sandbox;
[ViewableClass] public class Alpha { public int Count { get; set; } }
[ViewableClass] public class Beta { public int Value { get; set; } }
""";

		var firstGenerated = Assert.Single(Run(first, includeViewableContracts: true).GeneratedSources);
		var secondGenerated = Assert.Single(Run(second, includeViewableContracts: true).GeneratedSources);
		Assert.Equal(firstGenerated, secondGenerated);
	}

	[Fact]
	public void GeneratedSource_IsStableAcrossDescriptorDeclarationOrder() {
		const string first = """
using SteamEngine.CompiledScripts.Dialogs;
namespace Sandbox;
public class Target { }
[ViewDescriptor(typeof(Target), "A label")] public static class ADescriptor { }
[ViewDescriptor(typeof(Target), "Z label")] public static class ZDescriptor { }
""";
		const string second = """
using SteamEngine.CompiledScripts.Dialogs;
namespace Sandbox;
public class Target { }
[ViewDescriptor(typeof(Target), "Z label")] public static class ZDescriptor { }
[ViewDescriptor(typeof(Target), "A label")] public static class ADescriptor { }
""";

		var firstGenerated = Assert.Single(Run(first, includeViewableContracts: true).GeneratedSources);
		var secondGenerated = Assert.Single(Run(second, includeViewableContracts: true).GeneratedSources);
		Assert.Equal(firstGenerated, secondGenerated);
	}

	private static GeneratorRunSnapshot Run(
		string source,
		bool includeViewableContracts,
		params MetadataReference[] additionalReferences) {
		var syntaxTrees = new List<SyntaxTree> {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes)
		};
		if (includeViewableContracts) {
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(ViewableContracts));
		}
		syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));

		return GeneratorTestRunner.Run(
			new ViewableClassGenerator(),
			syntaxTrees,
			GeneratorTestRunner.GetRuntimeReferences().Concat(additionalReferences));
	}

	private static GeneratorRunSnapshot RunUnsafe(string source) {
		var syntaxTrees = new[] {
			CSharpSyntaxTree.ParseText(GeneratorTestStubs.CanonicalEngineTypes),
			CSharpSyntaxTree.ParseText(ViewableContracts),
			CSharpSyntaxTree.ParseText(source)
		};
		return GeneratorTestRunner.Run(
			new ViewableClassGenerator(),
			syntaxTrees,
			GeneratorTestRunner.GetRuntimeReferences(),
			allowUnsafe: true);
	}

	private static void AssertNoErrors(GeneratorRunSnapshot run, params string[] allowedDiagnosticIds) {
		var errors = run.Diagnostics.Concat(run.DriverDiagnostics).Concat(run.OutputCompilation.GetDiagnostics())
			.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		errors = errors.Where(diagnostic => !allowedDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal)).ToArray();
		Assert.Empty(errors);
	}
}
