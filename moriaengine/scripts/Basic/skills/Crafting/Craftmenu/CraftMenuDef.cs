using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - what a craft menu holds: Erebor's cmprep_* functions, run by the converter into craftmenus.def.</summary>
	public class CraftMenuDef : AbstractDef {
		private const int SpellsInCircle = 8;
		private readonly System.Collections.Generic.List<string[]> entries = new System.Collections.Generic.List<string[]>();

		//#966 - systems of their own: making one would take its materials and give nothing that works
		private static readonly System.Collections.Generic.HashSet<string> notPorted = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"i_scroll_case", "i_customscroll_manashield"
		};

		public CraftMenuDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static CraftMenuDef ForMenu(string menuName) {
			return AbstractScript.GetByDefname("craftmenu_" + menuName) as CraftMenuDef;
		}

		public override void LoadScriptLines(PropsSection ps) {
			this.entries.Clear();
			foreach (PropsLine line in ps.PropsLines) {
				if (StringComparer.OrdinalIgnoreCase.Equals(line.Name, "entry")) {
					string[] parts = line.Value.Split(new[] { ',' }, 3);
					for (int i = 0; i < parts.Length; i++) {
						parts[i] = parts[i].Trim();
					}
					this.entries.Add(parts);
				}
			}
		}

		public void FillIn(CraftmenuCategory menu) {
			System.Collections.Generic.List<CraftmenuCategory> path = new System.Collections.Generic.List<CraftmenuCategory> { menu };
			foreach (string[] entry in this.entries) {
				switch (entry[0].ToLowerInvariant()) {
					case "section":
						int depth = int.Parse(entry[1], System.Globalization.CultureInfo.InvariantCulture);
						CraftmenuCategory section = new CraftmenuCategory(entry[2]);
						path[depth].Contents.Add(section);
						path.RemoveRange(depth + 1, path.Count - depth - 1);
						path.Add(section);
						break;
					case "item":
						if (!notPorted.Contains(entry[1])) {
							AddItem(path[path.Count - 1], ThingDef.GetByDefname(entry[1]) as ItemDef);
						}
						break;
					case "circle":
						AddCircle(path[path.Count - 1], int.Parse(entry[1], System.Globalization.CultureInfo.InvariantCulture));
						break;
				}
			}
		}

		private static void AddItem(CraftmenuCategory section, ItemDef item) {
			if (item != null) {
				section.Contents.Add(new CraftmenuItem(item));
			}
		}

		private static void AddCircle(CraftmenuCategory section, int circle) {
			for (int id = circle * SpellsInCircle; id > (circle - 1) * SpellsInCircle; id--) {
				SpellDef spell = SpellDef.GetById(id);
				if (spell != null) {
					AddItem(section, spell.ScrollItem);
				}
			}
		}
	}
}
