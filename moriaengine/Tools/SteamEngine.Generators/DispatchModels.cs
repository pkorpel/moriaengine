namespace SteamEngine.Generators;

internal enum DispatchParameterConversion {
	None,
	Cast,
	Convert,
	EnumConvertThenCast
}

// How one trigger parameter is obtained and converted for invocation.
internal readonly record struct DispatchParameterModel(
	bool UsesScriptArgsInput,
	DispatchParameterConversion Conversion,
	string TypeName);

// Shared rendering data for invoking a plugin or compiled-group trigger.
internal sealed record DispatchMethodModel(
	string TriggerName,
	string MethodName,
	bool ReturnsVoid,
	EquatableArray<DispatchParameterModel> Parameters);
