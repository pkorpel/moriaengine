namespace SteamEngine.Generators;

internal enum DispatchParameterConversion {
	None,
	Cast,
	Convert,
	EnumConvertThenCast
}

internal readonly record struct DispatchParameterModel(
	bool UsesScriptArgsInput,
	DispatchParameterConversion Conversion,
	string TypeName);
