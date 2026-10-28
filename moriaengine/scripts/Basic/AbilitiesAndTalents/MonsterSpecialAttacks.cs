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
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_stun/
	/// e_npc_critic/e_npc_roundattack(_pullall) are upgraded from zero-effect PassiveAbilityDef
	/// stubs to real typed abilities with genuine combat behavior, same defnames (existing
	/// NPCDef.StartingAbilities grants via NPC.TryResolveUnknownTriggerGroupName keep working
	/// unchanged - only teventStubs.scp's own [XxxAbilityDef ...] header line per ability
	/// changes). Fields hold the engine-wide DEFAULT values (same numbers the original Sphere
	/// archive scripts themselves default to when a monster's own tag.xxx= override is
	/// unset) - a specific NPCDef can override any of these via its own matching per-species
	/// range property (NPC.cs's StunChance/StunDuration/etc, same two-layer precedent as
	/// NPCDef.SpellCastRange). GM-editable per-field via .info's existing Abilities ->
	/// "Def info" reflection-based editor (StatDrainingAbilityDef's own precedent), no new
	/// dialog code needed. Consumed by WeaponSkillDef.On_Stroke (stun/critic, on a landed
	/// melee hit) and NPCAiTimer (roundattack/pullall, a periodic AoE pulse) - see those
	/// files for the actual trigger logic; this file only declares the tunable data shape.
	/// </summary>
	[ViewableClass]
	public class StunAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue duration;
		private FieldValue cooldown;

		public StunAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("stunChance", new double[] { 250, 250 }, typeof(double[]));
			//#stun real archive unit: Sphere's own act.paralyze() built-in and tag.npc_stun_delay
			//both take DECISECONDS (raw 20 = 2.0s, raw 30 = 3.0s per the source script's own
			//comments) - Duration/Cooldown here stay in that SAME raw unit (divided by 10 at the
			//point of use in WeaponSkillDef, not here) so a .def author copying a real archive
			//tag.npc_stun_time/_delay value in verbatim just works, no silent unit mismatch.
			this.duration = this.InitTypedField("stunDuration", new double[] { 20, 20 }, typeof(double[]));
			this.cooldown = this.InitTypedField("stunCooldown", new double[] { 30, 30 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to stun on a landed melee swing -
		/// deliberately shadows AbilityDef.Chance (a single 0.0-1.0 fraction, different shape/
		/// scale, unrelated semantics) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Stun duration, raw archive deciseconds (divide by 10 for real seconds).</summary>
		public double[] Duration {
			get { return (double[]) this.duration.CurrentValue; }
			set { this.duration.CurrentValue = value; }
		}

		/// <summary>Minimum time between two stuns from the same attacker, raw archive
		/// deciseconds (divide by 10 for real seconds) - deliberately shadows AbilityDef.
		/// Cooldown (a single real-seconds scalar, different shape/unit) rather than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class CriticAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;

		public CriticAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("criticChance", new double[] { 250, 250 }, typeof(double[]));
			this.damage = this.InitTypedField("criticDamage", new double[] { 50, 60 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to land a critical hit, melee vs.
		/// players only (same real archive restriction) - deliberately shadows AbilityDef.Chance
		/// (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Bonus damage range added to the landed swing on a critical.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class RoundattackAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;
		private FieldValue cooldown;
		private FieldValue range;

		public RoundattackAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("roundattackChance", new double[] { 250, 250 }, typeof(double[]));
			//null (not a {min,max} default) is deliberate - the real archive source falls back
			//to the monster's OWN weapon damage (<def.dam>) when tag.roundattack_damage is
			//unset, not a fixed flat number. NPCAiTimer's consuming code checks for null and
			//reads self.WeaponAttack instead in that case.
			this.damage = this.InitTypedField("roundattackDamage", null, typeof(double[]));
			//real archive unit: already real seconds (the source script's own f_npc_rndAtt_
			//setVals multiplies the raw tag value by 10 "ticks", and 1 Sphere tick is 0.1s, so
			//the two conversions cancel out - unlike stun's Duration/Cooldown above, this one
			//is consumed directly as seconds, no division needed).
			this.cooldown = this.InitTypedField("roundattackCooldown", new double[] { 2, 4 }, typeof(double[]));
			this.range = this.InitTypedField("roundattackRange", (double) 3, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each recharge window -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>AoE damage range, or null to fall back to this NPC's own weapon damage
		/// (the real archive's own default behavior).</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between pulses, in real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class RoundattackPullAbilityDef : RoundattackAbilityDef {
		private FieldValue pullDistance;

		public RoundattackPullAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.pullDistance = this.InitTypedField("roundattackPullDistance", (double) 9, typeof(double));
		}

		/// <summary>How far away (tiles) a target still gets yanked toward this NPC right
		/// before the AoE pulse.</summary>
		public double PullDistance {
			get { return (double) this.pullDistance.CurrentValue; }
			set { this.pullDistance.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_bleed. Default
	/// {1000,1000} (100%) deliberately matches the real archive's own default: tag.npc_bleed
	/// is unset on most monsters, and the source script's own chance check only runs "if
	/// (<eval tag.npc_bleed>)" at all - unset means no chance roll, i.e. always bleeds; the tag
	/// only ever LOWERS the chance on specific species. NPCDef.BleedChance (same two-layer
	/// override precedent as the other 3 abilities) is how that per-species lowering is
	/// recovered from the archive.</summary>
	[ViewableClass]
	public class BleedAbilityDef : PassiveAbilityDef {
		private FieldValue chance;

		public BleedAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("bleedChance", new double[] { 1000, 1000 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to bleed on a landed melee swing -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_knockback. Real archive
	/// unit is a 0-100 scale, NOT the 0-1000 permille the other abilities use (confirmed in
	/// source: `{0 100} &lt; tag.npc_knockback`, default flat 1-in-3 when unset) - the converter
	/// multiplies by 10 on the way in (see ConvertedCharDef.cs) so NPCDef.KnockbackChance stays
	/// on the SAME 0-1000 scale as every other ability here, not a special case a future reader
	/// has to remember.</summary>
	[ViewableClass]
	public class KnockbackAbilityDef : PassiveAbilityDef {
		private FieldValue chance;

		public KnockbackAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("knockbackChance", new double[] { 333, 333 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to knock the target back on a
		/// landed melee swing - deliberately shadows AbilityDef.Chance (different shape/scale)
		/// rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_selfreser. Default 60
	/// seconds matches the real archive's own `tag(resstimer)` default. Deliberately does NOT
	/// port the archive's "cancel if the corpse was butchered" check - butchering itself isn't
	/// implemented in this engine yet (NPCDef.ButcherResources is data-only, see #586/#587) -
	/// so the respawn always happens once armed, same simplification precedent as other
	/// deliberately-trimmed archive mechanics this session.</summary>
	[ViewableClass]
	public class SelfReserAbilityDef : PassiveAbilityDef {
		private FieldValue delay;

		public SelfReserAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.delay = this.InitTypedField("selfresDelay", new double[] { 60, 60 }, typeof(double[]));
		}

		/// <summary>{min,max} seconds after death before a fresh instance of this NPC respawns
		/// at the death location - deliberately shadows AbilityDef.Cooldown (different shape)
		/// rather than reusing it.</summary>
		public double[] Delay {
			get { return (double[]) this.delay.CurrentValue; }
			set { this.delay.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_detect. Deliberately
	/// simplified from the real archive's own ground-trap-item mechanic (drops an item that
	/// reveals whoever steps near it) to a direct periodic area reveal while actively fighting -
	/// functionally equivalent for the common case, no new ground-trap item type needed. No
	/// real per-species tuning tag was found in the archive for this one (the pulse count/
	/// interval look hardcoded there too) - no NPCDef override exists for this ability,
	/// unlike the others in this batch, since there's no real per-species data to recover.
	/// </summary>
	[ViewableClass]
	public class DetectAbilityDef : PassiveAbilityDef {
		private FieldValue range;
		private FieldValue interval;

		public DetectAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.range = this.InitTypedField("detectRange", (double) 8, typeof(double));
			this.interval = this.InitTypedField("detectInterval", (double) 5, typeof(double));
		}

		/// <summary>Radius (tiles) around this NPC that gets force-revealed each pulse.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}

		/// <summary>Seconds between reveal pulses.</summary>
		public double Interval {
			get { return (double) this.interval.CurrentValue; }
			set { this.interval.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_leaper. User's explicit
	/// choice: the real archive mechanic (short teleport hops toward the target, bypassing real
	/// pathfinding) is deliberately simplified to a real speed boost instead - this NPC just
	/// takes extra pathfinding steps per NPCAiTimer think-tick while closing distance, using the
	/// SAME honest StepTowardsSmart A* movement every other NPC uses, never teleporting/
	/// clipping through walls.</summary>
	[ViewableClass]
	public class LeaperAbilityDef : PassiveAbilityDef {
		private FieldValue extraSteps;

		public LeaperAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.extraSteps = this.InitTypedField("leaperExtraSteps", (double) 2, typeof(double));
		}

		/// <summary>How many EXTRA pathfinding steps (on top of the normal one) this NPC takes
		/// per think-tick while approaching a target.</summary>
		public double ExtraSteps {
			get { return (double) this.extraSteps.CurrentValue; }
			set { this.extraSteps.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_looter. User's explicit
	/// choice: ported faithfully rather than simplified - reacts to stepping onto ANY lootable
	/// corpse (not just ones this NPC personally killed), same as the real archive's own
	/// `on=@ItemStep` trigger. No chance roll or per-species tag exists in the archive for this
	/// one - it's unconditional. Consumed via Corpse.On_Step (see Corpse.cs), not from this
	/// file's own Try* methods - a step-on-item trigger, not a swing/tick one.</summary>
	[ViewableClass]
	public class LooterAbilityDef : PassiveAbilityDef {
		public LooterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>
	/// Real-user-reported follow-up ("what are some other monster abilities we can
	/// implement?") - e_summoneater. Unconditional in the real archive source (no chance tag
	/// at all) - on a landed swing, if the target is a summoned creature (SummonSpellDef's own
	/// IsSummonedCreature tag), it's deleted outright instead of taking normal damage. No
	/// NPCDef override, no converter work - nothing to recover.</summary>
	[ViewableClass]
	public class SummonEaterAbilityDef : PassiveAbilityDef {
		public SummonEaterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>
	/// Real-user-reported follow-up - e_parakrik ("Paralyzing Scream"). Real archive default
	/// is chance(70,3)=7% per swing ATTEMPT (not just a landed one, same as roundattack), own
	/// cooldown default 50 raw deciseconds (5s, same raw-decisecond convention as StunAbilityDef's
	/// own Duration/Cooldown). Real mechanic pulses every non-GM, non-dead PLAYER in the
	/// monster's own map sector - approximated here with a flat tile Range (default 8, same
	/// precedent as RoundattackAbilityDef/DetectAbilityDef's own Range fields) since this engine
	/// has no equivalent "same sector" concept exposed for an AoE query.</summary>
	[ViewableClass]
	public class ParaKrikAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue range;

		public ParaKrikAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("parakrikChance", new double[] { 70, 70 }, typeof(double[]));
			this.cooldown = this.InitTypedField("parakrikCooldown", new double[] { 50, 50 }, typeof(double[]));
			this.range = this.InitTypedField("parakrikRange", (double) 8, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Minimum time between two screams, raw archive deciseconds (divide by 10 for
		/// real seconds) - deliberately shadows AbilityDef.Cooldown (a single real-seconds
		/// scalar, different shape/unit) rather than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles, approximating the real archive's "same sector" scope.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up - e_npc_curse ("Dark Curse"). Has the cleanest doc comment
	/// of any ability in the whole archive: "hurts all the enemies in the distance of 8 using
	/// dark and dirty magic". Real defaults: chance(110,3)=11%, delay {12,15} real seconds
	/// (randombetween then x10 - already real seconds, same convention as RoundattackAbilityDef's
	/// own Cooldown), dmg {50,60}.</summary>
	[ViewableClass]
	public class NpcCurseAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue damage;
		private FieldValue range;

		public NpcCurseAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("npcCurseChance", new double[] { 110, 110 }, typeof(double[]));
			this.cooldown = this.InitTypedField("npcCurseCooldown", new double[] { 12, 15 }, typeof(double[]));
			this.damage = this.InitTypedField("npcCurseDamage", new double[] { 50, 60 }, typeof(double[]));
			this.range = this.InitTypedField("npcCurseRange", (double) 8, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between pulses, real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE magic damage range.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up - e_npc_summoning ("Summoner"). Real defaults: chance 50
	/// (5%), cooldown 30s, summoned-creature lifetime 7200s (2h). The real archive's own
	/// per-species summon LIST (`tag.npc_summoning_char[i]`/`_count`) is recovered as
	/// NPCDef.SummonedCreatures - same list-of-NPCDef shape as the already-shipped
	/// NPCDef.Spells (#564).</summary>
	[ViewableClass]
	public class SummonerAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue summonLifetime;

		public SummonerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("summonerChance", new double[] { 50, 50 }, typeof(double[]));
			this.cooldown = this.InitTypedField("summonerCooldown", new double[] { 30, 30 }, typeof(double[]));
			this.summonLifetime = this.InitTypedField("summonerLifetime", new double[] { 7200, 7200 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between summon waves, real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>{min,max} seconds a summoned reinforcement survives before auto-despawning.</summary>
		public double[] SummonLifetime {
			get { return (double[]) this.summonLifetime.CurrentValue; }
			set { this.summonLifetime.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up - e_npc_sniper. Real archive requires BOTH the defender
	/// being a player AND the attacker actively fighting at range (`isfighting_ranged`) - a
	/// strictly narrower gate than CriticAbilityDef's "vs player only" (any weapon). Real
	/// defaults: chance(200,3)=20%, dmg flat 50 (archive's own range tag default, when unset,
	/// is a flat fallback rather than a true {min,max} - using {50,50} here reproduces that).</summary>
	[ViewableClass]
	public class SniperAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;

		public SniperAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("sniperChance", new double[] { 200, 200 }, typeof(double[]));
			this.damage = this.InitTypedField("sniperDamage", new double[] { 50, 50 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to land a critical hit while
		/// fighting at range, vs. players only (same real archive restriction) - deliberately
		/// shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Bonus damage range added to the landed swing on a critical.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Real-user-reported follow-up - e_npc_infiniteMana. Unconditional in the real archive
	/// source (no chance tag, no per-species tuning) - this NPC's mana is simply always kept
	/// at max. No NPCDef override, no converter work.</summary>
	[ViewableClass]
	public class InfiniteManaAbilityDef : PassiveAbilityDef {
		public InfiniteManaAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>
	/// Consumer side of StunAbilityDef/CriticAbilityDef - rolled from DamageManager.ProcessSwing
	/// on every landed melee swing (RoundattackAbilityDef/RoundattackPullAbilityDef are consumed
	/// separately, from NPCAiTimer's own periodic tick - a recharge-gated pulse, not a per-swing
	/// roll). Each roll checks the attacker's own per-species NPCDef override range first
	/// (NPC.cs's StunChance/CriticChance/etc - null if unconfigured), falling back to whichever
	/// of these two AbilityDefs the attacker was actually granted.
	/// </summary>
	public static class MonsterSpecialAttacks {
		//internal (not private) so a test can confirm the stun actually landed (ability
		//granted + chance roll + resist check all passed) by checking for this plugin
		//directly, same "internal for direct test invocation" precedent as NPCAiTimer.Fight.
		internal static readonly PluginKey stunPluginKey = PluginKey.Acquire("_npcStun_");

		private static StunAbilityDef stunAbilityDef;
		private static StunAbilityDef StunDef {
			get {
				if (stunAbilityDef == null) {
					stunAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_stun") as StunAbilityDef;
				}
				return stunAbilityDef;
			}
		}

		private static CriticAbilityDef criticAbilityDef;
		private static CriticAbilityDef CriticDef {
			get {
				if (criticAbilityDef == null) {
					criticAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_critic") as CriticAbilityDef;
				}
				return criticAbilityDef;
			}
		}

		/// <summary>Rolls `permille` chance in [0, 1000) - e.g. 250 means 25%.</summary>
		private static bool RollChance(double permille) {
			return Globals.dice.Next(1000) < permille;
		}

		/// <summary>Resolves a tunable range: the attacker's own NPCDef override if configured
		/// (per-species, e.g. NPCDef.StunChance), otherwise the granted AbilityDef's own default
		/// field. `npcOverride` is null when that specific NPCDef never set the override.</summary>
		private static double RollTunable(int[] npcOverride, double[] abilityDefault) {
			if (npcOverride != null) {
				return NPCDef.RollStatRange(npcOverride);
			}
			return ScriptUtil.GetRandInRange(abilityDefault[0], abilityDefault[1]);
		}

		/// <summary>#stun - on a landed melee swing, a chance to paralyze the defender for a
		/// short time, gated by its own per-attacker cooldown (so it can't stun-lock every
		/// swing) - see StunAbilityDef's own doc comment for the real archive source this
		/// mirrors. No-op if `attacker` was never granted a_tevent_e_npc_stun (every player and
		/// most NPCs) or StunDef failed to resolve (teventStubs.scp not loaded - shouldn't
		/// happen outside a broken boot).</summary>
		public static void TryApplyStun(Character attacker, Character defender) {
			StunAbilityDef def = StunDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}
			//real archive guard: a target heavily resistant to paralysis never gets stunned at
			//all, regardless of roll (ResistParalyse is the same 0-1000 permille scale the
			//archive's own tag.resist_para check used).
			if (defender.ResistParalyse > 900) {
				return;
			}
			if (Globals.TimeInSeconds < NextStunTime(attacker)) {
				return; //still on this attacker's own stun cooldown
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.StunChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double duration = RollTunable(npcDef == null ? null : npcDef.StunDuration, def.Duration);
			double cooldown = RollTunable(npcDef == null ? null : npcDef.StunCooldown, def.Cooldown);
			//real archive unit: raw deciseconds (see StunAbilityDef.Duration/Cooldown's own doc
			//comment) - divide by 10 for real seconds, here at the point of use.
			SetNextStunTime(attacker, Globals.TimeInSeconds + (cooldown / 10.0));
			ParalyzeEffectPluginDef.Paralyze(defender, stunPluginKey, TimeSpan.FromSeconds(duration / 10.0));
		}

		private static readonly TagKey nextStunTimeTK = TagKey.Acquire("_npcNextStunTime_");

		private static double NextStunTime(Character attacker) {
			object tag = attacker.GetTag(nextStunTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextStunTime(Character attacker, double value) {
			attacker.SetTag(nextStunTimeTK, value);
		}

		/// <summary>#critic - on a landed melee swing against a player specifically (same real
		/// archive restriction), a chance to add bonus damage to the swing that's already about
		/// to land. Must be called BEFORE DamageManager.CauseDamage, while swingArgs.DamageAfterAC
		/// is still mutable.</summary>
		public static void TryApplyCritic(Character attacker, WeaponSwingArgs swingArgs) {
			CriticAbilityDef def = CriticDef;
			if (def == null) {
				return;
			}
			if (!swingArgs.defender.IsPlayerForCombat) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.CriticChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double bonus = RollTunable(npcDef == null ? null : npcDef.CriticDamage, def.Damage);
			swingArgs.DamageAfterAC += bonus;
			CombatMessages.Send(attacker, "[Combat] Critical hit!");
			CombatMessages.Send(swingArgs.defender, "[Combat] " + attacker.Name + " critically hits you!");
		}

		private static BleedAbilityDef bleedAbilityDef;
		private static BleedAbilityDef BleedDef {
			get {
				if (bleedAbilityDef == null) {
					bleedAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_bleed") as BleedAbilityDef;
				}
				return bleedAbilityDef;
			}
		}

		/// <summary>#bleed - on a landed melee swing, a chance to apply/stack a bleed DoT
		/// (NpcBleedEffectPluginDef) scaled off the damage that swing actually dealt - same
		/// real archive ratio (bleedpower = actualDamage/3). Must be called AFTER
		/// DamageManager.CauseDamage, since it needs the real landed-damage amount, not the
		/// pre-armor swing value. A miss or a fully-blocked swing (actualDamage &lt;= 0) has
		/// nothing to scale off of - same as the real archive's own `wasHarmed` guard.</summary>
		public static void TryApplyBleed(Character attacker, Character defender, int actualDamage) {
			BleedAbilityDef def = BleedDef;
			if (def == null) {
				return;
			}
			if (actualDamage <= 0) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.BleedChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double effectPower = actualDamage / 3.0;
			NpcBleedEffectPluginDef bleedingDef = SingletonScript<NpcBleedEffectPluginDef>.Instance;
			bleedingDef.Apply(attacker, defender, EffectFlag.HarmfulEffect, effectPower, bleedingDef.MaxTicks);
		}

		private static RoundattackAbilityDef roundattackAbilityDef;
		private static RoundattackAbilityDef RoundattackDef {
			get {
				if (roundattackAbilityDef == null) {
					roundattackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack") as RoundattackAbilityDef;
				}
				return roundattackAbilityDef;
			}
		}

		private static RoundattackPullAbilityDef roundattackPullAbilityDef;
		private static RoundattackPullAbilityDef RoundattackPullDef {
			get {
				if (roundattackPullAbilityDef == null) {
					roundattackPullAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack_pullall") as RoundattackPullAbilityDef;
				}
				return roundattackPullAbilityDef;
			}
		}

		private static readonly TagKey nextRoundattackTimeTK = TagKey.Acquire("_npcNextRoundattackTime_");

		//North,NorthEast,East,SouthEast,South,SouthWest,West,NorthWest (GameCore/Enums.cs's own
		//Direction order) - used by PullToward below to find the one tile adjacent to the
		//attacker, on the side the target is already standing.
		private static readonly int[] directionDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
		private static readonly int[] directionDy = { -1, -1, 0, 1, 1, 1, 0, -1 };

		/// <summary>#roundattack - unlike stun/critic (rolled per landed swing), this is a
		/// periodic AoE pulse independent of whether this NPC's own swing just landed or missed
		/// - the real archive source fires its chance-check on practically every combat-related
		/// trigger this character experiences (attacking OR defending OR casting), gated by its
		/// own internal recharge timer either way. Checking once per NPCAiTimer think-tick while
		/// actively fighting (NPCAiTimer.Fight already only runs then) is a faithful, much
		/// simpler equivalent - the recharge timer is what actually controls the real-world
		/// pacing, not the exact trigger that happens to re-check it.</summary>
		public static void TryRoundattack(NPC self) {
			RoundattackPullAbilityDef pullDef = RoundattackPullDef;
			RoundattackAbilityDef def;
			bool pull;
			Ability ability;
			if ((pullDef != null) && self.HasAbility(pullDef, out ability)) {
				def = pullDef;
				pull = true;
			} else {
				RoundattackAbilityDef plainDef = RoundattackDef;
				if ((plainDef == null) || !self.HasAbility(plainDef, out ability)) {
					return;
				}
				def = plainDef;
				pull = false;
			}

			if (Globals.TimeInSeconds < NextRoundattackTime(self)) {
				return;
			}

			NPCDef npcDef = (NPCDef) self.Def;
			//real archive unit: roundattack_time is already real seconds (the source script's
			//own f_npc_rndAtt_setVals multiplies the raw tag by 10 "ticks" before adding to
			//serv.time, and a Sphere tick is 0.1s - the two conversions cancel out), unlike
			//stun's Duration/Cooldown which stay in raw deciseconds - see StunAbilityDef's doc
			//comment for that one.
			double cooldown = RollTunable(npcDef.RoundattackCooldown, def.Cooldown);
			SetNextRoundattackTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.RoundattackChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			if (pull) {
				RoundattackPullAbilityDef pullDefResolved = (RoundattackPullAbilityDef) def;
				int[] pullOverride = npcDef.PullDistance;
				double pullDistance = (pullOverride != null) ? NPCDef.RollStatRange(pullOverride) : pullDefResolved.PullDistance;
				foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, (int) pullDistance)) {
					Character pullTarget = ch as Character;
					if (IsValidRoundattackTarget(self, pullTarget)) {
						PullToward(self, pullTarget);
					}
				}
			}

			double damage;
			if (npcDef.RoundattackDamage != null) {
				damage = NPCDef.RollStatRange(npcDef.RoundattackDamage);
			} else if (def.Damage != null) {
				damage = ScriptUtil.GetRandInRange(def.Damage[0], def.Damage[1]);
			} else {
				//real archive default: falls back to the monster's own weapon damage
				//(<def.dam>) rather than a fixed flat number when unconfigured either way.
				damage = npcDef.WeaponAttack;
			}

			int range = (int) def.Range;
			AnimCalculator.PerformAnim(self, GenericAnim.AttackOverhead);
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if (IsValidRoundattackTarget(self, target)) {
					DamageManager.CauseDamage(self, target, self.WeaponDamageType, damage);
				}
			}
		}

		private static bool IsValidRoundattackTarget(NPC self, Character target) {
			return (target != null) && (target != self) && !target.IsDeleted && target.IsAliveAndValid &&
				!target.IsNotVisible && !target.IsGM;
		}

		private static void PullToward(NPC self, Character target) {
			if (Point2D.GetSimpleDistance(self, target) <= 1) {
				return; //already adjacent - nothing to pull
			}
			Direction dir = Point2D.GetDirFromTo(self, target);
			int x = self.X + directionDx[(int) dir];
			int y = self.Y + directionDy[(int) dir];
			Map map = self.GetMap();
			if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
				return; //no safe spot to land the pull - skip it rather than clip into something
			}
			int tileId = map.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable)) {
				return;
			}
			int z = map.GetTileZ(x, y);
			target.Go(x, y, z, target.M);
		}

		private static double NextRoundattackTime(NPC self) {
			object tag = self.GetTag(nextRoundattackTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextRoundattackTime(NPC self, double value) {
			self.SetTag(nextRoundattackTimeTK, value);
		}

		private static KnockbackAbilityDef knockbackAbilityDef;
		private static KnockbackAbilityDef KnockbackDef {
			get {
				if (knockbackAbilityDef == null) {
					knockbackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_knockback") as KnockbackAbilityDef;
				}
				return knockbackAbilityDef;
			}
		}

		/// <summary>#knockback - on a landed melee swing, a chance to push the defender one
		/// tile further away from the attacker - same tile-validity checks as PullToward
		/// above, just the inverse direction (away from, not toward, the attacker).</summary>
		public static void TryApplyKnockback(Character attacker, Character defender) {
			KnockbackAbilityDef def = KnockbackDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.KnockbackChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			PushAway(attacker, defender);
		}

		private static void PushAway(Character attacker, Character defender) {
			Direction dir = Point2D.GetDirFromTo(attacker, defender);
			int x = defender.X + directionDx[(int) dir];
			int y = defender.Y + directionDy[(int) dir];
			Map map = defender.GetMap();
			if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
				return; //no safe spot to land the push - skip it rather than clip into something
			}
			int tileId = map.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable)) {
				return;
			}
			int z = map.GetTileZ(x, y);
			defender.Go(x, y, z, defender.M);
		}

		private static SelfReserAbilityDef selfReserAbilityDef;
		private static SelfReserAbilityDef SelfReserDef {
			get {
				if (selfReserAbilityDef == null) {
					selfReserAbilityDef = AbstractScript.GetByDefname("a_tevent_e_selfreser") as SelfReserAbilityDef;
				}
				return selfReserAbilityDef;
			}
		}

		private static readonly TimerKey selfResTimerKey = TimerKey.Acquire("_npcSelfResTimer_");

		/// <summary>#selfreser - a while after death, a fresh instance of this exact monster
		/// respawns at the death location. Must be called AFTER corpse.InitFromChar(this),
		/// while `dying`'s own position is still valid (Character.CauseDeath deletes a
		/// non-player NPC right after this point). Attached to the freshly-created corpse
		/// (one per death, no TagHolder.AddTimer key-collision risk) rather than to the
		/// killer, who could in theory die to two self-resing monsters in quick succession -
		/// see TagHolder.AddTimer's own one-timer-per-key-per-object semantics.</summary>
		public static void TryApplySelfRes(Character dying, Corpse corpse) {
			if (corpse == null) {
				return;
			}
			SelfReserAbilityDef def = SelfReserDef;
			if (def == null) {
				return;
			}
			NPC npc = dying as NPC;
			if (npc == null) {
				return;
			}
			Ability ability;
			if (!npc.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = (NPCDef) npc.Def;
			double delay = RollTunable(npcDef.SelfresDelay, def.Delay);
			NpcSelfResTimer timer = new NpcSelfResTimer(npcDef, dying.X, dying.Y, dying.Z, dying.M);
			corpse.AddTimer(selfResTimerKey, timer).DueInSeconds = delay;
		}

		private static DetectAbilityDef detectAbilityDef;
		private static DetectAbilityDef DetectDef {
			get {
				if (detectAbilityDef == null) {
					detectAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_detect") as DetectAbilityDef;
				}
				return detectAbilityDef;
			}
		}

		private static readonly TagKey nextDetectTimeTK = TagKey.Acquire("_npcNextDetectTime_");

		/// <summary>#detect - periodically force-reveals any hidden/invisible character within
		/// range, while this NPC is actively fighting (called from NPCAiTimer.Fight, which
		/// already only runs then) - see DetectAbilityDef's own doc comment for why this is a
		/// deliberate simplification of the real archive's ground-trap-item mechanic.</summary>
		public static void TryApplyDetect(NPC self) {
			DetectAbilityDef def = DetectDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextDetectTime(self)) {
				return;
			}
			SetNextDetectTime(self, Globals.TimeInSeconds + def.Interval);

			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if ((target != null) && (target != self) && !target.IsDeleted) {
					target.Flag_Hidden = false;
					target.Flag_InvisByMagic = false;
				}
			}
		}

		private static double NextDetectTime(NPC self) {
			object tag = self.GetTag(nextDetectTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextDetectTime(NPC self, double value) {
			self.SetTag(nextDetectTimeTK, value);
		}

		private static LeaperAbilityDef leaperAbilityDef;
		private static LeaperAbilityDef LeaperDef {
			get {
				if (leaperAbilityDef == null) {
					leaperAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_leaper") as LeaperAbilityDef;
				}
				return leaperAbilityDef;
			}
		}

		/// <summary>#leaper (user-approved simplification - see LeaperAbilityDef's own doc
		/// comment) - how many EXTRA honest StepTowardsSmart pathfinding steps this NPC should
		/// take this think-tick while closing distance, on top of the normal one. 0 if `self`
		/// was never granted a_tevent_e_npc_leaper.</summary>
		public static int GetLeaperExtraSteps(NPC self) {
			LeaperAbilityDef def = LeaperDef;
			if (def == null) {
				return 0;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return 0;
			}
			return (int) def.ExtraSteps;
		}

		private static LooterAbilityDef looterAbilityDef;
		private static LooterAbilityDef LooterDef {
			get {
				if (looterAbilityDef == null) {
					looterAbilityDef = AbstractScript.GetByDefname("a_tevent_e_looter") as LooterAbilityDef;
				}
				return looterAbilityDef;
			}
		}

		/// <summary>#looter (user-approved: ported faithfully) - an NPC with this ability that
		/// steps onto any lootable corpse moves one item from it into its own backpack,
		/// unconditionally (no chance roll, no per-species tag exists in the archive for this
		/// one). Called from Corpse.On_Step.</summary>
		public static void TryApplyLoot(NPC looter, Corpse corpse) {
			if ((looter == null) || (corpse == null) || corpse.IsDeleted) {
				return;
			}
			LooterAbilityDef def = LooterDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!looter.HasAbility(def, out ability)) {
				return;
			}

			Item backpack = looter.Backpack;
			if (backpack == null) {
				return;
			}
			foreach (Item loot in corpse.EnumShallow()) {
				loot.Cont = backpack;
				return; //one item per step, matching the real archive's own single-grab shape
			}
		}

		private static SummonEaterAbilityDef summonEaterAbilityDef;
		private static SummonEaterAbilityDef SummonEaterDef {
			get {
				if (summonEaterAbilityDef == null) {
					summonEaterAbilityDef = AbstractScript.GetByDefname("a_tevent_e_summoneater") as SummonEaterAbilityDef;
				}
				return summonEaterAbilityDef;
			}
		}

		/// <summary>#summoneater - unconditional in the real archive: if the attacker lands a
		/// swing on a summoned creature, that creature is deleted outright instead of taking
		/// normal damage. Must be called right after Trigger_BeforeSwing succeeds, before the
		/// normal critic/damage/stun/bleed/knockback chain - returns true when the defender was
		/// consumed, telling the caller to skip the rest of the swing entirely (there's nothing
		/// left to apply any of that to).</summary>
		public static bool TryApplySummonEater(Character attacker, Character defender) {
			SummonEaterAbilityDef def = SummonEaterDef;
			if (def == null) {
				return false;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return false;
			}
			if (!SummonSpellDef.IsSummonedCreature(defender)) {
				return false;
			}
			defender.Delete();
			return true;
		}

		private static ParaKrikAbilityDef paraKrikAbilityDef;
		private static ParaKrikAbilityDef ParaKrikDef {
			get {
				if (paraKrikAbilityDef == null) {
					paraKrikAbilityDef = AbstractScript.GetByDefname("a_tevent_e_parakrik") as ParaKrikAbilityDef;
				}
				return paraKrikAbilityDef;
			}
		}

		private static readonly TagKey nextParaKrikTimeTK = TagKey.Acquire("_npcNextParaKrikTime_");

		/// <summary>#parakrik - periodic (checked once per NPCAiTimer.Fight think-tick while
		/// fighting, same "faithful simpler equivalent of a per-swing-attempt recharge-gated
		/// roll" precedent as roundattack) chance to paralyze every non-GM, living player within
		/// Range - pets/summons/other monsters are untouched, matching the real archive's own
		/// player-only filter. Reuses the exact same ParalyzeEffectPluginDef.Paralyze call stun
		/// already uses, including the same ResistParalyse&gt;900 immunity check.</summary>
		public static void TryApplyParaKrik(NPC self) {
			ParaKrikAbilityDef def = ParaKrikDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextParaKrikTime(self)) {
				return;
			}

			NPCDef npcDef = (NPCDef) self.Def;
			double cooldown = RollTunable(npcDef.ParaKrikCooldown, def.Cooldown);
			//real archive unit: raw deciseconds (see StunAbilityDef's own doc comment for this
			//same convention) - divide by 10 for real seconds, here at the point of use.
			SetNextParaKrikTime(self, Globals.TimeInSeconds + (cooldown / 10.0));

			double chance = RollTunable(npcDef.ParaKrikChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if ((target == null) || target.IsGM || !target.IsPlayerForCombat || !target.IsAliveAndValid) {
					continue;
				}
				if (target.ResistParalyse > 900) {
					continue;
				}
				ParalyzeEffectPluginDef.Paralyze(target, paraKrikPluginKey, TimeSpan.FromSeconds(3));
			}
		}

		//a dedicated key (not stunPluginKey) - a target can legitimately be stunned AND
		//screamed-at independently, each needs its own installed-plugin slot. internal (not
		//private) so a test can confirm the scream actually landed, same precedent as
		//stunPluginKey above.
		internal static readonly PluginKey paraKrikPluginKey = PluginKey.Acquire("_npcParaKrik_");

		private static double NextParaKrikTime(NPC self) {
			object tag = self.GetTag(nextParaKrikTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextParaKrikTime(NPC self, double value) {
			self.SetTag(nextParaKrikTimeTK, value);
		}

		private static NpcCurseAbilityDef npcCurseAbilityDef;
		private static NpcCurseAbilityDef NpcCurseDef {
			get {
				if (npcCurseAbilityDef == null) {
					npcCurseAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_curse") as NpcCurseAbilityDef;
				}
				return npcCurseAbilityDef;
			}
		}

		private static readonly TagKey nextNpcCurseTimeTK = TagKey.Acquire("_npcNextCurseTime_");

		/// <summary>#npc_curse ("Dark Curse") - periodic ranged AoE magic damage pulse, same
		/// once-per-think-tick-while-fighting pattern as roundattack/parakrik. Real archive
		/// target filter: any living, non-GM player, OR any tamed pet - wild monsters other
		/// than the caster itself are never hit (matches f_npcCurse_effect's own
		/// isPlayer-or-isPet check).</summary>
		public static void TryApplyNpcCurse(NPC self) {
			NpcCurseAbilityDef def = NpcCurseDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextNpcCurseTime(self)) {
				return;
			}

			NPCDef npcDef = (NPCDef) self.Def;
			double cooldown = RollTunable(npcDef.NpcCurseCooldown, def.Cooldown);
			SetNextNpcCurseTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.NpcCurseChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double damage = RollTunable(npcDef.NpcCurseDamage, def.Damage);
			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if (!IsValidNpcCurseTarget(target)) {
					continue;
				}
				//bare DamageType.Magic has no real-vs-phase-2 resist "subtype" bit and throws -
				//MagicMystical is this engine's existing generic bucket for "magic damage, no
				//specific element" (already used for the Mystic profession's own attack), which
				//fits "dark and dirty magic" just as well as any other non-elemental label would.
				DamageManager.CauseDamage(self, target, DamageType.MagicMystical, damage);
			}
		}

		private static bool IsValidNpcCurseTarget(Character target) {
			if ((target == null) || target.IsDeleted || !target.IsAliveAndValid || target.IsNotVisible) {
				return false;
			}
			if (target.IsPlayerForCombat) {
				return !target.IsGM;
			}
			NPC npc = target as NPC;
			return (npc != null) && (npc.ControlMaster != null); //a tamed pet
		}

		private static double NextNpcCurseTime(NPC self) {
			object tag = self.GetTag(nextNpcCurseTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextNpcCurseTime(NPC self, double value) {
			self.SetTag(nextNpcCurseTimeTK, value);
		}

		private static SummonerAbilityDef summonerAbilityDef;
		private static SummonerAbilityDef SummonerDef {
			get {
				if (summonerAbilityDef == null) {
					summonerAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_summoning") as SummonerAbilityDef;
				}
				return summonerAbilityDef;
			}
		}

		private static readonly TagKey nextSummoningTimeTK = TagKey.Acquire("_npcNextSummoningTime_");
		private static readonly TimerKey summonedCreatureDespawnTimerKey = TimerKey.Acquire("_npcSummonedCreatureDespawn_");

		/// <summary>#npc_summoning ("Summoner") - periodic (once-per-think-tick-while-fighting,
		/// same pattern as roundattack/parakrik/npc_curse) chance to spawn reinforcements. Real
		/// archive spawns ONE INSTANCE OF EVERY configured chardef per proc (a loop over all
		/// indices up to a count), not a single random pick - NPCDef.SummonedCreatures mirrors
		/// that shape directly. No-op if the NPCDef never configured any (same "incomplete
		/// content defaults permissive/inert" convention used elsewhere). Spawned reinforcements
		/// are autonomous Hostile (no ControlMaster) - same choice SummonSpellDef's own Hostile
		/// branch already makes for a "popular kill-all" summon - and despawn automatically via
		/// the same generic SummonSpellDef.SummonDespawnTimer the Mage summon spells use.</summary>
		public static void TryApplySummoning(NPC self) {
			SummonerAbilityDef def = SummonerDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			NPCDef npcDef = (NPCDef) self.Def;
			List<NPCDef> toSummon = npcDef.SummonedCreatures;
			if ((toSummon == null) || (toSummon.Count == 0)) {
				return;
			}
			if (Globals.TimeInSeconds < NextSummoningTime(self)) {
				return;
			}

			double cooldown = RollTunable(npcDef.SummoningCooldown, def.Cooldown);
			SetNextSummoningTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.SummoningChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double lifetimeSecs = RollTunable(npcDef.SummoningLifetime, def.SummonLifetime);
			foreach (NPCDef reinforcementDef in toSummon) {
				NPC reinforcement = (NPC) reinforcementDef.Create(self.X, self.Y, self.Z, self.M);
				reinforcement.AggroMode = AggroMode.Hostile;
				SummonSpellDef.MarkAsSummonedCreature(reinforcement);
				reinforcement.AddTimer(summonedCreatureDespawnTimerKey, new SummonSpellDef.SummonDespawnTimer())
					.DueInSpan = TimeSpan.FromSeconds(lifetimeSecs);
			}
		}

		private static double NextSummoningTime(NPC self) {
			object tag = self.GetTag(nextSummoningTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextSummoningTime(NPC self, double value) {
			self.SetTag(nextSummoningTimeTK, value);
		}

		private static SniperAbilityDef sniperAbilityDef;
		private static SniperAbilityDef SniperDef {
			get {
				if (sniperAbilityDef == null) {
					sniperAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_sniper") as SniperAbilityDef;
				}
				return sniperAbilityDef;
			}
		}

		/// <summary>#npc_sniper - same call-site/shape as TryApplyCritic (before CauseDamage,
		/// mutates swingArgs.DamageAfterAC while it's still mutable), but gated on the attacker
		/// actively fighting at RANGE (matches the real archive's own isfighting_ranged check) in
		/// addition to the defender being a player - strictly narrower than critic's "vs player
		/// only, any weapon".</summary>
		public static void TryApplySniper(Character attacker, WeaponSwingArgs swingArgs) {
			SniperAbilityDef def = SniperDef;
			if (def == null) {
				return;
			}
			if (!swingArgs.defender.IsPlayerForCombat) {
				return;
			}
			if ((attacker.WeaponType != WeaponType.Bow) && (attacker.WeaponType != WeaponType.XBow)) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.SniperChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double bonus = RollTunable(npcDef == null ? null : npcDef.SniperDamage, def.Damage);
			swingArgs.DamageAfterAC += bonus;
			CombatMessages.Send(swingArgs.defender, "[Combat] " + attacker.Name + " hits you with a precise shot!");
		}

		private static InfiniteManaAbilityDef infiniteManaAbilityDef;
		private static InfiniteManaAbilityDef InfiniteManaDef {
			get {
				if (infiniteManaAbilityDef == null) {
					infiniteManaAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_infinitemana") as InfiniteManaAbilityDef;
				}
				return infiniteManaAbilityDef;
			}
		}

		/// <summary>#npc_infiniteMana - unconditional in the real archive (no chance roll, no
		/// per-species tuning): this NPC's mana is simply always kept topped up. Checked once
		/// per NPCAiTimer.Fight think-tick, same cheap pattern as the other periodic checks here -
		/// a caster granted this ability never actually runs out of mana between casts.</summary>
		public static void TryApplyInfiniteMana(NPC self) {
			InfiniteManaAbilityDef def = InfiniteManaDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			self.Mana = self.MaxMana;
		}
	}

	/// <summary>Companion timer for SelfReserAbilityDef/MonsterSpecialAttacks.TryApplySelfRes -
	/// a plain BoundTimer (no .ct template needed, unlike a Plugin subclass) carrying just
	/// enough state (which NPCDef, and where) to respawn a fresh instance of the monster once
	/// it fires. Attached to the Corpse created by the death that armed it, not to any
	/// Character - see TryApplySelfRes's own doc comment for why.</summary>
	[SaveableClass, DeepCopyableClass]
	public class NpcSelfResTimer : BoundTimer {
		[SaveableData, CopyableData]
		public NPCDef npcDef;
		[SaveableData, CopyableData]
		public int x;
		[SaveableData, CopyableData]
		public int y;
		[SaveableData, CopyableData]
		public int z;
		[SaveableData, CopyableData]
		public byte m;

		[DeepCopyImplementation, LoadingInitializer]
		public NpcSelfResTimer() {
		}

		public NpcSelfResTimer(NPCDef npcDef, int x, int y, int z, byte m) {
			this.npcDef = npcDef;
			this.x = x;
			this.y = y;
			this.z = z;
			this.m = m;
		}

		protected override void OnTimeout(TagHolder cont) {
			this.npcDef.Create(this.x, this.y, this.z, this.m);
		}
	}
}
