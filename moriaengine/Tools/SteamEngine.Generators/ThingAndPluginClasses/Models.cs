using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ThingAndPluginClasses;

internal enum TemplateKind {
	Instance,
	Def
}

internal enum TemplateFamily {
	Thing,
	Plugin
}

internal sealed class TemplateCandidate {
	internal TemplateCandidate(INamedTypeSymbol symbol, TemplateKind kind, TemplateFamily family, bool? copyAllowed, Compilation compilation) {
		Symbol = symbol;
		Kind = kind;
		Family = family;
		CopyAllowed = copyAllowed;
		Compilation = compilation;
	}

	internal INamedTypeSymbol Symbol { get; }
	internal TemplateKind Kind { get; }
	internal TemplateFamily Family { get; }
	internal bool? CopyAllowed { get; }
	internal Compilation Compilation { get; }
}

internal sealed class TemplatePair {
	internal TemplatePair(
		string instanceName,
		string defName,
		TemplateFamily family,
		string namespaceName,
		string instanceBaseName,
		string defBaseName,
		bool instanceCopyAllowed,
		IReadOnlyList<DefFieldSpec> defFields,
		IReadOnlyList<InstanceFieldSpec> saveLoadMembers,
		IReadOnlyList<InstanceFieldSpec> copyMembers) {
		InstanceName = instanceName;
		DefName = defName;
		Family = family;
		NamespaceName = namespaceName;
		InstanceBaseName = instanceBaseName;
		DefBaseName = defBaseName;
		InstanceCopyAllowed = instanceCopyAllowed;
		DefFields = defFields;
		SaveLoadMembers = saveLoadMembers;
		CopyMembers = copyMembers;
	}

	internal string InstanceName { get; }
	internal string DefName { get; }
	internal TemplateFamily Family { get; }
	internal string NamespaceName { get; }
	internal string InstanceBaseName { get; }
	internal string DefBaseName { get; }
	internal bool InstanceCopyAllowed { get; }
	internal IReadOnlyList<DefFieldSpec> DefFields { get; }
	internal IReadOnlyList<InstanceFieldSpec> SaveLoadMembers { get; }
	internal IReadOnlyList<InstanceFieldSpec> CopyMembers { get; }
}

internal readonly struct DefFieldSpec {
	internal DefFieldSpec(string name, string typeName, string defaultExpression) {
		Name = name;
		TypeName = typeName;
		DefaultExpression = defaultExpression;
	}

	internal string Name { get; }
	internal string TypeName { get; }
	internal string DefaultExpression { get; }
}

internal readonly struct InstanceFieldSpec {
	internal InstanceFieldSpec(
		string name,
		string typeName,
		string defaultExpression,
		bool includeInSaveLoad,
		bool includeInCopy,
		MemberValueKind valueKind,
		Location? location) {
		Name = name;
		TypeName = typeName;
		DefaultExpression = defaultExpression;
		IncludeInSaveLoad = includeInSaveLoad;
		IncludeInCopy = includeInCopy;
		ValueKind = valueKind;
		Location = location;
	}

	internal string Name { get; }
	internal string TypeName { get; }
	internal string DefaultExpression { get; }
	internal bool IncludeInSaveLoad { get; }
	internal bool IncludeInCopy { get; }
	internal MemberValueKind ValueKind { get; }
	internal Location? Location { get; }
}

