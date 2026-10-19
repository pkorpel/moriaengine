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

		public int copperSkillThreshold { get { return WoodTiers.Copper.SkillThreshold; } set { WoodTiers.Copper.SkillThreshold = value; } }
		public int copperMinAmount { get { return WoodTiers.Copper.MinAmount; } set { WoodTiers.Copper.MinAmount = value; } }
		public int copperMaxAmount { get { return WoodTiers.Copper.MaxAmount; } set { WoodTiers.Copper.MaxAmount = value; } }
		public int copperRegenMinutes { get { return WoodTiers.Copper.RegenMinutes; } set { WoodTiers.Copper.RegenMinutes = value; } }

		public int ironSkillThreshold { get { return WoodTiers.Iron.SkillThreshold; } set { WoodTiers.Iron.SkillThreshold = value; } }
		public int ironMinAmount { get { return WoodTiers.Iron.MinAmount; } set { WoodTiers.Iron.MinAmount = value; } }
		public int ironMaxAmount { get { return WoodTiers.Iron.MaxAmount; } set { WoodTiers.Iron.MaxAmount = value; } }
		public int ironRegenMinutes { get { return WoodTiers.Iron.RegenMinutes; } set { WoodTiers.Iron.RegenMinutes = value; } }

		public int veriteSkillThreshold { get { return WoodTiers.Verite.SkillThreshold; } set { WoodTiers.Verite.SkillThreshold = value; } }
		public int veriteMinAmount { get { return WoodTiers.Verite.MinAmount; } set { WoodTiers.Verite.MinAmount = value; } }
		public int veriteMaxAmount { get { return WoodTiers.Verite.MaxAmount; } set { WoodTiers.Verite.MaxAmount = value; } }
		public int veriteRegenMinutes { get { return WoodTiers.Verite.RegenMinutes; } set { WoodTiers.Verite.RegenMinutes = value; } }

		public int valoriteSkillThreshold { get { return WoodTiers.Valorite.SkillThreshold; } set { WoodTiers.Valorite.SkillThreshold = value; } }
		public int valoriteMinAmount { get { return WoodTiers.Valorite.MinAmount; } set { WoodTiers.Valorite.MinAmount = value; } }
		public int valoriteMaxAmount { get { return WoodTiers.Valorite.MaxAmount; } set { WoodTiers.Valorite.MaxAmount = value; } }
		public int valoriteRegenMinutes { get { return WoodTiers.Valorite.RegenMinutes; } set { WoodTiers.Valorite.RegenMinutes = value; } }

		public int obsidianSkillThreshold { get { return WoodTiers.Obsidian.SkillThreshold; } set { WoodTiers.Obsidian.SkillThreshold = value; } }
		public int obsidianMinAmount { get { return WoodTiers.Obsidian.MinAmount; } set { WoodTiers.Obsidian.MinAmount = value; } }
		public int obsidianMaxAmount { get { return WoodTiers.Obsidian.MaxAmount; } set { WoodTiers.Obsidian.MaxAmount = value; } }
		public int obsidianRegenMinutes { get { return WoodTiers.Obsidian.RegenMinutes; } set { WoodTiers.Obsidian.RegenMinutes = value; } }

		public int adamantinumSkillThreshold { get { return WoodTiers.Adamantinum.SkillThreshold; } set { WoodTiers.Adamantinum.SkillThreshold = value; } }
		public int adamantinumMinAmount { get { return WoodTiers.Adamantinum.MinAmount; } set { WoodTiers.Adamantinum.MinAmount = value; } }
		public int adamantinumMaxAmount { get { return WoodTiers.Adamantinum.MaxAmount; } set { WoodTiers.Adamantinum.MaxAmount = value; } }
		public int adamantinumRegenMinutes { get { return WoodTiers.Adamantinum.RegenMinutes; } set { WoodTiers.Adamantinum.RegenMinutes = value; } }

		public int mithrilSkillThreshold { get { return WoodTiers.Mithril.SkillThreshold; } set { WoodTiers.Mithril.SkillThreshold = value; } }
		public int mithrilMinAmount { get { return WoodTiers.Mithril.MinAmount; } set { WoodTiers.Mithril.MinAmount = value; } }
		public int mithrilMaxAmount { get { return WoodTiers.Mithril.MaxAmount; } set { WoodTiers.Mithril.MaxAmount = value; } }
		public int mithrilRegenMinutes { get { return WoodTiers.Mithril.RegenMinutes; } set { WoodTiers.Mithril.RegenMinutes = value; } }

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

		// ---- Anti-macro delay (#424) - shared with Mining/Fishing, see class doc comment ----

		public double gatherDelayBaseSeconds { get { return GatheringDelaySystem.BaseDelaySeconds; } set { GatheringDelaySystem.BaseDelaySeconds = value; } }
		public double gatherDelayThresholdPercent { get { return GatheringDelaySystem.AntiMacroThresholdPercent; } set { GatheringDelaySystem.AntiMacroThresholdPercent = value; } }
		public double gatherDelayReductionPerPercent { get { return GatheringDelaySystem.DelayReductionPerPercentAboveThreshold; } set { GatheringDelaySystem.DelayReductionPerPercentAboveThreshold = value; } }
	}
}
