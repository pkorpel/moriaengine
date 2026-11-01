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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SteamEngine.CompiledScripts {

	/// <summary>#826 - who a character has been fighting, like Sphere's MEMORY_FIGHT: both sides of every hostile act
	/// (Character.Trigger_HostileAction) remember each other. Erebor's spk_guard sends guards after the caller's fight
	/// opponent. Transient on purpose (not saved): a fight doesn't outlive a restart.</summary>
	public static class FightMemory {
		private sealed class Entry {
			internal Character opponent;
			internal TimeSpan when;
			internal readonly Dictionary<Character, TimeSpan> everyone = new Dictionary<Character, TimeSpan>();
		}

		private static readonly ConditionalWeakTable<Character, Entry> memories = new ConditionalWeakTable<Character, Entry>();

		private static Character HealerOf(Character healed) {
			object until = healed.GetTag(Bandaging.healedUntilTK);
			if (until == null) {
				return null;
			}
			if ((double) until > Globals.TimeInSeconds) {
				return healed.GetTag(Bandaging.healedByTK) as Character;
			}
			healed.RemoveTag(Bandaging.healedUntilTK);
			healed.RemoveTag(Bandaging.healedByTK);
			return null;
		}

		public static void Record(Character victim, Character enemy) {
			if ((victim == null) || (enemy == null) || (victim == enemy)) {
				return;
			}
			TimeSpan now = Globals.TimeAsSpan;
			Remember(victim, enemy, now);
			Remember(enemy, victim, now);
			//#962 - f_addAllFightsForChar: a priest who healed one side shares in what the other side falls to
			Character healer = HealerOf(enemy);
			if (healer != null) {
				Remember(victim, healer, now);
			} else {
				healer = HealerOf(victim);
				if (healer != null) {
					Remember(enemy, healer, now);
				}
			}
		}

		private static void Remember(Character self, Character opponent, TimeSpan now) {
			Entry entry = memories.GetOrCreateValue(self);
			entry.opponent = opponent;
			entry.when = now;
			entry.everyone[opponent] = now;
		}

		/// <summary>#902 - everyone `self` has fought within `maxAge`, as Sphere's fight memories: who shares
		/// the experience and the kill when `self` dies.</summary>
		public static List<Character> GetFighters(Character self, TimeSpan maxAge) {
			List<Character> fighters = new List<Character>();
			Entry entry;
			if ((self != null) && memories.TryGetValue(self, out entry)) {
				TimeSpan now = Globals.TimeAsSpan;
				foreach (KeyValuePair<Character, TimeSpan> fight in entry.everyone) {
					if (!fight.Key.IsDeleted && ((now - fight.Value) <= maxAge)) {
						fighters.Add(fight.Key);
					}
				}
			}
			return fighters;
		}

		/// <summary>The character `self` most recently fought with, if that was within `maxAge` and the opponent
		/// is still alive and in the world.</summary>
		public static Character GetOpponent(Character self, TimeSpan maxAge) {
			Entry entry;
			if ((self == null) || !memories.TryGetValue(self, out entry)) {
				return null;
			}
			Character opponent = entry.opponent;
			if ((opponent == null) || opponent.IsDeleted || !opponent.IsAliveAndValid) {
				return null;
			}
			if ((Globals.TimeAsSpan - entry.when) > maxAge) {
				return null;
			}
			return opponent;
		}

		public static void Forget(Character self) {
			if (self != null) {
				memories.Remove(self);
			}
		}
	}
}
