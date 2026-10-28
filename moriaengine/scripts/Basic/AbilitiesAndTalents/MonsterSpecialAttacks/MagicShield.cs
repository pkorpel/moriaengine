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
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_npc_magicshield (11 monsters). Archive: a magic shield swallows
	/// tag.npc_magicShield percent (default 90) of every swing's damage. A Mystic's mind power
	/// pierces it: the shield is weaker by their level / 200 (a level-200 Mystic ignores it).
	/// The archive also drops the shield while the monster is silenced - this engine has no
	/// silence effect yet, so that part has nothing to hook into.</summary>
	[ViewableClass]
	public class MagicShieldAbilityDef : PassiveAbilityDef {
		private FieldValue strength;
		private FieldValue mysticPierceLevel;

		public MagicShieldAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.strength = this.InitTypedField("magicShieldStrength", new double[] { 90, 90 }, typeof(double[]));
			this.mysticPierceLevel = this.InitTypedField("magicShieldMysticPierceLevel", 200, typeof(int));
		}

		/// <summary>Percent of each swing's damage absorbed.</summary>
		public double[] Strength {
			get { return (double[]) this.strength.CurrentValue; }
			set { this.strength.CurrentValue = value; }
		}

		/// <summary>A Mystic of this level ignores the shield completely; lower levels pierce
		/// it proportionally.</summary>
		public int MysticPierceLevel {
			get { return (int) this.mysticPierceLevel.CurrentValue; }
			set { this.mysticPierceLevel.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static MagicShieldAbilityDef magicShieldAbilityDef;
		internal static MagicShieldAbilityDef MagicShieldDef {
			get {
				if (magicShieldAbilityDef == null) {
					magicShieldAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_magicshield") as MagicShieldAbilityDef;
				}
				return magicShieldAbilityDef;
			}
		}

		/// <summary>#797 e_npc_magicshield - defender side, before CauseDamage (mutates
		/// swingArgs.DamageAfterAC, same timing as critic).</summary>
		public static void TryApplyMagicShield(Character attacker, Character defender, WeaponSwingArgs swingArgs) {
			MagicShieldAbilityDef def = MagicShieldDef;
			if (!HasAbilityDef(defender, def)) {
				return;
			}
			NPCDef npcDef = defender.Def as NPCDef;
			double absorbed = AbsorbedByMagicShield(swingArgs.DamageAfterAC,
				RollTunable(npcDef == null ? null : npcDef.MagicShieldStrength, def.Strength),
				GetMysticLevel(attacker), def.MysticPierceLevel);
			swingArgs.DamageAfterAC -= absorbed;
			if (swingArgs.DamageAfterAC > 1) {
				attacker.SysMessage(defender.Name + "'s magic shield absorbs part of your blow.");
			} else {
				attacker.SysMessage(defender.Name + "'s magic shield absorbs your blow completely.");
			}
		}

		/// <summary>Pure formula: how much of `damage` the shield takes. `mysticLevel` is 0 for a
		/// non-Mystic attacker.</summary>
		public static double AbsorbedByMagicShield(double damage, double strengthPercent, int mysticLevel, int mysticPierceLevel) {
			if (mysticLevel > 0) {
				int level = Math.Min(mysticLevel, mysticPierceLevel);
				strengthPercent -= strengthPercent * level / mysticPierceLevel;
			}
			return damage * Math.Max(0, Math.Min(100, strengthPercent)) / 100;
		}

		private static int GetMysticLevel(Character attacker) {
			Player player = attacker as Player;
			if ((player == null) || !Professions.HasProfession(attacker, Professions.Mystic)) {
				return 0;
			}
			return Math.Max(1, player.Level);
		}
	}
}
