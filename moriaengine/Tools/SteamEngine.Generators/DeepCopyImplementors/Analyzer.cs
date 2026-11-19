using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal static class Analyzer {
	internal static readonly DiagnosticDescriptor InvalidDeepCopyImplementationDescriptor = new(
		"SEGEN203",
		"Invalid deep-copy implementation member",
		"Member '{0}' on '{1}' has [DeepCopyImplementation] but {2}",
		"Usage",
		DiagnosticSeverity.Error,
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

	internal static IReadOnlyList<DeepCopyModel> BuildModels(
		IReadOnlyCollection<DeepCopyCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		RequiredSymbols symbols) {
		var models = new List<DeepCopyModel>();
		var conflictingTypeNames = DuplicateNameValidator.Validate(
			candidates.Select(candidate => (
				Name: candidate.Symbol.Name,
				Symbol: (ISymbol) candidate.Symbol)),
			DuplicateGeneratedImplementorNameDescriptor,
			diagnostics);

		foreach (var candidate in candidates) {
			if (conflictingTypeNames.Contains(candidate.Symbol.Name)) {
				continue;
			}

			if (!TryBuildModel(candidate, symbols, diagnostics, out var model)) {
				continue;
			}
			models.Add(model);
		}

		return models;
	}

	private static bool TryBuildModel(
		DeepCopyCandidate candidate,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out DeepCopyModel model) {

		model = null!;
		var typeSymbol = candidate.Symbol;

		if (candidate.InitializerMethod is null) {
			return false;
		}
		var initializerMethod = candidate.InitializerMethod;
		var validationError = ValidateInitializer(typeSymbol, initializerMethod);
		if (validationError is not null) {
			diagnostics.Add(Diagnostic.Create(
				InvalidDeepCopyImplementationDescriptor,
				initializerMethod.Locations.FirstOrDefault(),
				initializerMethod.ToDisplayString(),
				typeSymbol.ToDisplayString(),
				validationError));
			return false;
		}

		var initializerKind = initializerMethod.MethodKind == MethodKind.Constructor
			? DeepCopyInitializerKind.Constructor
			: DeepCopyInitializerKind.StaticMethod;
		var initializer = new DeepCopyInitializerModel(
			initializerKind,
			initializerMethod.Name,
			initializerMethod.Parameters.Length == 1);
		var copyableMembers = candidate.CopyableMembers
			.Select(member => new DeepCopyMemberModel(
				member.Name,
				member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GetMemberValueKind(member.Type, symbols.CanonicalTypes, diagnostics, member.Location)))
			.ToList();

		var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var generatedTypeName = "GeneratedDeepCopyImplementor_" + IdentifierSanitizer.Sanitize(typeSymbol.Name);

		model = new DeepCopyModel(
			generatedTypeName,
			typeName,
			initializer,
			copyableMembers);
		return true;
	}

	private static string? ValidateInitializer(INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol) {
		if (methodSymbol.MethodKind == MethodKind.Ordinary) {
			if (!methodSymbol.IsStatic) {
				return "method is not static";
			}
			if (!CanAssign(typeSymbol, methodSymbol.ReturnType)) {
				return "method return type is not assignable from the declaring type";
			}
		} else if (methodSymbol.MethodKind != MethodKind.Constructor) {
			return "it must be placed on a constructor or static method";
		}

		if (!AcceptsSupportedInitializerParameters(typeSymbol, methodSymbol)) {
			return "it must have zero parameters or a single parameter of the declaring type";
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

	private static Diagnostic CreateTypeNameConflictDiagnostic(Location? location, ITypeSymbol actual, INamedTypeSymbol canonical) {
		return Diagnostic.Create(
			TypeNameConflictDescriptor,
			location,
			actual.Name,
			actual.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			canonical.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
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

}
