using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	[HasSavedMembers]
	public static class HuesCalculator {

		static Dictionary<Material, Constant> woodHues = new Dictionary<Material, Constant>();
		static Dictionary<Material, Constant> metalHues = new Dictionary<Material, Constant>();
		static Dictionary<Material, Constant> oreHues = new Dictionary<Material, Constant>();


		private static bool defsWithMaterialColored;
		[SavedMember]
		public static bool DefsWithMaterialColored {
			get {
				return defsWithMaterialColored;
			}
			set {
				if (!value) {
					SetColorOnDefsWithMaterial();
				}
				defsWithMaterialColored = true;
			}
		}

		//pusti se automaticky po druhym loadu novyho sveta
		public static void SetColorOnDefsWithMaterial() {
			foreach (AbstractScript scp in AbstractScript.AllScripts) {
				IObjectWithMaterial materialObj = scp as IObjectWithMaterial;
				if (materialObj != null) {
					materialObj.Color = GetHueForMaterial(materialObj.Material, materialObj.MaterialType);
				}
			}
		}

		public static ushort GetHueForMaterial(Material material, MaterialType type) {
			if (material == Material.None) {
				return 0;
			}
			if (type == MaterialType.None) {
				return 0;
			}

			Constant hue;

			switch (type) {
				case MaterialType.Ore:
					if (!oreHues.TryGetValue(material, out hue)) {
						if (material == Material.Sand) {
							hue = Constant.GetByName("color_o_pisek");
						} else {
							hue = GetConstantForMetal(material);
						}
						if (hue != null) {
							oreHues[material] = hue;
						}
					}
					break;
				case MaterialType.Metal:
					if (!metalHues.TryGetValue(material, out hue)) {
						hue = GetConstantForMetal(material);
						if (hue != null) {
							metalHues[material] = hue;
						}
					}
					break;
				case MaterialType.Wood:
					if (!woodHues.TryGetValue(material, out hue)) {
						hue = GetConstantForWood(material);
						if (hue != null) {
							woodHues[material] = hue;
						}
					}
					break;
				default:
					hue = null;
					break;
			}

			if (hue != null) {
				return Convert.ToUInt16(hue.Value);
			}
			throw new SEException("Can't find the hue for Material." + material + " of MaterialType." + type);
		}

		private static Constant GetConstantForWood(Material wood) {
			string constantName;
			switch (wood) {
				case Material.Spruce:
					constantName = "color_spruce";
					break;
				case Material.Chestnut:
					constantName = "color_chestnut";
					break;
				case Material.Oak:
					constantName = "color_oak";
					break;
				case Material.Teak:
					constantName = "color_teak";
					break;
				case Material.Mahagon:
					constantName = "color_mahagon";
					break;
				case Material.Eben:
					constantName = "color_eben";
					break;
				case Material.Elven:
					constantName = "color_elven";
					break;
				default:
					throw new SEException("Can't find the hue for wooden Material." + wood + ".");
			}
			return Constant.GetByName(constantName);
		}

		private static Constant GetConstantForMetal(Material metal) {
			string constantName;
			switch (metal) {
				case Material.Copper:
					constantName = "color_o_copper";
					break;
				case Material.Iron:
					constantName = "color_o_iron";
					break;
				case Material.Silver:
					constantName = "color_o_silver";
					break;
				case Material.Gold:
					constantName = "color_o_gold";
					break;
				case Material.Verite:
					constantName = "color_o_verite";
					break;
				case Material.Valorite:
					constantName = "color_o_valorite";
					break;
				case Material.Obsidian:
					constantName = "color_o_obsidian";
					break;
				case Material.Adamantinum:
					constantName = "color_o_adamantinum";
					break;
				case Material.Mithril:
					constantName = "color_o_mithril";
					break;
				default:
					throw new SEException("Can't find the hue for metallic Material." + metal + ".");
			}
			return Constant.GetByName(constantName);
		}
	}
}