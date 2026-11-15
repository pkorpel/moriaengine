using System;

namespace SteamEngine {
	/// <summary>
	/// Use to mark the instance fields and properties to be automatically saved and loaded.
	/// This works with both generic <c>[SaveableClass]</c> generation and <c>[ThingTemplate]</c>/<c>[PluginTemplate]</c> generation.
	/// </summary>
	/// <remarks>
	/// For generic <c>[SaveableClass]</c> generation, decorated members are expected to be public and properties must be simple (no indexers).
	/// For <c>[ThingTemplate]</c>/<c>[PluginTemplate]</c> generation, decorated fields do not have to be public.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class SaveableDataAttribute : Attribute {
		private readonly string description;

		public string Description {
			get {
				return this.description;
			}
		}

		public SaveableDataAttribute() {
		}

		public SaveableDataAttribute(string description) {
			this.description = description;
		}
	}

	/// <summary>
	/// Use this to decorate instance fields and properties that are supposed to be deep-copied with the main object.
	/// This works with both generic <c>[DeepCopyableClass]</c> generation and <c>[ThingTemplate]</c>/<c>[PluginTemplate]</c> generation.
	/// </summary>
	/// <remarks>
	/// For generic <c>[DeepCopyableClass]</c> generation, decorated members are expected to be public.
	/// For <c>[ThingTemplate]</c>/<c>[PluginTemplate]</c> generation, decorated fields do not have to be public.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class CopyableDataAttribute : Attribute {
	}
}
