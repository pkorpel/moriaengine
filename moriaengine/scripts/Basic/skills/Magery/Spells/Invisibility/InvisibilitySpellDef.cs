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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#489 - Invisibility: Character.Flag_InvisByMagic already existed and was
	/// fully wired into IsNotVisible/CanSeeVisibility (only GMs see through it), but nothing
	/// anywhere set it. Mirrors Paralyze's "timed character effect" shape, but as a plain
	/// DurableSpellDef + BoundTimer rather than a new EffectDurationPlugin subclass (like
	/// ParalyzeEffectPlugin) - a new Plugin class needs its own generated code-behind (produced
	/// by the Cruncher template tool from a .ct file, not run by plain dotnet build - same class
	/// of issue as the Generated/*.cs gotcha elsewhere in this codebase), which a BoundTimer
	/// (a plain hand-written class, same pattern as SpellEffectItem's own SpellEffectTimer)
	/// avoids entirely. Re-casting on an already-invisible target refreshes the duration (old
	/// timer deleted before the new one is added), matching DurableCharEffectSpellDef's own
	/// delete-then-reassign pattern for its plugin-based effects.</summary>
	[ViewableClass]
	public class InvisibilitySpellDef : DurableSpellDef {
		private static readonly TimerKey timerKey = TimerKey.Acquire("_invisibility_");

		public InvisibilitySpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>#923 - for .boosts.</summary>
		public static double SecondsLeft(Character target) {
			return target.GetTimer(timerKey)?.DueInSeconds ?? -1;
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			target.DeleteTimer(timerKey);
			target.Flag_InvisByMagic = true;
			target.SysMessage("You fade from view.");

			double durationSecs = this.GetDurationForValue(spellEffectArgs.SpellPower);
			BoundTimer timer = new InvisibilityExpiryTimer();
			target.AddTimer(timerKey, timer);
			timer.DueInSeconds = durationSecs;
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class InvisibilityExpiryTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public InvisibilityExpiryTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				Character character = cont as Character;
				if (character != null) {
					character.Flag_InvisByMagic = false;
					character.SysMessage("You are no longer invisible.");
				}
			}
		}
	}
}
