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

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #779 - Sphere 0.99z8's stamina rules for walking (EreborNext CheckMoveWalkDir,
	/// Calc_MaxCarryWeight, Calc_DropStamWhileMoving): at 0 stamina, or carrying more than
	/// maxLoadPercent of the max carry weight (STR*4+30 stones), a player can't move; every step
	/// costs 1 stamina with a chance that follows an S-curve around staminaLossAtWeight percent
	/// load, running counting as runningStaminaPenalty percent more load. Players only - NPC
	/// stamina and carried weight aren't kept Sphere-accurate in this engine. Tunable on the
	/// .settings "Movement" page (CharCollisionSettings).
	/// </summary>
	public static class MovementStamina {
		public static bool CheckCanMove(Character self) {
			if (!self.IsPlayer || self.Flag_Dead || !CharCollisionSettings.instance.fatigueAndOverload) {
				return true;
			}
			if (self.Stam <= 0) {
				self.SysMessage(self.Flag_Riding ? "Your mount is too fatigued to move." : "You are too fatigued to move.");
				return false;
			}
			if (LoadPercent(CarriedWeight(self), self.Str) > CharCollisionSettings.instance.maxLoadPercent) {
				self.SysMessage("You are carrying too much to move.");
				return false;
			}
			return true;
		}

		public static void DrainForStep(Character self, bool running) {
			if (!self.IsPlayer || self.Flag_Dead || !CharCollisionSettings.instance.fatigueAndOverload) {
				return;
			}
			CharCollisionSettings settings = CharCollisionSettings.instance;
			int chance = StaminaLossChance(LoadPercent(CarriedWeight(self), self.Str), running,
				settings.staminaLossAtWeight, settings.runningStaminaPenalty);
			if ((Globals.dice.Next(1000) <= chance) && (self.Stam > 0)) {
				self.Stam = (short) (self.Stam - 1);
			}
		}

		/// <summary>Worn and carried items (backpack included) - not the mount or vendor layers.</summary>
		public static float CarriedWeight(Character self) {
			float carried = 0;
			foreach (AbstractItem item in self) {
				if (item.Layer < (int) LayerNames.Mount) {
					carried += item.Weight;
				}
			}
			return carried;
		}

		/// <summary>Sphere's Calc_MaxCarryWeight, in stones.</summary>
		public static int MaxCarryWeight(int str) {
			return (Math.Max(0, str) * 4) + 30;
		}

		public static int LoadPercent(float carriedWeight, int str) {
			return (int) (carriedWeight * 100 / MaxCarryWeight(str));
		}

		/// <summary>Permille chance a step costs 1 stamina - Sphere's Calc_DropStamWhileMoving
		/// (a roll of 0..999 at or below it loses the point).</summary>
		public static int StaminaLossChance(int loadPercent, bool running, int lossAtWeight, int runningPenalty) {
			if (running) {
				loadPercent += runningPenalty;
			}
			return SCurve(loadPercent - lossAtWeight, 10);
		}

		/// <summary>Sphere's Calc_GetSCurve: 500 at 0, falling toward 0 below and rising toward 1000 above.</summary>
		public static int SCurve(int valueDiff, int variance) {
			int chance = BellCurve(valueDiff, variance);
			return (valueDiff > 0) ? 1000 - chance : chance;
		}

		/// <summary>Sphere's Calc_GetBellCurve: 500 at 0, halving every `variance` away from it.</summary>
		public static int BellCurve(int valueDiff, int variance) {
			if (variance <= 0) {
				return 500;
			}
			valueDiff = Math.Abs(valueDiff);
			int chance = 500;
			while ((valueDiff > variance) && (chance != 0)) {
				valueDiff -= variance;
				chance /= 2;
			}
			return chance - ((chance / 2 * valueDiff) / variance);
		}
	}
}
