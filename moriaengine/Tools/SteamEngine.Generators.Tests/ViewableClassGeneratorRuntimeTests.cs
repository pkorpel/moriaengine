using System;
using System.Collections.Generic;
using System.Linq;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.ViewableClasses;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Runs the Viewable generator against the real GameCore and Scripts assemblies, emits the
// result and drives the generated views through the real IDataView/IDataFieldView, as
// D_Info does. ViewableClassGeneratorTests covers diagnostics and generated text on stubs.
public sealed class ViewableClassGeneratorRuntimeTests {
	[Fact]
	public void PluginSubclass_GetsAViewWithoutAttribute() {
		const string source = """
namespace Sandbox;

public class BasePlugin : SteamEngine.Plugin { }

public sealed class ScriptPlugin : BasePlugin {
	public int State { get; set; }
}
""";

		var assembly = EmitAssembly(Run(source));
		var view = GetDataView(assembly.GetType("Sandbox.ScriptPlugin")!);
		// Plugin subclasses receive generated views even without [ViewableClass].
		Assert.Equal("ScriptPlugin", view.GetName(null!));
		Assert.Contains(GetFields(view, null!), field => field.GetName(null!) == "State");
		// The unannotated base plugin subclass also gets a generated view.
		GetDataView(assembly.GetType("Sandbox.BasePlugin")!);
	}

	[Fact]
	public void OverriddenButtons_CallTheMethodOnBaseAndDerivedViews() {
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

		var assembly = EmitAssembly(Run(source));
		foreach (var typeName in new[] { "Sandbox.BaseType", "Sandbox.DerivedType" }) {
			var target = Create(assembly, typeName);
			// Each view exposes the button and dispatches to the implementation for that view's type.
			Assert.Single(GetButtons(GetDataView(target), target)).OnButton(target);
			Assert.Equal(1, (int)((dynamic)target).Calls);
		}
	}

	[Fact]
	public void ViewableFieldsAndButtons_WorkAtRuntimeAndStructFieldsStayReadOnly() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class BaseTarget {
	public int BaseField { get; set; } = 11;
	public int Shared { get; set; } = 12;
}

[ViewableClass("Labeled target")]
public sealed class Target : BaseTarget {
	private int buttonCalls;

	[InfoField("Number label")]
	public int Number { get; set; } = 7;

	[InfoField("Raw field label")]
	public int RawField = 3;

	public readonly int ReadOnlyField = 4;

	public const int ConstantField = 5;

	[NoShow]
	public int HiddenField;

	[NoShow]
	public int HiddenProperty { get; set; }

	public int PrivateSetter { get; private set; } = 6;

	public int InitOnly { get; init; } = 8;

	public new string Shared { get; set; } = "derived";

	public int @event { get; set; }

	public static int StaticValue { get; set; }

	[Button("Increment")]
	public int Increment(int amount = 1) { buttonCalls += amount; return buttonCalls; }

	[Button]
	public void AddParams(params int[] values) { buttonCalls += values.Length; }

	[Button]
	public void @return() { buttonCalls += 2; }

	[Button]
	public static void IgnoredStaticButton() { }

	[Button]
	private void IgnoredPrivateButton() { }

	public int ButtonCalls => buttonCalls;
}

[ViewableClass("Position")]
public struct Position {
	public int X { get; set; }
}
""";

		var run = Run(source);
		var assembly = EmitAssembly(run);
		var target = Create(assembly, "Sandbox.Target");
		var view = GetDataView(target);
		// The type's explicit label is used for the dialog headline.
		Assert.Equal("Labeled target", view.GetName(target));

		// Public instance fields/properties are exposed with InfoField labels and inherited members;
		// NoShow, static, constant and shadowed base members are excluded.
		var fields = GetFields(view, target);
		Assert.Equal(fields.Length, view.GetFieldsCount(target));
		var fieldNames = fields.Select(field => field.GetName(target)).ToArray();
		Assert.Contains("Number label", fieldNames);
		Assert.Contains("Raw field label", fieldNames);
		Assert.Contains("ReadOnlyField", fieldNames);
		Assert.Contains("PrivateSetter", fieldNames);
		Assert.Contains("InitOnly", fieldNames);
		Assert.Contains("BaseField", fieldNames);
		Assert.Contains("Shared", fieldNames);
		Assert.Contains("event", fieldNames);
		Assert.DoesNotContain("HiddenField", fieldNames);
		Assert.DoesNotContain("HiddenProperty", fieldNames);
		Assert.DoesNotContain("StaticValue", fieldNames);
		Assert.DoesNotContain("ConstantField", fieldNames);

		// Writable fields support typed and string-based access; getters without public setters are read-only.
		var numberField = Assert.Single(fields, field => field.GetName(target) == "Number label");
		Assert.False(numberField.ReadOnly);
		Assert.Equal(7, numberField.GetValue(target));
		numberField.SetValue(target, 19);
		Assert.Equal(19, (int)((dynamic)target).Number);
		numberField.SetStringValue(target, "42");
		Assert.Equal(42, (int)((dynamic)target).Number);
		Assert.Equal("42", numberField.GetStringValue(target));

		var readOnlyField = Assert.Single(fields, field => field.GetName(target) == "ReadOnlyField");
		Assert.True(readOnlyField.ReadOnly);
		Assert.Equal(4, readOnlyField.GetValue(target));
		Assert.True(Assert.Single(fields, field => field.GetName(target) == "PrivateSetter").ReadOnly);
		Assert.True(Assert.Single(fields, field => field.GetName(target) == "InitOnly").ReadOnly);

		// A hidden base property is resolved to the derived property and its declared type.
		var sharedField = Assert.Single(fields, field => field.GetName(target) == "Shared");
		Assert.Equal(typeof(string), sharedField.FieldType);
		Assert.Equal("derived", sharedField.GetValue(target));

		// Eligible instance buttons are exposed and callable; both field and button pages honor offsets.
		var buttons = GetButtons(view, target);
		Assert.Equal(3, view.GetActionButtonsCount(target));
		Assert.Equal(buttons.Length - 1, GetButtons(view, target, firstIndex: 1).Length);
		Assert.Contains(buttons, button => button.GetName(target) == "Increment");
		Assert.Contains(buttons, button => button.GetName(target) == "AddParams");
		Assert.Contains(buttons, button => button.GetName(target) == "return");
		foreach (var button in buttons) {
			button.OnButton(target);
		}
		Assert.Equal(3, (int)((dynamic)target).ButtonCalls);
		Assert.Equal(fields.Length - 1, GetFields(view, target, firstIndex: 1).Length);

		// Struct fields are viewable but read-only, even when the property has a setter.
		var position = Create(assembly, "Sandbox.Position");
		var positionView = GetDataView(position);
		Assert.Equal("Position", positionView.GetName(position));
		var positionField = Assert.Single(GetFields(positionView, position));
		Assert.True(positionField.ReadOnly);
	}

	[Fact]
	public void DescriptorFieldsAndButtonsComposeParentFirstAndWorkAtRuntime() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class BaseTarget {
	public int Hidden { get; set; } = 1;
	public int Visible { get; set; } = 3;
}

[ViewableClass("Derived view")]
public sealed class DerivedTarget : BaseTarget {
	public string Own { get; set; } = "own";
}

[ViewDescriptor(typeof(BaseTarget), "Base descriptor", new[] { "Hidden" })]
public class BaseDescriptor {
	[GetMethod("Computed", typeof(int))]
	public static object GetComputed(object target) => ((BaseTarget)target).Visible * 2;

	[SetMethod("Computed", typeof(int))]
	public static void SetComputed(object target, object value) => ((BaseTarget)target).Visible = (int)value;

	[Button("Reset")]
	public static void Reset(object target) => ((BaseTarget)target).Visible = 0;

	[Button("Ignored private")]
	private static void HiddenButton(object target) { }

	[Button("Ignored instance")]
	public void InstanceButton(object target) { }
}

[ViewDescriptor(typeof(DerivedTarget), "Derived descriptor")]
public static class DerivedDescriptor {
	[GetMethod("Derived computed", typeof(string))]
	public static object GetDerived(object target) => ((DerivedTarget)target).Own;
}
""";

		var run = Run(source);
		var assembly = EmitAssembly(run);
		var target = Create(assembly, "Sandbox.DerivedTarget");
		var view = GetDataView(target);
		// A declared viewable label takes precedence over any descriptor label.
		Assert.Equal("Derived view", view.GetName(target));

		// Base and derived descriptors compose parent-first; the base descriptor can hide a member.
		var fields = GetFields(view, target);
		var names = fields.Select(field => field.GetName(target)).ToArray();
		Assert.Equal(new[] { "Computed", "Derived computed", "Own", "Visible" }, names);
		Assert.Equal(fields.Length, view.GetFieldsCount(target));
		Assert.DoesNotContain("Hidden", names);

		// Descriptor getters/setters participate in normal field access and string conversion.
		var computed = fields[0];
		Assert.Equal(6, computed.GetValue(target));
		computed.SetValue(target, 17);
		Assert.Equal(17, (int)((dynamic)target).Visible);
		computed.SetStringValue(target, "23");
		Assert.Equal(23, (int)((dynamic)target).Visible);
		Assert.Equal("46", computed.GetStringValue(target));

		// Only public static descriptor buttons are included and invoked against the target.
		var reset = Assert.Single(GetButtons(view, target));
		Assert.Equal(1, view.GetActionButtonsCount(target));
		Assert.Equal("Reset", reset.GetName(target));
		Assert.Empty(GetButtons(view, target, firstIndex: 1));
		reset.OnButton(target);
		Assert.Equal(0, (int)((dynamic)target).Visible);
	}

	[Fact]
	public void DescriptorLabelIsUsedUnlessHandledTypeHasViewableLabel() {
		const string source = """
using SteamEngine.CompiledScripts.Dialogs;

namespace Sandbox;

public class DefaultLabelTarget { }

[ViewDescriptor(typeof(DefaultLabelTarget))]
public static class DefaultLabel { }

public class DescriptorOnlyTarget { }

[ViewDescriptor(typeof(DescriptorOnlyTarget), "Descriptor label")]
public static class NamedDescriptor { }

[ViewableClass("Explicit viewable label")]
public class ExplicitTarget { }

[ViewDescriptor(typeof(ExplicitTarget), "Ignored descriptor label")]
public static class ExplicitTargetDescriptor { }

public struct StructTarget { }

[ViewDescriptor(typeof(StructTarget), "Struct descriptor label")]
public static class StructTargetDescriptor { }
""";

		var run = Run(source);
		var assembly = EmitAssembly(run);
		var defaultTarget = Create(assembly, "Sandbox.DefaultLabelTarget");
		// Without an explicit label, descriptor-only views fall back to the handled type name.
		Assert.Equal("DefaultLabelTarget", GetDataView(defaultTarget).GetName(defaultTarget));

		// A descriptor label names a type that has no [ViewableClass].
		var descriptorTarget = Create(assembly, "Sandbox.DescriptorOnlyTarget");
		Assert.Equal("Descriptor label", GetDataView(descriptorTarget).GetName(descriptorTarget));

		var explicitTarget = Create(assembly, "Sandbox.ExplicitTarget");
		// A [ViewableClass] label wins over a descriptor label for the same type.
		Assert.Equal("Explicit viewable label", GetDataView(explicitTarget).GetName(explicitTarget));

		// Descriptor labels also apply to struct targets.
		var structTarget = Create(assembly, "Sandbox.StructTarget");
		Assert.Equal("Struct descriptor label", GetDataView(structTarget).GetName(structTarget));
	}

	private static GeneratorRunSnapshot Run(string source) => EngineRuntimeHarness.Run(new ViewableClassGenerator(), source);

	private static System.Reflection.Assembly EmitAssembly(GeneratorRunSnapshot run) => EngineRuntimeHarness.Emit(run);

	private static object Create(System.Reflection.Assembly assembly, string typeName) => EngineRuntimeHarness.Create(assembly, typeName);

	private static IDataView GetDataView(object target) => GetDataView(target.GetType());

	// The emitted assembly's views, matched on HandledType like DataViewProvider does.
	private static IDataView GetDataView(Type handledType) {
		return Assert.Single(EngineRuntimeHarness.CreateAll<IDataView>(handledType.Assembly), view => view.HandledType == handledType);
	}

	private static IDataFieldView[] GetFields(IDataView view, object target, int firstIndex = 0) {
		return view.GetDataFieldsPage(firstIndex, target).ToArray();
	}

	private static ButtonDataFieldView[] GetButtons(IDataView view, object target, int firstIndex = 0) {
		return view.GetActionButtonsPage(firstIndex, target).ToArray();
	}
}
