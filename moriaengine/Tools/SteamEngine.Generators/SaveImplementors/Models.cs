using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.SaveImplementors;

internal sealed class SaveImplementorCandidate {
	internal SaveImplementorCandidate(
		INamedTypeSymbol symbol,
		IMethodSymbol? loadingInitializer,
		IMethodSymbol? loadSection,
		IMethodSymbol? loadLine,
		IMethodSymbol? loadingFinalizer,
		IMethodSymbol? saveMethod,
		IReadOnlyList<AttributedMember> saveableMembers) {
		Symbol = symbol;
		LoadingInitializer = loadingInitializer;
		LoadSection = loadSection;
		LoadLine = loadLine;
		LoadingFinalizer = loadingFinalizer;
		SaveMethod = saveMethod;
		SaveableMembers = saveableMembers;
	}

	internal INamedTypeSymbol Symbol { get; }
	internal IMethodSymbol? LoadingInitializer { get; }
	internal IMethodSymbol? LoadSection { get; }
	internal IMethodSymbol? LoadLine { get; }
	internal IMethodSymbol? LoadingFinalizer { get; }
	internal IMethodSymbol? SaveMethod { get; }
	internal IReadOnlyList<AttributedMember> SaveableMembers { get; }
}

internal sealed class SaveImplementorModel {
	internal SaveImplementorModel(
		string generatedTypeName,
		string qualifiedTypeName,
		string headerName,
		SaveFactoryMethod loadFactory,
		string? loadLineMethodName,
		string? loadingFinalizerMethodName,
		string? saveMethodName,
		IReadOnlyList<SaveableMemberModel> saveableMembers) {
		GeneratedTypeName = generatedTypeName;
		QualifiedTypeName = qualifiedTypeName;
		HeaderName = headerName;
		LoadFactory = loadFactory;
		LoadLineMethodName = loadLineMethodName;
		LoadingFinalizerMethodName = loadingFinalizerMethodName;
		SaveMethodName = saveMethodName;
		SaveableMembers = saveableMembers;
	}

	internal string GeneratedTypeName { get; }
	internal string QualifiedTypeName { get; }
	internal string HeaderName { get; }
	internal SaveFactoryMethod LoadFactory { get; }
	internal string? LoadLineMethodName { get; }
	internal string? LoadingFinalizerMethodName { get; }
	internal string? SaveMethodName { get; }
	internal IReadOnlyList<SaveableMemberModel> SaveableMembers { get; }
}

internal sealed class SaveFactoryMethod {
	internal SaveFactoryMethod(
		SaveFactoryKind kind,
		string declaringTypeName,
		string methodName,
		bool takesPropsSection,
		bool requiresCast) {
		Kind = kind;
		DeclaringTypeName = declaringTypeName;
		MethodName = methodName;
		TakesPropsSection = takesPropsSection;
		RequiresCast = requiresCast;
	}

	internal SaveFactoryKind Kind { get; }
	internal string DeclaringTypeName { get; }
	internal string MethodName { get; }
	internal bool TakesPropsSection { get; }
	internal bool RequiresCast { get; }
}

internal enum SaveFactoryKind {
	Constructor,
	StaticMethod
}

internal sealed class SaveableMemberModel {
	internal SaveableMemberModel(
		string name,
		string memberTypeName,
		MemberValueKind valueKind) {
		Name = name;
		MemberTypeName = memberTypeName;
		ValueKind = valueKind;
	}

	internal string Name { get; }
	internal string MemberTypeName { get; }
	internal MemberValueKind ValueKind { get; }
}
