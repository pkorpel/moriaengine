using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledTriggerGroups;

internal sealed class CompiledTriggerGroupCandidate {
	internal CompiledTriggerGroupCandidate(INamedTypeSymbol typeSymbol, IReadOnlyList<IMethodSymbol> triggerMethods) {
		TypeSymbol = typeSymbol;
		TriggerMethods = triggerMethods;
	}

	internal INamedTypeSymbol TypeSymbol { get; }
	internal IReadOnlyList<IMethodSymbol> TriggerMethods { get; }
}

internal sealed class CompiledTriggerGroupModel {
	internal CompiledTriggerGroupModel(
		string namespaceName,
		IReadOnlyList<ContainingTypeModel> containingTypes,
		string typeAccessibility,
		string typeName,
		IReadOnlyList<CompiledTriggerMethodModel> triggerMethods) {
		NamespaceName = namespaceName;
		ContainingTypes = containingTypes;
		TypeAccessibility = typeAccessibility;
		TypeName = typeName;
		TriggerMethods = triggerMethods;
	}

	internal string NamespaceName { get; }
	internal IReadOnlyList<ContainingTypeModel> ContainingTypes { get; }
	internal string TypeAccessibility { get; }
	internal string TypeName { get; }
	internal IReadOnlyList<CompiledTriggerMethodModel> TriggerMethods { get; }
}

internal sealed class ContainingTypeModel {
	internal ContainingTypeModel(
		string accessibility,
		bool isStatic,
		bool isAbstract,
		bool isSealed,
		string typeName) {
		Accessibility = accessibility;
		IsStatic = isStatic;
		IsAbstract = isAbstract;
		IsSealed = isSealed;
		TypeName = typeName;
	}

	internal string Accessibility { get; }
	internal bool IsStatic { get; }
	internal bool IsAbstract { get; }
	internal bool IsSealed { get; }
	internal string TypeName { get; }
}

internal sealed class CompiledTriggerMethodModel {
	internal CompiledTriggerMethodModel(
		string triggerName,
		string methodName,
		bool returnsVoid,
		IReadOnlyList<InvocationParameterModel> parameters) {
		TriggerName = triggerName;
		MethodName = methodName;
		ReturnsVoid = returnsVoid;
		Parameters = parameters;
	}

	internal string TriggerName { get; }
	internal string MethodName { get; }
	internal bool ReturnsVoid { get; }
	internal IReadOnlyList<InvocationParameterModel> Parameters { get; }
}

internal sealed class InvocationParameterModel {
	internal InvocationParameterModel(
		bool usesScriptArgsInput,
		InvocationParameterConversion conversion,
		string? typeName) {
		UsesScriptArgsInput = usesScriptArgsInput;
		Conversion = conversion;
		TypeName = typeName;
	}

	internal bool UsesScriptArgsInput { get; }
	internal InvocationParameterConversion Conversion { get; }
	internal string? TypeName { get; }
}

internal enum InvocationParameterConversion {
	None,
	Cast,
	Convert,
	EnumConvertThenCast
}
