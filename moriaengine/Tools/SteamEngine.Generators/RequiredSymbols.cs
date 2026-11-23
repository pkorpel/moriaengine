using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal sealed record RequiredTypeResolution(
	RequiredSymbols? Symbols,
	EquatableArray<Diagnostic> Diagnostics);

internal sealed class RequiredSymbols {
	internal RequiredSymbols(CanonicalTypeSymbols canonicalTypes, GeneratorAttributeSymbols attributes) {
		CanonicalTypes = canonicalTypes;
		Attributes = attributes;
	}

	internal CanonicalTypeSymbols CanonicalTypes { get; }
	internal GeneratorAttributeSymbols Attributes { get; }
}
