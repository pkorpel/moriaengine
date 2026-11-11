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
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1022 - Erebor's dragon fire (systems/aldaron.scp: f_dragon_hit_strong/weak, f_dragon_hit_bomb_*, the
	/// i_dragon_hit_* tiles and f_fire_dragon_efect_*), shared by the dragons' breath, bomb and spin. A tile burns 2 s and
	/// hurts the first player on it (not pets, NPCs or GMs; one standing there when it appears too, as Erebor's @step
	/// fired again each second for whoever stood on it): a strong tile its full damage, a weak one half, less the player's
	/// dragon resist percent ("Byl jsi poranen dracim ohnem."; "Draci ohen te nezranil." when the resist is higher), then
	/// the fire type's lingering effect, 6 ticks from a strong tile and 4 from a weak one. Types: 1 fire, 2 cold, 3 poison,
	/// 4 black (drains stamina), 5 white (drains mana). A spinning dragon's tiles (fire_dragon_round) skip the resist and
	/// the lingering effect, and its breath skips the line-of-sight check.
	/// </summary>
	public static partial class MonsterSpecialAttacks {
		public const int DragonFireTypes = 5;
		public const double DragonTileLifetime = 2;
		internal const int DragonLingerStrong = 6, DragonLingerWeak = 4;
		private static readonly int[] dragonTileStrongHue = { 0, 0x98e, 0x994, 0x97f, 0x692, 0x98b };
		private static readonly int[] dragonTileWeakHue = { 0, 0x992, 0x481, 0x111, 0x770, 0x481 };
		private static readonly int[] dragonBombHue = { 0, 0x986, 0x480, 0x97f, 0x692, 0x480 };

		//f_fire_dragon's cones: facing West and facing NorthWest (Erebor's lines 3348-3535), the rest turned from them.
		//{dx, dy, weak}
		private static readonly int[][] coneWest = {
			new[] { -1, 0, 0 }, new[] { -2, 0, 0 }, new[] { -3, 1, 0 }, new[] { -3, 0, 0 }, new[] { -3, -1, 0 }, new[] { -4, 1, 0 }, new[] { -4, 0, 0 },
			new[] { -4, -1, 0 }, new[] { -5, 2, 0 }, new[] { -5, 1, 0 }, new[] { -5, 0, 0 }, new[] { -5, -1, 0 }, new[] { -5, -2, 0 }, new[] { -6, 2, 0 },
			new[] { -6, 1, 1 }, new[] { -6, 0, 0 }, new[] { -6, -1, 1 }, new[] { -6, -2, 0 }, new[] { -7, 2, 1 }, new[] { -7, 0, 1 }, new[] { -7, -2, 1 }
		};
		private static readonly int[][] coneNorthWest = {
			new[] { -1, -1, 0 }, new[] { -2, -2, 0 }, new[] { -2, -3, 0 }, new[] { -3, -2, 0 }, new[] { -3, -3, 0 }, new[] { -3, -4, 0 }, new[] { -3, -5, 0 },
			new[] { -3, -6, 0 }, new[] { -4, -3, 0 }, new[] { -4, -4, 0 }, new[] { -4, -5, 0 }, new[] { -4, -6, 0 }, new[] { -4, -7, 1 }, new[] { -4, -8, 1 },
			new[] { -5, -3, 0 }, new[] { -5, -4, 0 }, new[] { -5, -5, 0 }, new[] { -5, -6, 1 }, new[] { -6, -3, 0 }, new[] { -6, -4, 0 }, new[] { -6, -5, 1 },
			new[] { -6, -6, 1 }, new[] { -7, -4, 1 }, new[] { -8, -4, 1 }
		};

		//f_fire_dragon_bomb: 13 strong tiles and 16 weak ones around the target
		private static readonly int[][] bombPattern = {
			new[] { 0, 0, 0 }, new[] { 0, 1, 0 }, new[] { 0, -1, 0 }, new[] { 0, 2, 0 }, new[] { 0, -2, 0 }, new[] { 1, 0, 0 }, new[] { -1, 0, 0 },
			new[] { 1, 1, 0 }, new[] { 1, -1, 0 }, new[] { -1, 1, 0 }, new[] { -1, -1, 0 }, new[] { 2, 0, 0 }, new[] { -2, 0, 0 },
			new[] { 0, 3, 1 }, new[] { 0, -3, 1 }, new[] { 1, 2, 1 }, new[] { 1, -2, 1 }, new[] { -1, 2, 1 }, new[] { -1, -2, 1 }, new[] { 2, 1, 1 },
			new[] { 2, -1, 1 }, new[] { -2, 1, 1 }, new[] { -2, -1, 1 }, new[] { 2, 2, 1 }, new[] { 2, -2, 1 }, new[] { -2, 2, 1 }, new[] { -2, -2, 1 },
			new[] { 3, 0, 1 }, new[] { -3, 0, 1 }
		};

		/// <summary>The cone of f_fire_dragon for a facing (0 North ... 7 NorthWest), the origin tile not included.</summary>
		internal static IEnumerable<int[]> DragonCone(int dir) {
			bool diagonal = (dir % 2) == 1;
			foreach (int[] t in diagonal ? coneNorthWest : coneWest) {
				int dx = t[0], dy = t[1];
				switch (dir) {
					case 6: //West
					case 7: //NorthWest
						yield return new[] { dx, dy, t[2] };
						break;
					case 2: //East
					case 1: //NorthEast
						yield return new[] { -dx, dy, t[2] };
						break;
					case 4: //South
						yield return new[] { dy, -dx, t[2] };
						break;
					case 0: //North
						yield return new[] { dy, dx, t[2] };
						break;
					case 5: //SouthWest
						yield return new[] { dx, -dy, t[2] };
						break;
					default: //3, SouthEast
						yield return new[] { -dx, -dy, t[2] };
						break;
				}
			}
		}

		private class DragonTile {
			internal Character owner;
			internal int damage, fireType;
			internal bool weak, raw;
			internal double expiresAt;
		}

		private static readonly Dictionary<long, DragonTile> dragonTiles = new Dictionary<long, DragonTile>();

		internal static int FxId(string defname, int fallback) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			return (def == null) ? fallback : def.Model;
		}

		/// <summary>One tile of dragon fire. `raw`: the spinning dragon's tile, no resist and no lingering effect.
		/// `needsLos`: dropped when the owner can't see the spot (Erebor's srccombatLOS), as the breath's tiles are.</summary>
		internal static void PlaceDragonTile(Character owner, int x, int y, int z, byte m, int damage, int fireType, bool weak, bool raw, bool needsLos) {
			if (!Map.IsValidPos(x, y, m)) {
				return;
			}
			Point4D point = new Point4D(x, y, z, m);
			if (needsLos && (!owner.CanSeeLOS(point).Allow || (Math.Abs(owner.Z - z) > 19))) {
				return;
			}
			int type = Math.Max(1, Math.Min(DragonFireTypes, fireType));
			EffectFactory.StationaryEffectAt(point, FxId("i_fx_smoke", 0x3728), 10, 40, true, false, weak ? dragonTileWeakHue[type] : dragonTileStrongHue[type], RenderModes.Opaque);
			DragonTile tile = new DragonTile { owner = owner, damage = damage, fireType = type, weak = weak, raw = raw, expiresAt = Globals.TimeInSeconds + DragonTileLifetime };
			if (dragonTiles.Count > 512) {
				PurgeDragonTiles();
			}
			foreach (AbstractCharacter ch in owner.GetMap().GetCharsInRange(x, y, 0)) {
				if ((ch is Character target) && (target.X == x) && (target.Y == y) && IsDragonFireVictim(target)) {
					BurnWithDragonTile(tile, target);
					return; //one victim, the tile went out
				}
			}
			dragonTiles[HazardKey(x, y, m)] = tile;
		}

		/// <summary>Character.On_StepTaken: a player walking onto live dragon fire.</summary>
		internal static void CheckDragonTileStep(Character stepping) {
			if (dragonTiles.Count == 0) {
				return;
			}
			long key = HazardKey(stepping.X, stepping.Y, stepping.M);
			if (!dragonTiles.TryGetValue(key, out DragonTile tile)) {
				return;
			}
			if ((Globals.TimeInSeconds > tile.expiresAt) || tile.owner.IsDeleted) {
				dragonTiles.Remove(key);
				return;
			}
			if (IsDragonFireVictim(stepping)) {
				dragonTiles.Remove(key);
				BurnWithDragonTile(tile, stepping);
			}
		}

		private static void PurgeDragonTiles() {
			double now = Globals.TimeInSeconds;
			List<long> expired = new List<long>();
			foreach (KeyValuePair<long, DragonTile> pair in dragonTiles) {
				if ((now > pair.Value.expiresAt) || pair.Value.owner.IsDeleted) {
					expired.Add(pair.Key);
				}
			}
			foreach (long key in expired) {
				dragonTiles.Remove(key);
			}
		}

		/// <summary>The dragon tiles still burning - for tests.</summary>
		public static int CountLiveDragonTiles() {
			PurgeDragonTiles();
			return dragonTiles.Count;
		}

		/// <summary>Players only, alive, not GMs (the tiles' @step).</summary>
		internal static bool IsDragonFireVictim(Character ch) {
			return (ch != null) && !ch.IsDeleted && ch.IsAliveAndValid && ch.IsPlayerForCombat && !ch.IsGM;
		}

		private static void BurnWithDragonTile(DragonTile tile, Character player) {
			int damage = tile.weak ? (tile.damage / 2) : tile.damage;
			if (tile.raw) {
				DamageManager.CauseDamageNoResponse(tile.owner, player, DamageType.MagicFire, damage);
				return;
			}
			int resist = PotionEffectPlugin.DragonResist(player);
			if (tile.damage <= resist) {
				player.SysMessage("Draci ohen te nezranil.");
				return;
			}
			player.SysMessage("Byl jsi poranen dracim ohnem.");
			DamageManager.CauseDamageNoResponse(tile.owner, player, DamageType.MagicFire, damage - ((damage * resist) / 100));
			if (!player.IsDeleted && player.IsAliveAndValid) {
				ApplyDragonLinger(player, tile.fireType, tile.damage, tile.weak ? DragonLingerWeak : DragonLingerStrong);
			}
		}

		/// <summary>f_fire_dragon: the cone in front of the owner as it faces now, its own tile strong too.</summary>
		internal static void BreatheDragonFire(Character owner, int damage, int fireType, bool raw) {
			int dir = (int) owner.Direction;
			PlaceDragonTile(owner, owner.X, owner.Y, owner.Z, owner.M, damage, fireType, false, raw, !raw);
			foreach (int[] t in DragonCone(dir)) {
				PlaceDragonTile(owner, owner.X + t[0], owner.Y + t[1], owner.Z, owner.M, damage, fireType, t[2] == 1, raw, !raw);
			}
		}

		/// <summary>f_fire_dragon_bomb: a cannonball at `center` and the 29-tile pattern there, no line of sight needed.
		/// Fixed from Erebor: the ball flies from the dragon (Erebor's started at the attacker).</summary>
		internal static void DropDragonBomb(Character owner, IPoint4D center, int damage, int fireType) {
			int type = Math.Max(1, Math.Min(DragonFireTypes, fireType));
			EffectFactory.EffectFromTo(owner, center, FxId("i_cannon_ball", 0xE73), 6, 15, true, false, dragonBombHue[type], RenderModes.Opaque);
			foreach (int[] t in bombPattern) {
				PlaceDragonTile(owner, center.X + t[0], center.Y + t[1], center.Z, center.M, damage, type, t[2] == 1, false, false);
			}
		}

		/// <summary>f_fire_dragon_efect_&lt;type&gt;: the lingering effect. One a type per player; a new hit only resets its
		/// power, as on Erebor.</summary>
		internal static void ApplyDragonLinger(Character player, int fireType, int damage, int power) {
			if (!IsDragonFireVictim(player)) {
				return;
			}
			int type = Math.Max(1, Math.Min(DragonFireTypes, fireType));
			TimerKey key = DragonLingerTimer.KeyOf(type);
			if (player.GetTimer(key) is DragonLingerTimer running) {
				running.power = power;
				return;
			}
			int resist = PotionEffectPlugin.DragonResist(player);
			if ((type == 2) && (damage <= resist)) {
				return; //the cold one goes at once
			}
			DragonLingerTimer timer = new DragonLingerTimer {
				fireType = type, baseValue = (damage > resist) ? ((damage - resist) / 10) : 0, power = power, maxPower = power
			};
			player.SysMessage(DragonLingerTimer.startTexts[type]);
			EffectFactory.StationaryEffect(player, FxId(DragonLingerTimer.effects[type], 0x374A), 3, 16);
			player.AddTimer(key, timer);
			timer.DueInSeconds = 1;
		}

		/// <summary>Players in the same 64x64 map cell as the point, on its map (Sphere's sector.allchars, the scripts'
		/// mapplane check), alive and not GMs.</summary>
		internal static List<Character> PlayersInSector(Character around, int x, int y) {
			List<Character> found = new List<Character>();
			int cellX = x >> 6, cellY = y >> 6;
			foreach (AbstractCharacter ch in around.GetMap().GetCharsInRange(x, y, 64)) {
				if ((ch is Character c) && ((c.X >> 6) == cellX) && ((c.Y >> 6) == cellY) && IsDragonFireVictim(c)) {
					found.Add(c);
				}
			}
			return found;
		}
	}

	/// <summary>i_fire_dragon_efect_*: a tick at +1 s, then every 4 s, of base*power/maxPower, power going down by one.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DragonLingerTimer : BoundTimer {
		private static readonly TimerKey[] keys = {
			null, TimerKey.Acquire("_dragonLingerFire_"), TimerKey.Acquire("_dragonLingerCold_"), TimerKey.Acquire("_dragonLingerPoison_"),
			TimerKey.Acquire("_dragonLingerBlack_"), TimerKey.Acquire("_dragonLingerWhite_")
		};
		internal static readonly string[] startTexts = { null, "Pali te draci ohen!", "Mrazi te draci ohen!", "Zabiji te draci jed!", "Citis, jak te zasah ohnem vycerpava!", "Draci ohen ti vysava manu!" };
		private static readonly string[] tickTexts = { null, "Poranil te draci ohen.", "Mrazi te draci ohen.", "Zabiji te draci jed.", "Citis, jak te zasah ohnem vycerpava!", "Draci ohen ti vysava manu!" };
		private static readonly string[] endTexts = { null, "Draci ohen dohorel!", "Draci ohen te prestal mrazit!", "Draci jed vyprchal!", "Draci ohen zmizel!", "Draci ohen zmizel!" };
		internal static readonly string[] effects = { null, "i_fire", "i_fx_vortex", "i_fx_curse", "i_fx_glow_spike", "i_fx_glow2" };
		private static readonly PluginKey coldParalysisKey = PluginKey.Acquire("_dragonColdParalysis_");

		[SaveableData, CopyableData]
		public int fireType;

		[SaveableData, CopyableData]
		public int baseValue;

		[SaveableData, CopyableData]
		public int power;

		[SaveableData, CopyableData]
		public int maxPower;

		[LoadingInitializer, DeepCopyImplementation]
		public DragonLingerTimer() {
		}

		internal static TimerKey KeyOf(int fireType) {
			return keys[fireType];
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Character player)) {
				return;
			}
			int value = (this.maxPower > 0) ? ((this.baseValue * this.power) / this.maxPower) : 0;
			bool cold = this.fireType == 2;
			if ((this.power < 1) || player.Flag_Dead || (!cold && (value < 1))) {
				player.SysMessage(endTexts[this.fireType]);
				return; //a one-shot timer, gone now
			}
			switch (this.fireType) {
				case 2:
					ParalyzeEffectPluginDef.Paralyze(player, coldParalysisKey, TimeSpan.FromSeconds(1));
					break;
				case 4:
					player.Stam = (short) Math.Max(0, player.Stam - value);
					break;
				case 5:
					player.Mana = (short) Math.Max(0, player.Mana - value);
					break;
				default:
					DamageManager.CauseDamage(null, player, DamageType.MagicFire, value);
					break;
			}
			if (player.IsDeleted || player.Flag_Dead) {
				return;
			}
			player.SysMessage(tickTexts[this.fireType]);
			EffectFactory.StationaryEffect(player, MonsterSpecialAttacks.FxId(effects[this.fireType], 0x374A), 3, 16);
			this.power--;
			DragonLingerTimer next = new DragonLingerTimer { fireType = this.fireType, baseValue = this.baseValue, power = this.power, maxPower = this.maxPower };
			player.AddTimer(KeyOf(this.fireType), next);
			next.DueInSeconds = 4;
		}
	}
}
