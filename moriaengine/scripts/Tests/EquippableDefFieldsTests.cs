using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1384 - EquippableDef's tool type and skill are typed .ct def fields, read from the same .def lines the
	/// hand-written parser read; UseSkill derives the skill as before; none of the old tags left on a def.</summary>
	public static class EquippableDefFieldsTests {
		private static readonly TagKey oldToolTypeTK = TagKey.Acquire("_toolType_");
		private static readonly TagKey oldSkillTK = TagKey.Acquire("_associatedSkill_");

		private static void Check(string name, bool ok) {
			Console.WriteLine("EQUIPDEFFIELDS_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestEquippableDefFields(Character self) {
			EquippableDef pickaxe = (EquippableDef) ThingDef.FindItemDef(0xe85); //"tooltype = Pickaxe" in item_tinker.def
			EquippableDef pole = (EquippableDef) ThingDef.FindItemDef(0xdbf); //"tooltype = FishingPole" in item_nabytek.def
			EquippableDef axe = (EquippableDef) ThingDef.GetByDefname("i_axe_copper");
			EquippableDef dagger = (EquippableDef) ThingDef.GetByDefname("i_dagger_copper");
			EquippableDef test = ThingDef.GetByDefname("i_equip_fields_test") as EquippableDef;
			Check("testDefLoaded", test != null);
			if (test == null) {
				Console.WriteLine("EQUIPDEFFIELDS_DONE");
				return;
			}
			Check("pickaxe", (pickaxe.ToolType == ToolType.Pickaxe) && (pickaxe.AssociatedSkill == SkillName.None) && (pickaxe.UseSkill == SkillName.Mining));
			Check("pole", (pole.ToolType == ToolType.FishingPole) && (pole.UseSkill == SkillName.Fishing));
			Check("axe", (axe.ToolType == ToolType.None) && (axe.UseSkill == SkillName.Lumberjacking));
			Check("dagger", (dagger.ToolType == ToolType.None) && (dagger.UseSkill == SkillName.None));
			Check("explicitSkill", (test.ToolType == ToolType.Pickaxe) && (test.AssociatedSkill == SkillName.Tailoring) && (test.UseSkill == SkillName.Tailoring));

			int defs = 0, oldTags = 0, unreadable = 0;
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if (script is EquippableDef def) {
					defs++;
					try { //an unparseable line resolves lazily and would throw at every use of the item
						if ((def.ToolType < ToolType.None) || (def.AssociatedSkill < SkillName.None) || (def.UseSkill < SkillName.None)) {
							unreadable++;
						}
					} catch (Exception e) {
						unreadable++;
						Console.WriteLine("EQUIPDEFFIELDS_UNREADABLE: " + def.PrettyDefname + " " + e.Message);
					}
					if ((def.GetTag(oldToolTypeTK) != null) || (def.GetTag(oldSkillTK) != null)) {
						oldTags++;
						Console.WriteLine("EQUIPDEFFIELDS_OLDTAG: " + def.PrettyDefname);
					}
				}
			}
			Check("noOldTags", (defs > 1000) && (oldTags == 0));
			Check("everyDefReads", unreadable == 0);

			Console.WriteLine("EQUIPDEFFIELDS_DONE");
		}
	}
}
