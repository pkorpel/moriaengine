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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	//utility class for easy access to armors and weapons as collections by material/type
	public static class ColoredItems {
		private static WeaponDef[] basicWeapons;
		private static WearableDef[][] basicArmors = new WearableDef[Tools.GetEnumLength<Material>()][];
		private static ProjectileDef[] basicProjectiles;

		private static Dictionary<int, ItemDef>[] coloredItems = new Dictionary<int, ItemDef>[Tools.GetEnumLength<Material>()];
		private static Dictionary<int, ColoredArmorDef[]>[] coloredArmors = new Dictionary<int, ColoredArmorDef[]>[Tools.GetEnumLength<Material>()];

		private static Material[] materials = InitMaterials();

		private static Material[] InitMaterials() {
			int n = Tools.GetEnumLength<Material>();
			Material[] retVal = new Material[n];
			for (int i = 0; i < n; i++) {
				retVal[i] = (Material) i;
			}
			return retVal;
		}

		/// <summary>Returns a collection of basic itemdefs for all colored weapons</summary>
		[SteamFunction]
		public static ICollection<WeaponDef> GetBasicWeapons() {
			if (basicWeapons == null) {
				basicWeapons = InitBasicItems<WeaponDef, ColoredWeaponDef>();
			}

			return basicWeapons;
		}

		/// <summary>Returns a collection of basic itemdefs for all colored armors</summary>
		[SteamFunction]
		public static ICollection<ProjectileDef> GetBasicProjectiles() {
			if (basicProjectiles == null) {
				basicProjectiles = InitBasicItems<ProjectileDef, ColoredProjectileDef>();
			}

			return basicProjectiles;
		}

		private static BaseType[] InitBasicItems<BaseType, ColoredType>()
			where BaseType : ItemDef
			where ColoredType : ItemDef, BaseType, IObjectWithMaterial {

			Dictionary<int, BaseType> dict = new Dictionary<int, BaseType>();
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				ColoredType def = script as ColoredType;
				if (def != null) {
					BaseType baseDef = (BaseType) ThingDef.FindItemDef(def.Model);
					if (baseDef == null) {
						throw new SEException("No baseDef for '" + def + "' ?!");
					}
					dict[def.Model] = baseDef;
				}
			}
			int n = dict.Count;
			BaseType[] retVal = new BaseType[n];
			dict.Values.CopyTo(retVal, 0);
			return retVal;
		}

		/// <summary>Returns a collection of basic itemdefs for all colored armors of given type</summary>
		public static ICollection<WearableDef> GetBasicArmors(WearableType type) {
			WearableDef[] arrOfType = basicArmors[(int) type];

			if (arrOfType == null) {
				Dictionary<int, WearableDef> dict = new Dictionary<int, WearableDef>();
				foreach (AbstractScript script in AbstractScript.AllScripts) {
					ColoredArmorDef def = script as ColoredArmorDef;
					if ((def != null) && (def.WearableType == type)) {
						WearableDef baseDef = (WearableDef) ThingDef.FindItemDef(def.Model);
						if (baseDef == null) {
							throw new SEException("No baseDef for '" + def + "' ?!");
						}
						dict[def.Model] = baseDef;
					}
				}
				int n = dict.Count;
				arrOfType = new WearableDef[n];
				dict.Values.CopyTo(arrOfType, 0);
				basicArmors[(int) type] = arrOfType;
			}

			return arrOfType;
		}

		[SteamFunction]
		public static ICollection<Material> GetAllMaterials() {
			return materials;
		}

		public static ColoredWeaponDef GetColoredWeapon(int model, Material material) {
			return GetColoredObject<ColoredWeaponDef>(model, material);
		}

		public static ColoredProjectileDef GetColoredProjectile(int model, Material material) {
			return GetColoredObject<ColoredProjectileDef>(model, material);
		}

		private static T GetColoredObject<T>(int model, Material material) where T : ItemDef, IObjectWithMaterial {
			Dictionary<int, ItemDef> dict = coloredItems[(int) material];
			if (dict == null) {
				dict = new Dictionary<int, ItemDef>();
				coloredItems[(int) material] = dict;
			}

			ItemDef retVal;
			if (!dict.TryGetValue(model, out retVal)) {
				foreach (AbstractScript script in AbstractScript.AllScripts) {
					T def = script as T;
					if ((def != null) && (def.Model == model) && (def.Material == material)) {
						//if (retVal == null) {
						retVal = def;
						break;
						//} else {
						//	throw new Exception("'" + def + "' and '" + retVal + "' have the same material and model - that is probably wrong.");
						//commented because some items can perhaps share the same model. Like bolts + jagged bolts? or some such
						//}
					}
				}
			}

			return (T) retVal;
		}


		public static ColoredArmorDef GetColoredArmor(int model, Material material, WearableType type) {
			Dictionary<int, ColoredArmorDef[]> dict = coloredArmors[(int) material];
			if (dict == null) {
				dict = new Dictionary<int, ColoredArmorDef[]>();
				coloredArmors[(int) material] = dict;
			}

			ColoredArmorDef[] retVal;
			if (!dict.TryGetValue(model, out retVal)) {
				retVal = new ColoredArmorDef[Tools.GetEnumLength<WearableType>()]; //most will be empty but we don't care. Probably only ring/chain stuff will have 2 entries cos they share the same models
				foreach (AbstractScript script in AbstractScript.AllScripts) {
					ColoredArmorDef def = script as ColoredArmorDef;
					if ((def != null) && (def.Model == model) && (def.Material == material)) {
						if (retVal[(int) def.WearableType] == null) {
							retVal[(int) def.WearableType] = def;
						} else {
							throw new Exception("'" + def + "' and '" + retVal[(int) def.WearableType] + "' have the same material, model and WearableType - that is probably wrong.");
						}
					}
				}
			}

			return retVal[(int) type];
		}

		#region SteamFunctions
		[SteamFunction]
		public static ICollection<WearableDef> GetBasicArmors(object ignoredSelf, WearableType type) {
			return GetBasicArmors(type);
		}

		[SteamFunction]
		public static ColoredArmorDef GetColoredArmor(object ignoredSelf, int model, Material material, WearableType type) {
			return GetColoredArmor(model, material, type);
		}

		[SteamFunction]
		public static ColoredWeaponDef GetColoredWeapon(object ignoredSelf, int model, Material material) {
			return GetColoredWeapon(model, material);
		}

		[SteamFunction]
		public static ColoredProjectileDef GetColoredProjectile(object ignoredSelf, int model, Material material) {
			return GetColoredProjectile(model, material);
		}

		#endregion SteamFunctions
	}
}