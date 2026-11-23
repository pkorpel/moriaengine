using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal sealed record RequiredTypeResolution(
	RequiredSymbols? Symbols,
	EquatableArray<Diagnostic> Diagnostics);

internal sealed class RequiredSymbols {
	internal RequiredSymbols(CanonicalTypeSymbols canonicalTypes, GeneratorAttributeSymbols attributes, INamedTypeSymbol? viewableClassDataViewType) {
		CanonicalTypes = canonicalTypes;
		Attributes = attributes;
		ViewableClassDataViewType = viewableClassDataViewType;
	}

	internal CanonicalTypeSymbols CanonicalTypes { get; }
	internal GeneratorAttributeSymbols Attributes { get; }
	internal INamedTypeSymbol? ViewableClassDataViewType { get; }
}
