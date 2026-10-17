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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>One of the 10 real Erebor ore tiers (sphereregion.scp's [RegionResource o_X]
	/// blocks) - skill threshold (the real `fixresource` check's single-value SKILL compare,
	/// using the range's own lower bound, x10-scaled to match this engine's internal skill
	/// storage), pool-size range, and regen time.</summary>
	public class OreTier {
		public readonly string ItemDefname;
		public readonly int SkillThreshold;
		public readonly int MinAmount;
		public readonly int MaxAmount;
		public readonly int RegenMinutes;

		public OreTier(string itemDefname, int skillThreshold, int minAmount, int maxAmount, int regenMinutes) {
			this.ItemDefname = itemDefname;
			this.SkillThreshold = skillThreshold;
			this.MinAmount = minAmount;
			this.MaxAmount = maxAmount;
			this.RegenMinutes = regenMinutes;
		}

		/// <summary>Rarity weight for the untouched-vein roll - the real AMOUNT range's own
		/// midpoint is used directly as a commonness proxy (copper/iron's much larger pools mean
		/// much higher weight, mithril's tiny pool means it's rolled far less often) - matches
		/// #333's own explicit design, not an invented formula.</summary>
		public double Weight {
			get { return (this.MinAmount + this.MaxAmount) / 2.0; }
		}

		private ItemDef itemDef;
		public ItemDef ItemDef {
			get {
				if (this.itemDef == null) {
					this.itemDef = (ItemDef) ThingDef.GetByDefname(this.ItemDefname);
				}
				return this.itemDef;
			}
		}
	}

	/// <summary>The 10 real Erebor ore tiers (#333) - all values straight from
	/// sphereregion.scp's own [RegionResource o_X] blocks, skill thresholds x10-scaled (this
	/// engine stores skill values x10 internally - e.g. real Erebor's Mining range 105-145 for
	/// mithril, the highest in the game, becomes threshold 1050, exceeding the classic 1000 cap
	/// on purpose, confirmed in source, not a transcription error).</summary>
	public static class OreTiers {
		public static readonly OreTier Copper = new OreTier("i_ore_copper", 0, 50, 75, 30);
		public static readonly OreTier Iron = new OreTier("i_ore_iron", 300, 50, 75, 60);
		public static readonly OreTier Silver = new OreTier("i_ore_silver", 550, 20, 40, 30);
		public static readonly OreTier Gold = new OreTier("i_ore_gold", 600, 10, 30, 30);
		public static readonly OreTier Verite = new OreTier("i_ore_verite", 700, 10, 15, 30);
		public static readonly OreTier Valorite = new OreTier("i_ore_valorite", 750, 5, 10, 30);
		public static readonly OreTier Obsidian = new OreTier("i_ore_obsidian", 900, 8, 10, 30);
		public static readonly OreTier Shadow = new OreTier("i_ore_shadow", 900, 4, 5, 30);
		public static readonly OreTier Adamantinum = new OreTier("i_ore_adamantinum", 990, 4, 5, 30);
		public static readonly OreTier Mithril = new OreTier("i_ore_mithril", 1050, 3, 4, 30);

		public static readonly OreTier[] All = {
			Copper, Iron, Silver, Gold, Verite, Valorite, Obsidian, Shadow, Adamantinum, Mithril
		};
	}

	/// <summary>
	/// Mining's per-tile vein system (#333, Phase A of #332) - lazily-created vein state per
	/// mined tile, no pre-authored vein registry (matching #332's own confirmed design: "full
	/// per-vein pool fidelity, scoped to individual tiles, not named regions - since region data
	/// doesn't exist"). A vein has ONE assigned ore type at a time; insufficient skill doesn't
	/// fail the attempt, it downgrades the yield to copper (real `fixresource` logic); the pool
	/// depletes by 1 per successful extraction and regenerates (with a fresh ore roll - veins
	/// vary over time, per the user's own explicit confirmation) once its timer elapses.
	///
	/// Known, deliberate simplification: vein state is in-memory only, not persisted across a
	/// server restart - every vein resets to "untouched" on reboot. Building real persistence
	/// for an arbitrary per-tile dictionary would need a new generic keyed-state save mechanism
	/// this engine doesn't have (the existing [SaveableClass]/[HasSavedMembers] pipeline is built
	/// for fixed-shape objects, not an open-ended per-tile map); not required by #333's own
	/// scope text, and low-stakes compared to the other explicit design deferrals already made
	/// for this feature (region-restriction, #338). Flagged here, not silently dropped.
	/// </summary>
	public static class MiningVeinSystem {

		private class Vein {
			public OreTier AssignedOre;
			public int RemainingPool;
			public double RegenReadyAtSeconds;
			public bool Untouched = true;
		}

		private static readonly Dictionary<(byte, int, int), Vein> veins = new Dictionary<(byte, int, int), Vein>();

		/// <summary>Mines one extraction at the given tile for `miner`. Returns the ItemDef to
		/// grant (already skill-gated-downgraded to copper if under-skilled for the vein's real
		/// assigned ore), or null if the vein is currently exhausted and hasn't regenerated yet.</summary>
		public static ItemDef Mine(Character miner, byte mapPlane, int x, int y) {
			(byte, int, int) key = (mapPlane, x, y);
			Vein vein;
			if (!veins.TryGetValue(key, out vein)) {
				vein = new Vein();
				veins[key] = vein;
			}

			if (vein.Untouched || (vein.RemainingPool <= 0 && Globals.TimeInSeconds >= vein.RegenReadyAtSeconds)) {
				AssignFreshOre(vein);
			}

			if (vein.RemainingPool <= 0) {
				return null; //still exhausted, regen hasn't elapsed yet
			}

			OreTier realOre = vein.AssignedOre;
			OreTier yieldOre = (miner.GetRealSkillValue(SkillName.Mining) >= realOre.SkillThreshold)
				? realOre
				: OreTiers.Copper; //real fixresource logic - under-skilled never fails outright, just downgrades

			vein.RemainingPool--;
			if (vein.RemainingPool <= 0) {
				vein.RegenReadyAtSeconds = Globals.TimeInSeconds + (vein.AssignedOre.RegenMinutes * 60);
			}

			return yieldOre.ItemDef;
		}

		/// <summary>Test-only override (#333): forces a specific tile's vein to a known
		/// ore/pool state, bypassing the random roll - needed for deterministic tests of
		/// depletion/downgrade/regen behavior. Matches this codebase's established
		/// SpeedUpSweepForTest-style test-hook pattern (a small, clearly test-only addition to
		/// production code, not a separate test-only copy of the system).</summary>
		internal static void ForceVeinForTest(byte mapPlane, int x, int y, OreTier ore, int poolAmount) {
			veins[(mapPlane, x, y)] = new Vein { AssignedOre = ore, RemainingPool = poolAmount, Untouched = false, RegenReadyAtSeconds = 0 };
		}

		internal static void ForceVeinRegenReadyForTest(byte mapPlane, int x, int y) {
			Vein vein;
			if (veins.TryGetValue((mapPlane, x, y), out vein)) {
				vein.RegenReadyAtSeconds = 0; //already elapsed relative to any real Globals.TimeInSeconds
			}
		}

		private static void AssignFreshOre(Vein vein) {
			double totalWeight = 0;
			foreach (OreTier tier in OreTiers.All) {
				totalWeight += tier.Weight;
			}

			double roll = Globals.dice.NextDouble() * totalWeight;
			OreTier picked = OreTiers.All[OreTiers.All.Length - 1];
			double accumulated = 0;
			foreach (OreTier tier in OreTiers.All) {
				accumulated += tier.Weight;
				if (roll < accumulated) {
					picked = tier;
					break;
				}
			}

			vein.AssignedOre = picked;
			vein.RemainingPool = Globals.dice.Next(picked.MinAmount, picked.MaxAmount + 1);
			vein.Untouched = false;
		}
	}
}
