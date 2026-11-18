using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Marks a public static script method as callable through the compiled script-function registry.
	/// </summary>
	/// <remarks>
	/// Use this on functions that should be discoverable by name from script infrastructure.
	/// Set <see cref="NewFunctionName"/> to expose a custom registry name instead of the method name.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SteamFunctionAttribute : Attribute {
		/// <summary>
		/// Creates a steam-function declaration that uses the method's own name.
		/// </summary>
		public SteamFunctionAttribute() {
			NewFunctionName = null;
		}

		/// <summary>
		/// Creates a steam-function declaration with an explicit exposed name.
		/// </summary>
		/// <param name="newFunctionName">Custom name to register for script lookup.</param>
		public SteamFunctionAttribute(string? newFunctionName) {
			NewFunctionName = newFunctionName;
		}

		/// <summary>
		/// Optional custom function name used in the script registry.
		/// </summary>
		public string? NewFunctionName { get; }
	}
}
