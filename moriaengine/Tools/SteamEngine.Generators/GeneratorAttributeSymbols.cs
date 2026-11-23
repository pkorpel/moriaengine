using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal sealed class GeneratorAttributeSymbols {
	internal INamedTypeSymbol SaveableDataAttribute { get; set; } = null!;
	internal INamedTypeSymbol CopyableDataAttribute { get; set; } = null!;
	internal INamedTypeSymbol SteamFunctionAttribute { get; set; } = null!;
	internal INamedTypeSymbol ThingTemplateAttribute { get; set; } = null!;
	internal INamedTypeSymbol ThingDefTemplateAttribute { get; set; } = null!;
	internal INamedTypeSymbol PluginTemplateAttribute { get; set; } = null!;
	internal INamedTypeSymbol PluginDefTemplateAttribute { get; set; } = null!;
	internal INamedTypeSymbol DefFieldAttribute { get; set; } = null!;
	internal INamedTypeSymbol DeepCopyableClassAttribute { get; set; } = null!;
	internal INamedTypeSymbol DeepCopyImplementationAttribute { get; set; } = null!;
	internal INamedTypeSymbol SaveableClassAttribute { get; set; } = null!;
	internal INamedTypeSymbol LoadingInitializerAttribute { get; set; } = null!;
	internal INamedTypeSymbol LoadingFinalizerAttribute { get; set; } = null!;
	internal INamedTypeSymbol LoadSectionAttribute { get; set; } = null!;
	internal INamedTypeSymbol LoadLineAttribute { get; set; } = null!;
	internal INamedTypeSymbol SaveAttribute { get; set; } = null!;
	internal INamedTypeSymbol ViewableClassAttribute { get; set; } = null!;
	internal INamedTypeSymbol NoShowAttribute { get; set; } = null!;
	internal INamedTypeSymbol InfoFieldAttribute { get; set; } = null!;
	internal INamedTypeSymbol ButtonAttribute { get; set; } = null!;
	internal INamedTypeSymbol ViewDescriptorAttribute { get; set; } = null!;
	internal INamedTypeSymbol GetMethodAttribute { get; set; } = null!;
	internal INamedTypeSymbol SetMethodAttribute { get; set; } = null!;

}
