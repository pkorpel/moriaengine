using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal static class Analyzer {
	internal static readonly DiagnosticDescriptor UnsupportedTemplateMemberDescriptor = new(
		"SEGEN001",
		"Unsupported template member usage",
		"Member '{0}' is marked with [{1}] but {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor TemplatePairConflictDescriptor = new(
		"SEGEN002",
		"Conflicting template declarations",
		"Template '{0}' conflicts with '{1}' because {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor DuplicateLoadKeyDescriptor = new(
		"SEGEN003",
		"Conflicting save/load member keys",
		"Members '{0}' and '{1}' map to the same save/load key '{2}' (case-insensitive)",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor TypeNameConflictDescriptor = new(
		"SEGEN005",
		"Type name conflicts with canonical engine type",
		"Type '{0}' matches canonical name '{1}' but is a different symbol; generation uses canonical symbol identity",
		"Usage",
		DiagnosticSeverity.Warning,
		true);
	internal static readonly DiagnosticDescriptor InvalidTemplateCopySettingDescriptor = new(
		"SEGEN006",
		"Invalid template copy setting",
		"Template attribute Copy must be literal true or false, but was '{0}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor CopyableDataWithCopyDisabledDescriptor = new(
		"SEGEN007",
		"CopyableData ignored when template copying is disabled",
		"Member '{0}' on template '{1}' is marked with [CopyableData], but template Copy=false disables generated copy support",
		"Usage",
		DiagnosticSeverity.Warning,
		true);
	internal static readonly DiagnosticDescriptor DuplicateTemplateNameDescriptor = new(
		"SEGEN010",
		"Duplicate template name",
		"Template name '{0}' for family '{1}' is declared by both '{2}' and '{3}' (names must be globally unique, case-insensitive)",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor InvalidDefFieldDefaultExpressionDescriptor = new(
		"SEGEN011",
		"Invalid DefField default expression",
		"DefField DefaultExpression must be a string literal, but was '{0}'",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor ForbiddenTemplateLoadFactoryAttributeDescriptor = new(
		"SEGEN012",
		"Template declares generated load-factory attribute",
		"Template member '{0}' on '{1}' uses [{2}], but template load factories are generated automatically",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor InvalidTemplateLoadingFinalizerDescriptor = new(
		"SEGEN013",
		"Invalid template loading finalizer",
		"Member '{0}' on '{1}' has [LoadingFinalizer] but {2}",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static IReadOnlyList<TemplatePair> BuildTemplatePairs(
		IReadOnlyCollection<TemplateCandidate> candidates,
		ICollection<Diagnostic> diagnostics,
		RequiredSymbols symbols) {
		var symbolPairs = new Dictionary<string, TemplatePairSymbols>(StringComparer.Ordinal);
		foreach (var candidate in candidates) {
			AddCandidateToPairs(symbolPairs, candidate, diagnostics);
		}
		var conflictingPairKeys = ValidateUniqueTemplateNames(symbolPairs, diagnostics);

		var templatePairs = new List<TemplatePair>();
		foreach (var pairEntry in symbolPairs) {
			if (conflictingPairKeys.Contains(pairEntry.Key)) {
				continue;
			}
			var symbolPair = pairEntry.Value;
			if (!TryCreateRenderPair(symbolPair, diagnostics, symbols, out var renderPair)) {
				continue;
			}
			if (renderPair is not null) {
				templatePairs.Add(renderPair);
			}
		}
		return templatePairs;
	}

	private static HashSet<string> ValidateUniqueTemplateNames(
		IReadOnlyDictionary<string, TemplatePairSymbols> symbolPairs,
		ICollection<Diagnostic> diagnostics) {
		var seenByName = new Dictionary<string, (string PairKey, TemplatePairSymbols Pair)>(StringComparer.OrdinalIgnoreCase);
		var conflicts = new HashSet<string>(StringComparer.Ordinal);

		foreach (var pairEntry in symbolPairs) {
			var pair = pairEntry.Value;
			var nameKey = pair.Family + "|" + pair.InstanceName;
			if (!seenByName.TryGetValue(nameKey, out var existing)) {
				seenByName[nameKey] = (pairEntry.Key, pair);
				continue;
			}

			conflicts.Add(existing.PairKey);
			conflicts.Add(pairEntry.Key);

			diagnostics.Add(Diagnostic.Create(
				DuplicateTemplateNameDescriptor,
				GetTemplatePairLocation(pair),
				pair.InstanceName,
				pair.Family.ToString(),
				GetTemplatePairDisplay(existing.Pair),
				GetTemplatePairDisplay(pair)));
		}

		return conflicts;
	}

	private static void AddCandidateToPairs(IDictionary<string, TemplatePairSymbols> pairs, TemplateCandidate candidate, ICollection<Diagnostic> diagnostics) {
		var isDef = candidate.Kind == TemplateKind.Def;
		var className = candidate.Symbol.Name;
		var instanceName = GetInstanceName(className, isDef);
		var defName = isDef ? className : className + "Def";
		var namespaceName = candidate.Symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
		var pairKey = namespaceName + "|" + candidate.Family + "|" + instanceName;

		if (!pairs.TryGetValue(pairKey, out var pair)) {
			pair = new TemplatePairSymbols(instanceName, defName, candidate.Family, namespaceName);
			pairs[pairKey] = pair;
		}
		UpdatePair(pair, candidate, isDef, diagnostics);
	}

	private static string GetInstanceName(string className, bool isDef) {
		if (!isDef || !className.EndsWith("Def", StringComparison.Ordinal)) {
			return className;
		}
		return className.Substring(0, className.Length - 3);
	}

	private static void UpdatePair(TemplatePairSymbols pair, TemplateCandidate candidate, bool isDef, ICollection<Diagnostic> diagnostics) {
		if (pair.Family != candidate.Family) {
			diagnostics.Add(CreateTemplatePairDiagnostic(candidate.Symbol, pair, "instance and def templates use different families"));
			return;
		}
		if (isDef) {
			if (pair.DefSymbol is not null && !SymbolEqualityComparer.Default.Equals(pair.DefSymbol, candidate.Symbol)) {
				diagnostics.Add(CreateTemplatePairDiagnostic(candidate.Symbol, pair, "multiple def templates resolve to the same instance name"));
				return;
			}
			pair.DefSymbol = candidate.Symbol;
			pair.DefName = candidate.Symbol.Name;
			pair.DefFields = candidate.DefFields;
			return;
		}

		if (pair.InstanceSymbol is not null && !SymbolEqualityComparer.Default.Equals(pair.InstanceSymbol, candidate.Symbol)) {
			diagnostics.Add(CreateTemplatePairDiagnostic(candidate.Symbol, pair, "multiple instance templates share the same name"));
			return;
		}
		pair.InstanceSymbol = candidate.Symbol;
		pair.InstanceName = candidate.Symbol.Name;
		pair.InstanceCopyAllowed = candidate.CopyAllowed ?? true;
		pair.InstanceInstanceFieldDefaultExpressions = candidate.InstanceFieldDefaultExpressions;
	}

	private static Diagnostic CreateTemplatePairDiagnostic(INamedTypeSymbol conflictingSymbol, TemplatePairSymbols existingPair, string reason) {
		var conflictingName = conflictingSymbol.ToDisplayString();
		var existingName = (existingPair.InstanceSymbol ?? existingPair.DefSymbol)?.ToDisplayString() ?? existingPair.InstanceName;
		var location = conflictingSymbol.Locations.FirstOrDefault();
		return Diagnostic.Create(TemplatePairConflictDescriptor, location, conflictingName, existingName, reason);
	}

	private static Location? GetTemplatePairLocation(TemplatePairSymbols pair) {
		return pair.InstanceSymbol?.Locations.FirstOrDefault()
			?? pair.DefSymbol?.Locations.FirstOrDefault();
	}

	private static string GetTemplatePairDisplay(TemplatePairSymbols pair) {
		return (pair.InstanceSymbol ?? pair.DefSymbol)?.ToDisplayString()
			?? pair.InstanceName;
	}

	private static bool TryGetBaseTypeNames(TemplatePairSymbols pair, out string instanceBaseName, out string defBaseName) {
		if (pair.InstanceSymbol?.BaseType is { } instanceBase) {
			instanceBaseName = instanceBase.Name;
			defBaseName = pair.DefSymbol?.BaseType?.Name ?? instanceBaseName + "Def";
			return true;
		}
		if (TryGetInstanceBaseNameFromDef(pair.DefSymbol, out instanceBaseName)) {
			defBaseName = pair.DefSymbol?.BaseType?.Name ?? instanceBaseName + "Def";
			return true;
		}
		instanceBaseName = string.Empty;
		defBaseName = string.Empty;
		return false;
	}

	private static bool TryCreateRenderPair(
		TemplatePairSymbols symbolPair,
		ICollection<Diagnostic> diagnostics,
		RequiredSymbols symbols,
		out TemplatePair? renderPair) {
		if (!TryGetBaseTypeNames(symbolPair, out var instanceBaseName, out var defBaseName)) {
			renderPair = null;
			return false;
		}

		var defFields = symbolPair.DefFields;
		var instanceFields = GetInstanceFields(
			symbolPair.InstanceSymbol,
			symbolPair.InstanceInstanceFieldDefaultExpressions,
			symbols,
			diagnostics).ToList();
		WarnCopyableDataWithCopyDisabled(symbolPair.InstanceCopyAllowed, symbolPair.InstanceSymbol?.Name ?? string.Empty, instanceFields, diagnostics);
		var saveLoadMembers = ValidateDistinctSaveLoadKeys(
			instanceFields.Where(f => f.IncludeInSaveLoad).ToList(),
			diagnostics);
		var copyMembers = instanceFields.Where(f => f.IncludeInCopy).ToList();
		// Template pairs own their loading strategy end-to-end. We intentionally reject
		// user-declared [LoadSection]/[LoadingInitializer] here and feed a fixed load
		// factory model into the satellite save implementor renderer for deterministic
		// parity with legacy template behavior.
		if (!ValidateNoUserLoadFactoryAttributes(symbolPair.InstanceSymbol, symbols, diagnostics)) {
			renderPair = null;
			return false;
		}
		if (!TryResolveLoadingFinalizerMethodName(symbolPair.InstanceSymbol, symbols, diagnostics, out var loadingFinalizerMethodName)) {
			renderPair = null;
			return false;
		}
		var loadFactory = CreateTemplateLoadFactory(symbolPair.Family, symbols);

		renderPair = new TemplatePair(
			symbolPair.InstanceName,
			GetQualifiedTypeName(symbolPair.NamespaceName, symbolPair.InstanceName),
			symbolPair.DefName,
			symbolPair.Family,
			symbolPair.NamespaceName,
			instanceBaseName,
			defBaseName,
			symbolPair.InstanceCopyAllowed,
			loadFactory,
			loadingFinalizerMethodName,
			defFields,
			saveLoadMembers,
			copyMembers);
		return true;
	}

	private static string GetQualifiedTypeName(string namespaceName, string typeName) {
		if (string.IsNullOrEmpty(namespaceName)) {
			return "global::" + typeName;
		}
		return "global::" + namespaceName + "." + typeName;
	}

	private static TemplateLoadFactory CreateTemplateLoadFactory(
		TemplateFamily family,
		RequiredSymbols symbols) {
		// Plugin templates are generated with a public parameterless constructor and
		// therefore load via constructor path in the save implementor model.
		if (family == TemplateFamily.Plugin) {
			return new TemplateLoadFactory(
				TemplateLoadFactoryKind.Constructor,
				methodName: null,
				declaringTypeName: null,
				takesPropsSection: false,
				requiresCast: false);
		}

		// Thing templates must load through the canonical static Thing.Load(PropsSection)
		return new TemplateLoadFactory(
			TemplateLoadFactoryKind.StaticMethod,
			methodName: "Load",
			declaringTypeName: symbols.CanonicalTypes.ThingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			takesPropsSection: true,
			requiresCast: true);
	}

	private static bool ValidateNoUserLoadFactoryAttributes(
		INamedTypeSymbol? instanceSymbol,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		if (instanceSymbol is null) {
			return true;
		}

		var valid = true;
		// Guardrail: these attributes are consumed by SaveImplementors, but template
		// generation now drives that renderer directly with fixed factory semantics.
		// Allowing template-defined attributes here would create conflicting load paths.
		foreach (var member in instanceSymbol.GetMembers().OfType<IMethodSymbol>()) {
			if (AttributeUsageValidator.HasAttribute(member, symbols.Attributes.LoadSectionAttribute, diagnostics)) {
				diagnostics.Add(Diagnostic.Create(
					ForbiddenTemplateLoadFactoryAttributeDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
					instanceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					"LoadSection"));
				valid = false;
			}
			if (AttributeUsageValidator.HasAttribute(member, symbols.Attributes.LoadingInitializerAttribute, diagnostics)) {
				diagnostics.Add(Diagnostic.Create(
					ForbiddenTemplateLoadFactoryAttributeDescriptor,
					member.Locations.FirstOrDefault(),
					member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
					instanceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					"LoadingInitializer"));
				valid = false;
			}
		}

		return valid;
	}

	private static bool TryResolveLoadingFinalizerMethodName(
		INamedTypeSymbol? instanceSymbol,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out string? loadingFinalizerMethodName) {
		loadingFinalizerMethodName = null;
		if (instanceSymbol is null) {
			return true;
		}

		var errorsBeforeResolve = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
		var method = AttributedMemberResolver.FindMethodOrCtorWithAttribute(
			instanceSymbol,
			symbols.Attributes.LoadingFinalizerAttribute,
			diagnostics,
			includeInheritedTypes: true);
		if (diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error) > errorsBeforeResolve) {
			return false;
		}
		if (method is null) {
			return true;
		}

		if (method.MethodKind != MethodKind.Ordinary || method.IsStatic || method.Parameters.Length != 0) {
			diagnostics.Add(Diagnostic.Create(
				InvalidTemplateLoadingFinalizerDescriptor,
				method.Locations.FirstOrDefault(),
				method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
				instanceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				"it must be an instance method with no parameters"));
			return false;
		}

		loadingFinalizerMethodName = method.Name;
		return true;
	}

	internal static List<InstanceFieldSpec> ValidateDistinctSaveLoadKeys(List<InstanceFieldSpec> saveLoadMembers, ICollection<Diagnostic> diagnostics) {
		var unique = new Dictionary<string, InstanceFieldSpec>(StringComparer.OrdinalIgnoreCase);
		foreach (var member in saveLoadMembers) {
			if (!unique.TryGetValue(member.Name, out var existing)) {
				unique[member.Name] = member;
				continue;
			}
			var key = member.Name.ToLowerInvariant();
			var location = member.Location ?? existing.Location;
			diagnostics.Add(Diagnostic.Create(
				DuplicateLoadKeyDescriptor,
				location,
				existing.Name,
				member.Name,
				key));
		}
		return unique.Values.ToList();
	}

	internal static void WarnCopyableDataWithCopyDisabled(bool instanceCopyAllowed, string instanceName, IEnumerable<InstanceFieldSpec> instanceFields, ICollection<Diagnostic> diagnostics) {
		if (instanceCopyAllowed || string.IsNullOrEmpty(instanceName)) {
			return;
		}

		foreach (var field in instanceFields.Where(f => f.IncludeInCopy)) {
			diagnostics.Add(Diagnostic.Create(
				CopyableDataWithCopyDisabledDescriptor,
				field.Location,
				field.Name,
				instanceName));
		}
	}

	private static bool TryGetInstanceBaseNameFromDef(INamedTypeSymbol? defSymbol, out string instanceBaseName) {
		if (defSymbol?.BaseType is not INamedTypeSymbol defBase) {
			instanceBaseName = string.Empty;
			return false;
		}

		var defBaseName = defBase.Name;
		if (!defBaseName.EndsWith("Def", StringComparison.Ordinal) || defBaseName.Length <= 3) {
			instanceBaseName = string.Empty;
			return false;
		}

		instanceBaseName = defBaseName.Substring(0, defBaseName.Length - 3);
		return true;
	}

	internal static IEnumerable<InstanceFieldSpec> GetInstanceFields(
		INamedTypeSymbol? instanceSymbol,
		IReadOnlyDictionary<ISymbol, string> instanceFieldDefaultExpressions,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		if (instanceSymbol is null) {
			return Array.Empty<InstanceFieldSpec>();
		}

		var fields = new List<InstanceFieldSpec>();
		foreach (var member in instanceSymbol.GetMembers()) {
			if (!TryCreateInstanceFieldSpec(instanceSymbol, member, instanceFieldDefaultExpressions, symbols, diagnostics, out var spec, out var error)) {
				if (error is not null) {
					diagnostics.Add(error);
				}
				continue;
			}
			fields.Add(spec);
		}
		return fields;
	}

	private static bool TryCreateInstanceFieldSpec(
		INamedTypeSymbol instanceSymbol,
		ISymbol member,
		IReadOnlyDictionary<ISymbol, string> instanceFieldDefaultExpressions,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out InstanceFieldSpec spec,
		out Diagnostic? error) {
		var hasSaveableData = HasMemberAttribute(member, symbols.Attributes.SaveableDataAttribute, diagnostics);
		var hasCopyableData = HasMemberAttribute(member, symbols.Attributes.CopyableDataAttribute, diagnostics);
		if (!hasSaveableData && !hasCopyableData) {
			spec = default;
			error = null;
			return false;
		}

		if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, instanceSymbol)) {
			spec = default;
			error = null;
			return false;
		}

		var unsupportedReason = GetUnsupportedMemberReason(member);
		if (unsupportedReason is not null) {
			spec = default;
			error = CreateUnsupportedMemberDiagnostic(member, hasSaveableData, hasCopyableData, unsupportedReason);
			return false;
		}

		if (member is IFieldSymbol field) {
			spec = CreateInstanceFieldSpec(field, instanceFieldDefaultExpressions, symbols, diagnostics, hasSaveableData, hasCopyableData);
			error = null;
			return true;
		}

		if (member is IPropertySymbol property) {
			spec = CreateInstancePropertySpec(property, instanceFieldDefaultExpressions, symbols, diagnostics, hasSaveableData, hasCopyableData);
			error = null;
			return true;
		}

		spec = default;
		error = CreateUnsupportedMemberDiagnostic(member, hasSaveableData, hasCopyableData, "only fields and properties are supported");
		return false;
	}

	private static bool HasMemberAttribute(ISymbol member, INamedTypeSymbol expectedSymbol, ICollection<Diagnostic> diagnostics) {
		return AttributeUsageValidator.HasAttribute(member, expectedSymbol, diagnostics);
	}

	private static string? GetUnsupportedMemberReason(ISymbol member) {
		switch (member) {
			case IFieldSymbol field when field.IsStatic:
				return "it is static";
			case IFieldSymbol field when field.IsConst:
				return "it is const";
			case IFieldSymbol field when field.Name.StartsWith("<", StringComparison.Ordinal):
				return "compiler-generated backing fields are not supported";
			case IPropertySymbol property when property.IsStatic:
				return "it is static";
			case IPropertySymbol property when property.IsIndexer:
				return "indexers are not supported";
			case IPropertySymbol property when property.GetMethod is null:
				return "properties without a getter are not supported";
			case IPropertySymbol property when property.SetMethod is null:
				return "properties without a setter are not supported";
			default:
				return null;
		}
	}

	private static Diagnostic CreateUnsupportedMemberDiagnostic(ISymbol member, bool hasSaveableData, bool hasCopyableData, string reason) {
		var attributes = new List<string>(2);
		if (hasSaveableData) {
			attributes.Add("SaveableData");
		}
		if (hasCopyableData) {
			attributes.Add("CopyableData");
		}
		var attributeList = string.Join(", ", attributes);
		var location = member.Locations.FirstOrDefault();
		return Diagnostic.Create(
			UnsupportedTemplateMemberDescriptor,
			location,
			member.Name,
			attributeList,
			reason);
	}

	private static InstanceFieldSpec CreateInstanceFieldSpec(
		IFieldSymbol member,
		IReadOnlyDictionary<ISymbol, string> instanceFieldDefaultExpressions,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		bool includeInSaveLoad,
		bool includeInCopy) {
		return CreateInstanceMemberSpec(
			member.Name,
			GetMemberInitializer(member, instanceFieldDefaultExpressions) ?? "default",
			member.Type,
			member.Locations.FirstOrDefault(),
			symbols,
			diagnostics,
			includeInSaveLoad,
			includeInCopy);
	}

	private static InstanceFieldSpec CreateInstancePropertySpec(
		IPropertySymbol member,
		IReadOnlyDictionary<ISymbol, string> instanceFieldDefaultExpressions,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		bool includeInSaveLoad,
		bool includeInCopy) {
		return CreateInstanceMemberSpec(
			member.Name,
			GetMemberInitializer(member, instanceFieldDefaultExpressions) ?? "default",
			member.Type,
			member.Locations.FirstOrDefault(),
			symbols,
			diagnostics,
			includeInSaveLoad,
			includeInCopy);
	}

	private static InstanceFieldSpec CreateInstanceMemberSpec(
		string name,
		string defaultExpression,
		ITypeSymbol type,
		Location? location,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		bool includeInSaveLoad,
		bool includeInCopy) {
		var typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

		return new InstanceFieldSpec(
			name,
			typeName,
			defaultExpression,
			includeInSaveLoad,
			includeInCopy,
			MemberValueKindClassifier.GetMemberValueKind(
				type,
				symbols.CanonicalTypes,
				(actual, canonical) => diagnostics.Add(CreateTypeNameConflictDiagnostic(location, actual, canonical))),
			location);
	}

	private static string? GetMemberInitializer(ISymbol member, IReadOnlyDictionary<ISymbol, string> instanceFieldDefaultExpressions) {
		// Missing key means the member had no explicit initializer in source.
		return instanceFieldDefaultExpressions.TryGetValue(member, out var initializer)
			? initializer
			: null;
	}

	private static Diagnostic CreateTypeNameConflictDiagnostic(Location? location, ITypeSymbol actual, INamedTypeSymbol canonical) {
		return Diagnostic.Create(
			TypeNameConflictDescriptor,
			location,
			actual.ToDisplayString(),
			canonical.ToDisplayString());
	}

	private sealed class TemplatePairSymbols {
		internal TemplatePairSymbols(string instanceName, string defName, TemplateFamily family, string namespaceName) {
			InstanceName = instanceName;
			DefName = defName;
			Family = family;
			NamespaceName = namespaceName;
		}

		internal string InstanceName { get; set; }
		internal string DefName { get; set; }
		internal TemplateFamily Family { get; }
		internal string NamespaceName { get; }
		internal INamedTypeSymbol? InstanceSymbol { get; set; }
		internal INamedTypeSymbol? DefSymbol { get; set; }
		internal bool InstanceCopyAllowed { get; set; } = true;
		internal IReadOnlyList<DefFieldSpec> DefFields { get; set; } = Array.Empty<DefFieldSpec>();
		// Parser result for instance-member initializer text; consumed during member spec creation.
		internal IReadOnlyDictionary<ISymbol, string> InstanceInstanceFieldDefaultExpressions { get; set; } =
			new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
	}
}

