using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal static class Analyzer {
	internal const string ThingTemplateAttributeMetadataName = "SteamEngine.Generators.Abstractions.ThingTemplateAttribute";
	internal const string ThingDefTemplateAttributeMetadataName = "SteamEngine.Generators.Abstractions.ThingDefTemplateAttribute";
	internal const string PluginTemplateAttributeMetadataName = "SteamEngine.Generators.Abstractions.PluginTemplateAttribute";
	internal const string PluginDefTemplateAttributeMetadataName = "SteamEngine.Generators.Abstractions.PluginDefTemplateAttribute";
	internal const string DefFieldAttributeMetadataName = "SteamEngine.Generators.Abstractions.DefFieldAttribute";

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
	internal static readonly DiagnosticDescriptor AttributeShadowingDescriptor = new(
		"SEGEN004",
		"Shadowed generator attribute",
		"Attribute '{0}' resolves to '{1}', but this generator expects '{2}'",
		"Usage",
		DiagnosticSeverity.Warning,
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
	internal static readonly DiagnosticDescriptor MissingGeneratorAttributeDeclarationDescriptor = new(
		"SEGEN008",
		"Missing generator attribute declaration",
		"Required generator attribute declaration '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);
	internal static readonly DiagnosticDescriptor MissingCanonicalEngineTypeDescriptor = new(
		"SEGEN009",
		"Missing canonical engine type",
		"Required canonical engine type '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	private static readonly string SaveableDataAttributeName = Parser.StripAttributeSuffix("SaveableDataAttribute");
	private static readonly string CopyableDataAttributeName = Parser.StripAttributeSuffix("CopyableDataAttribute");

	internal static bool ValidateRequiredDeclarations(Compilation compilation, ICollection<Diagnostic> diagnostics) {
		var requiredMetadataNames = new[] {
			ThingTemplateAttributeMetadataName,
			ThingDefTemplateAttributeMetadataName,
			PluginTemplateAttributeMetadataName,
			PluginDefTemplateAttributeMetadataName,
			DefFieldAttributeMetadataName
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
			"SteamEngine.Thing",
			"SteamEngine.AbstractScript",
			"SteamEngine.Regions.Region",
			"SteamEngine.Globals",
			"SteamEngine.IAbstractKey",
			"SteamEngine.AbstractAccount",
			"SteamEngine.SaveableDataAttribute",
			"SteamEngine.CopyableDataAttribute"
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

	internal static IReadOnlyList<TemplatePair> BuildTemplatePairs(IReadOnlyCollection<TemplateCandidate> candidates, ICollection<Diagnostic> diagnostics) {
		var symbolPairs = new Dictionary<string, TemplatePairSymbols>(StringComparer.Ordinal);
		foreach (var candidate in candidates) {
			AddCandidateToPairs(symbolPairs, candidate, diagnostics);
		}

		var templatePairs = new List<TemplatePair>();
		foreach (var symbolPair in symbolPairs.Values) {
			if (!TryCreateRenderPair(symbolPair, diagnostics, out var renderPair)) {
				continue;
			}
			if (renderPair is not null) {
				templatePairs.Add(renderPair);
			}
		}
		return templatePairs;
	}

	private static void AddCandidateToPairs(IDictionary<string, TemplatePairSymbols> pairs, TemplateCandidate candidate, ICollection<Diagnostic> diagnostics) {
		var isDef = candidate.Kind == TemplateKind.Def;
		var className = candidate.Symbol.Name;
		var instanceName = GetInstanceName(className, isDef);
		var defName = isDef ? className : className + "Def";
		var namespaceName = candidate.Symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
		var pairKey = namespaceName + "|" + candidate.Family + "|" + instanceName;

		if (!pairs.TryGetValue(pairKey, out var pair)) {
			pair = new TemplatePairSymbols(instanceName, defName, candidate.Family, namespaceName, candidate.Compilation);
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
			return;
		}

		if (pair.InstanceSymbol is not null && !SymbolEqualityComparer.Default.Equals(pair.InstanceSymbol, candidate.Symbol)) {
			diagnostics.Add(CreateTemplatePairDiagnostic(candidate.Symbol, pair, "multiple instance templates share the same name"));
			return;
		}
		pair.InstanceSymbol = candidate.Symbol;
		pair.InstanceName = candidate.Symbol.Name;
		pair.InstanceCopyAllowed = candidate.CopyAllowed ?? true;
	}

	private static Diagnostic CreateTemplatePairDiagnostic(INamedTypeSymbol conflictingSymbol, TemplatePairSymbols existingPair, string reason) {
		var conflictingName = conflictingSymbol.ToDisplayString();
		var existingName = (existingPair.InstanceSymbol ?? existingPair.DefSymbol)?.ToDisplayString() ?? existingPair.InstanceName;
		var location = conflictingSymbol.Locations.FirstOrDefault();
		return Diagnostic.Create(TemplatePairConflictDescriptor, location, conflictingName, existingName, reason);
	}

	private static bool TryGetBaseTypeNames(TemplatePairSymbols pair, out string instanceBaseName, out string defBaseName) {
		if (pair.InstanceSymbol?.BaseType is INamedTypeSymbol instanceBase) {
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

	private static bool TryCreateRenderPair(TemplatePairSymbols symbolPair, ICollection<Diagnostic> diagnostics, out TemplatePair? renderPair) {
		if (!TryGetBaseTypeNames(symbolPair, out var instanceBaseName, out var defBaseName)) {
			renderPair = null;
			return false;
		}

		var defFields = Parser.GetDefFields(symbolPair.DefSymbol, symbolPair.Compilation, diagnostics).ToList();
		var instanceFields = GetInstanceFields(symbolPair.InstanceSymbol, symbolPair.Compilation, diagnostics).ToList();
		WarnCopyableDataWithCopyDisabled(symbolPair.InstanceCopyAllowed, symbolPair.InstanceSymbol?.Name ?? string.Empty, instanceFields, diagnostics);
		var saveLoadMembers = ValidateDistinctSaveLoadKeys(
			instanceFields.Where(f => f.IncludeInSaveLoad).ToList(),
			diagnostics);
		var copyMembers = instanceFields.Where(f => f.IncludeInCopy).ToList();

		renderPair = new TemplatePair(
			symbolPair.InstanceName,
			symbolPair.DefName,
			symbolPair.Family,
			symbolPair.NamespaceName,
			instanceBaseName,
			defBaseName,
			symbolPair.InstanceCopyAllowed,
			defFields,
			saveLoadMembers,
			copyMembers);
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

	private sealed class KnownTypeSymbols {
		internal KnownTypeSymbols(Compilation compilation) {
			Thing = GetRequired(compilation, "SteamEngine.Thing");
			AbstractScript = GetRequired(compilation, "SteamEngine.AbstractScript");
			Region = GetRequired(compilation, "SteamEngine.Regions.Region");
			Globals = GetRequired(compilation, "SteamEngine.Globals");
			IAbstractKey = GetRequired(compilation, "SteamEngine.IAbstractKey");
			AbstractAccount = GetRequired(compilation, "SteamEngine.AbstractAccount");
			SaveableDataAttribute = GetRequired(compilation, "SteamEngine.SaveableDataAttribute");
			CopyableDataAttribute = GetRequired(compilation, "SteamEngine.CopyableDataAttribute");
		}

		internal INamedTypeSymbol Thing { get; }
		internal INamedTypeSymbol AbstractScript { get; }
		internal INamedTypeSymbol Region { get; }
		internal INamedTypeSymbol Globals { get; }
		internal INamedTypeSymbol IAbstractKey { get; }
		internal INamedTypeSymbol AbstractAccount { get; }
		internal INamedTypeSymbol SaveableDataAttribute { get; }
		internal INamedTypeSymbol CopyableDataAttribute { get; }

		private static INamedTypeSymbol GetRequired(Compilation compilation, string metadataName) {
			return compilation.GetTypeByMetadataName(metadataName)
				?? throw new InvalidOperationException("Canonical engine type " + metadataName + " is unavailable.");
		}
	}

	internal static IEnumerable<InstanceFieldSpec> GetInstanceFields(INamedTypeSymbol? instanceSymbol, Compilation compilation, ICollection<Diagnostic> diagnostics) {
		if (instanceSymbol is null) {
			return Array.Empty<InstanceFieldSpec>();
		}

		var knownTypes = new KnownTypeSymbols(compilation);
		var fields = new List<InstanceFieldSpec>();
		foreach (var member in instanceSymbol.GetMembers()) {
			if (!TryCreateInstanceFieldSpec(instanceSymbol, member, knownTypes, diagnostics, out var spec, out var error)) {
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
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		out InstanceFieldSpec spec,
		out Diagnostic? error) {
		var hasSaveableData = HasMemberAttribute(member, knownTypes.SaveableDataAttribute, SaveableDataAttributeName, diagnostics);
		var hasCopyableData = HasMemberAttribute(member, knownTypes.CopyableDataAttribute, CopyableDataAttributeName, diagnostics);
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
			spec = CreateInstanceFieldSpec(field, knownTypes, diagnostics, hasSaveableData, hasCopyableData);
			error = null;
			return true;
		}

		if (member is IPropertySymbol property) {
			spec = CreateInstancePropertySpec(property, knownTypes, diagnostics, hasSaveableData, hasCopyableData);
			error = null;
			return true;
		}

		spec = default;
		error = CreateUnsupportedMemberDiagnostic(member, hasSaveableData, hasCopyableData, "only fields and properties are supported");
		return false;
	}

	private static bool HasMemberAttribute(ISymbol member, INamedTypeSymbol expectedSymbol, string expectedAttributeName, ICollection<Diagnostic> diagnostics) {
		var found = false;
		var expectedFullName = expectedSymbol.ToDisplayString();
		foreach (var attr in member.GetAttributes()) {
			var attrClass = attr.AttributeClass;
			if (attrClass is null) {
				continue;
			}
			var simpleName = Parser.StripAttributeSuffix(attrClass.Name);
			if (!string.Equals(simpleName, expectedAttributeName, StringComparison.Ordinal)) {
				continue;
			}
			if (SymbolEqualityComparer.Default.Equals(attrClass, expectedSymbol)
				|| string.Equals(attrClass.ToDisplayString(), expectedFullName, StringComparison.Ordinal)) {
				found = true;
				continue;
			}
			diagnostics.Add(Diagnostic.Create(
				AttributeShadowingDescriptor,
				member.Locations.FirstOrDefault() ?? Location.None,
				expectedAttributeName,
				attrClass.ToDisplayString(),
				expectedSymbol.ToDisplayString()));
		}
		return found;
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
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		bool includeInSaveLoad,
		bool includeInCopy) {
		return CreateInstanceMemberSpec(
			member.Name,
			Parser.GetInitializer(member) ?? "default",
			member.Type,
			member.Locations.FirstOrDefault() ?? Location.None,
			knownTypes,
			diagnostics,
			includeInSaveLoad,
			includeInCopy);
	}

	private static InstanceFieldSpec CreateInstancePropertySpec(
		IPropertySymbol member,
		KnownTypeSymbols knownTypes,
		ICollection<Diagnostic> diagnostics,
		bool includeInSaveLoad,
		bool includeInCopy) {
		return CreateInstanceMemberSpec(
			member.Name,
			Parser.GetInitializer(member) ?? "default",
			member.Type,
			member.Locations.FirstOrDefault() ?? Location.None,
			knownTypes,
			diagnostics,
			includeInSaveLoad,
			includeInCopy);
	}

	private static InstanceFieldSpec CreateInstanceMemberSpec(
		string name,
		string defaultExpression,
		ITypeSymbol type,
		Location location,
		KnownTypeSymbols knownTypes,
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
			GetMemberValueKind(type, knownTypes, diagnostics, location),
			location);
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
		if (IsTypeOrBase(type, knownTypes.Thing, "Thing", diagnostics, location)) {
			return MemberValueKind.Thing;
		}
		if (IsTypeOrBase(type, knownTypes.Globals, "Globals", diagnostics, location)) {
			return MemberValueKind.Globals;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractScript, "AbstractScript", diagnostics, location)) {
			return MemberValueKind.AbstractScript;
		}
		if (IsTypeOrBase(type, knownTypes.Region, "Region", diagnostics, location)) {
			return MemberValueKind.Region;
		}
		if (ImplementsInterface(type, knownTypes.IAbstractKey, "IAbstractKey", diagnostics, location)) {
			return MemberValueKind.IAbstractKey;
		}
		if (IsTypeOrBase(type, knownTypes.AbstractAccount, "AbstractAccount", diagnostics, location)) {
			return MemberValueKind.AbstractAccount;
		}
		return MemberValueKind.Fallback;
	}

	private static bool IsTypeOrBase(ITypeSymbol type, INamedTypeSymbol canonical, string name, ICollection<Diagnostic> diagnostics, Location location) {
		if (canonical is null) {
			throw new InvalidOperationException("Canonical engine type " + name + " is unavailable.");
		}
		for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType) {
			if (SymbolEqualityComparer.Default.Equals(current, canonical)) {
				return true;
			}
			if (current.Name == name) {
				diagnostics.Add(CreateTypeNameConflictDiagnostic(location, current, canonical));
			}
		}
		return false;
	}

	private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol canonical, string interfaceName, ICollection<Diagnostic> diagnostics, Location location) {
		if (canonical is null) {
			throw new InvalidOperationException("Canonical engine interface " + interfaceName + " is unavailable.");
		}
		foreach (var iface in type.AllInterfaces) {
			if (SymbolEqualityComparer.Default.Equals(iface, canonical)) {
				return true;
			}
			if (iface.Name == interfaceName) {
				diagnostics.Add(CreateTypeNameConflictDiagnostic(location, iface, canonical));
			}
		}
		return false;
	}

	private static Diagnostic CreateTypeNameConflictDiagnostic(Location location, ITypeSymbol actual, INamedTypeSymbol canonical) {
		return Diagnostic.Create(
			TypeNameConflictDescriptor,
			location,
			actual.ToDisplayString(),
			canonical.ToDisplayString());
	}

	private sealed class TemplatePairSymbols {
		internal TemplatePairSymbols(string instanceName, string defName, TemplateFamily family, string namespaceName, Compilation compilation) {
			InstanceName = instanceName;
			DefName = defName;
			Family = family;
			NamespaceName = namespaceName;
			Compilation = compilation;
		}

		internal string InstanceName { get; set; }
		internal string DefName { get; set; }
		internal TemplateFamily Family { get; }
		internal string NamespaceName { get; }
		internal Compilation Compilation { get; }
		internal INamedTypeSymbol? InstanceSymbol { get; set; }
		internal INamedTypeSymbol? DefSymbol { get; set; }
		internal bool InstanceCopyAllowed { get; set; } = true;
	}
}

