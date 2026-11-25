namespace SteamEngine.Generators.CompiledScriptHolders;

// Parsed function with the display name and location needed for duplicate diagnostics.
internal sealed record ScriptHolderCandidate(
	string DisplayName, SourceLocationData Location, ScriptHolderModel Model);

// Parser output for one declaration; the source position preserves diagnostic order.
internal sealed record ScriptHolderParseResult(
	int TreeIndex, int SpanStart,
	ScriptHolderCandidate? Candidate, EquatableArray<GeneratorDiagnosticData> Diagnostics);

// Analyzer output: accepted models and diagnostics from all declarations.
internal sealed record ScriptHolderOutput(
	EquatableArray<ScriptHolderModel> Models, EquatableArray<GeneratorDiagnosticData> Diagnostics);

// Rendering data for one function; emitted only after function-name validation.
internal sealed record ScriptHolderModel(
	string GeneratedTypeName,
	string FunctionName,
	string? Description,
	string DeclaringTypeName,
	string MethodName,
	bool ReturnsVoid,
	bool NeedsArgvArray,
	EquatableArray<InvocationParameterModel> Parameters);

// Expression used to pass one argument to the script function.
internal sealed record InvocationParameterModel(string Expression);
