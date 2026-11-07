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

		/// <summary>#1266 - quest items the port keeps as player tags (the user's rule), never as items: a recipe can't ask for one in
		/// the pack, the server checks the tag itself (Q007.MayInscribe for the Inscription book's 8th circle).</summary>
		private static readonly string[] questTagTools = { "i_book_inscription" };

		//the recipe's SKILLMAKE without the quest-tag tools, which go to a comment line above it
		private static string WithoutQuestTags(string skillMake, StringBuilder output) {
			if (skillMake == null) {
				return null;
			}
			List<string> kept = new List<string>();
			foreach (string part in skillMake.Split(',')) {
				string[] words = part.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
				if ((words.Length == 2) && Array.Exists(questTagTools, t => string.Equals(t, words[1], StringComparison.OrdinalIgnoreCase))) {
					output.Append("//").Append(words[1]).Append(" is a quest tag (#1266), the server checks it\n");
				} else {
					kept.Add(part.Trim());
				}
			}
			return (kept.Count == 0) ? null : string.Join(", ", kept);
		}

		public static void Write(string outputPath) {
			StringBuilder output = new StringBuilder();
			List<string> fromVars = SphereWorldVars.RecipeItems(ResourcesVar, SkillMakeVar);
			foreach (string item in fromVars) {
				if (HandWrittenScripts.Items.Contains(item)) {
					string resources = SphereWorldVars.GetRecipe(ResourcesVar + item);
					output.Append("[CraftRecipeDef craftrecipe_").Append(item).Append("]\n");
					string skillMake = WithoutQuestTags(SphereWorldVars.GetRecipe(SkillMakeVar + item), output);
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
				skillMake = WithoutQuestTags(skillMake, output);
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
