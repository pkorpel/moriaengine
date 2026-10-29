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

	/// <summary>One of the 10 real Erebor ore tiers (sphereregion.scp's [RegionResource o_X]
	/// blocks) - skill threshold (the real `fixresource` check's single-value SKILL compare,
	/// using the range's own lower bound, x10-scaled to match this engine's internal skill
	/// storage), pool-size range, and regen time.</summary>
	public class OreTier {
		public readonly string ItemDefname;

		//#337 - mutable (not readonly) so MiningSettings' pass-through properties can write
		//through to these directly, same GM-tunable pattern as every other Mining constant.
		public int SkillThreshold;
		public int MinAmount;
		public int MaxAmount;
		public int RegenMinutes;

		public OreTier(string itemDefname, int skillThreshold, int minAmount, int maxAmount, int regenMinutes) {
			this.ItemDefname = itemDefname;
			this.SkillThreshold = skillThreshold;
			this.MinAmount = minAmount;
			this.MaxAmount = maxAmount;
			this.RegenMinutes = regenMinutes;
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

		private static readonly Dictionary<string, OreTier> extraTiers = new Dictionary<string, OreTier>(StringComparer.OrdinalIgnoreCase);

		/// <summary>#813 - the tier a region resource yields: one of All by its reaped item, or a
		/// tier made from the resource's own archive values (skill = the range's low end, like
		/// fixresource's check; amount; regen).</summary>
		public static OreTier ForResource(RegionResourceDef resource) {
			foreach (OreTier tier in All) {
				if (StringComparer.OrdinalIgnoreCase.Equals(tier.ItemDefname, resource.Reap)) {
					return tier;
				}
			}
			OreTier extra;
			if (!extraTiers.TryGetValue(resource.Reap, out extra)) {
				extra = new OreTier(resource.Reap, resource.SkillMin, resource.AmountMin, resource.AmountMax, Math.Max(1, resource.RegenSeconds / 60));
				extraTiers[resource.Reap] = extra;
			}
			return extra;
		}
	}

	/// <summary>
	/// Mining's per-tile vein system (#333, Phase A of #332) - lazily-created vein state per
	/// mined tile, like Sphere's resource spots. Since #813 what a spot holds is rolled from its
	/// region's t_rock region type in the Erebor archive (RegionResourceSystem) - open rock is
	/// mostly empty, mines are rich. A vein has ONE assigned ore type at a time; insufficient skill doesn't
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
			public bool CaveInBlocked;
			public double CaveInAutoClearAtSeconds;
		}

		/// <summary>Real Erebor's own cave-in roll (#334, Phase B of #332): ~1/120 chance per
		/// successful extraction (`{1 120}==43` in miningfix.scp's fixresource). Deliberately
		/// skips the real source's separate Tutorial-region 1/10 reduction - moriaengine has no
		/// tutorial-region equivalent to condition on, per #334's own explicit scope.
		/// Public/mutable (#337) so MiningSettings can expose it as GM-tunable.</summary>
		public static int CaveInChanceDenominator = 120;

		/// <summary>Real Erebor's own rubble-clearing roll range (#334): `{1000 1300} <
		/// player.mining` - a roll in this range must be BELOW the miner's skill to succeed,
		/// meaning skill below 1000 (100.0%) can never clear a cave-in at all (a genuine
		/// high-skill gate, not a transcription error).</summary>
		public static int CaveInClearRollMin = 1000;
		public static int CaveInClearRollMax = 1300;

		/// <summary>Real Erebor's own rubble item timer (60*60*20) - a 20-hour safety-net
		/// auto-clear in case nobody ever bothers to dig it out, not a meaningful regen
		/// mechanic from the player's own perspective (confirmed: the rubble item's own trigger
		/// group has no @timer handler at all, just creation/destroy triggers).</summary>
		public static int CaveInAutoClearSeconds = 60 * 60 * 20;

		private static readonly Dictionary<(byte, int, int), Vein> veins = new Dictionary<(byte, int, int), Vein>();

		/// <summary>Mines one extraction at the given tile for `miner`. Returns the ItemDef to
		/// grant (already skill-gated-downgraded to copper if under-skilled for the vein's real
		/// assigned ore), or null if the vein is currently exhausted (hasn't regenerated yet) or
		/// blocked by an unleared cave-in (#334) - callers should check IsCaveInBlocked first to
		/// route a cave-in-affected attempt through TryClearCaveIn instead of this method.</summary>
		public static ItemDef Mine(Character miner, byte mapPlane, int x, int y) {
			Vein vein = GetOrCreateVein(mapPlane, x, y);

			if (vein.CaveInBlocked) {
				if (Globals.TimeInSeconds < vein.CaveInAutoClearAtSeconds) {
					return null;
				}
				vein.CaveInBlocked = false; //20-hour safety-net auto-clear elapsed
			}

			if (vein.Untouched || (vein.RemainingPool <= 0 && Globals.TimeInSeconds >= vein.RegenReadyAtSeconds)) {
				AssignFreshOre(vein, mapPlane, x, y);
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

			if (Globals.dice.Next(CaveInChanceDenominator) == 0) { //real fixresource {1 120}==43 roll
				vein.CaveInBlocked = true;
				vein.CaveInAutoClearAtSeconds = Globals.TimeInSeconds + CaveInAutoClearSeconds;
			}

			return yieldOre.ItemDef;
		}

		/// <summary>#334 - is this tile currently blocked by an uncleared cave-in? Callers
		/// should check this BEFORE calling Mine, and route a blocked attempt through
		/// TryClearCaveIn instead (a completely separate roll, not a normal extraction).</summary>
		public static bool IsCaveInBlocked(byte mapPlane, int x, int y) {
			Vein vein;
			if (!veins.TryGetValue((mapPlane, x, y), out vein)) {
				return false;
			}
			if (vein.CaveInBlocked && Globals.TimeInSeconds >= vein.CaveInAutoClearAtSeconds) {
				vein.CaveInBlocked = false; //20-hour safety-net auto-clear elapsed
			}
			return vein.CaveInBlocked;
		}

		/// <summary>#334 - the real Erebor rubble-clearing roll: `{1000 1300} < player.mining`.
		/// Skill below 1000 (100.0%) can never succeed - a genuine high-skill gate, not a bug.
		/// Returns false (does nothing) if the tile isn't actually blocked.</summary>
		public static bool TryClearCaveIn(Character miner, byte mapPlane, int x, int y) {
			Vein vein;
			if (!veins.TryGetValue((mapPlane, x, y), out vein) || !vein.CaveInBlocked) {
				return false;
			}

			int roll = Globals.dice.Next(CaveInClearRollMin, CaveInClearRollMax + 1);
			if (roll < miner.GetRealSkillValue(SkillName.Mining)) {
				vein.CaveInBlocked = false;
				return true;
			}
			return false;
		}

		private static Vein GetOrCreateVein(byte mapPlane, int x, int y) {
			(byte, int, int) key = (mapPlane, x, y);
			Vein vein;
			if (!veins.TryGetValue(key, out vein)) {
				vein = new Vein();
				veins[key] = vein;
			}
			return vein;
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

		/// <summary>Test-only override (#334): forces a cave-in onto a tile without needing the
		/// real 1/120 roll to land - needed for deterministic tests of the clear-roll/blocking
		/// behavior.</summary>
		internal static void ForceCaveInForTest(byte mapPlane, int x, int y) {
			Vein vein = GetOrCreateVein(mapPlane, x, y);
			vein.CaveInBlocked = true;
			vein.CaveInAutoClearAtSeconds = Globals.TimeInSeconds + CaveInAutoClearSeconds;
		}

		/// <summary>#813 - what this spot holds comes from the region it's in: the area's
		/// t_rock region type from the Erebor archive (sphereregion.scp), weighted roll -
		/// RegionResourceSystem. "Nothing" (r_nothing, or no region type for t_rock here)
		/// leaves the spot empty until that resource's REGEN, then it rolls again (Sphere's
		/// resource spot). A resource that isn't one of the OreTiers (sand, gems...) gets a
		/// tier built from its own archive values.</summary>
		private static void AssignFreshOre(Vein vein, byte mapPlane, int x, int y) {
			vein.Untouched = false;
			RegionResourceDef resource = RegionResourceSystem.Pick(mapPlane, x, y, "t_rock");
			if ((resource == null) || resource.IsNothing) {
				vein.AssignedOre = null;
				vein.RemainingPool = 0;
				vein.RegenReadyAtSeconds = Globals.TimeInSeconds + (((resource != null) && (resource.RegenSeconds > 0)) ? resource.RegenSeconds : EmptySpotRegenSeconds);
				return;
			}
			OreTier picked = OreTiers.ForResource(resource);
			vein.AssignedOre = picked;
			vein.RemainingPool = Globals.dice.Next(picked.MinAmount, picked.MaxAmount + 1);
		}

		/// <summary>How long a spot with no region type at all stays empty (r_nothing's own REGEN).</summary>
		public static int EmptySpotRegenSeconds = 30 * 60;

		/// <summary>#813 - true when this spot rolled "nothing" (as opposed to being used up).</summary>
		public static bool IsEmptySpot(byte mapPlane, int x, int y) {
			Vein vein;
			return veins.TryGetValue((mapPlane, x, y), out vein) && !vein.Untouched && (vein.AssignedOre == null);
		}
	}
}
