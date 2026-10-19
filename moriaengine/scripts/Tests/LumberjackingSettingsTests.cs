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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helpers for LumberjackingSettings write-through tests (#425) - same
	/// "test the write-through logic itself, not the generic settings-dialog plumbing" scope
	/// decision as MiningSettingsTests (#337).</summary>
	public static class LumberjackingSettingsTests {

		[SteamFunction]
		public static void TestSetCopperAmountRangeViaLumberjackingSettings(Character self, ScriptArgs sa) {
			int min = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int max = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.copperMinAmount = min;
			LumberjackingSettings.instance.copperMaxAmount = max;
			Console.WriteLine("LUMBERJACKING_SETTINGS_COPPER_SET: min=" + min + " max=" + max);
		}

		/// <summary>Reads WoodTiers.Copper's own fields directly, bypassing LumberjackingSettings
		/// entirely - confirms the settings property wrote through to the real shared singleton
		/// instance, not a local copy (same TestReportCopperTierDirectly-style independent-
		/// reader precedent as #337).</summary>
		[SteamFunction]
		public static void TestReportWoodTierDirectly(Character self) {
			Console.WriteLine("WOOD_TIER_REPORT: min=" + WoodTiers.Copper.MinAmount + " max=" + WoodTiers.Copper.MaxAmount);
		}

		[SteamFunction]
		public static void TestSetAmbushChanceViaLumberjackingSettings(Character self, ScriptArgs sa) {
			int denominator = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.ambushChanceDenominator = denominator;
			Console.WriteLine("LUMBERJACKING_SETTINGS_AMBUSH_SET: denominator=" + LumberjackingAmbushSystem.AmbushChanceDenominator);
		}

		[SteamFunction]
		public static void TestSetAmbushWolfDefnameViaLumberjackingSettings(Character self, ScriptArgs sa) {
			string defname = Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.ambushWolfDefname = defname;
			Console.WriteLine("LUMBERJACKING_SETTINGS_AMBUSH_WOLF_SET: wolfDefname=" + LumberjackingAmbushSystem.WolfDefname);
		}

		[SteamFunction]
		public static void TestSetGatherDelayBaseViaLumberjackingSettings(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.gatherDelayBaseSeconds = seconds;
			Console.WriteLine("LUMBERJACKING_SETTINGS_DELAY_SET: baseSeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}
	}
}
