using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#962 - the shard's live recipes for items written by hand here, which never pass through ConvertedItemDef:
	/// spell scrolls above all, as the archive has no [ITEMDEF] for them. Written as [CraftRecipeDef]s, read at boot.</summary>
	internal static class ConvertedRecipes {
		private const string ResourcesVar = "nastaveni_cm_resources_", SkillMakeVar = "nastaveni_cm_skillmake_";

		public static void Write(string outputPath) {
			StringBuilder output = new StringBuilder();
			List<string> fromVars = SphereWorldVars.RecipeItems(ResourcesVar, SkillMakeVar);
			foreach (string item in fromVars) {
				if (HandWrittenScripts.Items.Contains(item)) {
					string resources = SphereWorldVars.GetRecipe(ResourcesVar + item);
					string skillMake = SphereWorldVars.GetRecipe(SkillMakeVar + item);
					output.Append("[CraftRecipeDef craftrecipe_").Append(item).Append("]\n");
					output.Append("Item = ").Append(item).Append('\n');
					if (resources != null) {
						output.Append("Resources = ").Append(resources).Append('\n');
					}
					if (skillMake != null) {
						output.Append("SkillMake = ").Append(skillMake).Append('\n');
					}
					output.Append('\n');
				}
			}
			//#1017 - a hand-written item with no recipe in sphereworld keeps its own archive [ITEMDEF]'s, as the converter
			//resolved it: i_scroll_frostbolt, i_scroll_summon_undead, i_customscroll_mass_bless
			HashSet<string> written = new HashSet<string>(fromVars, StringComparer.OrdinalIgnoreCase);
			SortedSet<string> handWritten = new SortedSet<string>(HandWrittenScripts.Items, StringComparer.OrdinalIgnoreCase);
			foreach (string item in handWritten) {
				if (written.Contains(item) || !ConvertedItemDef.itemsByDefname.TryGetValue(item, out ConvertedThingDef def)) {
					continue;
				}
				string resources = def.GetWritten("RESOURCES"), skillMake = def.GetWritten("SKILLMAKE");
				if ((resources == null) && (skillMake == null)) {
					continue;
				}
				output.Append("[CraftRecipeDef craftrecipe_").Append(item).Append("]\n");
				output.Append("Item = ").Append(item).Append('\n');
				if (resources != null) {
					output.Append("Resources = ").Append(resources).Append('\n');
				}
				if (skillMake != null) {
					output.Append("SkillMake = ").Append(skillMake).Append('\n');
				}
				output.Append('\n');
			}
			string target = Tools.CombineMultiplePaths(outputPath, "scripts", ConvertedCraftMenus.MenuFolder, "craftrecipes.def");
			Tools.EnsureDirectory(Path.GetDirectoryName(target));
			File.WriteAllText(target, output.ToString());
		}

	}
}
