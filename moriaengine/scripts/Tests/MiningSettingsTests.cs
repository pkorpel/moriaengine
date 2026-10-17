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

	/// <summary>Server-side helpers for MiningSettings write-through tests (#337) - same
	/// "test the write-through logic itself, not the generic settings-dialog plumbing"
	/// scope decision as SkillAdvancementSettingsTests (#330).</summary>
	public static class MiningSettingsTests {

		[SteamFunction]
		public static void TestSetCopperAmountRangeViaSettings(Character self, ScriptArgs sa) {
			int min = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int max = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			MiningSettings.instance.copperMinAmount = min;
			MiningSettings.instance.copperMaxAmount = max;
			Console.WriteLine("MINING_SETTINGS_COPPER_SET: min=" + min + " max=" + max);
		}

		/// <summary>Reads OreTiers.Copper's own fields directly, bypassing MiningSettings
		/// entirely - confirms the settings property wrote through to the real shared
		/// singleton instance, not a local copy (same TestReportAdvRate-style independent-
		/// reader precedent as #330).</summary>
		[SteamFunction]
		public static void TestReportCopperTierDirectly(Character self) {
			Console.WriteLine("ORE_TIER_REPORT: min=" + OreTiers.Copper.MinAmount + " max=" + OreTiers.Copper.MaxAmount);
		}

		[SteamFunction]
		public static void TestSetCaveInChanceViaSettings(Character self, ScriptArgs sa) {
			int denominator = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			MiningSettings.instance.caveInChanceDenominator = denominator;
			Console.WriteLine("MINING_SETTINGS_CAVEIN_SET: denominator=" + MiningVeinSystem.CaveInChanceDenominator);
		}

		[SteamFunction]
		public static void TestSetAmbushRatDefnameViaSettings(Character self, ScriptArgs sa) {
			string defname = Convert.ToString(sa.Argv[0], CultureInfo.InvariantCulture);
			MiningSettings.instance.ambushRatDefname = defname;
			Console.WriteLine("MINING_SETTINGS_AMBUSH_SET: ratDefname=" + MonsterAmbushSystem.RatDefname);
		}

		[SteamFunction]
		public static void TestSetGatherDelayBaseViaSettings(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			MiningSettings.instance.gatherDelayBaseSeconds = seconds;
			Console.WriteLine("MINING_SETTINGS_DELAY_SET: baseSeconds=" + GatheringDelaySystem.BaseDelaySeconds);
		}
	}
}
