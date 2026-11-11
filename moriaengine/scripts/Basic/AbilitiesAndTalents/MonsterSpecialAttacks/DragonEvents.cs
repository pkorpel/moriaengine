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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>A dragon's attack with a chance (permille), a cooldown (s), damage and a fire type (1-5), per species on
	/// the NPCDef.</summary>
	public abstract class DragonAttackAbilityDef : PassiveAbilityDef {
		private FieldValue chance, cooldown, damage, fireType;

		protected DragonAttackAbilityDef(string defname, string filename, int headerLine, string prefix, double chance, double cooldown, double damage, double fireType)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField(prefix + "Chance", new double[] { chance, chance }, typeof(double[]));
			this.cooldown = this.InitTypedField(prefix + "Cooldown", new double[] { cooldown, cooldown }, typeof(double[]));
			this.damage = this.InitTypedField(prefix + "Damage", new double[] { damage, damage }, typeof(double[]));
			this.fireType = this.InitTypedField(prefix + "FireType", new double[] { fireType, fireType }, typeof(double[]));
		}

		/// <summary>Permille, per hit taken.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Seconds before the next one.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>A strong tile's damage; a weak one does half.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>1 fire, 2 cold, 3 poison, 4 black, 5 white.</summary>
		public double[] FireType {
			get { return (double[]) this.fireType.CurrentValue; }
			set { this.fireType.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_fire_dragon (8 carriers): when hit, it draws breath (" se zhluboka nadychuje"), stands still 1 s,
	/// then breathes the dragon fire cone where it faces.</summary>
	[ViewableClass]
	public class FireDragonAbilityDef : DragonAttackAbilityDef {
		public FireDragonAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "fireDragon", 800, 20, 120, 4) {
		}
	}

	/// <summary>#1022 e_fire_dragon_bomb (5): when hit, a cannonball onto its target and the bomb pattern of dragon fire
	/// there.</summary>
	[ViewableClass]
	public class FireDragonBombAbilityDef : DragonAttackAbilityDef {
		public FireDragonBombAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "fireDragonBomb", 800, 10, 40, 1) {
		}
	}

	/// <summary>#1022 e_fire_dragon_round (the white dragon): when hit, 2 s later it stands still and spins, a turn of one
	/// direction and a breath each second, Turns*9 times; the tiles skip line of sight, resist and lingering effects.</summary>
	[ViewableClass]
	public class FireDragonRoundAbilityDef : DragonAttackAbilityDef {
		private FieldValue turns;

		public FireDragonRoundAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "fireDragonRound", 1000, 40, 100, 3) {
			this.turns = this.InitTypedField("fireDragonRoundTurns", new double[] { 2, 2 }, typeof(double[]));
		}

		/// <summary>Whole turns, each 9 breaths (Erebor's turns*9).</summary>
		public double[] Turns {
			get { return (double[]) this.turns.CurrentValue; }
			set { this.turns.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_fire_dragon_cage (the black dragon): when hit, a second later every player in its 64x64 map cell
	/// but a warrior who slips out (66%) is shut in by fire on the 8 tiles around them for Duration seconds. The fire
	/// doesn't burn; a double click weakens a tile ("Ohen se oslabil..."), the 9th puts it out ("Ohen zmizel"). Players
	/// can't step through it ("Zar je prilis silny, neda se tam projit!").</summary>
	[ViewableClass]
	public class FireDragonCageAbilityDef : PassiveAbilityDef {
		private FieldValue chance, cooldown, fireType, duration, warriorEscape;

		public FireDragonCageAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("fireDragonCageChance", new double[] { 1000, 1000 }, typeof(double[]));
			this.cooldown = this.InitTypedField("fireDragonCageCooldown", new double[] { 25, 25 }, typeof(double[]));
			this.fireType = this.InitTypedField("fireDragonCageFireType", new double[] { 4, 4 }, typeof(double[]));
			this.duration = this.InitTypedField("fireDragonCageDuration", new double[] { 16, 16 }, typeof(double[]));
			this.warriorEscape = this.InitTypedField("fireDragonCageWarriorEscape", 66.0, typeof(double));
		}

		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>Only the colour here.</summary>
		public double[] FireType {
			get { return (double[]) this.fireType.CurrentValue; }
			set { this.fireType.CurrentValue = value; }
		}

		/// <summary>Seconds the cage stands; a tile takes Duration/2 + 1 clicks.</summary>
		public double[] Duration {
			get { return (double[]) this.duration.CurrentValue; }
			set { this.duration.CurrentValue = value; }
		}

		/// <summary>Percent a warrior slips out (chance(66,2)).</summary>
		public double WarriorEscape {
			get { return (double) this.warriorEscape.CurrentValue; }
			set { this.warriorEscape.CurrentValue = value; }
		}
	}

	/// <summary>A dragon's aura: chance, cooldown, ticks (power), seconds between ticks, damage.</summary>
	public abstract class DragonAuraAbilityDef : PassiveAbilityDef {
		private FieldValue chance, cooldown, interval, ticks, damage;

		protected DragonAuraAbilityDef(string defname, string filename, int headerLine, string prefix, double cooldown, double interval, double damage)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField(prefix + "Chance", new double[] { 1000, 1000 }, typeof(double[]));
			this.cooldown = this.InitTypedField(prefix + "Cooldown", new double[] { cooldown, cooldown }, typeof(double[]));
			this.interval = this.InitTypedField(prefix + "Interval", new double[] { interval, interval }, typeof(double[]));
			this.ticks = this.InitTypedField(prefix + "Ticks", new double[] { 10, 10 }, typeof(double[]));
			this.damage = this.InitTypedField(prefix + "Damage", new double[] { damage, damage }, typeof(double[]));
		}

		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>Seconds between ticks.</summary>
		public double[] Interval {
			get { return (double[]) this.interval.CurrentValue; }
			set { this.interval.CurrentValue = value; }
		}

		/// <summary>Erebor's power: ticks, each weaker.</summary>
		public double[] Ticks {
			get { return (double[]) this.ticks.CurrentValue; }
			set { this.ticks.CurrentValue = value; }
		}

		/// <summary>The first tick's damage.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_fire_dragon_aura (6 dragons): when hit, every player in the attacker's 64x64 map cell gets the
	/// dragon aura, or has it renewed: a tick each Interval seconds of Damage*left/Ticks fire, less the dragon resist
	/// percent, which costs stamina too (all of it if they have less than the tick, else a third), and mages, necromancers
	/// and mystics take three quarters of the damage and lose a quarter of it in mana. Fixed from Erebor: the resist counts
	/// once, and only for a dragon whose aura resist is set (Erebor's took it twice, as its own comment says it shouldn't).</summary>
	[ViewableClass]
	public class FireDragonAuraAbilityDef : DragonAuraAbilityDef {
		private FieldValue resist;

		public FireDragonAuraAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "fireDragonAura", 30, 5, 50) {
			this.resist = this.InitTypedField("fireDragonAuraResist", new double[] { 1, 1 }, typeof(double[]));
		}

		/// <summary>1: the dragon resist lowers the aura; 0: it doesn't.</summary>
		public double[] Resist {
			get { return (double[]) this.resist.CurrentValue; }
			set { this.resist.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_bahenny_drak_aura (the swamp dragon): the aura of the attacker's map cell, plain damage, no resist,
	/// no antidote ("nejde na to resist ani protijed").</summary>
	[ViewableClass]
	public class SwampDragonAuraAbilityDef : DragonAuraAbilityDef {
		public SwampDragonAuraAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "swampDragonAura", 60, 10, 50) {
		}
	}

	/// <summary>#1022 e_bahenny_drak_krev (the swamp and green dragons): as it falls below 85, 65, 45, 25 and 5 percent of its
	/// hits, it bleeds 3 puddles of dragon blood within 3 tiles. A second later each puddle hurts everyone next to it (Damage,
	/// pets ten times as much); for 30 minutes stepping on it (once every 10 s) does Damage fire and poisons a player with
	/// dragon-blood poison: PoisonDamage, then less each 4 s, PoisonTicks times. Fixed from Erebor: a hit that jumps past a
	/// threshold still bleeds (Erebor's stage then blocked all the later ones).</summary>
	[ViewableClass]
	public class DragonBloodAbilityDef : PassiveAbilityDef {
		private FieldValue damage, poisonDamage, poisonTicks;

		public DragonBloodAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.damage = this.InitTypedField("dragonBloodDamage", 80.0, typeof(double));
			this.poisonDamage = this.InitTypedField("dragonBloodPoisonDamage", 40.0, typeof(double));
			this.poisonTicks = this.InitTypedField("dragonBloodPoisonTicks", 8.0, typeof(double));
		}

		public double Damage {
			get { return (double) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		public double PoisonDamage {
			get { return (double) this.poisonDamage.CurrentValue; }
			set { this.poisonDamage.CurrentValue = value; }
		}

		public double PoisonTicks {
			get { return (double) this.poisonTicks.CurrentValue; }
			set { this.poisonTicks.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_dragon_brain (c_dragon_swamp): once it has seen a player or been hit, every Interval seconds it
	/// heals Regeneration hits and picks, by weight, a round attack (a sound and a ripple: Erebor's swamp dragon had no round
	/// attack damage set, so it hurt nobody, as here), a stamp, a poison breath at its target, or acid. Fixed from Erebor: it
	/// stops thinking when no player is within 18 tiles (Erebor's ran forever after the first sight).</summary>
	[ViewableClass]
	public class DragonBrainAbilityDef : PassiveAbilityDef {
		private FieldValue interval, regeneration, weights, stampDamage, stampParalysis, stampCritical, poisonStrength, acidDamage;

		public DragonBrainAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.interval = this.InitTypedField("dragonBrainInterval", new double[] { 4, 8 }, typeof(double[]));
			this.regeneration = this.InitTypedField("dragonBrainRegeneration", 50.0, typeof(double));
			this.weights = this.InitTypedField("dragonBrainWeights", new double[] { 50, 20, 20, 10 }, typeof(double[]));
			this.stampDamage = this.InitTypedField("dragonBrainStampDamage", 120.0, typeof(double));
			this.stampParalysis = this.InitTypedField("dragonBrainStampParalysis", 2.0, typeof(double));
			this.stampCritical = this.InitTypedField("dragonBrainStampCritical", 20.0, typeof(double));
			this.poisonStrength = this.InitTypedField("dragonBrainPoisonStrength", 15.0, typeof(double));
			this.acidDamage = this.InitTypedField("dragonBrainAcidDamage", 89.0, typeof(double));
		}

		/// <summary>Seconds between thoughts.</summary>
		public double[] Interval {
			get { return (double[]) this.interval.CurrentValue; }
			set { this.interval.CurrentValue = value; }
		}

		/// <summary>Hits back each thought.</summary>
		public double Regeneration {
			get { return (double) this.regeneration.CurrentValue; }
			set { this.regeneration.CurrentValue = value; }
		}

		/// <summary>Weights of: round attack, stamp, poison breath, acid.</summary>
		public double[] Weights {
			get { return (double[]) this.weights.CurrentValue; }
			set { this.weights.CurrentValue = value; }
		}

		/// <summary>The stamp's damage at the dragon's feet, 6 less for each tile further.</summary>
		public double StampDamage {
			get { return (double) this.stampDamage.CurrentValue; }
			set { this.stampDamage.CurrentValue = value; }
		}

		/// <summary>Seconds a stamped player is paralysed.</summary>
		public double StampParalysis {
			get { return (double) this.stampParalysis.CurrentValue; }
			set { this.stampParalysis.CurrentValue = value; }
		}

		/// <summary>Permille-ish (of 101) chance a piece of the ceiling falls too, {40..70} more.</summary>
		public double StampCritical {
			get { return (double) this.stampCritical.CurrentValue; }
			set { this.stampCritical.CurrentValue = value; }
		}

		public double PoisonStrength {
			get { return (double) this.poisonStrength.CurrentValue; }
			set { this.poisonStrength.CurrentValue = value; }
		}

		public double AcidDamage {
			get { return (double) this.acidDamage.CurrentValue; }
			set { this.acidDamage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static T DefOf<T>(string defname, ref T cache) where T : AbilityDef {
			if (cache == null) {
				cache = AbstractScript.GetByDefname(defname) as T;
			}
			return cache;
		}

		private static FireDragonAbilityDef fireDragonAbilityDef;
		internal static FireDragonAbilityDef FireDragonDef {
			get { return DefOf("a_tevent_e_fire_dragon", ref fireDragonAbilityDef); }
		}
		private static FireDragonBombAbilityDef fireDragonBombAbilityDef;
		internal static FireDragonBombAbilityDef FireDragonBombDef {
			get { return DefOf("a_tevent_e_fire_dragon_bomb", ref fireDragonBombAbilityDef); }
		}
		private static FireDragonRoundAbilityDef fireDragonRoundAbilityDef;
		internal static FireDragonRoundAbilityDef FireDragonRoundDef {
			get { return DefOf("a_tevent_e_fire_dragon_round", ref fireDragonRoundAbilityDef); }
		}
		private static FireDragonCageAbilityDef fireDragonCageAbilityDef;
		internal static FireDragonCageAbilityDef FireDragonCageDef {
			get { return DefOf("a_tevent_e_fire_dragon_cage", ref fireDragonCageAbilityDef); }
		}
		private static FireDragonAuraAbilityDef fireDragonAuraAbilityDef;
		internal static FireDragonAuraAbilityDef FireDragonAuraDef {
			get { return DefOf("a_tevent_e_fire_dragon_aura", ref fireDragonAuraAbilityDef); }
		}
		private static SwampDragonAuraAbilityDef swampDragonAuraAbilityDef;
		internal static SwampDragonAuraAbilityDef SwampDragonAuraDef {
			get { return DefOf("a_tevent_e_bahenny_drak_aura", ref swampDragonAuraAbilityDef); }
		}
		private static DragonBloodAbilityDef dragonBloodAbilityDef;
		internal static DragonBloodAbilityDef DragonBloodDef {
			get { return DefOf("a_tevent_e_bahenny_drak_krev", ref dragonBloodAbilityDef); }
		}
		private static DragonBrainAbilityDef dragonBrainAbilityDef;
		internal static DragonBrainAbilityDef DragonBrainDef {
			get { return DefOf("a_tevent_e_dragon_brain", ref dragonBrainAbilityDef); }
		}

		private static readonly TagKey nextDragonFireTK = TagKey.Acquire("_nextDragonFire_");
		private static readonly TagKey nextDragonBombTK = TagKey.Acquire("_nextDragonBomb_");
		private static readonly TagKey nextDragonRoundTK = TagKey.Acquire("_nextDragonRound_");
		private static readonly TagKey nextDragonCageTK = TagKey.Acquire("_nextDragonCage_");
		private static readonly TagKey nextDragonAuraTK = TagKey.Acquire("_nextDragonAura_");
		private static readonly TagKey nextSwampAuraTK = TagKey.Acquire("_nextSwampAura_");
		private static readonly TagKey dragonBloodStageTK = TagKey.Acquire("_dragonBloodStage_");

		/// <summary>The NPCDef's per-species value, else the ability's default.</summary>
		private static int Tuned(Character self, string field, double[] fallback) {
			int[] own = TuningDef(self)?.AbilityTuning(field);
			return (int) RollTunable(own, fallback);
		}

		/// <summary>Erebor's idiom: the chance first, then the cooldown, which a failed roll doesn't use.</summary>
		private static bool RollWithCooldown(Character self, TagKey nextTK, int chance, int cooldown) {
			if (!RollChance(chance)) {
				return false;
			}
			if ((self.GetTag(nextTK) is double next) && (next > Globals.TimeInSeconds)) {
				return false;
			}
			self.SetTag(nextTK, Globals.TimeInSeconds + cooldown);
			return true;
		}

		/// <summary>DamageManager.ProcessSwing, defender side after a landed hit (Erebor's @aftergetswing): the dragon events.</summary>
		internal static void TryDragonReactions(Character attacker, Character defender) {
			if (!(defender is NPC dragon) || !dragon.IsAliveAndValid) {
				return;
			}
			TryFireDragon(dragon);
			TryFireDragonBomb(dragon, attacker);
			TryFireDragonRound(dragon);
			TryFireDragonCage(dragon);
			TryDragonAura(dragon, attacker, FireDragonAuraDef, nextDragonAuraTK, "fireDragonAura", false);
			TryDragonAura(dragon, attacker, SwampDragonAuraDef, nextSwampAuraTK, "swampDragonAura", true);
			TryDragonBlood(dragon);
			WakeDragonBrain(dragon);
		}

		private static void TryFireDragon(NPC dragon) {
			FireDragonAbilityDef def = FireDragonDef;
			if (!Reacts(dragon, def) ||
					!RollWithCooldown(dragon, nextDragonFireTK, Tuned(dragon, "fireDragonChance", def.Chance), Tuned(dragon, "fireDragonCooldown", def.Cooldown))) {
				return;
			}
			dragon.Emote(" se zhluboka nadychuje");
			NPCAiTimer.Freeze(dragon, 1);
			DragonActionTimer.Start(dragon, DragonActionTimer.Breath, 1, Tuned(dragon, "fireDragonDamage", def.Damage), Tuned(dragon, "fireDragonFireType", def.FireType), 0);
		}

		/// <summary>The breath itself, without the roll - for tests and the action timer.</summary>
		public static void ForceFireDragonBreath(NPC dragon, int damage, int fireType) {
			NPCAiTimer.Unfreeze(dragon);
			BreatheDragonFire(dragon, damage, fireType, false);
		}

		private static void TryFireDragonBomb(NPC dragon, Character attacker) {
			FireDragonBombAbilityDef def = FireDragonBombDef;
			if (!Reacts(dragon, def) ||
					!RollWithCooldown(dragon, nextDragonBombTK, Tuned(dragon, "fireDragonBombChance", def.Chance), Tuned(dragon, "fireDragonBombCooldown", def.Cooldown))) {
				return;
			}
			Character target = NPCAiTimer.GetFor(dragon)?.Target ?? attacker;
			if (target == null) {
				return;
			}
			DropDragonBomb(dragon, target, Tuned(dragon, "fireDragonBombDamage", def.Damage), Tuned(dragon, "fireDragonBombFireType", def.FireType));
		}

		private static void TryFireDragonRound(NPC dragon) {
			FireDragonRoundAbilityDef def = FireDragonRoundDef;
			if (!Reacts(dragon, def) ||
					!RollWithCooldown(dragon, nextDragonRoundTK, Tuned(dragon, "fireDragonRoundChance", def.Chance), Tuned(dragon, "fireDragonRoundCooldown", def.Cooldown))) {
				return;
			}
			dragon.Emote(" se zhluboka nadychuje");
			int breaths = Tuned(dragon, "fireDragonRoundTurns", def.Turns) * 9;
			DragonActionTimer.Start(dragon, DragonActionTimer.Round, 2, Tuned(dragon, "fireDragonRoundDamage", def.Damage), Tuned(dragon, "fireDragonRoundFireType", def.FireType), breaths);
		}

		/// <summary>One breath of the spin: frozen, a turn clockwise, a raw breath. False after the last.</summary>
		internal static void SpinDragon(NPC dragon, int damage, int fireType) {
			NPCAiTimer.Freeze(dragon, 1.5);
			dragon.Direction = (Direction) (((int) dragon.Direction + 1) % 8);
			BreatheDragonFire(dragon, damage, fireType, true);
		}

		private static void TryFireDragonCage(NPC dragon) {
			FireDragonCageAbilityDef def = FireDragonCageDef;
			if (!Reacts(dragon, def) ||
					!RollWithCooldown(dragon, nextDragonCageTK, Tuned(dragon, "fireDragonCageChance", def.Chance), Tuned(dragon, "fireDragonCageCooldown", def.Cooldown))) {
				return;
			}
			DragonActionTimer.Start(dragon, DragonActionTimer.Cage, 1, Tuned(dragon, "fireDragonCageDuration", def.Duration), Tuned(dragon, "fireDragonCageFireType", def.FireType), 0);
		}

		/// <summary>f_fire_dragon_cage: every player of the dragon's map cell but a lucky warrior, shut in.</summary>
		public static int CageAround(NPC dragon, int duration, int fireType) {
			FireDragonCageAbilityDef def = FireDragonCageDef;
			double escape = (def == null) ? 66 : def.WarriorEscape;
			int caged = 0;
			foreach (Character player in PlayersInSector(dragon, dragon.X, dragon.Y)) {
				if (Professions.HasProfession(player, Professions.Warrior) && (Globals.dice.Next(100) < escape)) {
					continue;
				}
				DragonCage.Build(player, duration, fireType);
				caged++;
			}
			return caged;
		}

		private static void TryDragonAura(NPC dragon, Character attacker, DragonAuraAbilityDef def, TagKey nextTK, string prefix, bool swamp) {
			if (!Reacts(dragon, def) || (attacker == null) ||
					!RollWithCooldown(dragon, nextTK, Tuned(dragon, prefix + "Chance", def.Chance), Tuned(dragon, prefix + "Cooldown", def.Cooldown))) {
				return;
			}
			bool resist = !swamp && (Tuned(dragon, "fireDragonAuraResist", ((FireDragonAuraAbilityDef) def).Resist) != 0);
			SpreadDragonAura(dragon, attacker, swamp, resist, Tuned(dragon, prefix + "Damage", def.Damage), Tuned(dragon, prefix + "Ticks", def.Ticks), Tuned(dragon, prefix + "Interval", def.Interval));
		}

		/// <summary>The aura onto every player of the attacker's map cell (Erebor's creator item stood where the attacker
		/// stood). Returns how many.</summary>
		public static int SpreadDragonAura(NPC dragon, Character attacker, bool swamp, bool resist, int damage, int ticks, int interval) {
			int count = 0;
			foreach (Character player in PlayersInSector(attacker, attacker.X, attacker.Y)) {
				DragonAuraTimer.Apply(player, swamp, resist, damage, ticks, interval);
				count++;
			}
			return count;
		}

		/// <summary>bahenny_drak_krev: bleeds at 85, 65, 45, 25 and 5 percent.</summary>
		private static void TryDragonBlood(NPC dragon) {
			DragonBloodAbilityDef def = DragonBloodDef;
			if (!Reacts(dragon, def) || (dragon.MaxHits <= 0)) {
				return;
			}
			int percent = (dragon.Hits * 100) / dragon.MaxHits;
			int[] thresholds = { 85, 65, 45, 25, 5 };
			int stage = (dragon.GetTag(dragonBloodStageTK) is int done) ? done : 0;
			while ((stage < thresholds.Length) && (percent < thresholds[stage])) {
				stage++;
				for (int i = 0; i < 3; i++) {
					DragonBlood.Place(dragon, dragon.X + Globals.dice.Next(-3, 4), dragon.Y + Globals.dice.Next(-3, 4), (int) def.Damage);
				}
			}
			dragon.SetTag(dragonBloodStageTK, stage);
		}

		/// <summary>e_dragon_brain's @NPCSeeNewPlayer and @aftergetswing: the brain starts thinking.</summary>
		internal static void WakeDragonBrain(NPC dragon) {
			DragonBrainAbilityDef def = DragonBrainDef;
			if (Reacts(dragon, def) && (dragon.GetTimer(DragonBrainTimer.Key) == null)) {
				DragonBrainTimer timer = new DragonBrainTimer();
				dragon.AddTimer(DragonBrainTimer.Key, timer);
				timer.DueInSeconds = ScriptUtil.GetRandInRange(def.Interval[0], def.Interval[1]);
			}
		}

		/// <summary>NPCAiTimer's tick: a brain dragon wakes on seeing a player within 18.</summary>
		internal static void TryDragonBrainSight(NPC dragon) {
			if (Reacts(dragon, DragonBrainDef) && (dragon.GetTimer(DragonBrainTimer.Key) == null) && (DragonBrainPlayerNear(dragon) != null)) {
				WakeDragonBrain(dragon);
			}
		}

		internal static Character DragonBrainPlayerNear(NPC dragon) {
			foreach (AbstractCharacter ch in dragon.GetMap().GetCharsInRange(dragon.X, dragon.Y, 18)) {
				if ((ch is Character c) && IsDragonFireVictim(c) && dragon.CanSeeLOS(c).Allow) {
					return c;
				}
			}
			return null;
		}

		/// <summary>dragonhandle: one thought. `action` -1 rolls by the weights; 0 round attack, 1 stamp, 2 poison breath,
		/// 3 acid. False when the picked action couldn't act (the next thought comes in 1 s, as on Erebor).</summary>
		public static bool DragonThink(NPC dragon, int action) {
			DragonBrainAbilityDef def = DragonBrainDef;
			if (def == null) {
				return true;
			}
			if (dragon.Hits < dragon.MaxHits) {
				dragon.Hits = (short) Math.Min(dragon.MaxHits, dragon.Hits + (int) def.Regeneration);
			}
			if (action < 0) {
				double total = 0;
				foreach (double w in def.Weights) {
					total += w;
				}
				double roll = Globals.dice.NextDouble() * total;
				action = 0;
				for (double sum = 0; action < def.Weights.Length; action++) {
					sum += def.Weights[action];
					if (roll < sum) {
						break;
					}
				}
			}
			switch (action) {
				case 0: //f_npc_roundattack with no damage set: the sound and the ripple
					dragon.Sound(512);
					EffectFactory.StationaryEffect(dragon, 0x2ED0, 0, 32);
					return true;
				case 1:
					DragonStamp(dragon, def);
					return true;
				case 2:
					return DragonPoisonBreath(dragon, def);
				default:
					foreach (AbstractCharacter ch in dragon.GetMap().GetCharsInRange(dragon.X, dragon.Y, 4)) {
						if ((ch is Player player) && IsDragonFireVictim(player) && dragon.CanSeeLOS(player).Allow) {
							Splash(dragon, player, (int) def.AcidDamage);
						}
					}
					return false; //f_dragon_acid_damage returns nothing: the next thought in 1 s
			}
		}

		private static readonly PluginKey stampParalysisKey = PluginKey.Acquire("_dragonStampParalysis_");

		/// <summary>f_dragon_stamp: it rears up, stands still a second, then the ground shakes in its map cell.</summary>
		private static void DragonStamp(NPC dragon, DragonBrainAbilityDef def) {
			NPCAiTimer.Freeze(dragon, 1);
			AnimCalculator.PerformAnim(dragon, GenericAnim.Cast);
			DragonActionTimer.Start(dragon, DragonActionTimer.Stamp, 1, (int) def.StampDamage, 0, 0);
		}

		/// <summary>The stamp's blow, without the wait - for tests and the action timer.</summary>
		public static void StampNow(NPC dragon, int stampDamage) {
			DragonBrainAbilityDef def = DragonBrainDef;
			NPCAiTimer.Unfreeze(dragon);
			int[] sounds = { 543, 542, 755 };
			foreach (Character player in PlayersInSector(dragon, dragon.X, dragon.Y)) {
				player.Sound(sounds[Globals.dice.Next(sounds.Length)]);
				int distance = Point2D.GetSimpleDistance(dragon, player);
				int stamp = stampDamage - ((distance - 1) * 6);
				if ((def != null) && (Globals.dice.Next(101) < def.StampCritical)) {
					player.SysMessage("Spadl na tebe kus stropu!");
					stamp += Globals.dice.Next(40, 71);
				}
				if (stamp > 0) {
					player.SysMessage("Zeme se pod tebou otrasla !");
					DamageManager.CauseDamageNoResponse(dragon, player, DamageType.Physical, stamp);
					if (!player.IsDeleted && player.IsAliveAndValid) {
						ParalyzeEffectPluginDef.Paralyze(player, stampParalysisKey, TimeSpan.FromSeconds((def == null) ? 2 : def.StampParalysis));
					}
				}
			}
		}

		/// <summary>f_dragon_breath_poison: at its target, if it sees it within 10; every player within 4 of that spot.</summary>
		private static bool DragonPoisonBreath(NPC dragon, DragonBrainAbilityDef def) {
			Character target = NPCAiTimer.GetFor(dragon)?.Target;
			if ((target == null) || !dragon.CanSeeLOS(target).Allow) {
				return false;
			}
			if (Point2D.GetSimpleDistance(dragon, target) > 10) {
				return true;
			}
			foreach (AbstractCharacter ch in target.GetMap().GetCharsInRange(target.X, target.Y, 4)) {
				if ((ch is Character c) && IsDragonFireVictim(c)) {
					Poison.Apply(c, dragon, (int) def.PoisonStrength, PoisonEffectSettings.instance.SecondsFor(PoisonKind.PercentOfHits), PoisonKind.PercentOfHits);
					c.Sound(586);
				}
			}
			return true;
		}
	}

	/// <summary>A dragon's delayed act: the breath after its 1 s intake, the spin (a breath a second), the cage after 1 s,
	/// the stamp's blow after 1 s.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DragonActionTimer : BoundTimer {
		internal const int Breath = 0, Round = 1, Cage = 2, Stamp = 3;
		private static readonly TimerKey[] keys = { TimerKey.Acquire("_dragonBreath_"), TimerKey.Acquire("_dragonRound_"), TimerKey.Acquire("_dragonCage_"), TimerKey.Acquire("_dragonStamp_") };

		[SaveableData, CopyableData]
		public int action, value, fireType, left;

		[LoadingInitializer, DeepCopyImplementation]
		public DragonActionTimer() {
		}

		internal static void Start(NPC dragon, int action, double delay, int value, int fireType, int left) {
			DragonActionTimer timer = new DragonActionTimer { action = action, value = value, fireType = fireType, left = left };
			dragon.AddTimer(keys[action], timer);
			timer.DueInSeconds = delay;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC dragon) || dragon.IsDeleted || dragon.Flag_Dead) {
				return;
			}
			switch (this.action) {
				case Breath:
					MonsterSpecialAttacks.ForceFireDragonBreath(dragon, this.value, this.fireType);
					break;
				case Round:
					MonsterSpecialAttacks.SpinDragon(dragon, this.value, this.fireType);
					if (this.left > 1) {
						Start(dragon, Round, 1, this.value, this.fireType, this.left - 1);
					} else {
						NPCAiTimer.Unfreeze(dragon);
					}
					break;
				case Cage:
					MonsterSpecialAttacks.CageAround(dragon, this.value, this.fireType);
					break;
				default:
					MonsterSpecialAttacks.StampNow(dragon, this.value);
					break;
			}
		}
	}

	/// <summary>i_mem_dragon_brain: a thought every Interval seconds, or 1 s after one that couldn't act.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DragonBrainTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_dragonBrain_");

		[LoadingInitializer, DeepCopyImplementation]
		public DragonBrainTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC dragon) || dragon.IsDeleted || dragon.Flag_Dead) {
				return;
			}
			if (MonsterSpecialAttacks.DragonBrainPlayerNear(dragon) == null) {
				return; //nobody around: the brain sleeps until it sees someone again
			}
			DragonBrainAbilityDef def = MonsterSpecialAttacks.DragonBrainDef;
			bool acted = MonsterSpecialAttacks.DragonThink(dragon, -1);
			DragonBrainTimer next = new DragonBrainTimer();
			dragon.AddTimer(Key, next);
			next.DueInSeconds = (!acted || (def == null)) ? 1 : ScriptUtil.GetRandInRange(def.Interval[0], def.Interval[1]);
		}
	}

	/// <summary>i_fire_dragon_aura / i_bahenny_drak_aura on a player: a tick at +1 s, then every interval.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DragonAuraTimer : BoundTimer {
		internal static readonly TimerKey FireKey = TimerKey.Acquire("_dragonAura_");
		internal static readonly TimerKey SwampKey = TimerKey.Acquire("_swampDragonAura_");

		[SaveableData, CopyableData]
		public bool swamp, resist;

		[SaveableData, CopyableData]
		public int damage, power, maxPower, interval;

		[LoadingInitializer, DeepCopyImplementation]
		public DragonAuraTimer() {
		}

		internal static void Apply(Character player, bool swamp, bool resist, int damage, int ticks, int interval) {
			TimerKey key = swamp ? SwampKey : FireKey;
			if (player.GetTimer(key) is DragonAuraTimer running) {
				running.power = ticks;
				return;
			}
			player.SysMessage(player.IsFemale ? "Byla jsi zasazena draci aurou!" : "Byl jsi zasazen draci aurou!");
			EffectFactory.StationaryEffect(player, MonsterSpecialAttacks.FxId("i_fx_explode_ball", 0x36B0), 3, 16);
			DragonAuraTimer timer = new DragonAuraTimer { swamp = swamp, resist = resist, damage = damage, power = ticks, maxPower = ticks, interval = Math.Max(1, interval) };
			player.AddTimer(key, timer);
			timer.DueInSeconds = 1;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Character player)) {
				return;
			}
			int value = (this.maxPower > 0) ? ((this.damage * this.power) / this.maxPower) : 0;
			if (this.resist) {
				value -= (value * PotionEffectPlugin.DragonResist(player)) / 100;
			}
			if ((this.power < 1) || (value < 1) || player.Flag_Dead) {
				player.SysMessage("Draci aura byla tak slaba, ze zmizla!");
				return;
			}
			if (this.swamp) {
				DamageManager.CauseDamage(null, player, DamageType.MagicFire, value);
			} else {
				bool caster = Professions.HasProfession(player, Professions.Mage) || Professions.HasProfession(player, Professions.Necromant) ||
					Professions.HasProfession(player, Professions.Mystic);
				DamageManager.CauseDamage(null, player, DamageType.MagicFire, caster ? ((value * 3) / 4) : value);
				if (!player.IsDeleted) {
					if (caster) {
						player.Mana = (short) ((player.Mana < value) ? 0 : (player.Mana - (value / 4)));
					}
					player.Stam = (short) ((player.Stam < value) ? 0 : (player.Stam - (value / 3)));
				}
			}
			if (player.IsDeleted || player.Flag_Dead) {
				return;
			}
			player.SysMessage("Poranila te draci aura");
			EffectFactory.StationaryEffect(player, MonsterSpecialAttacks.FxId("i_fx_explode_ball", 0x36B0), 3, 16);
			DragonAuraTimer next = new DragonAuraTimer { swamp = this.swamp, resist = this.resist, damage = this.damage, power = this.power - 1, maxPower = this.maxPower, interval = this.interval };
			player.AddTimer(this.swamp ? SwampKey : FireKey, next);
			next.DueInSeconds = this.interval;
		}
	}
}
