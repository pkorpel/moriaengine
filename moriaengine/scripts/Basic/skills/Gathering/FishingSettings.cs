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
	/// GM-tunable Fishing settings (#271), shown as a "Fishing" category in the .settings dialog -
	/// covers everything #271 built. Same pass-through-property shape as MiningSettings (#337) /
	/// LumberjackingSettings (#425): every field reads/writes the real live constant elsewhere
	/// directly (FishingCatchSystem's / FishingAmbushSystem's static fields, the shared
	/// GatheringDelaySystem), so an edit here takes effect immediately with zero extra sync step.
	/// Nothing of its own to persist - every constant already has its own backing store, same
	/// reasoning as the sibling settings categories.
	///
	/// The gatherDelay* fields point at GatheringDelaySystem's own static fields, which are SHARED
	/// across Mining/Fishing/Lumberjacking (one real formula, not per-skill) - editing them here
	/// also changes Mining's/Lumberjacking's delay and vice versa. Same shared-state characteristic
	/// the sibling categories already have (they expose the identical fields), kept for
	/// consistency rather than omitted.
	///
	/// Deliberately NOT exposed as individually tunable, same scope note the sibling categories
	/// follow: the fish-variety item defnames (content-tied, not tuning numbers) and the ambush
	/// table's per-category WEIGHTS (fixed rand(128) bucket boundaries, not a generic weighted
	/// table - see FishingAmbushSystem's own doc comment).
	/// </summary>
	[ViewableClass]
	public class FishingSettings : SettingsMetaCategory {

		public static FishingSettings instance = new FishingSettings();

		// ---- Junk-fish / treasure-map catch (#271) ----

		public int junkChanceNumerator { get { return FishingCatchSystem.JunkChanceNumerator; } set { FishingCatchSystem.JunkChanceNumerator = value; } }
		public int junkChanceDenominator { get { return FishingCatchSystem.JunkChanceDenominator; } set { FishingCatchSystem.JunkChanceDenominator = value; } }
		public int treasureMapChanceNumerator { get { return FishingCatchSystem.TreasureMapChanceNumerator; } set { FishingCatchSystem.TreasureMapChanceNumerator = value; } }
		public int treasureMapChanceDenominator { get { return FishingCatchSystem.TreasureMapChanceDenominator; } set { FishingCatchSystem.TreasureMapChanceDenominator = value; } }
		public string junkCrateDefname { get { return FishingCatchSystem.JunkCrateDefname; } set { FishingCatchSystem.JunkCrateDefname = value; } }
		public string treasureMapDefname { get { return FishingCatchSystem.TreasureMapDefname; } set { FishingCatchSystem.TreasureMapDefname = value; } }

		// ---- Monster ambush (#271) ----

		public int ambushChanceDenominator { get { return FishingAmbushSystem.AmbushChanceDenominator; } set { FishingAmbushSystem.AmbushChanceDenominator = value; } }
		public string ambushFrogDefname { get { return FishingAmbushSystem.FrogDefname; } set { FishingAmbushSystem.FrogDefname = value; } }
		public string ambushSeaSerpentDefname { get { return FishingAmbushSystem.SeaSerpentDefname; } set { FishingAmbushSystem.SeaSerpentDefname = value; } }
		public string ambushKrakenDefname { get { return FishingAmbushSystem.KrakenDefname; } set { FishingAmbushSystem.KrakenDefname = value; } }
		public string ambushWaterElementalDefname { get { return FishingAmbushSystem.WaterElementalDefname; } set { FishingAmbushSystem.WaterElementalDefname = value; } }
		public string ambushWaterSpiritDefname { get { return FishingAmbushSystem.WaterSpiritDefname; } set { FishingAmbushSystem.WaterSpiritDefname = value; } }

		// ---- Anti-macro delay (#271) - shared with Mining/Lumberjacking, see class doc comment ----

		// #536: "shared" prefix - editing this on ANY of Mining/Lumberjacking/Fishing's pages
		// changes what the other two show too, by design (one real formula, not per-skill).
		public double sharedGatherDelayBaseSeconds { get { return GatheringDelaySystem.BaseDelaySeconds; } set { GatheringDelaySystem.BaseDelaySeconds = value; } }
		public double sharedGatherDelayThresholdPercent { get { return GatheringDelaySystem.AntiMacroThresholdPercent; } set { GatheringDelaySystem.AntiMacroThresholdPercent = value; } }
		public double sharedGatherDelayReductionPerPercent { get { return GatheringDelaySystem.DelayReductionPerPercentAboveThreshold; } set { GatheringDelaySystem.DelayReductionPerPercentAboveThreshold = value; } }
	}
}
