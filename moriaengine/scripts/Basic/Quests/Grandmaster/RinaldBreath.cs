using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#837 - the breath of f_q000_rinaldTransfer: f_dragon_fire (systems/dragon_events.scp). As Rinald turns into a
	/// daemon, a wall of fire 9 tiles wide across his front (i_dragon_breath_fire on his tile and 4 to each side) runs 10 tiles,
	/// the first step after 0.5 s and then one every 0.2 s, straight ahead or a turn to either side (dorand 3). Each step of a
	/// flame leaves i_dragon_breath_after_fire on its tile for 1.5-2.5 s. Whoever steps onto a flame, or stands where it comes,
	/// takes its damage as fire (the target's resists count) and the flame goes out; an after-fire hurts by half and goes out
	/// too. Rinald himself and GMs aren't hurt. Fixed from Erebor: `f_dragon_fire(35) // give them smaller damage` put the 35
	/// into the argument the function doesn't read for damage, so it fell back to 50; the damage is RinaldAbilityDef's
	/// breathDamage (.settings, Rinald's breath), 35. Also assumed intent: the wall starts at Rinald's tile in his facing. On
	/// Erebor the delayed function ran from an item's @timer, whose src (where f_dragon_fire starts) was most likely the
	/// server, not Rinald, so the breath may never have shown there.</summary>
	public static class RinaldBreath {
		internal const int HalfWidth = 4, Range = 10;
		internal const double FirstStepSeconds = 0.5, StepSeconds = 0.2, AfterFireMin = 1.5, AfterFireMax = 2.5;
		internal static readonly TimerKey timerTK = TimerKey.Acquire("_rinaldBreath_");

		//Direction 0 North ... 7 NorthWest, Erebor's directionVectors
		private static readonly int[][] vectors = {
			new[] { 0, -1 }, new[] { 1, -1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 }, new[] { -1, 1 }, new[] { -1, 0 }, new[] { -1, -1 }
		};

		private class Tile {
			internal Character owner;
			internal int damage;
			internal double flameUntil, afterFireUntil;
		}

		private static readonly Dictionary<long, Tile> tiles = new Dictionary<long, Tile>();

		private static long Key(int x, int y, byte m) {
			return ((long) m << 40) | ((long) (x & 0xFFFFF) << 20) | (long) (y & 0xFFFFF);
		}

		/// <summary>f_dragon_fire from Rinald as he faces now.</summary>
		internal static void Breathe(NPC rinald, int damage) {
			int dir = ((int) rinald.Direction) & 7;
			int[] facing = vectors[dir];
			int[] move = vectors[(dir + Globals.dice.Next(3) + 7) % 8];
			RinaldBreathTimer timer = new RinaldBreathTimer {
				originX = rinald.X, originY = rinald.Y, z = rinald.Z, perpX = -facing[1], perpY = facing[0], moveX = move[0], moveY = move[1], damage = damage
			};
			for (int k = -HalfWidth; k <= HalfWidth; k++) {
				Place(rinald, timer, k, 0, false);
			}
			rinald.AddTimer(timerTK, timer).DueInSeconds = FirstStepSeconds;
		}

		/// <summary>A flame comes to its tile: the steps it has run, an after-fire left there unless it's the start.</summary>
		internal static void Place(Character owner, RinaldBreathTimer timer, int k, int steps, bool afterFire) {
			int x = timer.originX + (k * timer.perpX) + (steps * timer.moveX);
			int y = timer.originY + (k * timer.perpY) + (steps * timer.moveY);
			byte m = owner.M;
			if (!Map.IsValidPos(x, y, m)) {
				timer.PutOut(k);
				return;
			}
			Point4D point = new Point4D(x, y, timer.z, m);
			EffectFactory.StationaryEffectAt(point, MonsterSpecialAttacks.FxId("i_fx_explode", 0x36B0), 10, 10, true, false, 0, RenderModes.Opaque);
			double now = Globals.TimeInSeconds;
			Tile tile = new Tile { owner = owner, damage = timer.damage, flameUntil = now + ((steps == 0) ? FirstStepSeconds : StepSeconds) };
			if (afterFire) {
				tile.afterFireUntil = now + AfterFireMin + (Globals.dice.NextDouble() * (AfterFireMax - AfterFireMin));
				EffectFactory.StationaryEffectAt(point, MonsterSpecialAttacks.FxId("i_fire", 0x3996), 10, 20, true, false, 0, RenderModes.Opaque);
			}
			if (tiles.Count > 512) {
				Purge();
			}
			tiles[Key(x, y, m)] = tile;
			foreach (AbstractCharacter ch in owner.GetMap().GetCharsInRange(x, y, 0)) {
				if ((ch is Character standing) && (standing.X == x) && (standing.Y == y) && IsVictim(standing, owner)) {
					Burn(tile, standing);
					timer.PutOut(k);
					return;
				}
			}
		}

		/// <summary>Character.On_StepTaken.</summary>
		internal static void CheckStep(Character stepping) {
			if (tiles.Count == 0) {
				return;
			}
			if (tiles.TryGetValue(Key(stepping.X, stepping.Y, stepping.M), out Tile tile) && IsVictim(stepping, tile.owner)) {
				if (tile.owner.GetTimer(timerTK) is RinaldBreathTimer timer) {
					timer.PutOutAt(stepping.X, stepping.Y);
				}
				Burn(tile, stepping);
			}
		}

		private static bool IsVictim(Character ch, Character owner) {
			return (ch != owner) && !ch.IsDeleted && ch.IsAliveAndValid && !ch.IsGM;
		}

		/// <summary>The flame (full) and the after-fire (half) that are still there, both gone then.</summary>
		private static void Burn(Tile tile, Character victim) {
			double now = Globals.TimeInSeconds;
			int damage = ((now <= tile.flameUntil) ? tile.damage : 0) + ((now <= tile.afterFireUntil) ? (tile.damage / 2) : 0);
			tiles.Remove(Key(victim.X, victim.Y, victim.M));
			if ((damage > 0) && !tile.owner.IsDeleted) {
				DamageManager.CauseDamage(tile.owner, victim, DamageType.MagicFire, damage);
			}
		}

		private static void Purge() {
			double now = Globals.TimeInSeconds;
			List<long> gone = new List<long>();
			foreach (KeyValuePair<long, Tile> pair in tiles) {
				if (((now > pair.Value.flameUntil) && (now > pair.Value.afterFireUntil)) || pair.Value.owner.IsDeleted) {
					gone.Add(pair.Key);
				}
			}
			foreach (long key in gone) {
				tiles.Remove(key);
			}
		}

		/// <summary>The tiles still burning - for tests.</summary>
		public static int CountLiveTiles() {
			Purge();
			return tiles.Count;
		}
	}

	/// <summary>#837 - the breath's flames on their way: i_dragon_breath_fire's @timer for all nine.</summary>
	[SaveableClass, DeepCopyableClass]
	public class RinaldBreathTimer : BoundTimer {
		[SaveableData, CopyableData]
		public int originX, originY, z, perpX, perpY, moveX, moveY, damage, steps, outMask;

		[LoadingInitializer, DeepCopyImplementation]
		public RinaldBreathTimer() {
		}

		internal void PutOut(int k) {
			this.outMask |= 1 << (k + RinaldBreath.HalfWidth);
		}

		private bool IsOut(int k) {
			return (this.outMask & (1 << (k + RinaldBreath.HalfWidth))) != 0;
		}

		/// <summary>The flame now standing on (x, y), if any, goes out.</summary>
		internal void PutOutAt(int x, int y) {
			for (int k = -RinaldBreath.HalfWidth; k <= RinaldBreath.HalfWidth; k++) {
				if ((this.originX + (k * this.perpX) + (this.steps * this.moveX) == x) && (this.originY + (k * this.perpY) + (this.steps * this.moveY) == y)) {
					this.PutOut(k);
				}
			}
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Character rinald) || rinald.IsDeleted) {
				return;
			}
			this.steps++;
			bool any = false;
			for (int k = -RinaldBreath.HalfWidth; k <= RinaldBreath.HalfWidth; k++) {
				if (!this.IsOut(k)) {
					RinaldBreath.Place(rinald, this, k, this.steps, true);
					any = true;
				}
			}
			if (any && (this.steps < RinaldBreath.Range)) {
				RinaldBreathTimer next = new RinaldBreathTimer {
					originX = this.originX, originY = this.originY, z = this.z, perpX = this.perpX, perpY = this.perpY,
					moveX = this.moveX, moveY = this.moveY, damage = this.damage, steps = this.steps, outMask = this.outMask
				};
				rinald.AddTimer(RinaldBreath.timerTK, next).DueInSeconds = RinaldBreath.StepSeconds;
			}
		}
	}
}
