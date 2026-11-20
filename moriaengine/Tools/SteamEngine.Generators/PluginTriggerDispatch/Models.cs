using System.Collections.Immutable;
using SteamEngine.Generators;

namespace SteamEngine.Generators.PluginTriggerDispatch;

internal enum PluginTriggerDiagnosticKind {
	TypeMustBePartial,
	NestedTypeNotSupported
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

internal sealed record PluginTriggerMethodData(
	string TriggerName,
	string MethodName,
	bool ReturnsVoid,
	SourceLocationData Location,
	EquatableArray<DispatchParameterModel> Parameters);

internal sealed record PluginTriggerCandidate(
	string TypeIdentity,
	string NamespaceName,
	string TypeName,
	string FullyQualifiedTypeName,
	SourceLocationData TypeLocation,
	EquatableArray<PluginTriggerMethodData> TriggerMethods);

internal sealed record PluginTriggerParseResult(
	PluginTriggerCandidate? Candidate,
	EquatableArray<PluginTriggerDiagnosticData> Diagnostics);
