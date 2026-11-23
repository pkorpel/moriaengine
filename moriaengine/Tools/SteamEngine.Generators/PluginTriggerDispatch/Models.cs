namespace SteamEngine.Generators.PluginTriggerDispatch;

// Rendering data for one plugin; emitted only after its trigger names are validated.
internal sealed record PluginTriggerModel(
	string NamespaceName,
	string TypeName,
	string FullyQualifiedTypeName,
	EquatableArray<DispatchMethodModel> TriggerMethods);

// Parsed plugin and its diagnostic location, awaiting trigger-name validation.
internal sealed record PluginTriggerCandidate(
	PluginTriggerModel Model,
	SourceLocationData TypeLocation);

// Parser output for one plugin: a candidate or diagnostics.
internal sealed record PluginTriggerParseResult(
	PluginTriggerCandidate? Candidate,
	EquatableArray<GeneratorDiagnosticData> Diagnostics);

// Analyzer output for one plugin: an optional accepted model and diagnostics.
internal sealed record PluginTriggerOutput(
	PluginTriggerModel? Model,
	EquatableArray<GeneratorDiagnosticData> Diagnostics);
