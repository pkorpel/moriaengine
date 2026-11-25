namespace SteamEngine.Generators.CompiledTriggerGroups;

// Parser output for one declaration: a candidate or a diagnostic.
internal sealed record CompiledTriggerGroupParseResult(
	CompiledTriggerGroupCandidate? Candidate,
	GeneratorDiagnosticData? Diagnostic);

// Analyzer output: accepted models and diagnostics from all declarations.
internal sealed record CompiledTriggerGroupOutput(
	EquatableArray<CompiledTriggerGroupModel> Models,
	EquatableArray<GeneratorDiagnosticData> Diagnostics);

// Rendering data for one trigger group; emitted only after name validation.
internal sealed record CompiledTriggerGroupModel(
	string NamespaceName,
	string TypeName,
	EquatableArray<DispatchMethodModel> TriggerMethods);

// Parsed group with the type name and location needed for validation diagnostics.
internal sealed record CompiledTriggerGroupCandidate(
	CompiledTriggerGroupModel Model,
	string FullyQualifiedTypeName,
	SourceLocationData Location);
