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

using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Rolled from DamageManager.ProcessSwing on every landed melee swing, or from
	/// NPCAiTimer's own periodic tick for the non-swing-triggered abilities - see each
	/// ability's own file (one per ability, under this same folder) for its specific
	/// AbilityDef + consumer logic. This file only holds the few genuinely cross-ability
	/// helpers every (or nearly every) ability file reuses.
	/// </summary>
	public static partial class MonsterSpecialAttacks {
		/// <summary>Rolls `permille` chance in [0, 1000) - e.g. 250 means 25%.</summary>
		private static bool RollChance(double permille) {
			return Globals.dice.Next(1000) < permille;
		}

		/// <summary>Resolves a tunable range: the attacker's own NPCDef override if configured
		/// (per-species, e.g. NPCDef.StunChance), otherwise the granted AbilityDef's own default
		/// field. `npcOverride` is null when that specific NPCDef never set the override.</summary>
		private static double RollTunable(int[] npcOverride, double[] abilityDefault) {
			if (npcOverride != null) {
				return NPCDef.RollStatRange(npcOverride);
			}
			return ScriptUtil.GetRandInRange(abilityDefault[0], abilityDefault[1]);
		}

		//North,NorthEast,East,SouthEast,South,SouthWest,West,NorthWest (GameCore/Enums.cs's own
		//Direction order) - used by Roundattack's PullToward and Knockback's PushAway to find the
		//one tile adjacent to (respectively, toward/away from) the attacker.
		private static readonly int[] directionDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
		private static readonly int[] directionDy = { -1, -1, 0, 1, 1, 1, 0, -1 };
	}
}
