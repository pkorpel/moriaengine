using System;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1375 - NPCDef's single values are typed .ct def fields now, read by FieldValue from the same script lines the
	/// hand-written parser read: one def from the scripts per value, its value from its line, the defaults elsewhere, and none of
	/// the old tags left on a def.</summary>
	public static class NpcDefFieldsTests {
		private static NPCDef Def(string defname) {
			return (NPCDef) ThingDef.GetByDefname(defname);
		}

		private static void Check(string name, bool ok) {
			Console.WriteLine("NPCDEFFIELDS_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestNpcDefFields(Character self) {
			NPCDef guard = Def("c_ocs_guard_selmat"), plain = Def("npc_test_fame_karma"); //sets none of them
			Check("aggroGuards", guard.AggroMode == AggroMode.GuardsRealm);
			Check("aggroHostile", Def("c_sword_dancing").AggroMode == AggroMode.Hostile);
			Check("realm", guard.Realm == Realm.Gondor);
			Check("teaches", Def("mistr_anatomy").TeachesSkill == SkillName.Anatomy);
			Check("moveRate", Def("c_vetesnik").MoveRate == 4.0);
			Check("isBoss", Def("c_ztracene_mesto_necromancer").IsBoss);
			Check("dragonBrain", Def("c_dragon_bone").DragonBrain);
			Check("noAnimate", Def("c_warcamp_gorbag").NoAnimate);
			Check("hypnotizable", Def("c_ratman").Hypnotizable == 19);
			Check("resistSilence", Def("c_golem_crystal").ResistSilence == 350);
			Check("monsterType", Def("c_ratman").MonsterType == "humanoid,60");
			Check("monsterType2", Def("c_liche").MonsterType2 == "humanoid,20");
			Check("magicHorse", Def("C_A_MUSTANG_ROAN_pack").MagicHorse == "roan");
			Check("serpentForm", Def("c_druid_serpent").SerpentForm == "c_druid_had");
			Check("defaultAggro", plain.AggroMode == AggroMode.Calm);
			Check("defaultRealm", plain.Realm == Realm.None);
			Check("defaultTeaches", plain.TeachesSkill == SkillName.None);
			Check("defaultMoveRate", plain.MoveRate == 3.0);
			Check("defaultSpellCastRange", plain.SpellCastRange == -1);
			Check("defaultFlags", !plain.IsBoss && !plain.DragonBrain && !plain.NoAnimate);
			Check("defaultNumbers", (plain.Hypnotizable == 0) && (plain.ResistSilence == 0));
			Check("defaultStrings", (plain.MonsterType == null) && (plain.MagicHorse == null) && (plain.SerpentForm == null));
			bool noTags = true;
			foreach (string old in new[] { "_aggroMode_", "_npcDefRealm_", "_npcDefTeachesSkill_", "_tamingDifficulty_", "_moveRate_",
					"_npcSpellCastRange_", "_npcIsBoss_", "_npcDragonBrain_", "_npcNoAnimate_", "_npcHypnotizable_", "_npcResistSilence_",
					"_npcMonsterType_", "_npcMagicHorse_", "_npcSerpentForm_" }) {
				foreach (NPCDef def in new[] { guard, Def("c_ratman"), Def("mistr_anatomy"), Def("c_vetesnik") }) {
					noTags &= (def.GetTag(TagKey.Acquire(old)) == null);
				}
			}
			Check("noOldTags", noTags);

			//an older save's section: the retired tags are dropped, a saved typed field loads with its type
			NPCDef ratman = Def("c_ratman");
			Type sectionType = typeof(PropsSection);
			PropsSection section = (PropsSection) Activator.CreateInstance(sectionType, BindingFlags.NonPublic | BindingFlags.Instance, null,
				new object[] { "test", "NPCDef", "c_ratman", 0, "" }, null);
			MethodInfo add = sectionType.GetMethod("AddPropsLine", BindingFlags.NonPublic | BindingFlags.Instance);
			add.Invoke(section, new object[] { "tag._npcIsBoss_", "1", 1, "" });
			add.Invoke(section, new object[] { "tag._npcStartingAbilities_", "7", 2, "" });
			add.Invoke(section, new object[] { "hypnotizable", "77", 3, "" });
			ratman.LoadFromSaves(section);
			Check("savedRetiredDropped", (ratman.GetTag(TagKey.Acquire("_npcIsBoss_")) == null) && (ratman.GetTag(TagKey.Acquire("_npcStartingAbilities_")) == null) && !ratman.IsBoss);
			Check("savedTyped", ratman.Hypnotizable == 77);
			ratman.Hypnotizable = 19;
			Console.WriteLine("NPCDEFFIELDS_DONE");
		}
	}
}
