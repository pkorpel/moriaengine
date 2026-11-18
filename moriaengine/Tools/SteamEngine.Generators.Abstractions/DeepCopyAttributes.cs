using System;

namespace SteamEngine {
	/// <summary>
	/// Decorate your class by this attribute if you want it to be cloneable using the DeepCopyFactory framework.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class DeepCopyableClassAttribute : Attribute {
	}

	/// <summary>
	/// Use this to decorate a static method or constructor that implements deep copying of instances of the given class.
	/// It must return value of assignable (the same) type for static methods.
	/// If it has one parameter of its type, the original object being copied will be supplied using this parameter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class DeepCopyImplementationAttribute : Attribute {
	}
}
