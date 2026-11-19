using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.SaveImplementors;

internal static class Analyzer {
	internal static readonly DiagnosticDescriptor InvalidSaveImplementorMemberDescriptor = new(
		"SEGEN303",
		"Invalid save implementor member",
		"Member '{0}' on '{1}' has [{2}] but {3}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor MissingLoadingInitializerDescriptor = new(
		"SEGEN305",
		"Missing load initializer",
		"Type '{0}' is marked with [SaveableClass] but has no [LoadingInitializer] or [LoadSection] member",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor InvalidLoadConfigurationDescriptor = new(
		"SEGEN306",
		"Invalid loading configuration",
		"Type '{0}' has invalid loading configuration: {1}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor UnsupportedSaveableMemberDescriptor = new(
		"SEGEN307",
		"Unsupported [SaveableData] member",
		"Member '{0}' on '{1}' is marked with [SaveableData], but {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor MissingSavePathDescriptor = new(
		"SEGEN308",
		"Missing save path",
		"Type '{0}' is marked with [SaveableClass] but has no [Save] method and no valid [SaveableData] members",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor DuplicateSaveableTypeNameDescriptor = new(
		"SEGEN309",
		"Duplicate saveable type name",
		"Saveable type name '{0}' is declared by both '{1}' and '{2}' (names must be globally unique, case-insensitive)",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor TypeNameConflictDescriptor = new(
		"SEGEN311",
		"Type name conflicts with canonical engine type",
		"Type '{0}' matches canonical name '{1}' but is a different symbol; generation uses canonical symbol identity",
		"Usage",
		DiagnosticSeverity.Warning,
		true);

	internal static IReadOnlyList<SaveImplementorModel> BuildModels(
		IReadOnlyCollection<SaveImplementorCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		RequiredSymbols symbols) {
		var conflictingTypeNames = DuplicateNameValidator.Validate(
			candidates.Select(candidate => (
				Name: candidate.Symbol.Name,
				Symbol: (ISymbol) candidate.Symbol)),
			DuplicateSaveableTypeNameDescriptor,
			diagnostics);
		var models = new List<SaveImplementorModel>();
		foreach (var candidate in candidates) {
			if (conflictingTypeNames.Contains(candidate.Symbol.Name)) {
				continue;
			}

			if (!TryBuildModel(
				candidate.Symbol,
				symbols,
				diagnostics,
				out var model)) {
				continue;
			}

			models.Add(model);
		}

		return models;
	}

	private static bool TryBuildModel(
		INamedTypeSymbol typeSymbol,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out SaveImplementorModel model) {
		model = null!;
		var hasErrors = false;

		var loadingInitializer = ResolveSingleAttributedMethod(
			typeSymbol,
			symbols.Attributes.LoadingInitializerAttribute,
			"LoadingInitializer",
			static m => ValidateLoadingInitializer(m),
			diagnostics,
			ref hasErrors);
		var loadSection = ResolveSingleAttributedMethod(
			typeSymbol,
			symbols.Attributes.LoadSectionAttribute,
			"LoadSection",
			m => ValidateLoadSection(typeSymbol, m, symbols.CanonicalTypes.PropsSectionType),
			diagnostics,
			ref hasErrors);
		var loadLine = ResolveSingleAttributedMethod(
			typeSymbol,
			symbols.Attributes.LoadLineAttribute,
			"LoadLine",
			static m => ValidateLoadLine(m),
			diagnostics,
			ref hasErrors);
		var loadingFinalizer = ResolveSingleAttributedMethod(
			typeSymbol,
			symbols.Attributes.LoadingFinalizerAttribute,
			"LoadingFinalizer",
			static m => ValidateLoadingFinalizer(m),
			diagnostics,
			ref hasErrors);
		var saveMethod = ResolveSingleAttributedMethod(
			typeSymbol,
			symbols.Attributes.SaveAttribute,
			"Save",
			m => ValidateSave(typeSymbol, m, symbols.CanonicalTypes.SaveStreamType),
			diagnostics,
			ref hasErrors);

		var saveableMembers = ResolveSaveableMembers(typeSymbol, symbols.Attributes.SaveableDataAttribute, symbols.CanonicalTypes, diagnostics, ref hasErrors);

		if (loadSection is not null) {
			if (loadingInitializer is not null || loadLine is not null) {
				diagnostics.Add(Diagnostic.Create(
					InvalidLoadConfigurationDescriptor,
					typeSymbol.Locations.FirstOrDefault(),
					typeSymbol.ToDisplayString(),
					"cannot use [LoadSection] together with [LoadingInitializer] or [LoadLine]"));
				hasErrors = true;
			}
		} else {
			if (loadingInitializer is null) {
				diagnostics.Add(Diagnostic.Create(
					MissingLoadingInitializerDescriptor,
					typeSymbol.Locations.FirstOrDefault(),
					typeSymbol.ToDisplayString()));
				hasErrors = true;
			}

			if (loadLine is null && saveableMembers.Count == 0) {
				diagnostics.Add(Diagnostic.Create(
					InvalidLoadConfigurationDescriptor,
					typeSymbol.Locations.FirstOrDefault(),
					typeSymbol.ToDisplayString(),
					"no [LoadLine] method and no valid [SaveableData] members"));
				hasErrors = true;
			}
		}

		if (saveMethod is null && saveableMembers.Count == 0) {
			diagnostics.Add(Diagnostic.Create(
				MissingSavePathDescriptor,
				typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString()));
			hasErrors = true;
		}

		if (hasErrors) {
			return false;
		}

		var sourceTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var generatedTypeName = "GeneratedSaveImplementor_" + IdentifierSanitizer.Sanitize(typeSymbol.Name);
		var loadFactory = loadSection is not null
			? CreateFactory(typeSymbol, loadSection, symbols.CanonicalTypes.PropsSectionType)
			: CreateFactory(typeSymbol, loadingInitializer!, symbols.CanonicalTypes.PropsSectionType);

		model = new SaveImplementorModel(
			generatedTypeName,
			sourceTypeName,
			typeSymbol.Name,
			loadFactory,
			loadLine?.Name,
			loadingFinalizer?.Name,
			saveMethod?.Name,
			saveableMembers);
		return true;
	}

	private static SaveFactoryMethod CreateFactory(INamedTypeSymbol targetType, IMethodSymbol method, INamedTypeSymbol loadSectionType) {
		var takesPropsSection = method.Parameters.Length == 1
			&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, loadSectionType);
		var kind = method.MethodKind == MethodKind.Constructor
			? SaveFactoryKind.Constructor
			: SaveFactoryKind.StaticMethod;
		var requiresCast = kind == SaveFactoryKind.StaticMethod
			&& !SymbolEqualityComparer.Default.Equals(targetType, method.ReturnType);
		return new SaveFactoryMethod(
			kind,
			method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			method.Name,
			takesPropsSection,
			requiresCast);
	}

	private static IMethodSymbol? ResolveSingleAttributedMethod(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol expectedAttribute,
		string attributeDisplayName,
		Func<IMethodSymbol, string?> validator,
		ICollection<Diagnostic> diagnostics,
		ref bool hasErrors) {
		var diagnosticsBeforeResolve = diagnostics.Count;
		var method = AttributedMemberResolver.ResolveMethod(
			typeSymbol,
			expectedAttribute,
			diagnostics,
			includeInheritedTypes: true);

		if (diagnostics.Count > diagnosticsBeforeResolve) {
			hasErrors = true;
		}

		if (method is null) {
			return null;
		}

		var reason = validator(method);
		if (reason is null) {
			return method;
		}

		diagnostics.Add(Diagnostic.Create(
			InvalidSaveImplementorMemberDescriptor,
			method.Locations.FirstOrDefault(),
			method.ToDisplayString(),
			typeSymbol.ToDisplayString(),
			attributeDisplayName,
			reason));
		hasErrors = true;
		return null;
	}

	private static List<SaveableMemberModel> ResolveSaveableMembers(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol saveableDataAttribute,
		CanonicalTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		ref bool hasErrors) {
		var collection = AttributedMemberResolver.CollectFieldsAndProperties(
			typeSymbol,
			saveableDataAttribute,
			diagnostics,
			typeSymbol,
			UnsupportedSaveableMemberDescriptor,
			includeInheritedTypes: true);

		hasErrors |= collection.HasInvalidMembers;

		var unique = new Dictionary<string, SaveableMemberModel>(StringComparer.OrdinalIgnoreCase);
		foreach (var member in collection.Items
			.Select(collected => new SaveableMemberModel(
				collected.Name,
				collected.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GetMemberValueKind(collected.Type, knownTypes, diagnostics, collected.Location)))
			) {
			if (unique.ContainsKey(member.Name)) {
				diagnostics.Add(Diagnostic.Create(
					InvalidLoadConfigurationDescriptor,
					typeSymbol.Locations.FirstOrDefault(),
					typeSymbol.ToDisplayString(),
					"duplicate [SaveableData] member name '" + member.Name + "'"));
				hasErrors = true;
				continue;
			}
			unique[member.Name] = member;
		}

		return unique.Values.ToList();
	}

	private static string? ValidateLoadingInitializer(IMethodSymbol method) {
		if (method.MethodKind == MethodKind.Constructor) {
			return method.Parameters.Length == 0
				? null
				: "it must have no parameters";
		}
		if (method.MethodKind != MethodKind.Ordinary || !method.IsStatic) {
			return "it must be placed on a constructor or static method";
		}
		return method.Parameters.Length == 0
			? null
			: "it must have no parameters";
	}

	private static string? ValidateLoadSection(INamedTypeSymbol containingType, IMethodSymbol method, INamedTypeSymbol loadSectionType) {
		if (method.Parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, loadSectionType)) {
			return "it must have exactly one parameter of type PropsSection";
		}

		if (method.MethodKind == MethodKind.Constructor) {
			return null;
		}

		if (method.MethodKind != MethodKind.Ordinary || !method.IsStatic) {
			return "it must be placed on a constructor or static method";
		}
		return null;
	}

	private static string? ValidateLoadLine(IMethodSymbol method) {
		if (method.MethodKind != MethodKind.Ordinary || method.IsStatic) {
			return "it must be placed on an instance method";
		}

		if (method.Parameters.Length != 4) {
			return "it must have parameters (string filename, int line, string name, string value)";
		}

		return method.Parameters[0].Type.SpecialType == SpecialType.System_String
			&& method.Parameters[1].Type.SpecialType == SpecialType.System_Int32
			&& method.Parameters[2].Type.SpecialType == SpecialType.System_String
			&& method.Parameters[3].Type.SpecialType == SpecialType.System_String
			? null
			: "it must have parameters (string filename, int line, string name, string value)";
	}

	private static string? ValidateLoadingFinalizer(IMethodSymbol method) {
		if (method.MethodKind != MethodKind.Ordinary || method.IsStatic) {
			return "it must be placed on an instance method";
		}

		return method.Parameters.Length == 0
			? null
			: "it must have no parameters";
	}

	private static string? ValidateSave(INamedTypeSymbol containingType, IMethodSymbol method, INamedTypeSymbol saveStreamType) {
		if (method.MethodKind != MethodKind.Ordinary || method.IsStatic) {
			return "it must be placed on an instance method";
		}
		if (method.Parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, saveStreamType)) {
			return "it must have exactly one parameter of type SaveStream";
		}
		return null;
	}

	private static MemberValueKind GetMemberValueKind(
		ITypeSymbol type,
		CanonicalTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		Location? location) {
		return MemberValueKindClassifier.GetMemberValueKind(
			type,
			knownTypes,
			(actual, canonical) => diagnostics.Add(CreateTypeNameConflictDiagnostic(location, actual, canonical)));
	}

	private static Diagnostic CreateTypeNameConflictDiagnostic(Location? location, ITypeSymbol actualType, INamedTypeSymbol expectedType) {
		return Diagnostic.Create(
			TypeNameConflictDescriptor,
			location,
			actualType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			expectedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

}
