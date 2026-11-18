using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.CompiledScriptHolders;

internal sealed class ScriptHolderCandidate {
	internal ScriptHolderCandidate(IMethodSymbol methodSymbol, string functionName, string? description) {
		MethodSymbol = methodSymbol;
		FunctionName = functionName;
		Description = description;
	}

	internal IMethodSymbol MethodSymbol { get; }
	internal string FunctionName { get; }
	internal string? Description { get; }
}

internal sealed class ScriptHolderModel {
	internal ScriptHolderModel(
		string generatedTypeName,
		string functionName,
		string? description,
		string declaringTypeName,
		string methodName,
		bool returnsVoid,
		bool needsArgvArray,
		IReadOnlyList<InvocationParameterModel> parameters) {
		GeneratedTypeName = generatedTypeName;
		FunctionName = functionName;
		Description = description;
		DeclaringTypeName = declaringTypeName;
		MethodName = methodName;
		ReturnsVoid = returnsVoid;
		NeedsArgvArray = needsArgvArray;
		Parameters = parameters;
	}

	internal string GeneratedTypeName { get; }
	internal string FunctionName { get; }
	internal string? Description { get; }
	internal string DeclaringTypeName { get; }
	internal string MethodName { get; }
	internal bool ReturnsVoid { get; }
	internal bool NeedsArgvArray { get; }
	internal IReadOnlyList<InvocationParameterModel> Parameters { get; }
}

internal sealed class InvocationParameterModel {
	internal InvocationParameterModel(string expression) {
		Expression = expression;
	}

	internal string Expression { get; }
}
