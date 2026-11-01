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
			foreach (string item in SphereWorldVars.RecipeItems(ResourcesVar, SkillMakeVar)) {
				if (HandWrittenScripts.Items.Contains(item) && !ConvertedItemDef.itemsByDefname.ContainsKey(item)) {
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
			string target = Tools.CombineMultiplePaths(outputPath, "scripts", ConvertedCraftMenus.MenuFolder, "craftrecipes.def");
			Tools.EnsureDirectory(Path.GetDirectoryName(target));
			File.WriteAllText(target, output.ToString());
		}
	}
}
