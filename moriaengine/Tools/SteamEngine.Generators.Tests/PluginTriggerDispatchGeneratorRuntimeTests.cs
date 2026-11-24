using SteamEngine.Generators.PluginTriggerDispatch;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Runs the generated Run override of a real Plugin subclass. PluginTriggerDispatchGeneratorTests
// covers diagnostics and generated text on stubs.
public sealed class PluginTriggerDispatchGeneratorRuntimeTests {
	[Fact]
	public void Run_CallsTheTriggerMethodWithConvertedArguments() {
		const string source = """
using SteamEngine;

namespace PluginSandbox;

public enum Choice : long { None, Second }

public partial class ArenaPlugin : Plugin {
	public string Log = "";

	public void On_Assign() {
		Log += "assign;";
	}

	public object On_DenyMove(bool denied, int reason) {
		return denied ? reason : -reason;
	}

	public object On_Choose(Choice choice) {
		return choice;
	}

	public object On_Login(ScriptArgs args) {
		return args.Argv[0];
	}
}
""";

		var assembly = EngineRuntimeHarness.Compile(new PluginTriggerDispatchGenerator(), source);
		var plugin = (Plugin)EngineRuntimeHarness.Create(assembly, "PluginSandbox.ArenaPlugin");

		// Trigger names are normalized, and a void handler runs while returning null.
		Assert.Null(plugin.Run(TriggerKey.Acquire("assign"), new ScriptArgs()));
		Assert.Equal("assign;", (string)((dynamic)plugin).Log);

		// Arguments convert from ScriptArgs to bool/int/enum parameters, and handler results pass through.
		Assert.Equal(7, plugin.Run(TriggerKey.Acquire("denymove"), new ScriptArgs("true", 7L)));
		Assert.Equal(-7, plugin.Run(TriggerKey.Acquire("denymove"), new ScriptArgs(false, "7")));
		Assert.Equal("Second", plugin.Run(TriggerKey.Acquire("choose"), new ScriptArgs(1)).ToString());
		// A ScriptArgs parameter receives the original arguments; unknown triggers have no handler.
		Assert.Equal("hello", plugin.Run(TriggerKey.Acquire("login"), new ScriptArgs("hello")));
		Assert.Null(plugin.Run(TriggerKey.Acquire("nosuchtrigger"), new ScriptArgs()));
	}
}
