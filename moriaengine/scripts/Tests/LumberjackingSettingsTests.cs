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

		/// <summary>#536 - renamed from "Copper" to "Spruce" (the real wood name - i_log_copper
		/// displays as "spruce log", see WoodTiers' own doc comment).</summary>
		[SteamFunction]
		public static void TestSetSpruceAmountRangeViaLumberjackingSettings(Character self, ScriptArgs sa) {
			int min = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int max = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.spruceMinAmount = min;
			LumberjackingSettings.instance.spruceMaxAmount = max;
			Console.WriteLine("LUMBERJACKING_SETTINGS_SPRUCE_SET: min=" + min + " max=" + max);
		}

		/// <summary>Reads WoodTiers.Spruce's own fields directly, bypassing LumberjackingSettings
		/// entirely - confirms the settings property wrote through to the real shared singleton
		/// instance, not a local copy (same TestReportCopperTierDirectly-style independent-
		/// reader precedent as #337).</summary>
		[SteamFunction]
		public static void TestReportWoodTierDirectly(Character self) {
			Console.WriteLine("WOOD_TIER_REPORT: min=" + WoodTiers.Spruce.MinAmount + " max=" + WoodTiers.Spruce.MaxAmount);
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

		/// <summary>#536 - renamed to "Shared" to make the cross-skill sharing with Mining/
		/// Fishing explicit in the dialog (was a source of real GM confusion - editing one
		/// skill's delay silently changed what the OTHER skills' pages showed too).</summary>
		[SteamFunction]
		public static void TestSetSharedGatherDelayBaseViaLumberjackingSettings(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			LumberjackingSettings.instance.sharedGatherDelayBaseSeconds = seconds;
			Console.WriteLine("LUMBERJACKING_SETTINGS_DELAY_SET: baseSeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}
	}
}
