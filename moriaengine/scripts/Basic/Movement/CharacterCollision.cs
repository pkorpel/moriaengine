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
using System.Numerics;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Walking onto a tile another character stands on (#767), ported from Sphere 0.99z8
	/// (EreborNext's CChar.Movement.Pushing.cs / CheckMoveWalkDir):
	/// - dead/insubstantial movers (ghosts) walk through everyone; GMs never get here
	///   (AbstractCharacter.WalkRunOrFly skips the check for them).
	/// - dead, insubstantial and disconnected characters never block anyone.
	/// - NPCs can't shove at all - any other living character on the tile stops them.
	/// - players: invisible characters are walked through, hidden ones are revealed and
	///   shoved, anyone else needs full stamina (CharCollisionSettings) to shove. A shove
	///   costs stamina and fires @personalSpace on the shoved character.
	/// Deliberate deviation: Sphere also blocks NPCs with ghosts; here ghosts never block, so
	/// a dead player can't body-block monsters in a doorway.
	/// </summary>
	public static class CharacterCollision {

		/// <summary>Same body height Map.CanFit assumes for characters.</summary>
		private const int CharHeight = 16;

		/// <summary>Entry point from Character.CheckCharacterObstacles. Returns false to refuse
		/// the step. Has side effects on success (stamina, reveal, message, trigger).</summary>
		public static bool CheckStep(Character mover, Map map, int x, int y, int z) {
			if (mover.Flag_Dead || mover.Flag_Insubst) {
				return true;
			}
			CharCollisionSettings settings = CharCollisionSettings.instance;
			bool isPlayer = mover.IsPlayer;
			if (isPlayer ? !settings.playersBlockedByCharacters : !settings.npcsBlockedByCharacters) {
				return true;
			}

			Character obstacle = FindObstacle(mover, map, x, y, z);
			if (obstacle == null) {
				return true;
			}
			if (!isPlayer) {
				return false;
			}
			if (obstacle.Flag_InvisByMagic) {
				return true;
			}

			int cost = GetPushCost(obstacle);
			if (obstacle.Flag_Hidden) {
				mover.SysMessage("You stumble upon " + obstacle.Name + " hidden.");
				HidingSkillDef.UnHide(obstacle);
			} else if (!HasStaminaToPush(mover, cost)) {
				mover.SysMessage("You are not strong enough to push " + obstacle.Name + " out of the way.");
				return false;
			} else {
				mover.SysMessage("You shove " + obstacle.Name + " out of the way.");
			}

			mover.Stam = (short) Math.Max(0, mover.Stam - cost);
			obstacle.Trigger_PersonalSpace(mover);
			return true;
		}

		/// <summary>The first character at (x, y) within body height of z that blocks mover, or null.</summary>
		public static Character FindObstacle(Character mover, Map map, int x, int y, int z) {
			foreach (AbstractCharacter ch in map.GetCharsInRange(x, y, 0)) {
				Character other = ch as Character;
				if ((other != null) && IsObstacle(mover, other, x, y, z)) {
					return other;
				}
			}
			return null;
		}

		/// <summary>True when mover would be blocked from stepping onto (x, y, z) right now, ignoring
		/// stamina - used by NPC pathing to route around occupied tiles.</summary>
		public static bool IsTileBlockedFor(Character mover, Map map, int x, int y, int z) {
			if (mover.Flag_Dead || mover.Flag_Insubst) {
				return false;
			}
			CharCollisionSettings settings = CharCollisionSettings.instance;
			if (mover.IsPlayer ? !settings.playersBlockedByCharacters : !settings.npcsBlockedByCharacters) {
				return false;
			}
			return FindObstacle(mover, map, x, y, z) != null;
		}

		/// <summary>Sphere's Calc_WalkThroughChar: log2(obstacle STR)+1, unless a flat cost is configured.</summary>
		public static int GetPushCost(Character obstacle) {
			int flat = CharCollisionSettings.instance.flatPushCost;
			if (flat > 0) {
				return flat;
			}
			return BitOperations.Log2((uint) Math.Max(1, (int) obstacle.Str)) + 1;
		}

		private static bool HasStaminaToPush(Character mover, int cost) {
			if (CharCollisionSettings.instance.requireFullStamina) {
				return mover.Stam >= mover.MaxStam;
			}
			return mover.Stam > cost;
		}

		private static bool IsObstacle(Character mover, Character other, int x, int y, int z) {
			if ((other == mover) || other.IsDeleted || other.Flag_Dead || other.Flag_Insubst || other.Flag_Disconnected) {
				return false;
			}
			if ((other.X != x) || (other.Y != y)) {
				return false;
			}
			int otherZ = other.Z;
			return ((otherZ + CharHeight) > z) && ((z + CharHeight) > otherZ);
		}
	}
}
