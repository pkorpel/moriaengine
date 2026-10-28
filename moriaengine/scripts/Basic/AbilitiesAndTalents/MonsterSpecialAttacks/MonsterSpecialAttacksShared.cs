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

using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

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

		/// <summary>True when `ch` exists and carries the ability `def` (null def = never).</summary>
		private static bool HasAbilityDef(Character ch, AbilityDef def) {
			Ability ability;
			return (def != null) && (ch != null) && ch.HasAbility(def, out ability);
		}

		/// <summary>The archive's standard "isPlayer || isPet" victim filter shared by the area
		/// abilities: any living, visible, non-GM player, or any NPC with a master (a tamed pet,
		/// a summon, a mount) - never a wild monster.</summary>
		private static bool IsPlayerOrPetTarget(Character target) {
			if ((target == null) || target.IsDeleted || !target.IsAliveAndValid || target.IsNotVisible) {
				return false;
			}
			if (target.IsPlayerForCombat) {
				return !target.IsGM;
			}
			NPC npc = target as NPC;
			return (npc != null) && (npc.ControlMaster != null);
		}

		/// <summary>Snapshot of the characters around a point, so a loop that damages (and so
		/// possibly deletes) them never mutates the sector list it is walking.</summary>
		private static List<Character> CharsAround(Map map, int x, int y, int range) {
			List<Character> list = new List<Character>();
			foreach (AbstractCharacter ch in map.GetCharsInRange(x, y, range)) {
				Character c = ch as Character;
				if (c != null) {
					list.Add(c);
				}
			}
			return list;
		}

		//North,NorthEast,East,SouthEast,South,SouthWest,West,NorthWest (GameCore/Enums.cs's own
		//Direction order) - used by Roundattack's PullToward and Knockback's PushAway to find the
		//one tile adjacent to (respectively, toward/away from) the attacker.
		private static readonly int[] directionDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
		private static readonly int[] directionDy = { -1, -1, 0, 1, 1, 1, 0, -1 };
	}
}
