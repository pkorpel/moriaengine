using System;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Marks a class or struct as available to the .info dialog.</summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ViewableClassAttribute : Attribute {
		public ViewableClassAttribute() { }
		public ViewableClassAttribute(string name) {
			Name = name;
		}

		public string? Name { get; }
	}

	/// <summary>Excludes a public field or property from a viewable class.</summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class NoShowAttribute : Attribute { }

	/// <summary>Overrides the displayed label for a field or property.</summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class InfoFieldAttribute : Attribute {
		public InfoFieldAttribute(string name) {
			Name = name;
		}

		public string Name { get; }
	}

	/// <summary>Exposes a method as an action button in the .info dialog.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ButtonAttribute : Attribute {
		public ButtonAttribute() { }
		public ButtonAttribute(string name) {
			Name = name;
		}

		public string? Name { get; }
	}

	/// <summary>Adds descriptor-provided fields and buttons to a handled type's view.</summary>
	public class ViewDescriptorAttribute : Attribute {
		public ViewDescriptorAttribute(Type handledType) {
			HandledType = handledType;
		}

		public ViewDescriptorAttribute(Type handledType, string name) : this(handledType) {
			Name = name;
		}

		public ViewDescriptorAttribute(Type handledType, string name, string[] nonDisplayedFields) : this(handledType, name) {
			NonDisplayedFields = nonDisplayedFields;
		}

		public Type HandledType { get; }
		public string? Name { get; }
		public string[]? NonDisplayedFields { get; }
	}

	/// <summary>Supplies a descriptor-backed field getter and its value type.</summary>
	public class GetMethodAttribute : Attribute {
		public GetMethodAttribute(string name, Type fieldType) {
			Name = name;
			FieldType = fieldType;
		}

		public string Name { get; }
		public Type FieldType { get; }
	}

	/// <summary>Supplies a descriptor-backed field setter and its value type.</summary>
	public class SetMethodAttribute : Attribute {
		public SetMethodAttribute(string name, Type fieldType) {
			Name = name;
			FieldType = fieldType;
		}

		public string Name { get; }
		public Type FieldType { get; }
	}
}
