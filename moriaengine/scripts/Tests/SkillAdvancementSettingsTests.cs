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

	/// <summary>
	/// Server-side helpers for SkillAdvancementSettings write-through tests (#330). Drives the
	/// settings properties directly (not through a real .settings gump round-trip - the generic
	/// ViewableClassGenerator-based editing mechanism these properties rely on is independently
	/// verified by every other existing settings category already in production, and no protocol
	/// test anywhere in this codebase currently drives a real settings-dialog edit end to end;
	/// building that from scratch was judged not worth it for this one new category - what's
	/// actually new and worth testing here is the write-through logic itself, not the generic
	/// dialog plumbing).
	/// </summary>
	public static class SkillAdvancementSettingsTests {

		[SteamFunction]
		public static void TestReportCustomFormulaDefaults(Character self) {
			Console.WriteLine("ADV_SETTINGS_CUSTOM_DEFAULTS: lockpicking=" + SkillAdvancementSettings.instance.lockpickingAdvRate +
				" taming=" + SkillAdvancementSettings.instance.tamingAdvRate);
		}

		/// <summary>Sets Mining's 3-point curve via the settings property, then relies on the
		/// caller following up with the pre-existing .TestReportAdvRate (#328) to confirm the
		/// live SkillDef.AdvRate array actually changed - proves write-through, not just that
		/// the property itself stored something.</summary>
		[SteamFunction]
		public static void TestSetMiningAdvRateViaSettings(Character self, ScriptArgs sa) {
			double low = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			double mid = Convert.ToDouble(sa.Argv[1], CultureInfo.InvariantCulture);
			double high = Convert.ToDouble(sa.Argv[2], CultureInfo.InvariantCulture);
			SkillAdvancementSettings.instance.miningLow = low;
			SkillAdvancementSettings.instance.miningMid = mid;
			SkillAdvancementSettings.instance.miningHigh = high;
			Console.WriteLine("ADV_SETTINGS_MINING_SET: low=" + low + " mid=" + mid + " high=" + high);
		}

		/// <summary>Sets Lockpicking's custom-formula advrate via the settings property - for
		/// this one there's nothing separate to "push" into (Lockpicking.cs itself reads this
		/// exact property directly), so reading it back IS the full proof.</summary>
		[SteamFunction]
		public static void TestSetLockpickingAdvRateViaSettings(Character self, ScriptArgs sa) {
			double rate = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			SkillAdvancementSettings.instance.lockpickingAdvRate = rate;
			Console.WriteLine("ADV_SETTINGS_LOCKPICKING_SET: rate=" + SkillAdvancementSettings.instance.lockpickingAdvRate);
		}
	}
}
