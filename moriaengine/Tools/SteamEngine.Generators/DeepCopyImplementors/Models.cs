using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.DeepCopyImplementors;

internal sealed class DeepCopyCandidate {
	internal DeepCopyCandidate(
		INamedTypeSymbol symbol,
		IMethodSymbol? initializerMethod,
		IReadOnlyList<AttributedMember> copyableMembers) {
		Symbol = symbol;
		InitializerMethod = initializerMethod;
		CopyableMembers = copyableMembers;
	}

	internal INamedTypeSymbol Symbol { get; }
	internal IMethodSymbol? InitializerMethod { get; }
	internal IReadOnlyList<AttributedMember> CopyableMembers { get; }
}

internal sealed class DeepCopyModel {
	internal DeepCopyModel(
		string generatedTypeName,
		string qualifiedTypeName,
		DeepCopyInitializerModel initializer,
		IReadOnlyList<DeepCopyMemberModel> members) {
		GeneratedTypeName = generatedTypeName;
		QualifiedTypeName = qualifiedTypeName;
		Initializer = initializer;
		Members = members;
	}

	internal string GeneratedTypeName { get; }
	internal string QualifiedTypeName { get; }
	internal DeepCopyInitializerModel Initializer { get; }
	internal IReadOnlyList<DeepCopyMemberModel> Members { get; }
}

internal sealed class DeepCopyInitializerModel {
	internal DeepCopyInitializerModel(
		DeepCopyInitializerKind kind,
		string initializerName,
		bool acceptsSourceParameter) {
		Kind = kind;
		InitializerName = initializerName;
		AcceptsSourceParameter = acceptsSourceParameter;
	}

	internal DeepCopyInitializerKind Kind { get; }
	internal string InitializerName { get; }
	internal bool AcceptsSourceParameter { get; }
}

internal enum DeepCopyInitializerKind {
	Constructor,
	StaticMethod
}

internal sealed class DeepCopyMemberModel {
	internal DeepCopyMemberModel(
		string memberName,
		string memberTypeName,
		MemberValueKind valueKind) {
		MemberName = memberName;
		MemberTypeName = memberTypeName;
		ValueKind = valueKind;
	}

	internal string MemberName { get; }
	internal string MemberTypeName { get; }
	internal MemberValueKind ValueKind { get; }
}
