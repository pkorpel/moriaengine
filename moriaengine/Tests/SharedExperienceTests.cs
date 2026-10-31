using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#902 - Erebor's split of a monster's experience, in its own integer steps.</summary>
	public class SharedExperienceTests {
		[Theory]
		[InlineData(1, 1000)]
		[InlineData(2, 705)]
		[InlineData(3, 576)]
		[InlineData(4, 500)]
		[InlineData(5, 446)]
		public void EachFighterGetsLessAndTheGroupMore(int fighters, int each) {
			Assert.Equal(each, LevelingSystem.MonsterShare(1000, fighters));
		}
	}
}
