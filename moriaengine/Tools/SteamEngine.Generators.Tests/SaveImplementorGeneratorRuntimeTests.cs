using System.IO;
using System.Linq;
using System.Text;
using SteamEngine.Generators.SaveImplementors;
using SteamEngine.Persistence;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Saves a sandbox object with its generated ISaveImplementor and loads it back from the written
// section. SaveImplementorGeneratorTests covers diagnostics and generated text on stubs.
[Collection(EngineGlobalsCollection.Name)]
public sealed class SaveImplementorGeneratorRuntimeTests {
	[Fact]
	public void SaveAndLoadSection_RoundTripTheMarkedMembers() {
		const string source = """
using System;
using SteamEngine;
using SteamEngine.Persistence;

namespace SaveSandbox;

public enum Mood { Calm, Angry }

public class ArenaStateBase {
	public bool Finished;

	[LoadingFinalizer]
	public void FinishLoading() {
		Finished = true;
	}
}

[SaveableClass]
public sealed class ArenaState : ArenaStateBase {
	[SaveableData]
	public int Counter;

	[SaveableData]
	public string Title { get; set; }

	[SaveableData]
	public Mood Mood;

	[SaveableData]
	public object Payload { get; set; }

	public string Extra;

	public int NotSaved;

	[LoadingInitializer]
	public ArenaState() {
	}

	[LoadLine]
	public void LoadLine(string filename, int line, string name, string value) {
		if (string.Equals(name, "extra", StringComparison.OrdinalIgnoreCase)) {
			Extra = (string) ObjectSaver.Load(value);
		}
	}

	[Save]
	public void Save(SaveStream output) {
		output.WriteValue("extra", Extra);
	}
}
""";

		var assembly = EngineRuntimeHarness.Compile(new SaveImplementorsGenerator(), source);
		var implementor = Assert.Single(EngineRuntimeHarness.CreateAll<ISaveImplementor>(assembly));
		// The generated implementor handles the class under its expected save-section header.
		Assert.Equal("ArenaState", implementor.HeaderName);

		// Populate marked members, an unmarked member, and a custom Save/LoadLine member.
		dynamic original = EngineRuntimeHarness.Create(assembly, "SaveSandbox.ArenaState");
		original.Counter = 4;
		original.Title = "Arena \"one\"";
		original.Mood = (dynamic)System.Enum.ToObject(assembly.GetType("SaveSandbox.Mood")!, 1);
		original.Payload = 12;
		original.Extra = "bonus";
		original.NotSaved = 5;

		var text = new StringWriter();
		text.WriteLine("[" + implementor.HeaderName + " 0]");
		implementor.Save((object)original, new SaveStream(text));

		using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())));
		var section = Assert.Single(PropsFileParser.Load("runtime-test", reader, static _ => false, false).ToList());
		dynamic loaded = implementor.LoadSection(section);

		// Marked primitive, string, enum and object members round-trip through the generated saver.
		Assert.Equal(4, (int)loaded.Counter);
		Assert.Equal("Arena \"one\"", (string)loaded.Title);
		Assert.Equal("Angry", loaded.Mood.ToString());
		Assert.Equal(12, (object)loaded.Payload);
		Assert.Equal("bonus", (string)loaded.Extra);
		// Unmarked members stay at defaults, while the inherited loading finalizer runs.
		Assert.Equal(0, (int)loaded.NotSaved);
		Assert.True((bool)loaded.Finished);
	}
}
