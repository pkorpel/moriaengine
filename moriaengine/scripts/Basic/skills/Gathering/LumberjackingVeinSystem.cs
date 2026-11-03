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

namespace SteamEngine.CompiledScripts {

	/// <summary>One of the 7 real Erebor wood tiers (sphereregion.scp's [RegionResource l_X]
	/// blocks) - skill threshold (the real `fixresource` check's single-value SKILL compare,
	/// using the range's own lower bound, x10-scaled to match this engine's internal skill
	/// storage), pool-size range, and regen time. NOT i_log_lager - that item is exclusively
	/// the output of a separate labor-camp/prisoner-punishment region-type (r_lagr_tree,
	/// tag.trestanec), confirmed via direct source cross-reference (#270's corrected comment),
	/// not a normal wood tier at all.</summary>
	public class WoodTier {
		public readonly string ItemDefname;

		//mutable (not readonly), matching OreTier's own precedent, so a future .settings
		//category (#425) can write through to these directly.
		public int SkillThreshold;
		public int MinAmount;
		public int MaxAmount;
		public int RegenMinutes;

		public WoodTier(string itemDefname, int skillThreshold, int minAmount, int maxAmount, int regenMinutes) {
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

	/// <summary>The 7 real Erebor wood tiers (#422), confirmed directly against
	/// sphereregion.scp's own [RegionResource l_X] blocks after correcting an earlier wrong
	/// assumption that i_log_lager was the baseline (see WoodTier's own doc comment and
	/// #270's corrected comment for the full story).
	///
	/// Renamed from metal to wood names (#536, user-reported confusion - "even if log id is
	/// i_copper_log, it's named Spruce"). The item DEFNAMEs are unchanged (i_log_copper really
	/// is the spruce log - confirmed: scripts/converted/item_ore.def's own NAME="spruce log%s"
	/// on i_log_copper, chestnut/oak/teak/mahagon/eben/elven likewise on iron/verite/valorite/
	/// obsidian/adamantinum/mithril respectively - same material-tier aliasing as Mining's ore
	/// tiers, metal and wood are literally the same numeric tier with two display names) - only
	/// the C# identifiers/settings-dialog property names change, so a GM looking at the
	/// Lumberjacking settings page sees real wood names instead of metal names that collided
	/// (both visually and in spirit) with Mining's own identically-named ore-tier properties.</summary>
	public static class WoodTiers {
		public static readonly WoodTier Spruce = new WoodTier("i_log_copper", 0, 5, 20, 30);
		public static readonly WoodTier Chestnut = new WoodTier("i_log_iron", 400, 4, 15, 30);
		public static readonly WoodTier Oak = new WoodTier("i_log_verite", 550, 3, 11, 30);
		public static readonly WoodTier Teak = new WoodTier("i_log_valorite", 700, 2, 8, 30);
		public static readonly WoodTier Mahagon = new WoodTier("i_log_obsidian", 900, 1, 5, 30);
		public static readonly WoodTier Eben = new WoodTier("i_log_adamantinum", 990, 1, 4, 30);
		public static readonly WoodTier Elven = new WoodTier("i_log_mithril", 1050, 1, 3, 30);

		public static readonly WoodTier[] All = {
			Spruce, Chestnut, Oak, Teak, Mahagon, Eben, Elven
		};

		private static readonly Dictionary<string, WoodTier> extraTiers = new Dictionary<string, WoodTier>(StringComparer.OrdinalIgnoreCase);

		/// <summary>#813 - the tier a region resource yields: one of All by its reaped item, or a
		/// tier made from the resource's own archive values (skill = the range's low end, like
		/// fixresource's check; amount; regen).</summary>
		public static WoodTier ForResource(RegionResourceDef resource) {
			foreach (WoodTier tier in All) {
				if (StringComparer.OrdinalIgnoreCase.Equals(tier.ItemDefname, resource.Reap)) {
					return tier;
				}
			}
			WoodTier extra;
			if (!extraTiers.TryGetValue(resource.Reap, out extra)) {
				extra = new WoodTier(resource.Reap, resource.SkillMin, resource.AmountMin, resource.AmountMax, Math.Max(1, resource.RegenSeconds / 60));
				extraTiers[resource.Reap] = extra;
			}
			return extra;
		}

		/// <summary>Reverse lookup from a yielded ItemDef back to its WoodTier - used by
		/// LumberjackingAmbushSystem (#431) to key its Reaper variant off the wood tier a chop
		/// actually produced. Returns null for a null/unrecognised ItemDef.</summary>
		public static WoodTier ByItemDef(ItemDef itemDef) {
			if (itemDef == null) {
				return null;
			}
			foreach (WoodTier tier in All) {
				if (tier.ItemDef == itemDef) {
					return tier;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// Lumberjacking's per-tile vein system (#422, Phase A of #421) - mirrors Mining's
	/// MiningVeinSystem (#333) exactly: lazily-created vein state per chopped tree tile, no
	/// pre-authored vein registry (same reasoning as Mining - real named-forest-region data
	/// doesn't exist in moriaengine, see #426's deferral). A vein has ONE assigned wood type
	/// at a time; insufficient skill doesn't fail the attempt, it downgrades the yield to
	/// copper (real `fixresource` logic: `ACT.MORE1 = l_copper`); the pool depletes by 1 per
	/// successful extraction and regenerates (with a fresh wood roll) once its timer elapses.
	///
	/// No cave-in equivalent - confirmed Mining-specific in the real source, not ported here.
	///
	/// Same known, deliberate simplification as Mining: vein state is in-memory only, not
	/// persisted across a server restart.
	/// </summary>
	public static class LumberjackingVeinSystem {

		private static void AssignIfDue(Vein vein, byte mapPlane, int x, int y) {
			if (vein.Untouched || (vein.RemainingPool <= 0 && Globals.TimeInSeconds >= vein.RegenReadyAtSeconds)) {
				AssignFreshWood(vein, mapPlane, x, y);
			}
		}

		/// <summary>#962 - how hard this try is, by what the spot holds. Nothing there takes no skill.</summary>
		public static int RollDifficulty(byte mapPlane, int x, int y) {
			Vein vein = GetOrCreateVein(mapPlane, x, y);
			AssignIfDue(vein, mapPlane, x, y);
			return (vein.Resource == null) ? 0 : vein.Resource.RollDifficulty();
		}

		private class Vein {
			public RegionResourceDef Resource;
			public WoodTier AssignedWood;
			public int RemainingPool;
			public double RegenReadyAtSeconds;
			public bool Untouched = true;
		}

		private static readonly Dictionary<(byte, int, int), Vein> veins = new Dictionary<(byte, int, int), Vein>();

		/// <summary>Chops one extraction at the given tree tile for `chopper`. Returns the
		/// ItemDef to grant (already skill-gated-downgraded to copper if under-skilled for the
		/// vein's real assigned wood), or null if the vein is currently exhausted (hasn't
		/// regenerated yet).</summary>
		public static ItemDef Chop(Character chopper, byte mapPlane, int x, int y) {
			Vein vein = GetOrCreateVein(mapPlane, x, y);

			AssignIfDue(vein, mapPlane, x, y);

			if (vein.RemainingPool <= 0) {
				return null; //still exhausted, regen hasn't elapsed yet
			}

			//#1017 - a craftsman's vigour can find the next wood up, if the skill is there for it
			if (CraftsmanAbilities.FindsBetter(chopper, "vigourLumber")) {
				RegionResourceDef better = CraftsmanAbilities.Next(vein.Resource);
				if (better != null) {
					WoodTier betterWood = WoodTiers.ForResource(better);
					if (chopper.GetRealSkillValue(SkillName.Lumberjacking) >= betterWood.SkillThreshold) {
						vein.Resource = better;
						vein.AssignedWood = betterWood;
						chopper.SysMessage((chopper.IsFemale ? "Nasla" : "Nasel") + " jsi lepsi material!", ClassAbilities.ClassMessageHue);
					}
				}
			}

			//#427 - near an area's elven probe, a craftsman's elven tree has nothing and ebony can turn elven
			if (ElvenGrove.IsNear(chopper)) {
				if (vein.AssignedWood == WoodTiers.Elven) {
					vein.Resource = RegionResourceDef.GetByDefname("l_copper");
					vein.AssignedWood = WoodTiers.Spruce;
					vein.RemainingPool = 0;
					vein.RegenReadyAtSeconds = Globals.TimeInSeconds + (vein.AssignedWood.RegenMinutes * 60);
					return null;
				}
				if ((vein.AssignedWood == WoodTiers.Eben) && ElvenGrove.RollsElven(chopper)) {
					vein.Resource = RegionResourceDef.GetByDefname("l_mithril");
					vein.AssignedWood = WoodTiers.Elven;
					vein.RemainingPool = 1;
					Logger.WriteInfo(true, "Elven wood: " + chopper.Name + " (uid " + chopper.Uid + ", account " + chopper.Account?.Name + ") found an elven log at " + x + "," + y);
				}
			}

			WoodTier realWood = vein.AssignedWood;
			WoodTier yieldWood = (chopper.GetRealSkillValue(SkillName.Lumberjacking) >= realWood.SkillThreshold)
				? realWood
				: WoodTiers.Spruce; //real fixresource logic - under-skilled never fails outright, just downgrades

			vein.RemainingPool--;
			if (vein.RemainingPool <= 0) {
				vein.RegenReadyAtSeconds = Globals.TimeInSeconds + (vein.AssignedWood.RegenMinutes * 60);
			}

			return yieldWood.ItemDef;
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

		/// <summary>Test-only override (#422): forces a specific tile's vein to a known
		/// wood/pool state, bypassing the random roll - same precedent as Mining's
		/// ForceVeinForTest.</summary>
		internal static void ForceVeinForTest(byte mapPlane, int x, int y, WoodTier wood, int poolAmount) {
			veins[(mapPlane, x, y)] = new Vein { AssignedWood = wood, RemainingPool = poolAmount, Untouched = false, RegenReadyAtSeconds = 0 };
		}

		internal static void ForceVeinRegenReadyForTest(byte mapPlane, int x, int y) {
			Vein vein;
			if (veins.TryGetValue((mapPlane, x, y), out vein)) {
				vein.RegenReadyAtSeconds = 0; //already elapsed relative to any real Globals.TimeInSeconds
			}
		}

		/// <summary>#813 - what this spot holds comes from the region it's in: the area's
		/// t_tree region type from the Erebor archive (sphereregion.scp), weighted roll -
		/// RegionResourceSystem. "Nothing" (r_nothing, or no region type for t_tree here)
		/// leaves the spot empty until that resource's REGEN, then it rolls again (Sphere's
		/// resource spot). A resource that isn't one of the WoodTiers (sand, gems...) gets a
		/// tier built from its own archive values.</summary>
		private static void AssignFreshWood(Vein vein, byte mapPlane, int x, int y) {
			vein.Untouched = false;
			RegionResourceDef resource = RegionResourceSystem.Pick(mapPlane, x, y, "t_tree");
			vein.Resource = resource;
			if ((resource == null) || resource.IsNothing) {
				vein.AssignedWood = null;
				vein.RemainingPool = 0;
				vein.RegenReadyAtSeconds = Globals.TimeInSeconds + (((resource != null) && (resource.RegenSeconds > 0)) ? resource.RegenSeconds : EmptySpotRegenSeconds);
				return;
			}
			WoodTier picked = WoodTiers.ForResource(resource);
			vein.AssignedWood = picked;
			vein.RemainingPool = Globals.dice.Next(picked.MinAmount, picked.MaxAmount + 1);
		}

		/// <summary>How long a spot with no region type at all stays empty (r_nothing's own REGEN).</summary>
		public static int EmptySpotRegenSeconds = 30 * 60;

		/// <summary>#813 - true when this spot rolled "nothing" (as opposed to being used up).</summary>
		public static bool IsEmptySpot(byte mapPlane, int x, int y) {
			Vein vein;
			return veins.TryGetValue((mapPlane, x, y), out vein) && !vein.Untouched && (vein.AssignedWood == null);
		}
	}
}
