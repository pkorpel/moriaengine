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
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#813 - one gatherable resource from the Erebor archive's sphereregion.scp
	/// [RegionResource] (converted to [RegionResourceDef] in scripts/converted/region.def): the
	/// skill range, the pool a spot holds (AMOUNT), the item it yields (REAP) and how long a spot
	/// takes to come back (REGEN). r_nothing (REAP=i_unused, no AMOUNT) is "this spot is empty".
	/// Skill values are x10 like the engine's own (Sphere 30.0 = 300).</summary>
	[ViewableClass]
	public class RegionResourceDef : AbstractDef {
		private int skillMin, skillMax, amountMin, amountMax, regenSeconds;
		private string reap;
		private ItemDef reapDef;

		public RegionResourceDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static new RegionResourceDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as RegionResourceDef;
		}

		public int SkillMin { get { return this.skillMin; } }

		/// <summary>#962 - Sphere's Skill_NaturalResource_Setup: this attempt's difficulty, in tenths.</summary>
		public int RollDifficulty() {
			return this.skillMin + Globals.dice.Next(Math.Max(1, this.skillMax - this.skillMin));
		}

		public int SkillMax { get { return this.skillMax; } }
		public int AmountMin { get { return this.amountMin; } }
		public int AmountMax { get { return this.amountMax; } }
		public int RegenSeconds { get { return this.regenSeconds; } }
		public string Reap { get { return this.reap; } }

		public ItemDef ReapDef {
			get {
				if ((this.reapDef == null) && !string.IsNullOrEmpty(this.reap)) {
					this.reapDef = ThingDef.GetByDefname(this.reap) as ItemDef;
				}
				return this.reapDef;
			}
		}

		/// <summary>An empty spot (r_nothing): no pool, no item.</summary>
		public bool IsNothing {
			get { return (this.amountMax <= 0) || string.IsNullOrEmpty(this.reap) || StringComparer.OrdinalIgnoreCase.Equals(this.reap, "i_unused"); }
		}

		public override void LoadScriptLines(PropsSection ps) {
			foreach (PropsLine line in ps.PropsLines) {
				try {
					switch (line.Name.ToLowerInvariant()) {
						case "skill":
							ParseRange(line.Value, 10, out this.skillMin, out this.skillMax);
							break;
						case "amount":
							ParseRange(line.Value, 1, out this.amountMin, out this.amountMax);
							break;
						case "reap":
							this.reap = line.Value.Trim();
							this.reapDef = null;
							break;
						case "regenseconds":
							this.regenSeconds = ConvertTools.ParseInt32(line.Value.Trim());
							break;
						default:
							Logger.WriteWarning(ps.Filename, line.Line, "Unknown RegionResourceDef line " + LogStr.Ident(line.Name));
							break;
					}
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(ps.Filename, line.Line, e);
				}
			}
		}

		//"30.0,70.0" or "50,75" or a single value; multiplier 10 turns Sphere's 30.0 into 300
		private static void ParseRange(string value, int multiplier, out int min, out int max) {
			string[] parts = value.Split(',');
			min = (int) Math.Round(double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) * multiplier);
			max = (parts.Length > 1) ? (int) Math.Round(double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) * multiplier) : min;
		}
	}

	/// <summary>#813 - a Sphere [RegionType] with RESOURCES: what tiles of one item type (t_rock,
	/// t_tree, t_water, t_grass) yield in regions listing it, as weighted RegionResourceDefs
	/// ("100 r_nothing", "1 o_copper", ...).</summary>
	[ViewableClass]
	public class RegionTypeDef : AbstractDef {
		private string itemType;
		private readonly List<KeyValuePair<int, string>> entries = new List<KeyValuePair<int, string>>();
		private RegionResourceDef[] resolved;
		private int totalWeight;

		public RegionTypeDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static new RegionTypeDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as RegionTypeDef;
		}

		/// <summary>t_rock / t_tree / t_water / t_grass</summary>
		public string ItemType { get { return this.itemType; } }

		public override void LoadScriptLines(PropsSection ps) {
			this.entries.Clear();
			this.resolved = null;
			foreach (PropsLine line in ps.PropsLines) {
				switch (line.Name.ToLowerInvariant()) {
					case "itemtype":
						this.itemType = line.Value.Trim();
						break;
					case "resources":
						string[] parts = line.Value.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length == 2) {
							this.entries.Add(new KeyValuePair<int, string>(ConvertTools.ParseInt32(parts[0]), parts[1]));
						} else {
							Logger.WriteWarning(ps.Filename, line.Line, "Expected 'weight resource' in RegionTypeDef resources line, got " + LogStr.Ident(line.Value));
						}
						break;
					default:
						Logger.WriteWarning(ps.Filename, line.Line, "Unknown RegionTypeDef line " + LogStr.Ident(line.Name));
						break;
				}
			}
		}

		/// <summary>A weighted random pick, Sphere's GetRandMemberIndex.</summary>
		public RegionResourceDef PickRandom() {
			this.Resolve();
			if (this.totalWeight <= 0) {
				return null;
			}
			int roll = Globals.dice.Next(this.totalWeight);
			for (int i = 0; i < this.resolved.Length; i++) {
				int weight = this.entries[i].Key;
				if (roll < weight) {
					return this.resolved[i];
				}
				roll -= weight;
			}
			return null;
		}

		private void Resolve() {
			if (this.resolved != null) {
				return;
			}
			this.resolved = new RegionResourceDef[this.entries.Count];
			this.totalWeight = 0;
			for (int i = 0; i < this.entries.Count; i++) {
				this.resolved[i] = RegionResourceDef.GetByDefname(this.entries[i].Value);
				if (this.resolved[i] == null) {
					Logger.WriteWarning("RegionTypeDef " + LogStr.Ident(this.Defname) + " names an unknown RegionResourceDef " + LogStr.Ident(this.entries[i].Value) + ".");
				}
				this.totalWeight += this.entries[i].Key;
			}
		}
	}

	/// <summary>#813 - which resource a gathering spot gets, Sphere 0.99z8's CheckNaturalResource
	/// (EreborNext CWorld.NaturalResource.cs): the area at the spot (a house or ship's own region
	/// doesn't count - the static area under it does); if that area has no EVENTS and no RESOURCES
	/// at all, the world region instead; then its region type for this item type (t_rock, ...),
	/// weighted roll. No matching region type = nothing can be gathered there (e.g. a player-house
	/// area, which has events but no resources). Sphere keeps events and resources in one list, so
	/// both are searched.</summary>
	public static class RegionResourceSystem {
		public static RegionResourceDef Pick(byte mapPlane, int x, int y, string itemType) {
			RegionTypeDef type = FindRegionType(mapPlane, x, y, itemType);
			return (type == null) ? null : type.PickRandom();
		}

		public static RegionTypeDef FindRegionType(byte mapPlane, int x, int y, string itemType) {
			FlaggedRegion area = FindArea(mapPlane, x, y);
			if (area == null) {
				return null;
			}
			if ((area.RegionEvents.Count == 0) && (area.RegionResources.Count == 0)) {
				area = StaticRegion.WorldRegion as FlaggedRegion;
				if (area == null) {
					return null;
				}
			}
			return FindIn(area.RegionResources, itemType) ?? FindIn(area.RegionEvents, itemType);
		}

		private static RegionTypeDef FindIn(IList<string> names, string itemType) {
			foreach (string name in names) {
				RegionTypeDef def = RegionTypeDef.GetByDefname(name);
				if ((def != null) && StringComparer.OrdinalIgnoreCase.Equals(def.ItemType, itemType)) {
					return def;
				}
			}
			return null;
		}

		private static FlaggedRegion FindArea(byte mapPlane, int x, int y) {
			Map map = Map.GetMap(mapPlane);
			return (map == null) ? null : RegionEventSystem.AreaOf(map.GetRegionFor(x, y));
		}
	}
}
