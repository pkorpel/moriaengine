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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_rozdeleni (Bezejmenny, Halda Molle's boss): when hit at CopyFloor percent of its hits or more,
	/// CopyChance permille it splits: it loses a tenth of its hits, and a copy of it with 90% of what's left appears
	/// beside it for CopyLifetime seconds, worth a third of its experience ("Mostrum se rozdelilo."). While its copy
	/// lives nothing hurts it ("Monstrum ti odolava."). Fixed from Erebor: the guard ends when the copy is gone for any
	/// reason (Erebor's stayed for good if the copy timed out), and the boss keeps its own resists (Erebor's guard
	/// functions blanked its summon and bleed resists on the first blow, and could leave it immune).</summary>
	[ViewableClass]
	public class RozdeleniAbilityDef : PassiveAbilityDef {
		private FieldValue copyChance, copyFloor, copyLifetime;

		public RozdeleniAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.copyChance = this.InitTypedField("rozdeleniCopyChance", new double[] { 35, 35 }, typeof(double[]));
			this.copyFloor = this.InitTypedField("rozdeleniCopyFloor", 15, typeof(int));
			this.copyLifetime = this.InitTypedField("rozdeleniCopyLifetime", 43200.0, typeof(double));
		}

		public double[] CopyChance { get { return (double[]) this.copyChance.CurrentValue; } set { this.copyChance.CurrentValue = value; } }
		public int CopyFloor { get { return (int) this.copyFloor.CurrentValue; } set { this.copyFloor.CurrentValue = value; } }
		public double CopyLifetime { get { return (double) this.copyLifetime.CurrentValue; } set { this.copyLifetime.CurrentValue = value; } }
	}

	/// <summary>#1022 e_frosty_wave (Bezejmenny): when hit, and once on sight of each new player, Chance permille a wave of
	/// cold: tiles on the 8 rays at 1-2 tiles, a second later 3-4, then 5-6 (Strength more steps), each for a second. A
	/// player on one (not a GM) "Byl si zmrazen.", loses Damage hits whatever the armor, and is paralysed ParalyzeSeconds.</summary>
	[ViewableClass]
	public class FrostWaveAbilityDef : PassiveAbilityDef {
		private FieldValue chance, strength, damage, paralyzeSeconds;

		public FrostWaveAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("frostWaveChance", new double[] { 240, 240 }, typeof(double[]));
			this.strength = this.InitTypedField("frostWaveStrength", new double[] { 2, 2 }, typeof(double[]));
			this.damage = this.InitTypedField("frostWaveDamage", 59, typeof(int));
			this.paralyzeSeconds = this.InitTypedField("frostWaveParalyzeSeconds", 6.0, typeof(double));
		}

		public new double[] Chance { get { return (double[]) this.chance.CurrentValue; } set { this.chance.CurrentValue = value; } }
		public double[] Strength { get { return (double[]) this.strength.CurrentValue; } set { this.strength.CurrentValue = value; } }
		public int Damage { get { return (int) this.damage.CurrentValue; } set { this.damage.CurrentValue = value; } }
		public double ParalyzeSeconds { get { return (double) this.paralyzeSeconds.CurrentValue; } set { this.paralyzeSeconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_drop_egg (the warniaks): on sight of a new player, EggChance permille it lays an egg ("Vejce"). Unless
	/// someone carrying more than StompWeight stones steps on it ("Rozslapl si vejce."), HatchDelay seconds later a warniak
	/// of its kind (the species' WarniakType) hatches, and is gone CleanupDelay seconds after the egg was laid.</summary>
	[ViewableClass]
	public class DropEggAbilityDef : PassiveAbilityDef {
		private FieldValue eggChance, hatchDelay, stompWeight, cleanupDelay;

		public DropEggAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.eggChance = this.InitTypedField("dropEggEggChance", 500.0, typeof(double));
			this.hatchDelay = this.InitTypedField("dropEggHatchDelay", 60.0, typeof(double));
			this.stompWeight = this.InitTypedField("dropEggStompWeight", 400.0, typeof(double));
			this.cleanupDelay = this.InitTypedField("dropEggCleanupDelay", 700.0, typeof(double));
		}

		public double EggChance { get { return (double) this.eggChance.CurrentValue; } set { this.eggChance.CurrentValue = value; } }
		public double HatchDelay { get { return (double) this.hatchDelay.CurrentValue; } set { this.hatchDelay.CurrentValue = value; } }
		public double StompWeight { get { return (double) this.stompWeight.CurrentValue; } set { this.stompWeight.CurrentValue = value; } }
		public double CleanupDelay { get { return (double) this.cleanupDelay.CurrentValue; } set { this.cleanupDelay.CurrentValue = value; } }
	}

	/// <summary>#1022 e_warniak_degrade (warniak 4): dying, the next smaller warniak (the species' WarniakCount) rises on the
	/// spot, and degrades again in turn, down to the smallest.</summary>
	[ViewableClass]
	public class WarniakDegradeAbilityDef : PassiveAbilityDef {
		public WarniakDegradeAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>#1022 e_warniak_drop (Marali koren): when hit, HitChance permille at most once in HitCooldown seconds a
	/// warniak (tier 2, 1 or 3, 3:1:1) beside it; on sight of a new player, at most once in SightCooldown seconds a tier 2
	/// that attacks them; dying, one of each tier 1-4. Each lives Lifetime seconds.</summary>
	[ViewableClass]
	public class WarniakDropAbilityDef : PassiveAbilityDef {
		private FieldValue hitChance, hitCooldown, sightCooldown, lifetime;

		public WarniakDropAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hitChance = this.InitTypedField("warniakDropHitChance", new double[] { 100, 100 }, typeof(double[]));
			this.hitCooldown = this.InitTypedField("warniakDropHitCooldown", 10.0, typeof(double));
			this.sightCooldown = this.InitTypedField("warniakDropSightCooldown", 50.0, typeof(double));
			this.lifetime = this.InitTypedField("warniakDropLifetime", 3600.0, typeof(double));
		}

		public double[] HitChance { get { return (double[]) this.hitChance.CurrentValue; } set { this.hitChance.CurrentValue = value; } }
		public double HitCooldown { get { return (double) this.hitCooldown.CurrentValue; } set { this.hitCooldown.CurrentValue = value; } }
		public double SightCooldown { get { return (double) this.sightCooldown.CurrentValue; } set { this.sightCooldown.CurrentValue = value; } }
		public double Lifetime { get { return (double) this.lifetime.CurrentValue; } set { this.lifetime.CurrentValue = value; } }
	}

	public static partial class MonsterSpecialAttacks {
		internal static RozdeleniAbilityDef RozdeleniDef { get { return Lookup<RozdeleniAbilityDef>("a_tevent_e_rozdeleni"); } }
		internal static FrostWaveAbilityDef FrostWaveDef { get { return Lookup<FrostWaveAbilityDef>("a_tevent_e_frosty_wave"); } }
		internal static DropEggAbilityDef DropEggDef { get { return Lookup<DropEggAbilityDef>("a_tevent_e_drop_egg"); } }
		internal static WarniakDegradeAbilityDef WarniakDegradeDef { get { return Lookup<WarniakDegradeAbilityDef>("a_tevent_e_warniak_degrade"); } }
		internal static WarniakDropAbilityDef WarniakDropDef { get { return Lookup<WarniakDropAbilityDef>("a_tevent_e_warniak_drop"); } }

		private const int FrostEffect = 0x36B0, FrostHue = 0x984, EggHue = 0x481, HatchSound = 0x30C;
		private static readonly TagKey splitCopyTK = TagKey.Acquire("_rozdeleniCopy_");
		private static readonly TagKey warniakCountTK = TagKey.Acquire("_warniakCount_");
		private static readonly TagKey warniakHitNextTK = TagKey.Acquire("_warniakHitNext_");
		private static readonly TagKey warniakSightNextTK = TagKey.Acquire("_warniakSightNext_");
		internal static readonly PluginKey frostParalyzePK = PluginKey.Acquire("_frostWave_");

		/// <summary>TrySeeNewPlayers' list: the Halda Molle monsters that react to a new player in sight.</summary>
		internal static bool HasHaldaMolleSight(NPC self) {
			return Reacts(self, FrostWaveDef) || Reacts(self, DropEggDef) || Reacts(self, WarniakDropDef);
		}

		/// <summary>TryReactToHit's part for Halda Molle.</summary>
		internal static void TryHaldaMolleReactions(NPC self, Character attacker) {
			TrySplit(self, attacker);
			FrostWaveAbilityDef frost = FrostWaveDef;
			if (Reacts(self, frost) && RollChance(Tuned(self, "frostWaveChance", frost.Chance))) {
				StartFrostWave(self);
			}
			WarniakDropAbilityDef drop = WarniakDropDef;
			if (Reacts(self, drop)) {
				double now = Globals.TimeInSeconds;
				if (!((self.GetTag(warniakHitNextTK) is double next) && (next >= now)) && RollChance(Tuned(self, "warniakDropHitChance", drop.HitChance))) {
					self.SetTag(warniakHitNextTK, now + drop.HitCooldown);
					string[] tiers = { "c_hm_warniak_2", "c_hm_warniak_2", "c_hm_warniak_2", "c_hm_warniak_1", "c_hm_warniak_3" };
					SummonGroup(self, new[] { tiers[Globals.dice.Next(tiers.Length)] }, self.X, self.Y, 1, drop.Lifetime, null, false, false);
				}
			}
		}

		/// <summary>SeeNewPlayer's part for Halda Molle.</summary>
		internal static void HaldaMolleSeesPlayer(NPC self, Player player) {
			FrostWaveAbilityDef frost = FrostWaveDef;
			if (Reacts(self, frost) && RollChance(Tuned(self, "frostWaveChance", frost.Chance))) {
				StartFrostWave(self);
			}
			DropEggAbilityDef egg = DropEggDef;
			if (Reacts(self, egg) && RollChance(egg.EggChance)) {
				LayEgg(self, egg);
			}
			WarniakDropAbilityDef drop = WarniakDropDef;
			if (Reacts(self, drop)) {
				double now = Globals.TimeInSeconds;
				if (!((self.GetTag(warniakSightNextTK) is double next) && (next >= now))) {
					self.SetTag(warniakSightNextTK, now + drop.SightCooldown);
					SummonGroup(self, new[] { "c_hm_warniak_2" }, self.X, self.Y, 1, drop.Lifetime, player, false, false);
				}
			}
		}

		/// <summary>OnMonsterDeath's part for Halda Molle.</summary>
		internal static void OnHaldaMolleDeath(NPC self) {
			WarniakDropAbilityDef drop = WarniakDropDef;
			if (Reacts(self, drop)) {
				SummonGroup(self, new[] { "c_hm_warniak_1", "c_hm_warniak_2", "c_hm_warniak_3", "c_hm_warniak_4" }, self.X, self.Y, 1, drop.Lifetime, null, false, false);
			}
			int count;
			if (self.GetTag(warniakCountTK) is int left) {
				count = left;
			} else if (Reacts(self, WarniakDegradeDef)) {
				count = Tuned(self, "warniakCount", new double[] { 0, 0 });
			} else {
				return;
			}
			if ((count > 0) && (ThingDef.GetByDefname("c_hm_warniak_" + count) is NPCDef smaller)) {
				NPC child = (NPC) smaller.Create(self.X, self.Y, self.Z, self.M);
				child.SetTag(warniakCountTK, count - 1);
			}
		}

		#region e_rozdeleni
		private static void TrySplit(NPC self, Character attacker) {
			RozdeleniAbilityDef def = RozdeleniDef;
			if (!Reacts(self, def) || IsGuardedByCopy(self) || (self.MaxHits <= 0) ||
					((self.Hits * 100 / self.MaxHits) < def.CopyFloor) || !RollChance(Tuned(self, "rozdeleniCopyChance", def.CopyChance))) {
				return;
			}
			Split(self, def, attacker);
		}

		internal static NPC Split(NPC self, RozdeleniAbilityDef def, Character attacker) {
			self.Hits = (short) (self.Hits * 90 / 100);
			NPC copy = SummonGroup(self, new[] { self.Def.PrettyDefname }, self.X, self.Y, 1, def.CopyLifetime, null, false, false)[0];
			copy.Hits = (short) Math.Min(copy.MaxHits, self.Hits * 90 / 100);
			copy.ExperienceReward = self.ExperienceReward / 3;
			NPCAiTimer own = NPCAiTimer.GetFor(self), its = NPCAiTimer.GetFor(copy);
			if ((own != null) && (its != null) && own.hasHome) {
				its.hasHome = true;
				its.homeX = own.homeX;
				its.homeY = own.homeY;
				its.homeZ = own.homeZ;
				its.homeM = own.homeM;
			}
			self.SetTag(splitCopyTK, copy);
			attacker?.SysMessage("Mostrum se rozdelilo.");
			return copy;
		}

		internal static bool IsGuardedByCopy(Character self) {
			if (!(self.GetTag(splitCopyTK) is NPC copy)) {
				return false;
			}
			if (copy.IsDeleted || copy.Flag_Dead) {
				self.RemoveTag(splitCopyTK);
				return false;
			}
			return true;
		}

		/// <summary>DamageManager.CauseDamage: true when the split boss can't be hurt while its copy lives.</summary>
		internal static bool CopyGuardRefuses(Character attacker, Character defender, DamageType flags) {
			if ((flags == DamageType.Irresistable) || !IsGuardedByCopy(defender)) { //no resist bits at all: Erebor's group resists didn't cover it either
				return false;
			}
			attacker?.RedMessage("Monstrum ti odolava.");
			return true;
		}
		#endregion

		#region e_frosty_wave
		private class FrostTile {
			internal Character owner;
			internal double expiresAt;
			internal HashSet<Character> hit;
		}

		private static readonly Dictionary<long, FrostTile> frostTiles = new Dictionary<long, FrostTile>();
		private static readonly int[][] frostRays = { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, -1 }, new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 } };

		internal static void StartFrostWave(NPC self) {
			FrostWaveTimer wave = new FrostWaveTimer { step = 0, steps = Tuned(self, "frostWaveStrength", FrostWaveDef.Strength), x = self.X, y = self.Y, z = self.Z, m = self.M };
			FrostWaveStep(self, wave);
			if (wave.step <= wave.steps) {
				self.AddTimer(FrostWaveTimer.Key, wave);
				wave.DueInSeconds = 1;
				wave.PeriodInSeconds = 1;
			}
		}

		/// <summary>One ring of the wave: distance 2*step+1 and 2*step+2 on the 8 rays.</summary>
		internal static void FrostWaveStep(NPC owner, FrostWaveTimer wave) {
			for (int distance = (2 * wave.step) + 1; distance <= (2 * wave.step) + 2; distance++) {
				foreach (int[] ray in frostRays) {
					PlaceFrostTile(owner, wave.x + (ray[0] * distance), wave.y + (ray[1] * distance), wave.z, wave.m);
				}
			}
			wave.step++;
		}

		private static void PlaceFrostTile(Character owner, int x, int y, int z, byte m) {
			if (!Map.IsValidPos(x, y, m)) {
				return;
			}
			EffectFactory.StationaryEffectAt(new Point4D(x, y, z, m), FrostEffect, 10, 10, true, false, FrostHue, RenderModes.Opaque);
			FrostTile tile = new FrostTile { owner = owner, expiresAt = Globals.TimeInSeconds + 1, hit = new HashSet<Character>() };
			if (frostTiles.Count > 256) {
				List<long> old = new List<long>();
				foreach (KeyValuePair<long, FrostTile> pair in frostTiles) {
					if (pair.Value.expiresAt < Globals.TimeInSeconds) {
						old.Add(pair.Key);
					}
				}
				foreach (long key in old) {
					frostTiles.Remove(key);
				}
			}
			frostTiles[HazardKey(x, y, m)] = tile;
			foreach (AbstractCharacter ch in Map.GetMap(m).GetCharsInRange(x, y, 0)) {
				if ((ch is Character standing) && (standing.X == x) && (standing.Y == y)) {
					Freeze(tile, standing);
				}
			}
		}

		/// <summary>Character.On_StepTaken: a frost tile still cold.</summary>
		internal static void CheckFrostStep(Character stepping) {
			if ((frostTiles.Count > 0) && frostTiles.TryGetValue(HazardKey(stepping.X, stepping.Y, stepping.M), out FrostTile tile)) {
				if (tile.expiresAt < Globals.TimeInSeconds) {
					frostTiles.Remove(HazardKey(stepping.X, stepping.Y, stepping.M));
				} else {
					Freeze(tile, stepping);
				}
			}
		}

		private static void Freeze(FrostTile tile, Character victim) {
			FrostWaveAbilityDef def = FrostWaveDef;
			if ((def == null) || !(victim is Player) || victim.IsGM || !victim.IsAliveAndValid || !tile.hit.Add(victim)) {
				return;
			}
			victim.SysMessage("Byl si zmrazen.");
			DamageManager.CauseDamage(tile.owner.IsDeleted ? null : tile.owner, victim, DamageType.Irresistable, def.Damage);
			if (!victim.IsDeleted && !victim.Flag_Dead) {
				ParalyzeEffectPluginDef.Paralyze(victim, frostParalyzePK, TimeSpan.FromSeconds(def.ParalyzeSeconds));
			}
		}
		#endregion

		#region e_drop_egg
		private static readonly HashSet<Item> warniakEggs = new HashSet<Item>();

		internal static Item LayEgg(NPC self, DropEggAbilityDef def) {
			if (!(ThingDef.GetByDefname("i_hm_warniak_egg") is ThingDef eggDef)) {
				return null;
			}
			int type = Tuned(self, "warniakType", new double[] { 1, 1 });
			Item egg = (Item) eggDef.Create(self.X, self.Y, self.Z, self.M);
			egg.Name = "Vejce";
			egg.Color = EggHue;
			egg.SetAttribute(ItemAttributes.attr_move_never, true);
			WarniakEggTimer hatch = new WarniakEggTimer { warniak = "c_hm_warniak_" + type, laidAt = Globals.TimeInSeconds };
			egg.AddTimer(WarniakEggTimer.Key, hatch);
			hatch.DueInSeconds = def.HatchDelay;
			warniakEggs.Add(egg);
			return egg;
		}

		/// <summary>Character.On_StepTaken: stomping an egg.</summary>
		internal static void CheckEggStep(Character stepping) {
			if ((warniakEggs.Count == 0) || !(stepping is Player player) || player.IsGM) {
				return;
			}
			foreach (Item egg in new List<Item>(warniakEggs)) {
				if (egg.IsDeleted) {
					warniakEggs.Remove(egg);
				} else if ((egg.X == player.X) && (egg.Y == player.Y) && (egg.M == player.M) && (egg.GetTimer(WarniakEggTimer.Key) is WarniakEggTimer hatch) && !hatch.stomped) {
					StompEgg(player, egg, hatch);
				}
			}
		}

		internal static bool StompEgg(Player player, Item egg, WarniakEggTimer hatch) {
			DropEggAbilityDef def = DropEggDef;
			if ((def == null) || (MovementStamina.CarriedWeight(player) <= def.StompWeight)) {
				player.SysMessage("Nedokazes rozeslapnout vejce.");
				return false;
			}
			player.SysMessage("Rozslapl si vejce.");
			hatch.stomped = true;
			hatch.DueInSeconds = def.HatchDelay;
			return true;
		}

		internal static NPC HatchEgg(Item egg, WarniakEggTimer hatch) {
			warniakEggs.Remove(egg);
			NPC warniak = null;
			if (!hatch.stomped && (ThingDef.GetByDefname(hatch.warniak) is NPCDef def)) {
				warniak = (NPC) def.Create(egg.X, egg.Y, egg.Z, egg.M);
				egg.Sound(HatchSound);
				double cleanup = (DropEggDef?.CleanupDelay ?? 700) - (Globals.TimeInSeconds - hatch.laidAt);
				VanishIn(warniak, Math.Max(1, cleanup));
			}
			egg.Delete();
			return warniak;
		}
		#endregion
	}

	/// <summary>The cold going out, a ring a second.</summary>
	[SaveableClass, DeepCopyableClass]
	public class FrostWaveTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_frostWave_");

		[SaveableData, CopyableData]
		public int step, steps, x, y, z;

		[SaveableData, CopyableData]
		public byte m;

		[LoadingInitializer, DeepCopyImplementation]
		public FrostWaveTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC owner) || owner.IsDeleted || (this.step > this.steps)) {
				cont.DeleteTimer(Key);
				return;
			}
			MonsterSpecialAttacks.FrostWaveStep(owner, this);
		}
	}

	/// <summary>i_hm_warniak_egg's timer: it hatches unless stomped.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WarniakEggTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_warniakEgg_");

		[SaveableData, CopyableData]
		public string warniak;

		[SaveableData, CopyableData]
		public double laidAt;

		[SaveableData, CopyableData]
		public bool stomped;

		[LoadingInitializer, DeepCopyImplementation]
		public WarniakEggTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Item egg) && !egg.IsDeleted) {
				MonsterSpecialAttacks.HatchEgg(egg, this);
			}
		}
	}
}
