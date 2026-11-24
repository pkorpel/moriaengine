using SteamEngine.CompiledScripts;
using SteamEngine.Generators.CompiledTriggerGroups;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Runs the generated Run dispatch of a real CompiledTriggerGroup. CompiledTriggerGroupGeneratorTests
// covers diagnostics and generated text on stubs.
public sealed class CompiledTriggerGroupGeneratorRuntimeTests {
	[Fact]
	public void Run_CallsTheTriggerMethodWithConvertedArguments() {
		const string source = """
using SteamEngine;
using SteamEngine.CompiledScripts;

namespace TriggerGroupSandbox;

public enum Choice : long { None, Second }

public abstract class BaseTriggerGroup : CompiledTriggerGroup {
	public object on_Login(object self, ScriptArgs args) {
		return args.Argv[0];
	}
}

public partial class ArenaTriggerGroup : BaseTriggerGroup {
	public string Log = "";

	public void On_DClick(object self, int value, bool enabled) {
		Log += self + ":" + value + ":" + enabled + ";";
	}

	public object On_Choose(object self, Choice choice) {
		return choice;
	}
}
""";

		var assembly = EngineRuntimeHarness.Compile(new CompiledTriggerGroupsGenerator(), source);
		var group = (CompiledTriggerGroup)EngineRuntimeHarness.Create(assembly, "TriggerGroupSandbox.ArenaTriggerGroup");
		// The generated group retains the concrete class name as its defname.
		Assert.Equal("ArenaTriggerGroup", group.Defname);

		// Dispatch to a local void handler, converting its self/value/bool arguments.
		Assert.Null(group.Run("me", TriggerKey.Acquire("dclick"), new ScriptArgs(5L, "true")));
		Assert.Equal("me:5:True;", (string)((dynamic)group).Log);

		// Dispatch also finds inherited handlers and returns their results; unknown triggers are ignored.
		Assert.Equal("Second", group.Run("me", TriggerKey.Acquire("choose"), new ScriptArgs(1)).ToString());
		Assert.Equal("hello", group.Run("me", TriggerKey.Acquire("login"), new ScriptArgs("hello")));
		Assert.Null(group.Run("me", TriggerKey.Acquire("nosuchtrigger"), new ScriptArgs()));
		// The inherited handlers did not mutate the local handler's state.
		Assert.Equal("me:5:True;", (string)((dynamic)group).Log);
	}
}
