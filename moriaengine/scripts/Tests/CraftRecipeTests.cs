using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - server-side helpers for Tests/Protocol/CraftRecipeTests.cs.</summary>
	public static class CraftRecipeTests {
		/// <summary>Every item's recipe through the real parser, which resolves names only when asked.</summary>
		[SteamFunction]
		public static void TestRecipesParse(Character self) {
			int recipes = 0, broken = 0;
			string names = "";
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				ItemDef def = script as ItemDef;
				if (def != null) {
					try {
						if ((def.Resources != null) || (def.SkillMake != null)) {
							recipes++;
						}
					} catch (Exception e) when (!(e is FatalException)) {
						broken++;
						if (broken <= 8) {
							names += def.PrettyDefname + ":" + e.Message + " | ";
						}
					}
				}
			}
			Console.WriteLine("RECIPES: recipes=" + recipes + " broken=" + broken + " " + names);
		}

		[SteamFunction]
		public static void TestRecipeOf(Character self, ScriptArgs sa) {
			ItemDef def = (ItemDef) ThingDef.GetByDefname(sa.Args.Trim('"'));
			Console.WriteLine("RECIPE_OF: " + def.PrettyDefname + " resources=" + def.Resources + " skillmake=" + def.SkillMake);
		}
	}
}
