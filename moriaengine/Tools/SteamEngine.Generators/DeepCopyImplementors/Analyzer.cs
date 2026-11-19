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
		out DeepCopyModel model) {
		model = null!;

		var hasInitializer = TryResolveInitializer(
			typeSymbol,
			symbols.Attributes.DeepCopyImplementationAttribute,
			diagnostics,
			out var initializer);
		var problemWithCopyableMembers = TryResolveCopyableMembers(
			typeSymbol,
			symbols.Attributes.CopyableDataAttribute,
			symbols.CanonicalTypes,
			diagnostics,
			out var copyableMembers);

		if (!hasInitializer || !problemWithCopyableMembers) {
			return false;
		}

		var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var generatedTypeName = "GeneratedDeepCopyImplementor_" + IdentifierSanitizer.Sanitize(typeSymbol.Name);

		model = new DeepCopyModel(
			generatedTypeName,
			typeName,
			initializer,
			copyableMembers);
		return true;
	}

	private static bool TryResolveInitializer(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol deepCopyImplementationAttribute,
		ICollection<Diagnostic> diagnostics,
		out DeepCopyInitializerModel initializer) {
		initializer = null!;

		var diagnosticsBeforeResolve = diagnostics.Count;
		var methodSymbol = AttributedMemberResolver.ResolveMethod(
			typeSymbol,
			deepCopyImplementationAttribute,
			diagnostics,
			includeInheritedTypes: false);

		if (methodSymbol is null && diagnostics.Count == diagnosticsBeforeResolve) {
			diagnostics.Add(Diagnostic.Create(
				MissingDeepCopyImplementationDescriptor,
				typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString()));
			return false;
		}

		if (methodSymbol is null) {
			return false;
		}

		var validationError = ValidateInitializer(typeSymbol, methodSymbol);
		if (validationError is not null) {
			diagnostics.Add(Diagnostic.Create(
				InvalidDeepCopyImplementationDescriptor,
				methodSymbol.Locations.FirstOrDefault(),
				methodSymbol.ToDisplayString(),
				typeSymbol.ToDisplayString(),
				validationError));
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

	private static bool TryResolveCopyableMembers(
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol copyableDataAttribute,
		CanonicalTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		out IReadOnlyList<DeepCopyMemberModel> members) {
		var result = AttributedMemberResolver.CollectFieldsAndProperties(
			typeSymbol,
			copyableDataAttribute,
			diagnostics,
			typeSymbol,
			UnsupportedCopyableMemberDescriptor,
			includeInheritedTypes: false);

		members = result.Items
			.Select(member => new DeepCopyMemberModel(
				member.Name,
				member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GetMemberValueKind(member.Type, knownTypes, diagnostics, member.Location)))
			.ToList();
		return !result.HasInvalidMembers;
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
