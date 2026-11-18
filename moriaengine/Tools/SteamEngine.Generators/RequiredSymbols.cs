namespace SteamEngine.Generators;

internal sealed class RequiredSymbols {
	internal RequiredSymbols(CanonicalTypeSymbols canonicalTypes, GeneratorAttributeSymbols attributes) {
		CanonicalTypes = canonicalTypes;
		Attributes = attributes;
	}

	internal CanonicalTypeSymbols CanonicalTypes { get; }
	internal GeneratorAttributeSymbols Attributes { get; }
}
