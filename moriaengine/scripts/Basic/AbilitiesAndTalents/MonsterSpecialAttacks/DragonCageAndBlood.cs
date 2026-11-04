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

	/// <summary>#1022 - e_fire_dragon_cage's cage (systems/aldaron.scp: i_dragon_cage_fire, i_dragon_cage_dlazka): fire on the
	/// 8 tiles around a player for its duration. It doesn't burn. Players can't step onto it, whichever side they come from
	/// (Erebor pushed them back off its invisible tile; here the step is refused, the same outcome); NPCs, pets and ghosts
	/// pass. A double click on a fire weakens it, at most once a second ("Nic se nestalo" sooner): duration/2 times "Ohen se
	/// oslabil...", then "Ohen zmizel" and the tile is open.</summary>
	public static class DragonCage {
		private static readonly int[] dx = { -1, 0, 1, 1, 1, 0, -1, -1 };
		private static readonly int[] dy = { -1, -1, -1, 0, 1, 1, 1, 0 };
		private static readonly int[] hues = { 0, 0x98e, 0x994, 0x97f, 0x692, 0x98b };
		private static readonly TagKey strengthTK = TagKey.Acquire("_dragonCageStrength_");
		private static readonly TagKey lastClickTK = TagKey.Acquire("_dragonCageClicked_");
		private static readonly Dictionary<long, Item> blockers = new Dictionary<long, Item>();

		private static long Key(int x, int y, byte m) {
			return ((long) m << 40) | ((long) (x & 0xFFFFF) << 20) | (long) (y & 0xFFFFF);
		}

		public static void Build(Character player, int duration, int fireType) {
			ItemDef def = ThingDef.GetByDefname("i_dragon_cage_fire") as ItemDef;
			if (def == null) {
				return;
			}
			int type = Math.Max(1, Math.Min(MonsterSpecialAttacks.DragonFireTypes, fireType));
			for (int i = 0; i < 8; i++) {
				int x = player.X + dx[i], y = player.Y + dy[i];
				if (!Map.IsValidPos(x, y, player.M)) {
					continue;
				}
				Item fire = (Item) def.Create(x, y, player.Z, player.M);
				fire.Color = hues[type];
				fire.SetAttribute(ItemAttributes.attr_move_never, true);
				fire.SetTag(strengthTK, duration / 2);
				ItemDecaySystem.ScheduleIn(fire, duration);
				blockers[Key(x, y, player.M)] = fire;
			}
		}

		/// <summary>Character.CheckCharacterObstacles: a living player stepping into a cage's fire.</summary>
		public static bool Refuses(Character ch, int x, int y, byte m) {
			if ((blockers.Count == 0) || !MonsterSpecialAttacks.IsDragonFireVictim(ch)) {
				return false;
			}
			long key = Key(x, y, m);
			if (!blockers.TryGetValue(key, out Item fire)) {
				return false;
			}
			if (fire.IsDeleted) {
				blockers.Remove(key);
				return false;
			}
			ch.SysMessage("Zar je prilis silny, neda se tam projit!");
			return true;
		}

		/// <summary>Character.On_ItemDClick: true when it was a cage's fire.</summary>
		public static bool TryClick(Character ch, Item item) {
			if ((item == null) || !(item.GetTag(strengthTK) is int strength)) {
				return false;
			}
			if ((item.GetTag(lastClickTK) is double last) && (Globals.TimeInSeconds - last < 1)) {
				ch.SysMessage("Nic se nestalo");
				return true;
			}
			item.SetTag(lastClickTK, Globals.TimeInSeconds);
			if (strength > 0) {
				item.SetTag(strengthTK, strength - 1);
				ch.SysMessage("Ohen se oslabil...");
			} else {
				ch.SysMessage("Ohen zmizel");
				blockers.Remove(Key(item.X, item.Y, item.M));
				item.Delete();
			}
			return true;
		}

		/// <summary>For tests: fire standing at that tile.</summary>
		public static Item FireAt(int x, int y, byte m) {
			return (blockers.TryGetValue(Key(x, y, m), out Item fire) && !fire.IsDeleted) ? fire : null;
		}
	}

	/// <summary>#1022 - e_bahenny_drak_krev's blood (i_bahenny_drak_krev): a puddle for 30 minutes. A second after it falls it
	/// hurts everyone right next to it (players its damage, pets ten times as much, never the dragon or wild monsters).
	/// Stepping on it, at most once every 10 s per puddle, does its damage as fire to anyone but a GM, and poisons a player
	/// with dragon-blood poison ("Zabiji te jed z draci krve!"; once more: "Znova jsi se otravil!").</summary>
	public static class DragonBlood {
		private static readonly TagKey damageTK = TagKey.Acquire("_dragonBloodDamage_");
		private static readonly TagKey nextStepTK = TagKey.Acquire("_dragonBloodNextStep_");
		private static readonly Dictionary<long, Item> puddles = new Dictionary<long, Item>();
		internal const double Lifetime = 1800, StepGate = 10;

		private static long Key(int x, int y, byte m) {
			return ((long) m << 40) | ((long) (x & 0xFFFFF) << 20) | (long) (y & 0xFFFFF);
		}

		public static Item Place(NPC dragon, int x, int y, int damage) {
			ItemDef def = ThingDef.GetByDefname("i_bahenny_drak_krev") as ItemDef;
			if ((def == null) || !Map.IsValidPos(x, y, dragon.M)) {
				return null;
			}
			Item blood = (Item) def.Create(x, y, dragon.Z, dragon.M);
			blood.SetAttribute(ItemAttributes.attr_move_never, true);
			blood.SetTag(damageTK, damage);
			ItemDecaySystem.ScheduleIn(blood, Lifetime);
			puddles[Key(x, y, dragon.M)] = blood;
			DragonBloodSplashTimer splash = new DragonBloodSplashTimer();
			blood.AddTimer(DragonBloodSplashTimer.Key, splash);
			splash.DueInSeconds = 1;
			return blood;
		}

		/// <summary>f_blood_rounddamage: everyone at distance 1 from the puddle.</summary>
		internal static void Splash(Item blood) {
			if (!(blood.GetTag(damageTK) is int damage)) {
				return;
			}
			EffectFactory.StationaryEffect(blood, 0x2ED0, 0, 32);
			List<Character> near = new List<Character>();
			foreach (AbstractCharacter ch in blood.GetMap().GetCharsInRange(blood.X, blood.Y, 1)) {
				if ((ch is Character c) && (Point2D.GetSimpleDistance(blood, c) == 1)) {
					near.Add(c);
				}
			}
			foreach (Character c in near) {
				if (MonsterSpecialAttacks.IsDragonFireVictim(c)) {
					DamageManager.CauseDamage(null, c, DamageType.Physical, damage);
				} else if ((c is NPC npc) && (npc.ControlMaster != null) && npc.IsAliveAndValid) {
					DamageManager.CauseDamage(null, npc, DamageType.Physical, damage * 10);
				}
			}
		}

		/// <summary>Character.On_StepTaken: onto a puddle.</summary>
		internal static void CheckStep(Character stepping) {
			if ((puddles.Count == 0) || stepping.IsGM || !stepping.IsAliveAndValid) {
				return;
			}
			long key = Key(stepping.X, stepping.Y, stepping.M);
			if (!puddles.TryGetValue(key, out Item blood)) {
				return;
			}
			if (blood.IsDeleted) {
				puddles.Remove(key);
				return;
			}
			if ((blood.GetTag(nextStepTK) is double next) && (next > Globals.TimeInSeconds)) {
				return;
			}
			blood.SetTag(nextStepTK, Globals.TimeInSeconds + StepGate);
			DragonBloodAbilityDef def = MonsterSpecialAttacks.DragonBloodDef;
			int damage = (blood.GetTag(damageTK) is int d) ? d : 80;
			if (MonsterSpecialAttacks.IsDragonFireVictim(stepping) && (def != null)) {
				DragonBloodPoisonTimer.Apply(stepping, (int) def.PoisonDamage, (int) def.PoisonTicks);
			}
			DamageManager.CauseDamage(null, stepping, DamageType.MagicFire, damage);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class DragonBloodSplashTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_dragonBloodSplash_");

		[LoadingInitializer, DeepCopyImplementation]
		public DragonBloodSplashTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Item blood) && !blood.IsDeleted) {
				DragonBlood.Splash(blood);
			}
		}
	}

	/// <summary>i_fire_dragon_blood_poison: fire damage, PoisonDamage*left/ticks each 4 s; a new step puts the count back.
	/// The damage is gone if the player's dragon resist is as high (Erebor's poison_dmg 0).</summary>
	[SaveableClass, DeepCopyableClass]
	public class DragonBloodPoisonTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_dragonBloodPoison_");

		[SaveableData, CopyableData]
		public int damage, power, maxPower;

		[LoadingInitializer, DeepCopyImplementation]
		public DragonBloodPoisonTimer() {
		}

		internal static void Apply(Character player, int damage, int ticks) {
			if (player.GetTimer(Key) is DragonBloodPoisonTimer running) {
				running.power = ticks;
				player.SysMessage("Znova jsi se otravil!");
				return;
			}
			player.SysMessage("Zabiji te jed z draci krve!");
			EffectFactory.StationaryEffect(player, MonsterSpecialAttacks.FxId("i_fx_curse", 0x374A), 3, 16);
			DragonBloodPoisonTimer timer = new DragonBloodPoisonTimer {
				damage = (damage > PotionEffectPlugin.DragonResist(player)) ? damage : 0, power = ticks, maxPower = ticks
			};
			player.AddTimer(Key, timer);
			timer.DueInSeconds = 1;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Character player)) {
				return;
			}
			int value = (this.maxPower > 0) ? ((this.damage * this.power) / this.maxPower) : 0;
			if ((this.power < 1) || (value < 1) || player.Flag_Dead) {
				player.SysMessage("Jed z draci krve vyprchal!");
				return;
			}
			DamageManager.CauseDamage(null, player, DamageType.MagicFire, value);
			if (player.IsDeleted || player.Flag_Dead) {
				return;
			}
			player.SysMessage("Zabiji te jed z draci krve!");
			EffectFactory.StationaryEffect(player, MonsterSpecialAttacks.FxId("i_fx_curse", 0x374A), 3, 16);
			DragonBloodPoisonTimer next = new DragonBloodPoisonTimer { damage = this.damage, power = this.power - 1, maxPower = this.maxPower };
			player.AddTimer(Key, next);
			next.DueInSeconds = 4;
		}
	}
}
