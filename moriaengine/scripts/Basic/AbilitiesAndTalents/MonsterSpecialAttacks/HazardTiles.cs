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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 - the archive's i_fire_column_damage / i_lightning_damage: a short-lived (2 s)
	/// burning/electrified tile that damages the first player or pet (summons and mounts
	/// included) who walks onto it, then goes out. Used by Phoenix Fire, the Phoenix's
	/// rebirth fire ring and Lightnings. User decision: a tile ALSO hits whoever already
	/// stands on it the moment it appears (the archive is step-on only, so a melee fighter
	/// standing still never got hit). No real item is created - the server only keeps a
	/// tile registry and sends the client a stationary effect, which is all the archive's
	/// item ever did visually.
	/// </summary>
	public static partial class MonsterSpecialAttacks {
		public const int FireColumnEffect = 0x3709;
		public const double HazardTileLifetime = 2;

		private class HazardTile {
			internal Character owner;
			internal double damage;
			internal DamageType damageType;
			internal double expiresAt;
		}

		private static readonly Dictionary<long, HazardTile> hazardTiles = new Dictionary<long, HazardTile>();

		private static long HazardKey(int x, int y, byte m) {
			return ((long) m << 40) | ((long) (x & 0xFFFFF) << 20) | (long) (y & 0xFFFFF);
		}

		/// <summary>Places one hazard tile at (x, y) on the owner's own map and Z (the effect is
		/// only visual, damage goes by x/y), hitting anyone valid already standing there.
		/// `lightning` picks the lightning-bolt effect instead of a fire column.</summary>
		public static void PlaceHazardTile(Character owner, int x, int y, double damage, DamageType damageType, bool lightning) {
			Map map = owner.GetMap();
			byte m = owner.M;
			if (!Map.IsValidPos(x, y, m)) {
				return;
			}
			Point4D point = new Point4D(x, y, owner.Z, m);
			if (lightning) {
				EffectFactory.LightningEffectAt(point);
			} else {
				EffectFactory.StationaryEffectAt(point, FireColumnEffect, 10, 40);
			}

			HazardTile tile = new HazardTile {
				owner = owner,
				damage = damage,
				damageType = damageType,
				expiresAt = Globals.TimeInSeconds + HazardTileLifetime
			};
			if (hazardTiles.Count > 256) {
				PurgeExpiredHazardTiles();
			}
			foreach (AbstractCharacter ch in map.GetCharsInRange(x, y, 0)) {
				Character target = ch as Character;
				if ((target != null) && (target.X == x) && (target.Y == y) && IsValidHazardTarget(target, owner)) {
					DamageManager.CauseDamage(owner, target, damageType, damage);
					return; //the tile went out on its first victim, same as stepping on it
				}
			}
			hazardTiles[HazardKey(x, y, m)] = tile;
		}

		/// <summary>Character.On_StepTaken - a player or pet walking onto a live hazard tile takes
		/// its damage and the tile goes out.</summary>
		internal static void CheckHazardTileStep(Character stepping) {
			if (hazardTiles.Count == 0) {
				return;
			}
			long key = HazardKey(stepping.X, stepping.Y, stepping.M);
			HazardTile tile;
			if (!hazardTiles.TryGetValue(key, out tile)) {
				return;
			}
			if ((Globals.TimeInSeconds > tile.expiresAt) || tile.owner.IsDeleted) {
				hazardTiles.Remove(key);
				return;
			}
			if (!IsValidHazardTarget(stepping, tile.owner)) {
				return;
			}
			hazardTiles.Remove(key);
			DamageManager.CauseDamage(tile.owner, stepping, tile.damageType, tile.damage);
		}

		/// <summary>Number of hazard tiles still burning - for tests.</summary>
		public static int CountLiveHazardTiles() {
			PurgeExpiredHazardTiles();
			return hazardTiles.Count;
		}

		private static void PurgeExpiredHazardTiles() {
			double now = Globals.TimeInSeconds;
			List<long> expired = null;
			foreach (KeyValuePair<long, HazardTile> pair in hazardTiles) {
				if ((now > pair.Value.expiresAt) || pair.Value.owner.IsDeleted) {
					(expired ?? (expired = new List<long>())).Add(pair.Key);
				}
			}
			if (expired != null) {
				foreach (long key in expired) {
					hazardTiles.Remove(key);
				}
			}
		}

		//archive: "src.isPlayer || src.isPet", never GMs, the dead, or the tile's own creator
		private static bool IsValidHazardTarget(Character target, Character owner) {
			return (target != owner) && IsPlayerOrPetTarget(target);
		}
	}
}
