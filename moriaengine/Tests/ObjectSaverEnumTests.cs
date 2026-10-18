using SteamEngine.Persistence;
using Xunit;

namespace SteamEngine.Tests {
	public enum ObjectSaverTestEnum {
		Alpha,
		Beta,
		Gamma,
	}

	/// <summary>
	/// Pure logic test for #407 - ObjectSaver.Save/Load's enum round-trip. No engine boot
	/// needed. Found while investigating why Player.Race/Nation and Character.Realm all
	/// showed "None" after a real save/reload: ObjectSaver.Save saved any enum as a bare,
	/// type-erased number ("1"), and on load ConvertTools.TryParseAnyNumber always resolved
	/// it back to a plain int, never the original enum - every "tag is SomeEnum" getter
	/// pattern then silently fell back to its default value. Save now embeds the enum's own
	/// type name so Load can resolve it back exactly.
	/// </summary>
	public class ObjectSaverEnumTests {
		[Fact]
		public void EnumValueRoundTripsThroughSaveAndLoad() {
			string saved = ObjectSaver.Save(ObjectSaverTestEnum.Beta);
			object loaded = ObjectSaver.Load(saved);
			Assert.IsType<ObjectSaverTestEnum>(loaded);
			Assert.Equal(ObjectSaverTestEnum.Beta, loaded);
		}

		[Fact]
		public void SavedFormatEmbedsTheEnumTypeName() {
			string saved = ObjectSaver.Save(ObjectSaverTestEnum.Gamma);
			Assert.Contains("ObjectSaverTestEnum", saved);
			Assert.DoesNotContain("(ObjectSaverTestEnum)", saved); // not the reference-coordinator shape
		}

		[Fact]
		public void ZeroValuedEnumRoundTrips() {
			string saved = ObjectSaver.Save(ObjectSaverTestEnum.Alpha);
			object loaded = ObjectSaver.Load(saved);
			Assert.Equal(ObjectSaverTestEnum.Alpha, loaded);
		}
	}
}
