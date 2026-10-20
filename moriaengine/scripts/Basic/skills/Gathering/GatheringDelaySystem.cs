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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Mining's anti-macro delay scaling (#336, Phase D of #332) - real Erebor's own
	/// `f_mine_checkDelayer`/`f_return_delay_bonus`/`f_count_delay_bonus` (miningfix.scp): a
	/// flat base delay between gather attempts, reduced once skill exceeds 100.0% -
	/// "1% above 100 = 0.03s less delay" (the real source's own comment). The real function
	/// applies the identical formula to Fishing and Lumberjacking too (`f_return_delay_bonus`
	/// branches on all three skills) - built as a reusable, skill-name-parameterized utility
	/// for that reason.
	///
	/// Base delay default changed 20.0 -> 2.0 (#533) per direct user request - this engine's
	/// own gameplay feel, not a faithfulness claim about real Erebor's actual 20s value.
	///
	/// Persistence (#533): this used to be a plain `static class` with bare mutable static
	/// fields - a live `.settings` edit took effect immediately but was never actually saved,
	/// silently resetting to the hardcoded default on every restart (unlike CombatSettings/
	/// RangerSettings/DecaySettings, which DO use this same [SaveableClass] pipeline). Converted
	/// to a real persisted singleton; the original static field/method API is kept unchanged as
	/// pass-through wrappers so every existing call site (MiningSkillDef/LumberjackingSkillDef/
	/// FishingSkillDef's On_Start, the 3 sibling *Settings classes' pass-through properties, and
	/// the test suite) needed zero changes. Doesn't need to inherit SettingsMetaCategory/
	/// [ViewableClass] itself - it's not its own dialog category, Mining/Fishing/Lumberjacking's
	/// settings categories already expose it via their own pass-through properties.
	///
	/// Known broader gap, NOT fixed here (flagging, not silently dropping, same convention as
	/// MiningVeinSystem's own per-tile-state note): OreTiers/AmbushSystem defnames+chances/
	/// FishingCatchSystem's junk-chances have the identical unpersisted-static-field problem -
	/// out of this issue's scope, which was specifically about gather delay.
	///
	/// Architecture note, worth remembering if this pattern looks reusable for something else:
	/// `SkillDef.Delay`/`GetDelayForChar`'s existing `EvalRangePermille` 2/3-point curve
	/// mechanism (the exact same shape #328 reused for `AdvRate`) does NOT fit this mechanic,
	/// despite the issue's own suggestion to check it first - confirmed by reading
	/// `ScriptUtil.EvalRangePermille` directly: it always divides by a fixed 1000 (skill's own
	/// 0-100.0% scale), so the curve's own "100%" endpoint IS skill value 1000 - there's no way
	/// to express "flat below 100%, decreasing ABOVE 100%, uncapped" using a bounded percentage
	/// curve whose own domain ends exactly at that same threshold. Implemented as its own small
	/// direct formula instead.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	public class GatheringDelaySystem {

		[SavedMember]
		public static GatheringDelaySystem instance = new GatheringDelaySystem();

		[LoadingInitializer]
		public GatheringDelaySystem() {
		}

		[SaveableData]
		public double baseDelaySeconds = 2.0; //#533 - was 20.0, changed per user request

		[SaveableData]
		public double antiMacroThresholdPercent = 100.0;

		[SaveableData]
		public double delayReductionPerPercentAboveThreshold = 0.03;

		//Backward-compatible static API - unchanged signatures, now backed by the persisted
		//instance above instead of bare static fields.
		public static double BaseDelaySeconds {
			get { return instance.baseDelaySeconds; }
			set { instance.baseDelaySeconds = value; }
		}

		public static double AntiMacroThresholdPercent {
			get { return instance.antiMacroThresholdPercent; }
			set { instance.antiMacroThresholdPercent = value; }
		}

		public static double DelayReductionPerPercentAboveThreshold {
			get { return instance.delayReductionPerPercentAboveThreshold; }
			set { instance.delayReductionPerPercentAboveThreshold = value; }
		}

		/// <summary>The real delay (seconds) between gather attempts for `self` at `skillName`
		/// - thin wrapper around the pure formula below, extracting the skill's own display
		/// percentage (internal values are stored x10).</summary>
		public static double GetDelaySeconds(Character self, SkillName skillName) {
			return GetDelaySecondsForSkillPercent(self.GetRealSkillValue(skillName) / 10.0);
		}

		/// <summary>The pure formula itself, independent of any live Character - flat
		/// BaseDelaySeconds at or below the threshold, linearly reduced above it, never
		/// negative. Split out so this can be unit-tested without a full server boot.</summary>
		public static double GetDelaySecondsForSkillPercent(double skillPercent) {
			if (skillPercent <= AntiMacroThresholdPercent) {
				return BaseDelaySeconds;
			}

			double reduction = (skillPercent - AntiMacroThresholdPercent) * DelayReductionPerPercentAboveThreshold;
			return Math.Max(0, BaseDelaySeconds - reduction);
		}
	}
}
