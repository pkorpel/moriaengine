using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal static class Analyzer {
	internal const string DeepCopyableClassAttributeMetadataName = "SteamEngine.DeepCopyableClassAttribute";
	internal const string DeepCopyImplementationAttributeMetadataName = "SteamEngine.DeepCopyImplementationAttribute";
	internal const string CopyableDataAttributeMetadataName = "SteamEngine.CopyableDataAttribute";

	internal static readonly DiagnosticDescriptor MissingGeneratorAttributeDeclarationDescriptor = new(
		"SEGEN201",
		"Missing generator attribute declaration",
		"Required generator attribute declaration '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor MissingCanonicalEngineTypeDescriptor = new(
		"SEGEN202",
		"Missing canonical engine type",
		"Required canonical engine type '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor InvalidDeepCopyImplementationDescriptor = new(
		"SEGEN203",
		"Invalid deep-copy implementation member",
		"Member '{0}' on '{1}' has [DeepCopyImplementation] but {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateDeepCopyImplementationDescriptor = new(
		"SEGEN204",
		"Multiple deep-copy implementation members",
		"Type '{0}' has multiple members marked with [DeepCopyImplementation]",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor MissingDeepCopyImplementationDescriptor = new(
		"SEGEN205",
		"Missing deep-copy implementation member",
		"Type '{0}' is marked with [DeepCopyableClass] but has no valid [DeepCopyImplementation] member",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor UnsupportedCopyableMemberDescriptor = new(
		"SEGEN206",
		"Unsupported [CopyableData] member usage",
		"Member '{0}' on '{1}' is marked with [CopyableData], but {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor AttributeShadowingDescriptor = new(
		"SEGEN207",
		"Shadowed generator attribute",
		"Attribute '{0}' resolves to '{1}', but this generator expects '{2}'",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static readonly DiagnosticDescriptor DuplicateGeneratedImplementorNameDescriptor = new(
		"SEGEN208",
		"Duplicate deep-copyable type name",
		"Deep-copyable type name '{0}' is declared by both '{1}' and '{2}' (names must be globally unique, case-insensitive)",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor TypeNameConflictDescriptor = new(
		"SEGEN209",
		"Type name conflicts with canonical engine type",
		"Type '{0}' matches canonical name '{1}' but is a different symbol; generation uses canonical symbol identity",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static bool ValidateRequiredDeclarations(Compilation compilation, ICollection<Diagnostic> diagnostics) {
		var requiredMetadataNames = new[] {
			DeepCopyableClassAttributeMetadataName,
			DeepCopyImplementationAttributeMetadataName,
			CopyableDataAttributeMetadataName
		};

		var allPresent = true;
		foreach (var metadataName in requiredMetadataNames) {
			if (compilation.GetTypeByMetadataName(metadataName) is not null) {
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				MissingGeneratorAttributeDeclarationDescriptor,
				Location.None,
				metadataName));
			allPresent = false;
		}
		return allPresent;
	}

	internal static bool ValidateRequiredCanonicalTypes(Compilation compilation, ICollection<Diagnostic> diagnostics) {
		var requiredMetadataNames = new[] {
			"SteamEngine.IDeepCopyImplementor",
			"SteamEngine.DeepCopyFactory",
			"SteamEngine.AbstractScript",
			"SteamEngine.Regions.Region",
			"SteamEngine.Globals",
			"SteamEngine.IAbstractKey",
			"SteamEngine.AbstractAccount"
		};

		var allPresent = true;
		foreach (var metadataName in requiredMetadataNames) {
			if (compilation.GetTypeByMetadataName(metadataName) is not null) {
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				MissingCanonicalEngineTypeDescriptor,
				Location.None,
				metadataName));
			allPresent = false;
		}
		return allPresent;
	}

	internal static IReadOnlyList<DeepCopyModel> BuildModels(
		IReadOnlyCollection<DeepCopyCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		Compilation compilation) {
		var deepCopyImplementationAttribute = compilation.GetTypeByMetadataName(DeepCopyImplementationAttributeMetadataName);
		var copyableDataAttribute = compilation.GetTypeByMetadataName(CopyableDataAttributeMetadataName);
		var knownTypes = KnownTypeSymbols.Create(compilation);
		if (deepCopyImplementationAttribute is null || copyableDataAttribute is null) {
			return Array.Empty<DeepCopyModel>();
		}

		var models = new List<DeepCopyModel>();
		var conflictingTypeNames = ValidateDistinctSourceTypeNames(candidates, diagnostics);
		foreach (var candidate in candidates
			.OrderBy(static c => c.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)) {
			if (conflictingTypeNames.Contains(candidate.Symbol.Name)) {
				continue;
			}

			if (!TryBuildModel(candidate.Symbol, deepCopyImplementationAttribute, copyableDataAttribute, knownTypes, diagnostics, out var model)) {
				continue;
			}
			models.Add(model);
		}

		return models;
	}

	private static bool TryBuildModel(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol deepCopyImplementationAttribute,
		INamedTypeSymbol copyableDataAttribute,
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		out DeepCopyModel model) {
		model = null!;

		var hasErrors = false;
		if (!TryResolveInitializer(typeSymbol, deepCopyImplementationAttribute, diagnostics, out var initializer)) {
			hasErrors = true;
		}

		if (!TryResolveCopyableMembers(typeSymbol, copyableDataAttribute, knownTypes, diagnostics, out var copyableMembers)) {
			hasErrors = true;
		}

		if (hasErrors) {
			return false;
		}

		var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var generatedTypeName = "GeneratedDeepCopyImplementor_" + SanitizeIdentifier(typeSymbol.Name);
		var members = new List<DeepCopyMemberModel>();
		foreach (var member in copyableMembers) {
			members.Add(new DeepCopyMemberModel(
				member.Name,
				member.GetMemberTypeDisplayName(),
				member.ValueKind));
		}

		model = new DeepCopyModel(
			generatedTypeName,
			typeName,
			initializer,
			members);
		return true;
	}

	private static bool TryResolveInitializer(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol deepCopyImplementationAttribute,
		ICollection<Diagnostic> diagnostics,
		out DeepCopyInitializerModel initializer) {
		initializer = null!;

		var attributedMembers = new List<IMethodSymbol>();
		foreach (var member in typeSymbol.GetMembers()) {
			if (!HasCanonicalAttribute(member, deepCopyImplementationAttribute, diagnostics)) {
				continue;
			}

			if (member is not IMethodSymbol attributedMethod) {
				diagnostics.Add(Diagnostic.Create(
					InvalidDeepCopyImplementationDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(),
					typeSymbol.ToDisplayString(),
					"it must be placed on a constructor or static method"));
				continue;
			}

			attributedMembers.Add(attributedMethod);
		}

		if (attributedMembers.Count == 0) {
			diagnostics.Add(Diagnostic.Create(
				MissingDeepCopyImplementationDescriptor,
				typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString()));
			return false;
		}

		if (attributedMembers.Count > 1) {
			diagnostics.Add(Diagnostic.Create(
				DuplicateDeepCopyImplementationDescriptor,
				attributedMembers[1].Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString()));
			return false;
		}

		var methodSymbol = attributedMembers[0];
		if (methodSymbol.MethodKind == MethodKind.Ordinary) {
			if (!methodSymbol.IsStatic) {
				diagnostics.Add(Diagnostic.Create(
					InvalidDeepCopyImplementationDescriptor,
					methodSymbol.Locations.FirstOrDefault(),
					methodSymbol.ToDisplayString(),
					typeSymbol.ToDisplayString(),
					"method is not static"));
				return false;
			}
			if (!CanAssign(typeSymbol, methodSymbol.ReturnType)) {
				diagnostics.Add(Diagnostic.Create(
					InvalidDeepCopyImplementationDescriptor,
					methodSymbol.Locations.FirstOrDefault(),
					methodSymbol.ToDisplayString(),
					typeSymbol.ToDisplayString(),
					"method return type is not assignable from the declaring type"));
				return false;
			}
		} else if (methodSymbol.MethodKind != MethodKind.Constructor) {
			diagnostics.Add(Diagnostic.Create(
				InvalidDeepCopyImplementationDescriptor,
				methodSymbol.Locations.FirstOrDefault(),
				methodSymbol.ToDisplayString(),
				typeSymbol.ToDisplayString(),
				"it must be placed on a constructor or static method"));
			return false;
		}

		if (!AcceptsSupportedInitializerParameters(typeSymbol, methodSymbol)) {
			diagnostics.Add(Diagnostic.Create(
				InvalidDeepCopyImplementationDescriptor,
				methodSymbol.Locations.FirstOrDefault(),
				methodSymbol.ToDisplayString(),
				typeSymbol.ToDisplayString(),
				"it must have zero parameters or a single parameter of the declaring type"));
			return false;
		}

		var initializerKind = methodSymbol.MethodKind == MethodKind.Constructor
			? DeepCopyInitializerKind.Constructor
			: DeepCopyInitializerKind.StaticMethod;
		initializer = new DeepCopyInitializerModel(
			initializerKind,
			methodSymbol.Name,
			methodSymbol.Parameters.Length == 1);
		return true;
	}

	private static bool TryResolveCopyableMembers(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol copyableDataAttribute,
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		out IReadOnlyList<IPropertyOrFieldSymbol> members) {
		var result = new List<IPropertyOrFieldSymbol>();
		var hasErrors = false;

		foreach (var member in typeSymbol.GetMembers()) {
			if (!HasCanonicalAttribute(member, copyableDataAttribute, diagnostics)) {
				continue;
			}

			if (member is IFieldSymbol fieldSymbol) {
				if (fieldSymbol.IsStatic || fieldSymbol.DeclaredAccessibility != Accessibility.Public) {
					diagnostics.Add(Diagnostic.Create(
						UnsupportedCopyableMemberDescriptor,
						fieldSymbol.Locations.FirstOrDefault(),
						fieldSymbol.ToDisplayString(),
						typeSymbol.ToDisplayString(),
						"only public instance fields are supported"));
					hasErrors = true;
					continue;
				}

				result.Add(new FieldWrapper(
					fieldSymbol,
					GetMemberValueKind(fieldSymbol.Type, knownTypes, diagnostics, fieldSymbol.Locations.FirstOrDefault() ?? Location.None)));
				continue;
			}

			if (member is IPropertySymbol propertySymbol) {
				var getterPublic = propertySymbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;
				var setterPublic = propertySymbol.SetMethod?.DeclaredAccessibility == Accessibility.Public;
				if (propertySymbol.IsStatic || propertySymbol.Parameters.Length != 0 || !getterPublic || !setterPublic) {
					diagnostics.Add(Diagnostic.Create(
						UnsupportedCopyableMemberDescriptor,
						propertySymbol.Locations.FirstOrDefault(),
						propertySymbol.ToDisplayString(),
						typeSymbol.ToDisplayString(),
						"only non-indexer public instance properties with public getter and setter are supported"));
					hasErrors = true;
					continue;
				}

				result.Add(new PropertyWrapper(
					propertySymbol,
					GetMemberValueKind(propertySymbol.Type, knownTypes, diagnostics, propertySymbol.Locations.FirstOrDefault() ?? Location.None)));
				continue;
			}

			diagnostics.Add(Diagnostic.Create(
				UnsupportedCopyableMemberDescriptor,
				member.Locations.FirstOrDefault(),
				member.ToDisplayString(),
				typeSymbol.ToDisplayString(),
				"only fields and properties are supported"));
			hasErrors = true;
		}

		members = result
			.OrderBy(static m => m.Name, StringComparer.Ordinal)
			.ToList();
		return !hasErrors;
	}

	private static HashSet<string> ValidateDistinctSourceTypeNames(
		IReadOnlyCollection<DeepCopyCandidate> candidates,
		ICollection<Diagnostic> diagnostics) {
		var byName = new Dictionary<string, INamedTypeSymbol>(StringComparer.OrdinalIgnoreCase);
		var conflictingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var candidate in candidates
			.OrderBy(static c => c.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)) {
			var current = candidate.Symbol;
			if (!byName.TryGetValue(current.Name, out var existing)) {
				byName[current.Name] = current;
				continue;
			}

			conflictingNames.Add(current.Name);
			diagnostics.Add(Diagnostic.Create(
				DuplicateGeneratedImplementorNameDescriptor,
				current.Locations.FirstOrDefault() ?? Location.None,
				current.Name,
				existing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		}

		return conflictingNames;
	}

	private static MemberValueKind GetMemberValueKind(
		ITypeSymbol type,
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		if (type.TypeKind == TypeKind.Enum) {
			return MemberValueKind.Enum;
		}
		if (type.SpecialType == SpecialType.System_Object) {
			return MemberValueKind.Object;
		}

		switch (type.SpecialType) {
			case SpecialType.System_String: return MemberValueKind.String;
			case SpecialType.System_Boolean: return MemberValueKind.Boolean;
			case SpecialType.System_Byte: return MemberValueKind.Byte;
			case SpecialType.System_SByte: return MemberValueKind.SByte;
			case SpecialType.System_Int16: return MemberValueKind.Int16;
			case SpecialType.System_UInt16: return MemberValueKind.UInt16;
			case SpecialType.System_Int32: return MemberValueKind.Int32;
			case SpecialType.System_UInt32: return MemberValueKind.UInt32;
			case SpecialType.System_Int64: return MemberValueKind.Int64;
			case SpecialType.System_UInt64: return MemberValueKind.UInt64;
			case SpecialType.System_Single: return MemberValueKind.Single;
			case SpecialType.System_Double: return MemberValueKind.Double;
			case SpecialType.System_Decimal: return MemberValueKind.Decimal;
			case SpecialType.System_Char: return MemberValueKind.Char;
		}

		if (string.Equals(type.Name, "DateTime", StringComparison.Ordinal)) {
			return MemberValueKind.DateTime;
		}
		if (string.Equals(type.Name, "TimeSpan", StringComparison.Ordinal)) {
			return MemberValueKind.TimeSpan;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractScript, "AbstractScript", diagnostics, location)) {
			return MemberValueKind.AbstractScript;
		}
		if (IsTypeOrBase(type, knownTypes.Region, "Region", diagnostics, location)) {
			return MemberValueKind.Region;
		}
		if (IsTypeOrBase(type, knownTypes.Globals, "Globals", diagnostics, location)) {
			return MemberValueKind.Globals;
		}
		if (ImplementsInterface(type, knownTypes.IAbstractKey, "IAbstractKey", diagnostics, location)) {
			return MemberValueKind.IAbstractKey;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractAccount, "AbstractAccount", diagnostics, location)) {
			return MemberValueKind.AbstractAccount;
		}

		return MemberValueKind.Fallback;
	}

	private static bool IsTypeOrBase(
		ITypeSymbol type,
		INamedTypeSymbol canonical,
		string expectedName,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType) {
			if (SymbolEqualityComparer.Default.Equals(current, canonical)) {
				return true;
			}

			if (string.Equals(current.Name, expectedName, StringComparison.Ordinal)) {
				diagnostics.Add(CreateTypeNameConflictDiagnostic(location, current, canonical));
			}
		}

		return false;
	}

	private static bool ImplementsInterface(
		ITypeSymbol type,
		INamedTypeSymbol canonical,
		string expectedName,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		foreach (var iface in type.AllInterfaces) {
			if (SymbolEqualityComparer.Default.Equals(iface, canonical)) {
				return true;
			}

			if (string.Equals(iface.Name, expectedName, StringComparison.Ordinal)) {
				diagnostics.Add(CreateTypeNameConflictDiagnostic(location, iface, canonical));
			}
		}

		return false;
	}

	private static Diagnostic CreateTypeNameConflictDiagnostic(Location location, ITypeSymbol actual, INamedTypeSymbol canonical) {
		return Diagnostic.Create(
			TypeNameConflictDescriptor,
			location,
			actual.Name,
			actual.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			canonical.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	private static bool HasCanonicalAttribute(
		ISymbol member,
		INamedTypeSymbol canonicalAttribute,
		ICollection<Diagnostic> diagnostics) {
		var expectedSimpleName = Parser.StripAttributeSuffix(canonicalAttribute.Name);
		var foundCanonical = false;

		foreach (var attribute in member.GetAttributes()) {
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null) {
				continue;
			}

			var simpleName = Parser.StripAttributeSuffix(attributeClass.Name);
			if (!string.Equals(simpleName, expectedSimpleName, StringComparison.Ordinal)) {
				continue;
			}

			if (SymbolEqualityComparer.Default.Equals(attributeClass, canonicalAttribute)) {
				foundCanonical = true;
				continue;
			}

			var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
				?? member.Locations.FirstOrDefault();
			diagnostics.Add(Diagnostic.Create(
				AttributeShadowingDescriptor,
				location,
				simpleName,
				attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				canonicalAttribute.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
		}

		return foundCanonical;
	}

	private static bool AcceptsSupportedInitializerParameters(INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol) {
		if (methodSymbol.Parameters.Length == 0) {
			return true;
		}

		return methodSymbol.Parameters.Length == 1
			&& SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, typeSymbol);
	}

	private static bool CanAssign(ITypeSymbol sourceType, ITypeSymbol targetType) {
		if (SymbolEqualityComparer.Default.Equals(sourceType, targetType)) {
			return true;
		}

		if (sourceType is not INamedTypeSymbol namedSource) {
			return false;
		}

		foreach (var implementedInterface in namedSource.AllInterfaces) {
			if (SymbolEqualityComparer.Default.Equals(implementedInterface, targetType)) {
				return true;
			}
		}

		var current = namedSource.BaseType;
		while (current is not null) {
			if (SymbolEqualityComparer.Default.Equals(current, targetType)) {
				return true;
			}
			current = current.BaseType;
		}
		return false;
	}

	private static string SanitizeIdentifier(string name) {
		var builder = new StringBuilder(name.Length);
		foreach (var ch in name) {
			builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
		}
		if (builder.Length == 0) {
			return "Unnamed";
		}
		if (char.IsDigit(builder[0])) {
			builder.Insert(0, '_');
		}
		return builder.ToString();
	}

	private interface IPropertyOrFieldSymbol {
		string Name { get; }
		string GetMemberTypeDisplayName();
		MemberValueKind ValueKind { get; }
	}

	private sealed class FieldWrapper : IPropertyOrFieldSymbol {
		private readonly IFieldSymbol symbol;
		public MemberValueKind ValueKind { get; }

		internal FieldWrapper(IFieldSymbol symbol, MemberValueKind valueKind) {
			this.symbol = symbol;
			ValueKind = valueKind;
		}

		public string Name => symbol.Name;

		public string GetMemberTypeDisplayName() {
			return symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}
	}

	private sealed class PropertyWrapper : IPropertyOrFieldSymbol {
		private readonly IPropertySymbol symbol;
		public MemberValueKind ValueKind { get; }

		internal PropertyWrapper(IPropertySymbol symbol, MemberValueKind valueKind) {
			this.symbol = symbol;
			ValueKind = valueKind;
		}

		public string Name => symbol.Name;

		public string GetMemberTypeDisplayName() {
			return symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}
	}

	private sealed class KnownTypeSymbols {
		internal KnownTypeSymbols(
			INamedTypeSymbol abstractScript,
			INamedTypeSymbol region,
			INamedTypeSymbol globals,
			INamedTypeSymbol iAbstractKey,
			INamedTypeSymbol abstractAccount) {
			AbstractScript = abstractScript;
			Region = region;
			Globals = globals;
			IAbstractKey = iAbstractKey;
			AbstractAccount = abstractAccount;
		}

		internal INamedTypeSymbol AbstractScript { get; }
		internal INamedTypeSymbol Region { get; }
		internal INamedTypeSymbol Globals { get; }
		internal INamedTypeSymbol IAbstractKey { get; }
		internal INamedTypeSymbol AbstractAccount { get; }

		internal static KnownTypeSymbols Create(Compilation compilation) {
			return new KnownTypeSymbols(
				compilation.GetTypeByMetadataName("SteamEngine.AbstractScript")
					?? throw new InvalidOperationException("Canonical engine type AbstractScript is unavailable."),
				compilation.GetTypeByMetadataName("SteamEngine.Regions.Region")
					?? throw new InvalidOperationException("Canonical engine type Region is unavailable."),
				compilation.GetTypeByMetadataName("SteamEngine.Globals")
					?? throw new InvalidOperationException("Canonical engine type Globals is unavailable."),
				compilation.GetTypeByMetadataName("SteamEngine.IAbstractKey")
					?? throw new InvalidOperationException("Canonical engine type IAbstractKey is unavailable."),
				compilation.GetTypeByMetadataName("SteamEngine.AbstractAccount")
					?? throw new InvalidOperationException("Canonical engine type AbstractAccount is unavailable."));
		}
	}
}
