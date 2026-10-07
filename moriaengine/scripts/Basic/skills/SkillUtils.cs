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
	public static class SkillUtils {
		public const int skillCheckVariance = 250;


		public static bool CheckSuccess(int skillValue, int difficulty) {
			// Chance to complete skill check given skill x and difficulty y
			// ARGS:
			//  skillValue = 0-1000
			//  difficulty = 0-1000
			// RETURN:
			//  true = success check.

			if (difficulty < 0 || skillValue < 0)       // auto failure.
				return (false);

			int chanceForSuccess = GetSCurve(skillValue - difficulty);
			int roll = Globals.dice.Next(1000);

			return (roll <= chanceForSuccess);
		}

		public static int GetSCurve(int valDiff) {
			// ARGS:
			//   valDiff = Difference between our skill level and difficulty.
			//              positive = high chance, negative = lower chance
			//              0 = 50.0% chance.
			//   skillCheckVariance = the 25.0% difference point of the bell curve
			// RETURN:
			//       what is the (0-100.0)% chance of success = 0-1000
			// NOTE:
			//   Chance of skill gain is inverse to chance of success.
			//
			int iChance = GetBellCurve(valDiff);
			if (valDiff > 0)
				return (1000 - iChance);
			return (iChance);
		}

		private static int GetBellCurve(int valDiff) {
			// Produce a log curve.
			//
			// 50    +
			//       |
			//       |
			//       |
			// 25    |  +
			//       |
			//       |         +
			//       |                +
			//      0 --+--+--+--+------
			//    iVar                              iValDiff
			//
			// ARGS:
			//  valDiff = Given a value relative to 0
			//              0 = 50.0% chance.
			//  skillCheckVariance = the 25.0% point of the bell curve
			// RETURN:
			//  (0-100.0) % chance at this iValDiff.
			//  Chance gets smaller as Diff gets bigger.
			// EXAMPLE:
			//  if ( iValDiff == skillCheckVariance ) return( 250 )
			//  if ( iValDiff == 0 ) return( 500 );
			//

			if (valDiff < 0) valDiff = -valDiff;

#if DEBUG
			int count = 32;
#endif

			int iChance = 500;
			while ((valDiff > skillCheckVariance) && (iChance > 0)) {
				valDiff -= skillCheckVariance;
				iChance /= 2;   // chance is halved for each Variance period.
#if DEBUG
				count--;
				Sanity.IfTrueThrow(count < 1, "Calculation too complex in SkillUtils.GetBellCurve");
#endif
			}

			return (iChance - (((iChance / 2) * valDiff) / skillCheckVariance));
		}
	}
}