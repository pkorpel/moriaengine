using System.Collections.Immutable;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal enum PluginTriggerDiagnosticKind {
	TypeMustBePartial,
	ContainingTypeMustBePartial,
	DuplicateTriggerName
}

internal enum PluginInvocationParameterConversion {
	None,
	Cast,
	Convert,
	EnumConvertThenCast
}

internal readonly record struct SourceLocationData(
	string FilePath,
	int StartLine,
	int StartCharacter,
	int EndLine,
	int EndCharacter);

internal readonly record struct PluginTriggerDiagnosticData(
	PluginTriggerDiagnosticKind Kind,
	SourceLocationData Location,
	string Argument0,
	string Argument1);

internal readonly record struct PluginInvocationParameterData(
	bool UsesScriptArgsInput,
	PluginInvocationParameterConversion Conversion,
	string TypeName);

internal sealed record PluginTriggerMethodData(
	string TriggerName,
	string MethodName,
	bool ReturnsVoid,
	SourceLocationData Location,
	ImmutableArray<PluginInvocationParameterData> Parameters);

internal sealed record ContainingTypeData(
	string Accessibility,
	bool IsStatic,
	bool IsAbstract,
	bool IsSealed,
	string TypeName);

internal sealed record PluginTriggerCandidate(
	string TypeIdentity,
	string NamespaceName,
	ImmutableArray<ContainingTypeData> ContainingTypes,
	string TypeAccessibility,
	string TypeName,
	string FullyQualifiedTypeName,
	SourceLocationData TypeLocation,
	ImmutableArray<PluginTriggerMethodData> TriggerMethods);

internal sealed record PluginTriggerParseResult(
	PluginTriggerCandidate? Candidate,
	ImmutableArray<PluginTriggerDiagnosticData> Diagnostics);
