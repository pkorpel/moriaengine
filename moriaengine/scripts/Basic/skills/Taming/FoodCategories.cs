/*
	This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Maps the real archive's FOODTYPE food-category tokens ("t_meat_raw", "t_fruit", etc)
	/// onto real moriaengine itemdefs (#593) - none of these categories exist as actual
	/// content/TemplateDefs in this engine, so matching a held item against one means
	/// resolving a hand-authored list of real items that plausibly belong to it. Only the
	/// categories actually seen driving common tameable-animal diets (meat/fruit/crops/
	/// grain/grass/fish) are mapped - rarer ones (t_coin/t_ore/t_ingot/t_arock/t_gem/t_spell/
	/// t_water_wash, each only a handful of exotic creatures in the real archive) are left
	/// unmapped deliberately, not silently invented - an NPCDef configured with one of those
	/// just can never be fed via that specific entry until a future pass adds it.
	/// </summary>
	public static class FoodCategories {
		private static readonly Dictionary<string, string[]> categoryDefnames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
			{ "t_meat_raw", new[] { "i_ribs_raw" } },
			{ "t_fruit", new[] { "i_fruit_apple", "i_fruit_carrot" } },
			{ "t_crops", new[] { "i_crop_wheat", "i_crop_wheat3", "i_crop_wheat4", "i_crop_carrot", "i_crop_hay", "i_crop_hay1", "i_crop_hay2" } },
			{ "t_grain", new[] { "i_crop_wheat", "i_crop_wheat3", "i_crop_wheat4" } },
			{ "t_grass", new[] { "i_plant_grass", "i_plant_grass_2", "i_plant_pampasgrass", "i_plant_pampasgrass_2", "i_plant_grasses", "i_hay", "i_hay_scattered", "i_hay_sheaf", "i_hay_bits_scattered_small" } },
			{ "t_fish", new[] { "i_fish_raw_whole", "i_fish_raw", "i_fish_raw_head", "i_fish_raw_head_pile" } },
		};

		//Resolved lazily (ThingDef.GetByDefname needs content already loaded, which isn't
		//true yet at this static initializer's own run time) and cached per category -
		//mirrors the established "resolve via GetByDefname, compare by reference" precedent
		//used elsewhere in this codebase (e.g. Mage's summon-defname checks), rather than
		//comparing raw Defname strings (ThingDef.Defname is the hex header id, not the
		//readable alias - a repeat footgun already documented elsewhere in this repo).
		private static readonly Dictionary<string, ItemDef[]> resolvedCache = new Dictionary<string, ItemDef[]>(StringComparer.OrdinalIgnoreCase);

		private static ItemDef[] ResolveCategory(string category) {
			ItemDef[] cached;
			if (resolvedCache.TryGetValue(category, out cached)) {
				return cached;
			}
			string[] defnames;
			ItemDef[] result;
			if (categoryDefnames.TryGetValue(category, out defnames)) {
				List<ItemDef> resolved = new List<ItemDef>();
				foreach (string defname in defnames) {
					ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
					if (def != null) {
						resolved.Add(def);
					}
				}
				result = resolved.ToArray();
			} else {
				result = Array.Empty<ItemDef>();
			}
			resolvedCache[category] = result;
			return result;
		}

		/// <summary>True if `candidate` satisfies `categoryOrDefname` - either a real "t_"
		/// food category (resolved against the mapping above) or a literal itemdef
		/// (e.g. dragons' real "FOODTYPE=8000 i_gold").</summary>
		public static bool Matches(ItemDef candidate, string categoryOrDefname) {
			if ((candidate == null) || string.IsNullOrEmpty(categoryOrDefname)) {
				return false;
			}
			if (categoryOrDefname.StartsWith("t_", StringComparison.OrdinalIgnoreCase)) {
				foreach (ItemDef mapped in ResolveCategory(categoryOrDefname)) {
					if (mapped == candidate) {
						return true;
					}
				}
				return false;
			}
			return (ThingDef.GetByDefname(categoryOrDefname) as ItemDef) == candidate;
		}
	}
}
