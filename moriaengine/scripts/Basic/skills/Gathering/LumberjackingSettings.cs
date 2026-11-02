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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// GM-tunable Lumberjacking settings (#425, Phase E of #421), shown as a "Lumberjacking"
	/// category in the .settings dialog - covers everything #422/#431/#424 built. Same
	/// pass-through-property shape as MiningSettings (#337) - every field reads/writes the
	/// real live constant elsewhere directly (WoodTier instances, LumberjackingAmbushSystem's
	/// static fields, the shared GatheringDelaySystem), so an edit here takes effect
	/// immediately with zero extra sync step. Nothing of its own to persist - no
	/// [SaveableClass]/[HasSavedMembers] needed, every constant already has its own backing
	/// store, same reasoning as MiningSettings.
	///
	/// The gatherDelay* fields point at GatheringDelaySystem's own static fields, which are
	/// SHARED across Mining/Fishing/Lumberjacking (one real formula, not per-skill) - editing
	/// them here also changes Mining's delay and vice versa. Same shared-state characteristic
	/// MiningSettings already has (it exposes the identical fields), not something new
	/// introduced here - kept for consistency with that precedent rather than omitted.
	///
	/// Deliberately NOT exposed as individually tunable, same #337 scope note MiningSettings
	/// itself follows: each wood tier's own item defname (tied to real content, not a tunable
	/// number) and the ambush table's per-category WEIGHTS (fixed rand(128) bucket boundaries,
	/// not a generic weighted table - see LumberjackingAmbushSystem's own doc comment).
	/// </summary>
	[ViewableClass]
	public class LumberjackingSettings : SettingsMetaCategory {

		public static LumberjackingSettings instance = new LumberjackingSettings();

		// ---- Per-wood tiers (#422) - skill threshold / pool amount range / regen minutes ----
		// Renamed from metal to wood names (#536) - the item defnames are unchanged (i_log_copper
		// really is "spruce log" in content), only these C#/dialog-facing names changed, so they
		// no longer collide (visually, in the settings dialog) with Mining's identically-shaped
		// copperSkillThreshold/etc. properties on the SAME material tier.

		public int spruceSkillThreshold { get { return WoodTiers.Spruce.SkillThreshold; } set { WoodTiers.Spruce.SkillThreshold = value; } }
		public int spruceMinAmount { get { return WoodTiers.Spruce.MinAmount; } set { WoodTiers.Spruce.MinAmount = value; } }
		public int spruceMaxAmount { get { return WoodTiers.Spruce.MaxAmount; } set { WoodTiers.Spruce.MaxAmount = value; } }
		public int spruceRegenMinutes { get { return WoodTiers.Spruce.RegenMinutes; } set { WoodTiers.Spruce.RegenMinutes = value; } }

		public int chestnutSkillThreshold { get { return WoodTiers.Chestnut.SkillThreshold; } set { WoodTiers.Chestnut.SkillThreshold = value; } }
		public int chestnutMinAmount { get { return WoodTiers.Chestnut.MinAmount; } set { WoodTiers.Chestnut.MinAmount = value; } }
		public int chestnutMaxAmount { get { return WoodTiers.Chestnut.MaxAmount; } set { WoodTiers.Chestnut.MaxAmount = value; } }
		public int chestnutRegenMinutes { get { return WoodTiers.Chestnut.RegenMinutes; } set { WoodTiers.Chestnut.RegenMinutes = value; } }

		public int oakSkillThreshold { get { return WoodTiers.Oak.SkillThreshold; } set { WoodTiers.Oak.SkillThreshold = value; } }
		public int oakMinAmount { get { return WoodTiers.Oak.MinAmount; } set { WoodTiers.Oak.MinAmount = value; } }
		public int oakMaxAmount { get { return WoodTiers.Oak.MaxAmount; } set { WoodTiers.Oak.MaxAmount = value; } }
		public int oakRegenMinutes { get { return WoodTiers.Oak.RegenMinutes; } set { WoodTiers.Oak.RegenMinutes = value; } }

		public int teakSkillThreshold { get { return WoodTiers.Teak.SkillThreshold; } set { WoodTiers.Teak.SkillThreshold = value; } }
		public int teakMinAmount { get { return WoodTiers.Teak.MinAmount; } set { WoodTiers.Teak.MinAmount = value; } }
		public int teakMaxAmount { get { return WoodTiers.Teak.MaxAmount; } set { WoodTiers.Teak.MaxAmount = value; } }
		public int teakRegenMinutes { get { return WoodTiers.Teak.RegenMinutes; } set { WoodTiers.Teak.RegenMinutes = value; } }

		public int mahagonSkillThreshold { get { return WoodTiers.Mahagon.SkillThreshold; } set { WoodTiers.Mahagon.SkillThreshold = value; } }
		public int mahagonMinAmount { get { return WoodTiers.Mahagon.MinAmount; } set { WoodTiers.Mahagon.MinAmount = value; } }
		public int mahagonMaxAmount { get { return WoodTiers.Mahagon.MaxAmount; } set { WoodTiers.Mahagon.MaxAmount = value; } }
		public int mahagonRegenMinutes { get { return WoodTiers.Mahagon.RegenMinutes; } set { WoodTiers.Mahagon.RegenMinutes = value; } }

		public int ebenSkillThreshold { get { return WoodTiers.Eben.SkillThreshold; } set { WoodTiers.Eben.SkillThreshold = value; } }
		public int ebenMinAmount { get { return WoodTiers.Eben.MinAmount; } set { WoodTiers.Eben.MinAmount = value; } }
		public int ebenMaxAmount { get { return WoodTiers.Eben.MaxAmount; } set { WoodTiers.Eben.MaxAmount = value; } }
		public int ebenRegenMinutes { get { return WoodTiers.Eben.RegenMinutes; } set { WoodTiers.Eben.RegenMinutes = value; } }

		public int elvenSkillThreshold { get { return WoodTiers.Elven.SkillThreshold; } set { WoodTiers.Elven.SkillThreshold = value; } }
		public int elvenMinAmount { get { return WoodTiers.Elven.MinAmount; } set { WoodTiers.Elven.MinAmount = value; } }
		public int elvenMaxAmount { get { return WoodTiers.Elven.MaxAmount; } set { WoodTiers.Elven.MaxAmount = value; } }
		public int elvenRegenMinutes { get { return WoodTiers.Elven.RegenMinutes; } set { WoodTiers.Elven.RegenMinutes = value; } }

		// ---- Monster ambush (#431) ----

		public int ambushChanceDenominator { get { return LumberjackingAmbushSystem.AmbushChanceDenominator; } set { LumberjackingAmbushSystem.AmbushChanceDenominator = value; } }
		public string ambushWolfDefname { get { return LumberjackingAmbushSystem.WolfDefname; } set { LumberjackingAmbushSystem.WolfDefname = value; } }
		public string ambushGrizzlyBearDefname { get { return LumberjackingAmbushSystem.GrizzlyBearDefname; } set { LumberjackingAmbushSystem.GrizzlyBearDefname = value; } }
		public string ambushForestElementalDefname { get { return LumberjackingAmbushSystem.ForestElementalDefname; } set { LumberjackingAmbushSystem.ForestElementalDefname = value; } }
		public string ambushForestSpiritDefname { get { return LumberjackingAmbushSystem.ForestSpiritDefname; } set { LumberjackingAmbushSystem.ForestSpiritDefname = value; } }
		public string ambushReaperSpruceDefname { get { return LumberjackingAmbushSystem.ReaperSpruceDefname; } set { LumberjackingAmbushSystem.ReaperSpruceDefname = value; } }
		public string ambushReaperChestnutDefname { get { return LumberjackingAmbushSystem.ReaperChestnutDefname; } set { LumberjackingAmbushSystem.ReaperChestnutDefname = value; } }
		public string ambushReaperOakDefname { get { return LumberjackingAmbushSystem.ReaperOakDefname; } set { LumberjackingAmbushSystem.ReaperOakDefname = value; } }
		public string ambushReaperTeakDefname { get { return LumberjackingAmbushSystem.ReaperTeakDefname; } set { LumberjackingAmbushSystem.ReaperTeakDefname = value; } }
		public string ambushReaperMahaganDefname { get { return LumberjackingAmbushSystem.ReaperMahaganDefname; } set { LumberjackingAmbushSystem.ReaperMahaganDefname = value; } }
		public string ambushReaperEbenDefname { get { return LumberjackingAmbushSystem.ReaperEbenDefname; } set { LumberjackingAmbushSystem.ReaperEbenDefname = value; } }
		public string ambushReaperElvenDefname { get { return LumberjackingAmbushSystem.ReaperElvenDefname; } set { LumberjackingAmbushSystem.ReaperElvenDefname = value; } }

		// ---- Anti-macro delay (#424) - SHARED with Mining/Fishing (#536: renamed with a
		// "shared" prefix so this is unmistakable in the dialog - editing this on ANY of the
		// three skills' pages changes what the other two show too, by design, one real formula). ----

		public double sharedGatherDelayBaseSeconds { get { return GatheringDelaySystem.BaseDelaySeconds; } set { GatheringDelaySystem.BaseDelaySeconds = value; } }
		public double sharedGatherDelayThresholdPercent { get { return GatheringDelaySystem.AntiMacroThresholdPercent; } set { GatheringDelaySystem.AntiMacroThresholdPercent = value; } }
		public double sharedGatherDelayReductionPerPercent { get { return GatheringDelaySystem.DelayReductionPerPercentAboveThreshold; } set { GatheringDelaySystem.DelayReductionPerPercentAboveThreshold = value; } }
		public int sharedGatherLeastStrokes { get { return GatheringDelaySystem.instance.leastStrokes; } set { GatheringDelaySystem.instance.leastStrokes = value; } }
		public int sharedGatherMostStrokes { get { return GatheringDelaySystem.instance.mostStrokes; } set { GatheringDelaySystem.instance.mostStrokes = value; } }
	}
}
