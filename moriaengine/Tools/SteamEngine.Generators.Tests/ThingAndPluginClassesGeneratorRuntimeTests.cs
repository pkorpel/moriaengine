using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SteamEngine.Generators.ThingAndPluginClasses;
using SteamEngine.Persistence;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Runs the generated Plugin/PluginDef and Thing/ThingDef partials against the real engine:
// def fields, creating an instance from its def, deep copy and save/load of the template's
// [SaveableData]/[CopyableData] fields. ThingAndPluginClassesGeneratorTests covers diagnostics
// and generated text on stubs.
[Collection(EngineGlobalsCollection.Name)]
public sealed class ThingAndPluginClassesGeneratorRuntimeTests {
	[Fact]
	public void EngineFixture_DoesNotRequestExitOrOpenLogFiles() {
		Assert.False(MainClass.ExitToken.IsCancellationRequested);
		Assert.False(Globals.LogToFiles);
	}

	[Fact]
	public void PluginPair_DefFieldsCreateCopyAndSaveLoadWork() {
		const string source = """
using SteamEngine;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace TemplateSandbox;

public enum Mood { Calm, Angry }

[PluginTemplate]
public partial class EchoWard : Plugin {
	[SaveableData, CopyableData]
	private int pulseSeconds = 5;

	[SaveableData, CopyableData]
	private string label = "ward";

	[SaveableData, CopyableData]
	private Mood mood;

	[SaveableData]
	private int savedOnly = 1;

	private int neither = 2;
}

[PluginDefTemplate]
public partial class EchoWardDef : PluginDef {
	[DefField(typeof(int), DefaultExpression = "7")]
	private FieldValue pulseSeconds;

	[DefField(typeof(string), DefaultExpression = "\"Echo\"")]
	private FieldValue school;
}
""";

		var assembly = EngineRuntimeHarness.Compile(new ThingAndPluginClassesGenerator(), source, nameof(PluginPair_DefFieldsCreateCopyAndSaveLoadWork));
		var defType = assembly.GetType("TemplateSandbox.EchoWardDef", throwOnError: true)!;
		var def = (PluginDef)System.Activator.CreateInstance(defType, "p_runtime_echo_ward", "runtime-test", 1)!;
		// DefField templates generate typed properties with defaults and writable values.
		dynamic dynDef = def;
		Assert.Equal(7, (int)dynDef.PulseSeconds);
		Assert.Equal("Echo", (string)dynDef.School);
		dynDef.PulseSeconds = 11;
		Assert.Equal(11, (int)dynDef.PulseSeconds);

		var plugin = def.Create();
		// The generated factory creates the declared plugin and attaches its definition.
		Assert.Equal("TemplateSandbox.EchoWard", plugin.GetType().FullName);
		Assert.Same(def, plugin.Def);
		Assert.Same(def, (object)((dynamic)plugin).TypeDef);
		SetField(plugin, "pulseSeconds", 9);
		SetField(plugin, "label", "changed");
		SetField(plugin, "mood", System.Enum.ToObject(assembly.GetType("TemplateSandbox.Mood")!, 1));
		SetField(plugin, "savedOnly", 3);
		SetField(plugin, "neither", 4);

		// Deep-copy copies only CopyableData: the shared opted-in values copy, but save-only
		// and unannotated fields retain their defaults.
		RegisterImplementors(assembly);
		var copy = (Plugin)DeepCopyFactory.GetCopy(plugin);
		Assert.NotSame(plugin, copy);
		Assert.Same(def, copy.Def);
		Assert.Equal(9, GetField(copy, "pulseSeconds"));
		Assert.Equal("changed", GetField(copy, "label"));
		Assert.Equal("Angry", GetField(copy, "mood").ToString());
		Assert.Equal(1, GetField(copy, "savedOnly"));
		Assert.Equal(2, GetField(copy, "neither"));

		// Saving includes SaveableData (including save-only fields), but excludes unannotated fields;
		// loading resolves the plugin's registered definition.
		def.Register(); // a saved plugin names its def, and loading finds it by defname
		var loaded = (Plugin)SaveAndLoad(assembly, plugin);
		Assert.Same(def, loaded.Def);
		Assert.Equal(9, GetField(loaded, "pulseSeconds"));
		Assert.Equal("changed", GetField(loaded, "label"));
		Assert.Equal("Angry", GetField(loaded, "mood").ToString());
		Assert.Equal(3, GetField(loaded, "savedOnly"));
		Assert.Equal(2, GetField(loaded, "neither"));
	}

	[Fact]
	public void ThingPair_DefFieldsConstructCopyAndSaveLoadWork() {
		const string source = """
using SteamEngine;
using SteamEngine.Generators.Abstractions;

namespace SteamEngine.CompiledScripts.ThingSandbox;

[ThingTemplate]
public partial class RuneLantern : Item {
	[SaveableData, CopyableData]
	private int charge = 3;

	[SaveableData]
	private string inscription = "rune";

	private int neither = 2;
}

[ThingDefTemplate]
public partial class RuneLanternDef : ItemDef {
	[DefField(typeof(int), DefaultExpression = "9")]
	private FieldValue maxCharge;
}
""";

		var assembly = EngineRuntimeHarness.Compile(new ThingAndPluginClassesGenerator(), source, nameof(ThingPair_DefFieldsConstructCopyAndSaveLoadWork));
		var defType = assembly.GetType("SteamEngine.CompiledScripts.ThingSandbox.RuneLanternDef", throwOnError: true)!;
		var def = (ThingDef)System.Activator.CreateInstance(defType, "i_runtime_rune_lantern", "runtime-test", 1)!;
		// ThingDef templates expose their generated defaulted property and allow it to be changed.
		dynamic dynDef = def;
		Assert.Equal(9, (int)dynDef.MaxCharge);
		dynDef.MaxCharge = 12;
		Assert.Equal(12, (int)dynDef.MaxCharge);
		def.Register();

		var thingType = assembly.GetType("SteamEngine.CompiledScripts.ThingSandbox.RuneLantern", throwOnError: true)!;
		var thing = (Thing)System.Activator.CreateInstance(thingType, def)!;
		try {
			// Constructing from a definition initializes both the public Def and generated TypeDef views.
			Assert.Same(def, thing.Def);
			Assert.Same(def, (object)((dynamic)thing).TypeDef);
			SetField(thing, "charge", 7);
			SetField(thing, "inscription", "changed");
			SetField(thing, "neither", 4);

			// Deep-copy preserves opted-in values and the definition, excludes save-only/unmarked values,
			// and allocates a fresh world uid.
			RegisterImplementors(assembly);
			var copy = (Thing)DeepCopyFactory.GetCopy(thing);
			try {
				Assert.NotSame(thing, copy);
				Assert.NotEqual(thing.Uid, copy.Uid);
				Assert.Same(def, copy.Def);
				Assert.Equal(7, GetField(copy, "charge"));
				Assert.Equal("rune", GetField(copy, "inscription"));
				Assert.Equal(2, GetField(copy, "neither"));
			} finally {
				copy.Delete();
			}

			// Loading a whole Thing is world-load machinery (Thing.Load, uid reservation); test the
			// generated LoadLine override directly with persisted fields and confirm others stay default.
			var section = Save(assembly, thing);
			Assert.Null(section.TryPopPropsLine("neither"));
			var loaded = (Thing)System.Activator.CreateInstance(thingType, def)!;
			try {
				foreach (var name in new[] { "charge", "inscription" }) {
					var line = section.TryPopPropsLine(name);
					Assert.NotNull(line);
					loaded.LoadLine("runtime-test", line.Line, line.Name.ToLowerInvariant(), line.Value);
				}
				Assert.Equal(7, GetField(loaded, "charge"));
				Assert.Equal("changed", GetField(loaded, "inscription"));
				Assert.Equal(2, GetField(loaded, "neither"));
			} finally {
				loaded.Delete();
			}
		} finally {
			thing.Delete();
		}
	}

	private static void RegisterImplementors(System.Reflection.Assembly assembly) {
		foreach (var implementor in EngineRuntimeHarness.CreateAll<IDeepCopyImplementor>(assembly)) {
			DeepCopyFactory.RegisterImplementor(implementor);
		}
	}

	// Writes the object's section with its generated ISaveImplementor and parses it back.
	private static PropsSection Save(System.Reflection.Assembly assembly, object target) {
		var text = new StringWriter();
		text.WriteLine("[" + GetSaveImplementor(assembly, target).HeaderName + " 0]");
		GetSaveImplementor(assembly, target).Save(target, new SaveStream(text));
		using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())));
		return Assert.Single(PropsFileParser.Load("runtime-test", reader, static _ => false, false).ToList());
	}

	private static ISaveImplementor GetSaveImplementor(System.Reflection.Assembly assembly, object target) {
		return Assert.Single(EngineRuntimeHarness.CreateAll<ISaveImplementor>(assembly),
			candidate => candidate.HandledType == target.GetType());
	}

	private static object SaveAndLoad(System.Reflection.Assembly assembly, object target) {
		var section = Save(assembly, target);
		ObjectSaver.StartingLoading();
		try {
			return GetSaveImplementor(assembly, target).LoadSection(section);
		} finally {
			ObjectSaver.LoadingFinished();
		}
	}

	// The template's own fields are private; the generated code is what the test is about.
	private static object GetField(object target, string name) {
		return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
	}

	private static void SetField(object target, string name, object value) {
		target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
	}
}

// Thing construction reads Globals, which loads steamengine.ini from the current directory once
// per process. A minimal ini prevents first-boot shutdown and keeps log files closed.
// Saving a Thing also needs the engine's simple savers (position, timespan...), which a boot registers.
public sealed class EngineGlobalsFixture : System.IDisposable {
	private readonly string tempDir = Path.Combine(Path.GetTempPath(), "steamengine-generator-tests-" + System.Guid.NewGuid());
	private readonly string originalDirectory = System.Environment.CurrentDirectory;

	public EngineGlobalsFixture() {
		Directory.CreateDirectory(this.tempDir);
		File.WriteAllText(Path.Combine(this.tempDir, "steamengine.ini"), "[files]\nlogToFiles=false\n");
		System.Environment.CurrentDirectory = this.tempDir;
		_ = Globals.ServerName;
		foreach (var assembly in new[] { typeof(Thing).Assembly, typeof(SteamEngine.CompiledScripts.Item).Assembly }) {
			foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && typeof(ISimpleSaveImplementor).IsAssignableFrom(t) && t.GetConstructor(System.Type.EmptyTypes) != null)) {
				ObjectSaver.RegisterSimpleImplementor((ISimpleSaveImplementor)System.Activator.CreateInstance(type)!);
			}
		}
	}

	public void Dispose() {
		System.Environment.CurrentDirectory = this.originalDirectory;
		Directory.Delete(this.tempDir, true);
	}
}

[CollectionDefinition("Engine globals", DisableParallelization = true)]
public sealed class EngineGlobalsCollection : ICollectionFixture<EngineGlobalsFixture> {
	public const string Name = "Engine globals";
}
