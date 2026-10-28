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

using SteamEngine.Persistence;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_leaper. User's explicit
	/// choice: the real archive mechanic (short teleport hops toward the target, bypassing real
	/// pathfinding) is deliberately simplified to a real speed boost instead - this NPC just
	/// takes extra pathfinding steps per NPCAiTimer think-tick while closing distance, using the
	/// SAME honest StepTowardsSmart A* movement every other NPC uses, never teleporting/
	/// clipping through walls. Default bumped to 10 (user's explicit call, faster than the
	/// real-running-player-speed math below would suggest) - the NPC AI tick is 1.5s and all
	/// of a tick's extra steps fire in one burst, while a real running player covers 1 tile
	/// per 0.18s (MovementState.RunStepTime, ~5.56 tiles/sec) - 1 normal + 7 extra per tick
	/// was the closest whole-step match to THAT pace; 10 extra (~7.33 tiles/sec) deliberately
	/// exceeds it.</summary>
	[ViewableClass]
	public class LeaperAbilityDef : PassiveAbilityDef {
		private FieldValue extraSteps;

		public LeaperAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.extraSteps = this.InitTypedField("leaperExtraSteps", (double) 10, typeof(double));
		}

		/// <summary>How many EXTRA pathfinding steps (on top of the normal one) this NPC takes
		/// per think-tick while approaching a target.</summary>
		public double ExtraSteps {
			get { return (double) this.extraSteps.CurrentValue; }
			set { this.extraSteps.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static LeaperAbilityDef leaperAbilityDef;
		internal static LeaperAbilityDef LeaperDef {
			get {
				if (leaperAbilityDef == null) {
					leaperAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_leaper") as LeaperAbilityDef;
				}
				return leaperAbilityDef;
			}
		}

		/// <summary>#leaper (user-approved simplification - see LeaperAbilityDef's own doc
		/// comment) - how many EXTRA honest StepTowardsSmart pathfinding steps this NPC should
		/// take this think-tick while closing distance, on top of the normal one. 0 if `self`
		/// was never granted a_tevent_e_npc_leaper.</summary>
		public static int GetLeaperExtraSteps(NPC self) {
			LeaperAbilityDef def = LeaperDef;
			if (def == null) {
				return 0;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return 0;
			}
			return (int) def.ExtraSteps;
		}
	}
}
