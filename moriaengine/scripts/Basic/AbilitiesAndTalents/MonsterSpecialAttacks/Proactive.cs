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

namespace SteamEngine.CompiledScripts {

	/// <summary>#1146 - proactive monsters, the user's design beyond Erebor: the boss reactions of #1022 (dragons, summoning
	/// bosses, lava bomb, OCS raising, the helm's cry, Darkmoon's forms, the shapeshifters, Halda Molle's frost and
	/// warniaks) also roll on the monster's own initiative against its target, every NpcAggroSettings.monsterInitiativeSeconds
	/// while it fights, and when its own blow lands. Each at its own odds and cooldowns, as if it had been hit. Some stay
	/// reactions only: the Balrog's reflection, Bezejmenny's split, Arghelior's per-blow lines, the guardian's warrior
	/// line. NpcAggroSettings.monsterInitiativeOff lists more by defname.</summary>
	public static partial class MonsterSpecialAttacks {
		[ThreadStatic]
		private static bool rollingInitiative;

		internal static readonly TagKey nextInitiativeTK = TagKey.Acquire("_npcNextInitiative_");

		/// <summary>The reaction abilities' gate: the monster has it, and when it acts on its own initiative, the ability may.</summary>
		private static bool Reacts(Character ch, AbilityDef def) {
			return HasAbilityDef(ch, def) && (!rollingInitiative || InitiativeAllows(def));
		}

		internal static bool InitiativeAllows(AbilityDef def) {
			if ((def == BalrogDef) || (def == RozdeleniDef) || (def == BeridorNecroDef) || (def == SandGuardianDef)) {
				return false;
			}
			string off = NpcAggroSettings.instance.monsterInitiativeOff;
			if (string.IsNullOrEmpty(off)) {
				return true;
			}
			foreach (string name in off.Split(',')) {
				if (string.Equals(name.Trim(), def.PrettyDefname, StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}
			return true;
		}

		/// <summary>NPCAiTimer.Fight: the timed initiative.</summary>
		internal static bool TryInitiativeTick(NPC self, Character target) {
			double seconds = NpcAggroSettings.instance.monsterInitiativeSeconds;
			if ((seconds <= 0) || (target == null) || !target.IsAliveAndValid || !self.IsAliveAndValid) {
				return false;
			}
			double now = Globals.TimeInSeconds;
			if (!(self.GetTag(nextInitiativeTK) is double next)) {
				self.SetTag(nextInitiativeTK, now + seconds); //the first roll a full interval into the fight
				return false;
			}
			if (next > now) {
				return false;
			}
			self.SetTag(nextInitiativeTK, now + seconds);
			TryInitiative(self, target);
			return true;
		}

		/// <summary>TryReactToOwnSwing: its own landed blow.</summary>
		private static void TryInitiativeOnOwnBlow(Character attacker, Character defender) {
			if (NpcAggroSettings.instance.monsterInitiativeOnOwnBlows && (attacker is NPC self) && !defender.IsDeleted && defender.IsAliveAndValid) {
				TryInitiative(self, defender);
			}
		}

		/// <summary>Every reaction the monster has, against its target, as if the target had hit it.</summary>
		internal static void TryInitiative(NPC self, Character target) {
			if (rollingInitiative) {
				return;
			}
			rollingInitiative = true;
			try {
				TryDragonReactions(target, self);
				if (!self.IsDeleted) {
					TrySummonerReactions(self, target);
				}
				if (!self.IsDeleted) {
					TryDeathBossReactions(self, target);
				}
				if (!self.IsDeleted) {
					TryDamageRuleReactions(self, target);
				}
				if (!self.IsDeleted) {
					TryDarkmoonMorph(self);
				}
				if (!self.IsDeleted) {
					TryHaldaMolleReactions(self, target);
				}
				if (!self.IsDeleted) {
					TryShapeshift(self);
				}
			} finally {
				rollingInitiative = false;
			}
		}
	}
}
