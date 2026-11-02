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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// GM-tunable Mining settings (#337, Phase E of #332), shown as a "Mining" category in
	/// the .settings dialog - covers everything #333-336 built. Same pattern as
	/// SkillAdvancementSettings (#330): every field here is a plain pass-through PROPERTY,
	/// not stored data - each getter/setter reads/writes the real live constant elsewhere
	/// directly (OreTier instances, MiningVeinSystem/MonsterAmbushSystem/GatheringDelaySystem's
	/// own static fields), so an edit here takes effect immediately with zero extra sync step.
	/// This class has nothing of its own to persist - [SaveableClass]/[HasSavedMembers] aren't
	/// needed at all (unlike SkillAdvancementSettings, which DID need 2 real [SaveableData]
	/// fields for Lockpicking/Taming's own flat constants with no other backing store - every
	/// constant here already has one).
	///
	/// Deliberately NOT exposed as individually tunable, per #337's own "exact shape to be
	/// finalized" scope note: each ore's own item defname (not a tunable number, tied to real
	/// content) and the ambush table's per-tier WEIGHTS (the real mechanic uses fixed rand(128)
	/// bucket boundaries, not a generic weighted table - exposing those as separately editable
	/// would need restructuring PickWeightedMonsterDefname into a real weighted-lookup table,
	/// judged a bigger change than this integration-pass issue's own scope).
	/// </summary>
	[ViewableClass]
	public class MiningSettings : SettingsMetaCategory {

		public static MiningSettings instance = new MiningSettings();

		// ---- Per-ore tiers (#333) - skill threshold / pool amount range / regen minutes ----

		public int copperSkillThreshold { get { return OreTiers.Copper.SkillThreshold; } set { OreTiers.Copper.SkillThreshold = value; } }
		public int copperMinAmount { get { return OreTiers.Copper.MinAmount; } set { OreTiers.Copper.MinAmount = value; } }
		public int copperMaxAmount { get { return OreTiers.Copper.MaxAmount; } set { OreTiers.Copper.MaxAmount = value; } }
		public int copperRegenMinutes { get { return OreTiers.Copper.RegenMinutes; } set { OreTiers.Copper.RegenMinutes = value; } }

		public int ironSkillThreshold { get { return OreTiers.Iron.SkillThreshold; } set { OreTiers.Iron.SkillThreshold = value; } }
		public int ironMinAmount { get { return OreTiers.Iron.MinAmount; } set { OreTiers.Iron.MinAmount = value; } }
		public int ironMaxAmount { get { return OreTiers.Iron.MaxAmount; } set { OreTiers.Iron.MaxAmount = value; } }
		public int ironRegenMinutes { get { return OreTiers.Iron.RegenMinutes; } set { OreTiers.Iron.RegenMinutes = value; } }

		public int silverSkillThreshold { get { return OreTiers.Silver.SkillThreshold; } set { OreTiers.Silver.SkillThreshold = value; } }
		public int silverMinAmount { get { return OreTiers.Silver.MinAmount; } set { OreTiers.Silver.MinAmount = value; } }
		public int silverMaxAmount { get { return OreTiers.Silver.MaxAmount; } set { OreTiers.Silver.MaxAmount = value; } }
		public int silverRegenMinutes { get { return OreTiers.Silver.RegenMinutes; } set { OreTiers.Silver.RegenMinutes = value; } }

		public int goldSkillThreshold { get { return OreTiers.Gold.SkillThreshold; } set { OreTiers.Gold.SkillThreshold = value; } }
		public int goldMinAmount { get { return OreTiers.Gold.MinAmount; } set { OreTiers.Gold.MinAmount = value; } }
		public int goldMaxAmount { get { return OreTiers.Gold.MaxAmount; } set { OreTiers.Gold.MaxAmount = value; } }
		public int goldRegenMinutes { get { return OreTiers.Gold.RegenMinutes; } set { OreTiers.Gold.RegenMinutes = value; } }

		public int veriteSkillThreshold { get { return OreTiers.Verite.SkillThreshold; } set { OreTiers.Verite.SkillThreshold = value; } }
		public int veriteMinAmount { get { return OreTiers.Verite.MinAmount; } set { OreTiers.Verite.MinAmount = value; } }
		public int veriteMaxAmount { get { return OreTiers.Verite.MaxAmount; } set { OreTiers.Verite.MaxAmount = value; } }
		public int veriteRegenMinutes { get { return OreTiers.Verite.RegenMinutes; } set { OreTiers.Verite.RegenMinutes = value; } }

		public int valoriteSkillThreshold { get { return OreTiers.Valorite.SkillThreshold; } set { OreTiers.Valorite.SkillThreshold = value; } }
		public int valoriteMinAmount { get { return OreTiers.Valorite.MinAmount; } set { OreTiers.Valorite.MinAmount = value; } }
		public int valoriteMaxAmount { get { return OreTiers.Valorite.MaxAmount; } set { OreTiers.Valorite.MaxAmount = value; } }
		public int valoriteRegenMinutes { get { return OreTiers.Valorite.RegenMinutes; } set { OreTiers.Valorite.RegenMinutes = value; } }

		public int obsidianSkillThreshold { get { return OreTiers.Obsidian.SkillThreshold; } set { OreTiers.Obsidian.SkillThreshold = value; } }
		public int obsidianMinAmount { get { return OreTiers.Obsidian.MinAmount; } set { OreTiers.Obsidian.MinAmount = value; } }
		public int obsidianMaxAmount { get { return OreTiers.Obsidian.MaxAmount; } set { OreTiers.Obsidian.MaxAmount = value; } }
		public int obsidianRegenMinutes { get { return OreTiers.Obsidian.RegenMinutes; } set { OreTiers.Obsidian.RegenMinutes = value; } }

		public int shadowSkillThreshold { get { return OreTiers.Shadow.SkillThreshold; } set { OreTiers.Shadow.SkillThreshold = value; } }
		public int shadowMinAmount { get { return OreTiers.Shadow.MinAmount; } set { OreTiers.Shadow.MinAmount = value; } }
		public int shadowMaxAmount { get { return OreTiers.Shadow.MaxAmount; } set { OreTiers.Shadow.MaxAmount = value; } }
		public int shadowRegenMinutes { get { return OreTiers.Shadow.RegenMinutes; } set { OreTiers.Shadow.RegenMinutes = value; } }

		public int adamantinumSkillThreshold { get { return OreTiers.Adamantinum.SkillThreshold; } set { OreTiers.Adamantinum.SkillThreshold = value; } }
		public int adamantinumMinAmount { get { return OreTiers.Adamantinum.MinAmount; } set { OreTiers.Adamantinum.MinAmount = value; } }
		public int adamantinumMaxAmount { get { return OreTiers.Adamantinum.MaxAmount; } set { OreTiers.Adamantinum.MaxAmount = value; } }
		public int adamantinumRegenMinutes { get { return OreTiers.Adamantinum.RegenMinutes; } set { OreTiers.Adamantinum.RegenMinutes = value; } }

		public int mithrilSkillThreshold { get { return OreTiers.Mithril.SkillThreshold; } set { OreTiers.Mithril.SkillThreshold = value; } }
		public int mithrilMinAmount { get { return OreTiers.Mithril.MinAmount; } set { OreTiers.Mithril.MinAmount = value; } }
		public int mithrilMaxAmount { get { return OreTiers.Mithril.MaxAmount; } set { OreTiers.Mithril.MaxAmount = value; } }
		public int mithrilRegenMinutes { get { return OreTiers.Mithril.RegenMinutes; } set { OreTiers.Mithril.RegenMinutes = value; } }

		// ---- Cave-ins (#334) ----

		public int caveInChanceDenominator { get { return MiningVeinSystem.CaveInChanceDenominator; } set { MiningVeinSystem.CaveInChanceDenominator = value; } }
		public int caveInClearRollMin { get { return MiningVeinSystem.CaveInClearRollMin; } set { MiningVeinSystem.CaveInClearRollMin = value; } }
		public int caveInClearRollMax { get { return MiningVeinSystem.CaveInClearRollMax; } set { MiningVeinSystem.CaveInClearRollMax = value; } }
		public int caveInAutoClearSeconds { get { return MiningVeinSystem.CaveInAutoClearSeconds; } set { MiningVeinSystem.CaveInAutoClearSeconds = value; } }

		// ---- Monster ambush (#335) ----

		public int ambushChanceDenominator { get { return MonsterAmbushSystem.AmbushChanceDenominator; } set { MonsterAmbushSystem.AmbushChanceDenominator = value; } }
		public string ambushRatDefname { get { return MonsterAmbushSystem.RatDefname; } set { MonsterAmbushSystem.RatDefname = value; } }
		public string ambushTrollDefname { get { return MonsterAmbushSystem.TrollDefname; } set { MonsterAmbushSystem.TrollDefname = value; } }
		public string ambushEarthElementalDefname { get { return MonsterAmbushSystem.EarthElementalDefname; } set { MonsterAmbushSystem.EarthElementalDefname = value; } }
		public string ambushRockElementalDefname { get { return MonsterAmbushSystem.RockElementalDefname; } set { MonsterAmbushSystem.RockElementalDefname = value; } }
		public string ambushCopperSpiritDefname { get { return MonsterAmbushSystem.CopperSpiritDefname; } set { MonsterAmbushSystem.CopperSpiritDefname = value; } }
		public string ambushGolemDefname { get { return MonsterAmbushSystem.GolemDefname; } set { MonsterAmbushSystem.GolemDefname = value; } }

		// ---- Anti-macro delay (#336) ----

		// #536: "shared" prefix - editing this on ANY of Mining/Lumberjacking/Fishing's pages
		// changes what the other two show too, by design (one real formula, not per-skill).
		public double sharedGatherDelayBaseSeconds { get { return GatheringDelaySystem.BaseDelaySeconds; } set { GatheringDelaySystem.BaseDelaySeconds = value; } }
		public double sharedGatherDelayThresholdPercent { get { return GatheringDelaySystem.AntiMacroThresholdPercent; } set { GatheringDelaySystem.AntiMacroThresholdPercent = value; } }
		public double sharedGatherDelayReductionPerPercent { get { return GatheringDelaySystem.DelayReductionPerPercentAboveThreshold; } set { GatheringDelaySystem.DelayReductionPerPercentAboveThreshold = value; } }
		public int sharedGatherLeastStrokes { get { return GatheringDelaySystem.instance.leastStrokes; } set { GatheringDelaySystem.instance.leastStrokes = value; } }
		public int sharedGatherMostStrokes { get { return GatheringDelaySystem.instance.mostStrokes; } set { GatheringDelaySystem.instance.mostStrokes = value; } }
	}
}
