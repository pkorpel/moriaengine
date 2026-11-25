using System.Linq;
using SteamEngine.CompiledScripts;
using SteamEngine.Generators.CompiledScriptHolders;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Runs the generated CompiledScriptHolder of each [SteamFunction]. CompiledScriptHolderGeneratorTests
// covers diagnostics and generated text on stubs.
public sealed class CompiledScriptHolderGeneratorRuntimeTests {
	[Fact]
	public void Holders_CallTheFunctionWithConvertedArguments() {
		const string source = """
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace HolderSandbox;

public enum Choice : ulong { None, Second }

public static class Commands {
	private static ScriptArgs lastArgs;

	[SteamFunction]
	public static int Reveal(object self, int range) {
		return range * 2;
	}

	[SteamFunction("holder_runtime_cast")]
	public static void Cast(object self, ScriptArgs args) {
		lastArgs = args;
	}

	[SteamFunction]
	public static object LastCast(object self) {
		return lastArgs;
	}

	[SteamFunction]
	public static object Choose(object self, Choice choice, bool enabled) {
		return enabled ? self + ":" + choice : null;
	}
}
""";

		var assembly = EngineRuntimeHarness.Compile(new CompiledScriptHoldersGenerator(), source);
		var holders = EngineRuntimeHarness.CreateAll<CompiledScriptHolder>(assembly).ToDictionary(holder => holder.Name);
		// Generated holders are discoverable by script-function name, including an explicit override.
		Assert.Equal(new[] { "Choose", "LastCast", "Reveal", "holder_runtime_cast" }, holders.Keys.OrderBy(name => name, System.StringComparer.Ordinal));

		// String arguments convert to the declared parameter types, and the function result is returned.
		Assert.Equal(42, holders["Reveal"].Run("me", "21"));
		// Enum and bool arguments convert too; the function returns null when disabled.
		Assert.Equal("me:Second", holders["Choose"].Run("me", 1L, "true"));
		Assert.Null(holders["Choose"].Run("me", "1", false));

		// ScriptArgs pass through unchanged, void functions return null, and holder state is shared across calls.
		var args = new ScriptArgs("anything");
		Assert.Null(holders["holder_runtime_cast"].Run("me", args));
		Assert.Same(args, holders["LastCast"].Run("me"));
	}
}
