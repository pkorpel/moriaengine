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
					"everyDefReads", "readAllQuickly", "savedTyped",
					"rangeTwo", "rangeSingle", "rangeExperience", "rangeMaxHits", "rangeTable", "rangeByName", "rangeDefault",
					"combatSpell", "mountCharDef", "defRefDefaults");
			}
		}

		/// <summary>#1405 - every NPCDef's add-up lists (spells, summons, loot, equipment, resources, food, skill and resist
		/// ranges, movement) read exactly as the def tags did before they became typed fields: the dump is compared with
		/// NpcDefLists.txt, made from the code before #1405. A missing file is written and the test fails, so a new
		/// baseline is a deliberate step. Server side: scripts/Tests/NpcDefListsTests.cs.</summary>
		[Fact]
		public void TheListsReadAsTheyDid() {
			string baseline = System.IO.Path.Combine(this.fixture.RepoRoot, "Tests", "Protocol", "Data", "NpcDefLists.txt");
			string dump = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "npcdeflists-" + System.Guid.NewGuid().ToString("N") + ".txt");
			using (TestGameClient game = this.Login("Seznamovy")) {
				this.Run(game, ".TestDumpNpcDefLists \"" + dump + "\"", "NPCDEFLISTS_DUMPED");
			}
			string now = System.IO.File.ReadAllText(dump);
			System.IO.File.Delete(dump);
			if (!System.IO.File.Exists(baseline)) {
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(baseline));
				System.IO.File.WriteAllText(baseline, now);
				Assert.Fail("No baseline: written " + baseline + " (" + now.Length + " chars).");
			}
			string[] was = System.IO.File.ReadAllText(baseline).Split('\n');
			string[] got = now.Split('\n');
			for (int i = 0; i < System.Math.Max(was.Length, got.Length); i++) {
				string a = (i < was.Length) ? was[i] : "<end>", b = (i < got.Length) ? got[i] : "<end>";
				Assert.True(a == b, "Line " + (i + 1) + " differs:\n was: " + a + "\n now: " + b);
			}
		}
	}

	[CollectionDefinition(Name)]
	public class NpcDefFieldsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcDefFields";
	}
}
