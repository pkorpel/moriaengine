using System.Collections.Generic;

namespace SteamEngine.Generators.ViewableClasses;

// Parser output is made of records over strings and EquatableArray, never Roslyn symbols,
// so the incremental pipeline can compare and cache it between compilations.

/// <summary>What the parser got from one type declaration: a candidate (if any) and its diagnostics.</summary>
internal sealed record ViewableParseResult(
	ViewableCandidate? Candidate,
	EquatableArray<GeneratorDiagnosticData> Diagnostics);

/// <summary>
/// A viewable type, a descriptor, or both: a descriptor also carries a snapshot of the
/// type it handles, so that type gets a view even without [ViewableClass].
/// </summary>
internal sealed record ViewableCandidate(
	ViewableTypeData? ViewType,
	ViewDescriptorData? Descriptor);

/// <summary>
/// One type that gets a data view: its rows and buttons, its base types (nearest first) for
/// matching descriptors, and whether it came from a descriptor rather than its own declaration.
/// </summary>
internal sealed record ViewableTypeData(
	string TypeKey,
	string TypeName,
	string Label,
	EquatableArray<string> BaseTypeKeys,
	EquatableArray<ViewableFieldData> Fields,
	EquatableArray<ViewableButtonData> Buttons,
	bool IsDescriptorTargetSnapshot);

/// <summary>
/// One [ViewDescriptor] class: the type it handles, its label, the member names it hides
/// and the rows/buttons its static methods add.
/// </summary>
internal sealed record ViewDescriptorData(
	string TypeKey,
	string HandledTypeKey,
	string Label,
	EquatableArray<string> NonDisplayedFields,
	EquatableArray<ViewableFieldData> Fields,
	EquatableArray<ViewableButtonData> Buttons);

/// <summary>
/// One dialog row. Either a member (TargetTypeName + MemberName) or a descriptor row
/// (DescriptorTypeName + getter/setter methods). Identity is what helper classes are shared by.
/// </summary>
internal sealed record ViewableFieldData(
	string Identity,
	string Label,
	string FieldTypeName,
	string LoadExpression,
	bool CanWrite,
	string? TargetTypeName,
	string? MemberName,
	string? DescriptorTypeName,
	string? GetterMethodName,
	string? SetterMethodName);

/// <summary>
/// One action button: an instance method on TargetTypeName, or a static descriptor method
/// when DescriptorTypeName is set.
/// </summary>
internal sealed record ViewableButtonData(
	string Identity,
	string Label,
	string TargetTypeName,
	string MethodName,
	string? DescriptorTypeName);

// Analyzer output, consumed only by the Renderer within one RegisterSourceOutput call.

/// <summary>One generated data view class: handled type, headline and its rows in order.</summary>
internal sealed class ViewableDataViewModel {
	internal string TypeName { get; set; } = string.Empty;
	internal string Label { get; set; } = string.Empty;
	internal string ClassName { get; set; } = string.Empty;
	internal List<ViewableFieldData> Fields { get; set; } = new();
	internal List<ViewableButtonData> Buttons { get; set; } = new();
}

/// <summary>A shared row helper class and the row it renders.</summary>
internal sealed class ViewableFieldViewModel {
	internal string ClassName { get; set; } = string.Empty;
	internal ViewableFieldData Field { get; set; } = null!;
}

/// <summary>A shared button helper class and the button it renders.</summary>
internal sealed class ViewableButtonViewModel {
	internal string ClassName { get; set; } = string.Empty;
	internal ViewableButtonData Button { get; set; } = null!;
}

/// <summary>Everything the renderer prints, plus identity-to-class-name maps for wiring views to helpers.</summary>
internal sealed class ViewableOutput {
	internal List<ViewableDataViewModel> DataViews { get; set; } = new();
	internal List<ViewableFieldViewModel> FieldViews { get; set; } = new();
	internal List<ViewableButtonViewModel> ButtonViews { get; set; } = new();
	internal IReadOnlyDictionary<string, string> FieldClassNames { get; set; } = new Dictionary<string, string>();
	internal IReadOnlyDictionary<string, string> ButtonClassNames { get; set; } = new Dictionary<string, string>();
}
