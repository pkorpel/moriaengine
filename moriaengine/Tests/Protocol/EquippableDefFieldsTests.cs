using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1384 - EquippableDef's tool type and skill as typed .ct def fields. Server side:
	/// scripts/Tests/EquippableDefFieldsTests.cs.</summary>
	[Collection(NpcDefFieldsCollection.Name)]
	public class EquippableDefFieldsTests : LivingWorldTestBase {
		public EquippableDefFieldsTests(GameServerPairFixture servers)
			: base(servers, "npcdeffields", "EQUIPDEFFIELDS") {
		}

		[Fact]
		public void ToolTypeAndSkillAreTypedDefFields() {
			using (TestGameClient game = this.Login("Nastrojar")) {
				this.Run(game, ".TestEquippableDefFields", "EQUIPDEFFIELDS_DONE");
				this.AssertChecks("testDefLoaded", "pickaxe", "pole", "axe", "dagger", "explicitSkill", "noOldTags", "everyDefReads");
			}
		}
	}
}
