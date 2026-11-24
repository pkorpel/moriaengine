using SteamEngine.Generators.DeepCopyImplementors;
using Xunit;

namespace SteamEngine.Generators.Tests;

// Copies sandbox objects through the real DeepCopyFactory with the generated implementors
// registered. DeepCopyImplementorGeneratorTests covers diagnostics and generated text on stubs.
[Collection(EngineGlobalsCollection.Name)]
public sealed class DeepCopyImplementorGeneratorRuntimeTests {
	[Fact]
	public void GetCopy_CopiesMarkedMembersAndKeepsSharedReferencesShared() {
		const string source = """
using SteamEngine;

namespace DeepCopySandbox;

[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	public int Charges;

	[CopyableData]
	public string Name { get; set; }

	[CopyableData]
	public Rune First;

	[CopyableData]
	public Rune Second;

	public int NotCopied;

	public bool MadeByCopyConstructor;

	public SpellState() {
	}

	[DeepCopyImplementation]
	public SpellState(SpellState copyFrom) {
		MadeByCopyConstructor = true;
	}
}

[DeepCopyableClass]
public sealed class Rune {
	[CopyableData]
	public int Power;

	public Rune() {
	}

	[DeepCopyImplementation]
	public Rune(Rune copyFrom) {
	}
}
""";

		var assembly = EngineRuntimeHarness.Compile(new DeepCopyImplementorsGenerator(), source);
		// The generated implementors register the sandbox types with the real copy factory.
		foreach (var implementor in EngineRuntimeHarness.CreateAll<IDeepCopyImplementor>(assembly)) {
			DeepCopyFactory.RegisterImplementor(implementor);
		}

		// Two fields point to the same nested object, so the copy must preserve that alias.
		dynamic rune = EngineRuntimeHarness.Create(assembly, "DeepCopySandbox.Rune");
		rune.Power = 9;
		dynamic original = EngineRuntimeHarness.Create(assembly, "DeepCopySandbox.SpellState");
		original.Charges = 3;
		original.Name = "Fireball";
		original.First = rune;
		original.Second = rune;
		original.NotCopied = 5;

		dynamic copy = DeepCopyFactory.GetCopy((object)original);
		// The copy is a new object; its custom copy constructor and marked fields are honored.
		Assert.NotSame((object)original, (object)copy);
		Assert.True((bool)copy.MadeByCopyConstructor);
		Assert.Equal(3, (int)copy.Charges);
		Assert.Equal("Fireball", (string)copy.Name);
		// Unmarked data is not copied, and repeated references still point to one copied Rune.
		Assert.Equal(0, (int)copy.NotCopied);
		Assert.NotSame((object)rune, (object)copy.First);
		Assert.Same((object)copy.First, (object)copy.Second);
		Assert.Equal(9, (int)copy.First.Power);
	}
}
