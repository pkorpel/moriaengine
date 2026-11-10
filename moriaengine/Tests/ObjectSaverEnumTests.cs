using SteamEngine.Persistence;
using Xunit;

namespace SteamEngine.Tests {
	public enum ObjectSaverTestEnum {
		Alpha,
		Beta,
		Gamma,
	}

	public enum ObjectSaverTestBigEnum : ulong {
		Max = ulong.MaxValue,
	}

	public enum ObjectSaverTestSignedEnum : short {
		MinusOne = -1,
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
		public void TheCachedNameGivesTheSameTextEveryTimeAndPerType() { //#1360
			string first = ObjectSaver.Save(ObjectSaverTestEnum.Gamma);
			Assert.Equal(first, ObjectSaver.Save(ObjectSaverTestEnum.Gamma));
			Assert.Equal(first.Replace("2", "1"), ObjectSaver.Save(ObjectSaverTestEnum.Beta));
			Assert.Equal("(Enum:" + typeof(ObjectSaverTestBigEnum).FullName + ")18446744073709551615", ObjectSaver.Save(ObjectSaverTestBigEnum.Max));
			Assert.Equal("(Enum:" + typeof(ObjectSaverTestSignedEnum).FullName + ")-1", ObjectSaver.Save(ObjectSaverTestSignedEnum.MinusOne));
			Assert.Equal(ObjectSaverTestBigEnum.Max, ObjectSaver.Load(ObjectSaver.Save(ObjectSaverTestBigEnum.Max)));
			Assert.Equal(ObjectSaverTestSignedEnum.MinusOne, ObjectSaver.Load(ObjectSaver.Save(ObjectSaverTestSignedEnum.MinusOne)));
		}

		[Fact]
		public void ZeroValuedEnumRoundTrips() {
			string saved = ObjectSaver.Save(ObjectSaverTestEnum.Alpha);
			object loaded = ObjectSaver.Load(saved);
			Assert.Equal(ObjectSaverTestEnum.Alpha, loaded);
		}
	}
}
