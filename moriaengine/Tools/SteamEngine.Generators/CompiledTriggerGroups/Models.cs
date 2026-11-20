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
		string typeName,
		IReadOnlyList<CompiledTriggerMethodModel> triggerMethods) {
		NamespaceName = namespaceName;
		TypeName = typeName;
		TriggerMethods = triggerMethods;
	}

	internal string NamespaceName { get; }
	internal string TypeName { get; }
	internal IReadOnlyList<CompiledTriggerMethodModel> TriggerMethods { get; }
}

internal sealed class CompiledTriggerMethodModel {
	internal CompiledTriggerMethodModel(
		string triggerName,
		string methodName,
		bool returnsVoid,
		IReadOnlyList<DispatchParameterModel> parameters) {
		TriggerName = triggerName;
		MethodName = methodName;
		ReturnsVoid = returnsVoid;
		Parameters = parameters;
	}

	internal string TriggerName { get; }
	internal string MethodName { get; }
	internal bool ReturnsVoid { get; }
	internal IReadOnlyList<DispatchParameterModel> Parameters { get; }
}
