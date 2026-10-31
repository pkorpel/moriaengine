using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#937 - what a clean boot must not have cost: defs that shared a model number or were a dupe with a
	/// name of their own, and the engine's own flag values over Sphere's.</summary>
	public static class BootLogTests {
		private static readonly string[] Defs = {
			"c_orc", "c_orc_nb", "c_vrk", "c_alligator",
			"i_door_sliding", "i_crop_sprouts3", "i_fruit_lettuce", "i_sign_post_new", "i_wooden_sign_post",
			"i_cleric_surcoat", "i_clerikova_stuha", "i_rock_3", "i_pet_horse", "i_multi_tower", "i_lemon_halved",
			"i_qv1_zakladzdi",
		};

		[SteamFunction]
		public static void TestBootDefs(Character self) {
			string missing = "";
			foreach (string defname in Defs) {
				if (ThingDef.GetByDefname(defname) == null) {
					missing += " " + defname;
				}
			}
			Console.WriteLine("BOOT_DEFS: missing=[" + missing.Trim() + "] attrStatic=" + Constant.GetValue("attr_static")
				+ " statfWar=" + Constant.GetValue("statf_war") + " sewingKit=" + (TemplateDef.GetByDefname("VENDOR_S_TAILOR") != null));
		}
	}
}
