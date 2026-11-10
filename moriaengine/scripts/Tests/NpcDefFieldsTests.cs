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

		private static bool Same(int[] range, int min, int max) {
			return (range != null) && (range.Length == 2) && (range[0] == min) && (range[1] == max);
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
			//#1377 - the ranges: "min max", a single value for both ends, a renamed keyword, a field read by its name
			Check("rangeTwo", Same(Def("c_man_gm").StrRange, 130, 150));
			Check("rangeSingle", Same(Def("c_ratman_mace").StunChance, 200, 200));
			Check("rangeExperience", Same(Def("c_namestek_valky_g").ExperienceWorth, 40, 60));
			Check("rangeMaxHits", Same(Def("c_ratman").MaxHitsRange, 340, 390));
			Check("rangeTable", Same(Def("c_spirit_adamantinum").PhoenixFireDamage, 50, 65));
			Check("rangeByName", Same(Def("c_drak_hnedy").AbilityTuning("fireDragonChance"), 800, 800));
			Check("rangeDefault", (plain.StrRange == null) && (plain.StunChance == null) && (plain.AbilityTuning("fireDragonChance") == null));

			bool noTags = true;
			foreach (string old in new[] { "_aggroMode_", "_npcDefRealm_", "_npcDefTeachesSkill_", "_tamingDifficulty_", "_moveRate_",
					"_npcSpellCastRange_", "_npcIsBoss_", "_npcDragonBrain_", "_npcNoAnimate_", "_npcHypnotizable_", "_npcResistSilence_",
					"_npcMonsterType_", "_npcMagicHorse_", "_npcSerpentForm_", "_experienceWorth_", "_strRange_", "_npcStunChance_", "_npcFireDragonChance_" }) {
				foreach (NPCDef def in new[] { guard, Def("c_ratman"), Def("mistr_anatomy"), Def("c_vetesnik"), Def("c_drak_hnedy"), Def("c_man_gm") }) {
					noTags &= (def.GetTag(TagKey.Acquire(old)) == null);
				}
			}
			Check("noOldTags", noTags);

			//every NPCDef's 15 values read: a script value the field can't parse throws on read
			int defs = 0, unreadable = 0;
			System.Diagnostics.Stopwatch readAll = System.Diagnostics.Stopwatch.StartNew();
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				NPCDef def = script as NPCDef;
				if (def == null) {
					continue;
				}
				defs++;
				try {
					object[] values = { def.AggroMode, def.Realm, def.TeachesSkill, def.MoveRate, def.SpellCastRange, def.IsBoss, def.DragonBrain,
						def.NoAnimate, def.Hypnotizable, def.ResistSilence, def.MonsterType, def.MonsterType2, def.MonsterType3, def.MagicHorse, def.SerpentForm };
					if (values.Length != 15) {
						unreadable++;
					}
					foreach (PropertyInfo range in typeof(NPCDef).GetProperties()) {
						if (range.PropertyType == typeof(int[])) {
							int[] value = (int[]) range.GetValue(def);
							if ((value != null) && (value.Length != 2)) {
								unreadable++;
								Console.WriteLine("NPCDEFFIELDS_UNREADABLE: " + def.PrettyDefname + " " + range.Name + " has " + value.Length + " numbers");
							}
						}
					}
				} catch (Exception e) {
					unreadable++;
					Console.WriteLine("NPCDEFFIELDS_UNREADABLE: " + def.PrettyDefname + " " + e.Message);
				}
			}
			Console.WriteLine("NPCDEFFIELDS_READALL: defs=" + defs + " ms=" + readAll.ElapsedMilliseconds);
			Check("everyDefReads", (defs > 1000) && (unreadable == 0));
			//#1377 - an array field's elements resolved through LScript took minutes here; the number shortcut takes well under a second
			Check("readAllQuickly", readAll.ElapsedMilliseconds < 15000);

			//an older save's section: the retired tags are dropped, a saved typed field loads with its type
			NPCDef ratman = Def("c_ratman");
			int hypnotizableBefore = ratman.Hypnotizable;
			Type sectionType = typeof(PropsSection);
			PropsSection section = (PropsSection) Activator.CreateInstance(sectionType, BindingFlags.NonPublic | BindingFlags.Instance, null,
				new object[] { "test", "NPCDef", "c_ratman", 0, "" }, null);
			MethodInfo add = sectionType.GetMethod("AddPropsLine", BindingFlags.NonPublic | BindingFlags.Instance);
			add.Invoke(section, new object[] { "tag._npcIsBoss_", "1", 1, "" });
			add.Invoke(section, new object[] { "tag._npcStartingAbilities_", "7", 2, "" });
			add.Invoke(section, new object[] { "hypnotizable", "77", 3, "" });
			ratman.LoadFromSaves(section);
			Check("savedRetiredDropped", (ratman.GetTag(TagKey.Acquire("_npcIsBoss_")) == null) && (ratman.GetTag(TagKey.Acquire("_npcStartingAbilities_")) == null));
			Check("savedTyped", ratman.Hypnotizable == 77);
			ratman.Hypnotizable = hypnotizableBefore;
			Console.WriteLine("NPCDEFFIELDS_DONE");
		}
	}
}
