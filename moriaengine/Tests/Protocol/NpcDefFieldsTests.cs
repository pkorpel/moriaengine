using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1375 - NPCDef's single values as typed .ct def fields, read from the same script lines as before. Server side:
	/// scripts/Tests/NpcDefFieldsTests.cs.</summary>
	[Collection(NpcDefFieldsCollection.Name)]
	public class NpcDefFieldsTests : LivingWorldTestBase {
		public NpcDefFieldsTests(GameServerPairFixture servers)
			: base(servers, "npcdeffields", "NPCDEFFIELDS") {
		}

		[Fact]
		public void TheScriptsValuesAreTypedDefFields() {
			using (TestGameClient game = this.Login("Zkoumal")) {
				this.Run(game, ".TestNpcDefFields", "NPCDEFFIELDS_DONE");
				this.AssertChecks("aggroGuards", "aggroHostile", "realm", "teaches", "moveRate", "isBoss", "dragonBrain", "noAnimate",
					"hypnotizable", "resistSilence", "monsterType", "monsterType2", "magicHorse", "serpentForm", "defaultAggro", "defaultRealm",
					"defaultTeaches", "defaultMoveRate", "defaultSpellCastRange", "defaultFlags", "defaultNumbers", "defaultStrings", "noOldTags",
					"savedRetiredDropped", "savedTyped");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class NpcDefFieldsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcDefFields";
	}
}
