using System;

namespace SteamEngine.CompiledScripts {
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SteamFunctionAttribute : Attribute {
		public SteamFunctionAttribute() {
			NewFunctionName = null;
		}

		public SteamFunctionAttribute(string? newFunctionName) {
			NewFunctionName = newFunctionName;
		}

		public string? NewFunctionName { get; }
	}
}
