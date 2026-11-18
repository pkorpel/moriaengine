using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledTriggerGroups;

internal static class Parser {
	internal static readonly DiagnosticDescriptor TypeMustBePartialDescriptor = new(
		"SEGEN403",
		"Compiled trigger group must be partial",
		"Compiled trigger group '{0}' must be declared partial for source-generated Run dispatch",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static CompiledTriggerGroupCandidate? DiscoverCandidate(
		Compilation compilation,
		TypeDeclarationSyntax declaration,
		global::SteamEngine.Generators.RequiredSymbols requiredSymbols,
		ICollection<Diagnostic> diagnostics) {
		if (declaration is not ClassDeclarationSyntax) {
			return null;
		}

		var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
		if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol) {
			return null;
		}

		if (typeSymbol.IsAbstract || typeSymbol.IsSealed) {
			return null;
		}

		if (!IsAssignableTo(typeSymbol, requiredSymbols.CanonicalTypes.CompiledTriggerGroupType)) {
			return null;
		}

		if (!typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.All(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))) {
			diagnostics.Add(Diagnostic.Create(
				TypeMustBePartialDescriptor,
				typeSymbol.Locations.FirstOrDefault(),
				typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
			return null;
		}

		var triggerMethods = GetTriggerMethods(typeSymbol);
		return new CompiledTriggerGroupCandidate(typeSymbol, triggerMethods);
	}

	private static bool IsAssignableTo(INamedTypeSymbol source, INamedTypeSymbol targetBaseType) {
		for (var current = source.BaseType; current is not null; current = current.BaseType) {
			if (SymbolEqualityComparer.Default.Equals(current, targetBaseType)) {
				return true;
			}
		}

		return false;
	}

	private static IReadOnlyList<IMethodSymbol> GetTriggerMethods(INamedTypeSymbol typeSymbol) {
		var methods = new List<IMethodSymbol>();

		void AddMethodsFromType(INamedTypeSymbol symbol, bool includeOverridden) {
			foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()) {
				if (method.MethodKind != MethodKind.Ordinary) {
					continue;
				}

				if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public) {
					continue;
				}

				if (!method.Name.StartsWith("on_", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!includeOverridden && IsOverriddenByExisting(methods, method)) {
					continue;
				}

				methods.Add(method);
			}
		}

		AddMethodsFromType(typeSymbol, includeOverridden: true);
		for (var current = typeSymbol.BaseType; current is not null; current = current.BaseType) {
			AddMethodsFromType(current, includeOverridden: false);
		}

		return methods;
	}

	private static bool IsOverriddenByExisting(IEnumerable<IMethodSymbol> existingMethods, IMethodSymbol baseMethod) {
		foreach (var existing in existingMethods) {
			for (var current = existing; current is not null; current = current.OverriddenMethod) {
				if (SymbolEqualityComparer.Default.Equals(current, baseMethod)) {
					return true;
				}
			}
		}

		return false;
	}
}
