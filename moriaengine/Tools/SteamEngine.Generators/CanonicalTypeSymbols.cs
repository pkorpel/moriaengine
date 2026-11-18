using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal sealed class CanonicalTypeSymbols {
	internal INamedTypeSymbol ThingType { get; set; } = null!;
	internal INamedTypeSymbol AbstractScriptType { get; set; } = null!;
	internal INamedTypeSymbol RegionType { get; set; } = null!;
	internal INamedTypeSymbol GlobalsType { get; set; } = null!;
	internal INamedTypeSymbol IAbstractKeyType { get; set; } = null!;
	internal INamedTypeSymbol AbstractAccountType { get; set; } = null!;
	internal INamedTypeSymbol ScriptHolderType { get; set; } = null!;
	internal INamedTypeSymbol ScriptArgsType { get; set; } = null!;
	internal INamedTypeSymbol CompiledScriptHolderType { get; set; } = null!;
	internal INamedTypeSymbol DeepCopyFactoryType { get; set; } = null!;
	internal INamedTypeSymbol IDeepCopyImplementorType { get; set; } = null!;
	internal INamedTypeSymbol PropsSectionType { get; set; } = null!;
	internal INamedTypeSymbol PropsLineType { get; set; } = null!;
	internal INamedTypeSymbol SaveStreamType { get; set; } = null!;
	internal INamedTypeSymbol LoadObjectParamType { get; set; } = null!;
	internal INamedTypeSymbol ObjectSaverType { get; set; } = null!;
	internal INamedTypeSymbol DecoratedClassesSaveImplementorType { get; set; } = null!;
	internal INamedTypeSymbol CoreLoggerType { get; set; } = null!;
	internal INamedTypeSymbol SEExceptionType { get; set; } = null!;
	internal INamedTypeSymbol FatalExceptionType { get; set; } = null!;

}
