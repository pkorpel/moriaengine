using System;

namespace SteamEngine.Generators.Abstractions {
	/// <summary>
	/// Use this on a Thing instance class to mark it for Generator output.
	/// The generator emits the instance partial (save/load/deepcopy/type-def helpers) from annotated fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ThingTemplateAttribute : Attribute {
		/// <summary>
		/// Controls copy-constructor generation for this template class.
		/// Leave true for normal behavior; set false to suppress generated deep-copy constructor/attribute.
		/// </summary>
		public bool Copy { get; set; } = true;
	}

	/// <summary>
	/// Use this on a ThingDef class that corresponds to a Thing template instance class.
	/// Fields decorated with <see cref="DefFieldAttribute"/> become generated typed def properties.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ThingDefTemplateAttribute : Attribute {
	}

	/// <summary>
	/// Use this on a Plugin instance class to mark it for Generator output.
	/// The generator emits persistence/deepcopy scaffolding according to annotated template fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PluginTemplateAttribute : Attribute {
		/// <summary>
		/// Controls copy-constructor generation for this template class.
		/// Leave true for normal behavior; set false to suppress generated deep-copy constructor/attribute.
		/// </summary>
		public bool Copy { get; set; } = true;
	}

	/// <summary>
	/// Use this on a PluginDef class that corresponds to a Plugin template instance class.
	/// Fields decorated with <see cref="DefFieldAttribute"/> become generated typed def properties.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PluginDefTemplateAttribute : Attribute {
	}

	/// <summary>
	/// Marks a def backing field that should be exposed as a generated typed def property.
	/// The field type is described by <see cref="ValueType"/>, optionally with a literal <see cref="DefaultExpression"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DefFieldAttribute : Attribute {
		/// <summary>
		/// Creates a generated-def field descriptor for the provided value type.
		/// </summary>
		public DefFieldAttribute(Type valueType) {
			ValueType = valueType;
			DefaultExpression = null;
		}

		/// <summary>
		/// The runtime type used for generated typed accessors and InitTypedField wiring.
		/// </summary>
		public Type ValueType { get; }

		/// <summary>
		/// Optional C# expression emitted as the default value in generated InitTypedField calls.
		/// When omitted, the generator uses default(ValueType).
		/// </summary>
		public string? DefaultExpression { get; set; }
	}
}
