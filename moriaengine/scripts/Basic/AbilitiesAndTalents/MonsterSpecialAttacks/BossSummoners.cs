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
using System.Runtime.CompilerServices;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_fire_lord (c_fire_lord and its summoned copy): when hit, 1 in ExplosionOneIn "Heru Beleg Ruin Val" and
	/// Columns fire columns within 5 tiles (Erebor's fireexplosion, 50-80 each); 1 in SummonOneIn "Heru Ruin Val" and two fire
	/// phoenixes and a fire eye, its own, for SummonLifetime seconds. As it dies, everyone who fought it and is in sight takes
	/// DeathDamage less by distance.</summary>
	[ViewableClass]
	public class FireLordAbilityDef : PassiveAbilityDef {
		private FieldValue explosionOneIn, columns, summonOneIn, summonLifetime, deathDamage;

		public FireLordAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.explosionOneIn = this.InitTypedField("fireLordExplosionOneIn", 12, typeof(int));
			this.columns = this.InitTypedField("fireLordColumns", 10, typeof(int));
			this.summonOneIn = this.InitTypedField("fireLordSummonOneIn", 20, typeof(int));
			this.summonLifetime = this.InitTypedField("fireLordSummonLifetime", 20.0, typeof(double));
			this.deathDamage = this.InitTypedField("fireLordDeathDamage", 180.0, typeof(double));
		}

		public int ExplosionOneIn { get { return (int) this.explosionOneIn.CurrentValue; } set { this.explosionOneIn.CurrentValue = value; } }
		public int Columns { get { return (int) this.columns.CurrentValue; } set { this.columns.CurrentValue = value; } }
		public int SummonOneIn { get { return (int) this.summonOneIn.CurrentValue; } set { this.summonOneIn.CurrentValue = value; } }
		public double SummonLifetime { get { return (double) this.summonLifetime.CurrentValue; } set { this.summonLifetime.CurrentValue = value; } }
		public double DeathDamage { get { return (double) this.deathDamage.CurrentValue; } set { this.deathDamage.CurrentValue = value; } }
	}

	/// <summary>#1022 e_strach_lord2 (Lord Strachu of Pevnost Strachu level 2): one swing in TauntOneIn, a taunt; when hit, the
	/// first of: 1 in 30 a fire daemon for 150 s ("Zhynes Strachem a Ohnem!!!"), 1 in 30 a fire tarantula for 60 s ("Zhynes
	/// Strachem!!!"), 1 in 100 a Fire Lord for 40 s ("Zhynes Ohnem!!!"), 1 in 20 ten fire columns ("Heru Beleg Ruin Val").</summary>
	[ViewableClass]
	public class StrachLord2AbilityDef : PassiveAbilityDef {
		private FieldValue tauntOneIn, daemonOneIn, tarantulaOneIn, fireLordOneIn, explosionOneIn;

		public StrachLord2AbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.tauntOneIn = this.InitTypedField("strachLord2TauntOneIn", 3, typeof(int));
			this.daemonOneIn = this.InitTypedField("strachLord2DaemonOneIn", 30, typeof(int));
			this.tarantulaOneIn = this.InitTypedField("strachLord2TarantulaOneIn", 30, typeof(int));
			this.fireLordOneIn = this.InitTypedField("strachLord2FireLordOneIn", 100, typeof(int));
			this.explosionOneIn = this.InitTypedField("strachLord2ExplosionOneIn", 20, typeof(int));
		}

		public int TauntOneIn { get { return (int) this.tauntOneIn.CurrentValue; } set { this.tauntOneIn.CurrentValue = value; } }
		public int DaemonOneIn { get { return (int) this.daemonOneIn.CurrentValue; } set { this.daemonOneIn.CurrentValue = value; } }
		public int TarantulaOneIn { get { return (int) this.tarantulaOneIn.CurrentValue; } set { this.tarantulaOneIn.CurrentValue = value; } }
		public int FireLordOneIn { get { return (int) this.fireLordOneIn.CurrentValue; } set { this.fireLordOneIn.CurrentValue = value; } }
		public int ExplosionOneIn { get { return (int) this.explosionOneIn.CurrentValue; } set { this.explosionOneIn.CurrentValue = value; } }
	}

	/// <summary>#1022 e_serpentdruid (the serpent druid and the Arcidruid): one swing in SummonOneIn, three snakes of its own
	/// within 2 tiles for 30 s; below half its hits it hisses ("hissing.") and takes its serpent form (NPCDef.SerpentForm, the
	/// archive's tag.had) for good, and is gone FormLifetime seconds later if still alive. In serpent form it summons no more,
	/// as Erebor's BODY swap took its events away. Its own snakes can't hurt it. Changed from Erebor, the user's call: the form
	/// changes the look only, so the Arcidruid's serpent doesn't take up regeneration.</summary>
	[ViewableClass]
	public class SerpentDruidAbilityDef : PassiveAbilityDef {
		private FieldValue summonOneIn, summonLifetime, formLifetime;

		public SerpentDruidAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.summonOneIn = this.InitTypedField("serpentDruidSummonOneIn", 10, typeof(int));
			this.summonLifetime = this.InitTypedField("serpentDruidSummonLifetime", 30.0, typeof(double));
			this.formLifetime = this.InitTypedField("serpentDruidFormLifetime", 2000.0, typeof(double));
		}

		public int SummonOneIn { get { return (int) this.summonOneIn.CurrentValue; } set { this.summonOneIn.CurrentValue = value; } }
		public double SummonLifetime { get { return (double) this.summonLifetime.CurrentValue; } set { this.summonLifetime.CurrentValue = value; } }
		public double FormLifetime { get { return (double) this.formLifetime.CurrentValue; } set { this.formLifetime.CurrentValue = value; } }
	}

	/// <summary>#1022 e_cajun (Cajun and Cajun Lord): when hit, unless asleep, 1 in SummonOneIn "Heru Val" and its three children
	/// around the attacker, and 1 in SummonOneIn just the third; they vanish after SummonLifetime seconds. As it dies, everyone who
	/// fought it and is in sight takes DeathDamage less by distance, "Ruth Mor", and two children rise. Fixed from Erebor: those
	/// two vanish after SummonLifetime too (Erebor's timer landed on another item, a clear bug).</summary>
	[ViewableClass]
	public class CajunAbilityDef : PassiveAbilityDef {
		private FieldValue summonOneIn, summonLifetime, deathDamage;

		public CajunAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.summonOneIn = this.InitTypedField("cajunSummonOneIn", 50, typeof(int));
			this.summonLifetime = this.InitTypedField("cajunSummonLifetime", 600.0, typeof(double));
			this.deathDamage = this.InitTypedField("cajunDeathDamage", 50.0, typeof(double));
		}

		public int SummonOneIn { get { return (int) this.summonOneIn.CurrentValue; } set { this.summonOneIn.CurrentValue = value; } }
		public double SummonLifetime { get { return (double) this.summonLifetime.CurrentValue; } set { this.summonLifetime.CurrentValue = value; } }
		public double DeathDamage { get { return (double) this.deathDamage.CurrentValue; } set { this.deathDamage.CurrentValue = value; } }
	}

	/// <summary>#1022 e_drone_large (the Elder Drone): when hit, 1 in ThreeOneIn three drones around it ("Heru Val Drone"), 1 in
	/// OneOneIn one, each for SummonLifetime seconds; either way it goes back home, leaving them with the players.</summary>
	[ViewableClass]
	public class DroneSummonerAbilityDef : PassiveAbilityDef {
		private FieldValue threeOneIn, oneOneIn, summonLifetime;

		public DroneSummonerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.threeOneIn = this.InitTypedField("droneSummonerThreeOneIn", 25, typeof(int));
			this.oneOneIn = this.InitTypedField("droneSummonerOneOneIn", 20, typeof(int));
			this.summonLifetime = this.InitTypedField("droneSummonerSummonLifetime", 60.0, typeof(double));
		}

		public int ThreeOneIn { get { return (int) this.threeOneIn.CurrentValue; } set { this.threeOneIn.CurrentValue = value; } }
		public int OneOneIn { get { return (int) this.oneOneIn.CurrentValue; } set { this.oneOneIn.CurrentValue = value; } }
		public double SummonLifetime { get { return (double) this.summonLifetime.CurrentValue; } set { this.summonLifetime.CurrentValue = value; } }
	}

	/// <summary>#1022 e_wormdrop (SwARPUS): sees a new player: three worms, and it attacks; as it dies, six worms; a Poison or
	/// Poison Field spell on it is shrugged off ("resist a poison attack"), any other spell on it brings two worms. The worms
	/// are plain monsters, as on Erebor.</summary>
	[ViewableClass]
	public class WormDropAbilityDef : PassiveAbilityDef {
		private FieldValue onSight, onDeath, onSpell;

		public WormDropAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.onSight = this.InitTypedField("wormDropOnSight", 3, typeof(int));
			this.onDeath = this.InitTypedField("wormDropOnDeath", 6, typeof(int));
			this.onSpell = this.InitTypedField("wormDropOnSpell", 2, typeof(int));
		}

		public int OnSight { get { return (int) this.onSight.CurrentValue; } set { this.onSight.CurrentValue = value; } }
		public int OnDeath { get { return (int) this.onDeath.CurrentValue; } set { this.onDeath.CurrentValue = value; } }
		public int OnSpell { get { return (int) this.onSpell.CurrentValue; } set { this.onSpell.CurrentValue = value; } }
	}

	/// <summary>#1022 e_sumoner_strazce_spicich (Strazce Mrtvich): sees a new player: a necromancer may pass ("Ucen strascu temnoty
	/// muze projit v pokoji"), a priest is attacked with a curse, anyone else brings a sand golem. Hit by a warrior: "Valecniku
	/// blaznivej...". His own blows and spells on him heal him (Greater Heal at HealPower). Dying, he blasts DeathDamage fire
	/// within 2 and is gone: no body, loot or experience. Fixed from Erebor: the heal goes to himself (Erebor's went to his
	/// target), the warrior line to warriors (Erebor's test was turned round), the necromancer's line once per sighting (Erebor's
	/// repeated it on every look).</summary>
	[ViewableClass]
	public class SandGuardianAbilityDef : PassiveAbilityDef {
		private FieldValue healPower, deathDamage;

		public SandGuardianAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.healPower = this.InitTypedField("sandGuardianHealPower", 1000, typeof(int));
			this.deathDamage = this.InitTypedField("sandGuardianDeathDamage", 10.0, typeof(double));
		}

		public int HealPower { get { return (int) this.healPower.CurrentValue; } set { this.healPower.CurrentValue = value; } }
		public double DeathDamage { get { return (double) this.deathDamage.CurrentValue; } set { this.deathDamage.CurrentValue = value; } }
	}

	/// <summary>#1022 e_ps_fanatik (Fanatik of Pevnost Strachu): "Nyni zemrete!" to each new player it sees, and one swing in
	/// TauntOneIn one of five taunts.</summary>
	[ViewableClass]
	public class FanatikAbilityDef : PassiveAbilityDef {
		private FieldValue tauntOneIn;

		public FanatikAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.tauntOneIn = this.InitTypedField("fanatikTauntOneIn", 5, typeof(int));
		}

		public int TauntOneIn { get { return (int) this.tauntOneIn.CurrentValue; } set { this.tauntOneIn.CurrentValue = value; } }
	}

	public static partial class MonsterSpecialAttacks {
		internal static FireLordAbilityDef FireLordDef { get { return Lookup<FireLordAbilityDef>("a_tevent_e_fire_lord"); } }
		internal static StrachLord2AbilityDef StrachLord2Def { get { return Lookup<StrachLord2AbilityDef>("a_tevent_e_strach_lord2"); } }
		internal static SerpentDruidAbilityDef SerpentDruidDef { get { return Lookup<SerpentDruidAbilityDef>("a_tevent_e_serpentdruid"); } }
		internal static CajunAbilityDef CajunDef { get { return Lookup<CajunAbilityDef>("a_tevent_e_cajun"); } }
		internal static DroneSummonerAbilityDef DroneSummonerDef { get { return Lookup<DroneSummonerAbilityDef>("a_tevent_e_drone_large"); } }
		internal static WormDropAbilityDef WormDropDef { get { return Lookup<WormDropAbilityDef>("a_tevent_e_wormdrop"); } }
		internal static SandGuardianAbilityDef SandGuardianDef { get { return Lookup<SandGuardianAbilityDef>("a_tevent_e_sumoner_strazce_spicich"); } }
		internal static FanatikAbilityDef FanatikDef { get { return Lookup<FanatikAbilityDef>("a_tevent_e_ps_fanatik"); } }

		private static readonly TagKey serpentFormTK = TagKey.Acquire("_serpentDruidForm_");
		private static readonly TimerKey bossVanishTK = TimerKey.Acquire("_bossVanish_");

		/// <summary>isSleeping / isSilenced: a monster put to sleep by Peacemaking, or silenced, does nothing special.</summary>
		private static bool IsSubdued(NPC npc) {
			return PeacemakingSkillDef.IsAsleep(npc) || npc.HasTimer(MageAbilities.SilencedTK);
		}

		/// <summary>Erebor's batch summon: each def at the spot within `spread`, for `lifetime` seconds (0 = for good), attacking
		/// `target`. `conjured`: no loot, no experience. `owned`: the boss's own (Erebor's MEMORY_IPET), so they never hurt it.</summary>
		internal static List<NPC> SummonGroup(NPC boss, IEnumerable<string> defnames, int x, int y, int spread, double lifetime, Character target, bool conjured, bool owned) {
			List<NPC> made = new List<NPC>();
			foreach (string defname in defnames) {
				if (!(ThingDef.GetByDefname(defname) is NPCDef def)) {
					continue;
				}
				NPC summon = (NPC) def.Create((ushort) (x + Globals.dice.Next(-spread, spread + 1)), (ushort) (y + Globals.dice.Next(-spread, spread + 1)), boss.Z, boss.M);
				if (conjured) {
					SummonSpellDef.MarkAsSummonedCreature(summon);
				}
				if (owned) {
					summon.ControlMaster = boss;
				}
				if (lifetime > 0) {
					summon.AddTimer(summonedCreatureDespawnTimerKey, new SummonSpellDef.SummonDespawnTimer()).DueInSpan = TimeSpan.FromSeconds(lifetime);
				}
				if ((target != null) && target.IsAliveAndValid) {
					NPCAiTimer.Attack(summon, target);
				}
				made.Add(summon);
			}
			return made;
		}

		/// <summary>fireexplosion(n): fire columns at random tiles within 5, 50-80 each, with no emote.</summary>
		internal static void FireColumns(Character owner, int count) {
			for (int i = 0; i < count; i++) {
				PlaceHazardTile(owner, owner.X + Globals.dice.Next(-5, 6), owner.Y + Globals.dice.Next(-5, 6), Globals.dice.Next(50, 81), DamageType.MagicFire, false);
			}
		}

		/// <summary>multi_damage on every fighter in sight: damage less by distance, an explosion on each.</summary>
		private static void DeathBlast(NPC self, Character killedBy, double damage) {
			foreach (Character fighter in LevelingSystem.FindFighters(self, killedBy, false)) {
				if ((fighter != self) && self.CanSeeLOS(fighter).Allow) {
					int distance = Point2D.GetSimpleDistance(self, fighter);
					EffectFactory.StationaryEffect(fighter, BlowUpEffect, 6, 15);
					fighter.Sound(ExplosionSound);
					DamageManager.CauseDamage(null, fighter, DamageType.Irresistable, (distance > 0) ? (damage / distance) : damage);
				}
			}
		}

		/// <summary>The boss's target, else the one who hit it.</summary>
		private static Character TargetOf(NPC boss, Character fallback) {
			return NPCAiTimer.GetFor(boss)?.Target ?? fallback;
		}

		/// <summary>TryReactToHit: the summoning bosses when hit.</summary>
		internal static void TrySummonerReactions(NPC self, Character attacker) {
			FireLordAbilityDef fireLord = FireLordDef;
			if (HasAbilityDef(self, fireLord) && !IsSubdued(self)) {
				if (OneIn(fireLord.ExplosionOneIn)) {
					self.Say("Heru Beleg Ruin Val");
					FireColumns(self, fireLord.Columns);
				}
				if (OneIn(fireLord.SummonOneIn)) {
					self.Say("Heru Ruin Val");
					SummonGroup(self, new[] { "c_m_phoenix_fire", "c_m_phoenix_fire", "c_eye_fire" }, self.X, self.Y, 1, fireLord.SummonLifetime, TargetOf(self, attacker), true, true);
				}
			}
			StrachLord2AbilityDef lord2 = StrachLord2Def;
			if (HasAbilityDef(self, lord2)) {
				if (OneIn(lord2.DaemonOneIn)) {
					self.Say("Zhynes Strachem a Ohnem!!!");
					SummonGroup(self, new[] { "c_daemon_fireps" }, self.X, self.Y, 1, 150, TargetOf(self, attacker), true, true);
				} else if (OneIn(lord2.TarantulaOneIn)) {
					self.Say("Zhynes Strachem!!!");
					SummonGroup(self, new[] { "c_tarantule_fire" }, self.X, self.Y, 1, 60, TargetOf(self, attacker), true, true);
				} else if (OneIn(lord2.FireLordOneIn)) {
					self.Say("Zhynes Ohnem!!!");
					SummonGroup(self, new[] { "c_fire_lord_summon" }, self.X, self.Y, 1, 40, TargetOf(self, attacker), true, true);
				} else if (OneIn(lord2.ExplosionOneIn)) {
					self.Say("Heru Beleg Ruin Val");
					FireColumns(self, 10);
				}
			}
			SerpentDruidAbilityDef druid = SerpentDruidDef;
			if (HasAbilityDef(self, druid) && !IsSubdued(self) && !self.HasTag(serpentFormTK) && (self.Hits < (self.MaxHits / 2))) {
				TakeSerpentForm(self, druid);
			}
			CajunAbilityDef cajun = CajunDef;
			if (HasAbilityDef(self, cajun) && !IsSubdued(self) && (attacker != null)) {
				if (OneIn(cajun.SummonOneIn)) {
					self.Say("Heru Val");
					self.Sound(533);
					SummonGroup(self, new[] { "c_cajun_child1", "c_cajun_child2", "c_cajun_child3" }, attacker.X, attacker.Y, 1, cajun.SummonLifetime, null, true, false);
				}
				if (OneIn(cajun.SummonOneIn)) {
					self.Say("Heru Val");
					self.Sound(533);
					SummonGroup(self, new[] { "c_cajun_child3" }, attacker.X, attacker.Y, 1, cajun.SummonLifetime, null, true, false);
				}
			}
			DroneSummonerAbilityDef drone = DroneSummonerDef;
			if (HasAbilityDef(self, drone)) {
				bool three = OneIn(drone.ThreeOneIn), one = OneIn(drone.OneOneIn);
				if (three || one) {
					self.Say("Heru Val Drone");
					List<string> drones = new List<string>();
					for (int i = 0; i < (three ? 3 : 0) + (one ? 1 : 0); i++) {
						drones.Add("c_drone_small");
					}
					SummonGroup(self, drones, self.X, self.Y, 1, drone.SummonLifetime, null, true, false);
					NPCAiTimer ai = NPCAiTimer.GetFor(self);
					if ((ai != null) && ai.hasHome) {
						self.Go(ai.homeX, ai.homeY, ai.homeZ, ai.homeM); //gohome
					}
				}
			}
			if (HasAbilityDef(self, SandGuardianDef) && (attacker != null) && Professions.HasProfession(attacker, Professions.Warrior)) {
				self.Say("Valecniku  blaznivej...   takovich jako ty jsem uz v mladi skolil desitky..");
			}
		}

		/// <summary>The serpent druid's change, for good: the serpent's look, no more summoning, gone in FormLifetime seconds.</summary>
		internal static void TakeSerpentForm(NPC self, SerpentDruidAbilityDef def) {
			self.Emote("hissing.");
			self.SetTag(serpentFormTK, 1);
			string form = (self.Def as NPCDef)?.SerpentForm;
			if (!string.IsNullOrEmpty(form) && (ThingDef.GetByDefname(form) is NPCDef serpent)) {
				self.Model = serpent.Model;
			}
			BossVanishTimer timer = new BossVanishTimer();
			self.AddTimer(bossVanishTK, timer);
			timer.DueInSeconds = def.FormLifetime;
		}

		internal static bool InSerpentForm(Character ch) {
			return ch.HasTag(serpentFormTK);
		}

		private static readonly string[] lord2Taunts = { "Nemeli jste sem odvazovat!", "Pripravte se na smrt!", "Bojte se!", "Treste se!", "Zemres!", "Ve jmenu Ohne a Strachu!" };
		private static readonly string[] fanatikTaunts = { "Priprav se na smrt!", "Konec se blizi!", "Za Pana Desu!", "Ceka te uz jen smrt.", "Muhaha dobra trefa." };

		/// <summary>TryReactToOwnSwing: the bosses' own blows.</summary>
		internal static void TrySummonerOwnSwing(Character attacker, Character defender) {
			if (!(attacker is NPC self)) {
				return;
			}
			StrachLord2AbilityDef lord2 = StrachLord2Def;
			if (HasAbilityDef(self, lord2) && OneIn(lord2.TauntOneIn)) {
				self.Say(lord2Taunts[Globals.dice.Next(lord2Taunts.Length)]);
			}
			FanatikAbilityDef fanatik = FanatikDef;
			if (HasAbilityDef(self, fanatik) && OneIn(fanatik.TauntOneIn)) {
				self.Say(fanatikTaunts[Globals.dice.Next(fanatikTaunts.Length)]);
			}
			SerpentDruidAbilityDef druid = SerpentDruidDef;
			if (HasAbilityDef(self, druid) && !InSerpentForm(self) && OneIn(druid.SummonOneIn)) {
				SummonGroup(self, new[] { "c_serpent_malej", "c_serpent_malej", "c_serpent_malej" }, self.X, self.Y, 2, druid.SummonLifetime, defender, true, true);
			}
			if (HasAbilityDef(self, SandGuardianDef)) {
				SandGuardianHeal(self);
			}
		}

		/// <summary>Greater Heal on the guardian himself.</summary>
		private static void SandGuardianHeal(NPC self) {
			SpellDef greaterHeal = SpellDef.GetById(29);
			if ((greaterHeal != null) && (self.Hits < self.MaxHits)) {
				greaterHeal.ApplyEffectChar(self, self, SandGuardianDef.HealPower, EffectFlag.FromAbility);
			}
		}

		/// <summary>Character.On_SpellEffect on a monster: true cancels the spell. SwARPUS shrugs off poison and calls worms for
		/// anything else; the guardian heals.</summary>
		internal static bool TryBossSpellEffect(Character target, SpellDef spell) {
			if (!(target is NPC self) || (spell == null)) {
				return false;
			}
			WormDropAbilityDef worms = WormDropDef;
			if (HasAbilityDef(self, worms)) {
				if ((spell.Id == 20) || (spell.Id == 39)) {
					self.Emote("resist a poison attack");
					return true;
				}
				Worms(self, worms.OnSpell, null);
			}
			if (HasAbilityDef(self, SandGuardianDef)) {
				SandGuardianHeal(self);
			}
			return false;
		}

		private static void Worms(NPC self, int count, Character target) {
			List<string> list = new List<string>();
			for (int i = 0; i < count; i++) {
				list.Add("c_cerv");
			}
			SummonGroup(self, list, self.X, self.Y, 1, 0, target, false, false);
		}

		/// <summary>OnMonsterDeath: the bosses' @death. Runs before the experience is shared.</summary>
		internal static void OnSummonerDeath(NPC self, Character killedBy) {
			FireLordAbilityDef fireLord = FireLordDef;
			if (HasAbilityDef(self, fireLord)) {
				DeathBlast(self, killedBy, fireLord.DeathDamage);
			}
			CajunAbilityDef cajun = CajunDef;
			if (HasAbilityDef(self, cajun)) {
				DeathBlast(self, killedBy, cajun.DeathDamage);
				self.Say("Ruth Mor");
				SummonGroup(self, new[] { "c_cajun_child1", "c_cajun_child1" }, self.X, self.Y, 1, cajun.SummonLifetime, null, false, false);
			}
			WormDropAbilityDef worms = WormDropDef;
			if (HasAbilityDef(self, worms)) {
				Worms(self, worms.OnDeath, null);
			}
			SandGuardianAbilityDef guardian = SandGuardianDef;
			if (HasAbilityDef(self, guardian)) {
				EffectFactory.StationaryEffect(self, BlowUpEffect, 6, 15);
				self.Sound(ExplosionSound);
				foreach (Character near in CharsAround(self.GetMap(), self.X, self.Y, 2)) {
					if ((near != self) && near.IsAliveAndValid) {
						DamageManager.CauseDamage(null, near, DamageType.MagicFire, guardian.DeathDamage);
					}
				}
				self.ExperienceReward = 0; //Erebor's return 1 cancelled the death: nobody earned anything
			}
		}

		/// <summary>Character.CauseDeath's corpse: none for the guardian, who just goes.</summary>
		internal static bool VanishesAtDeath(Character self) {
			return HasAbilityDef(self, SandGuardianDef);
		}

		#region @NPCSeeNewPlayer
		private static readonly ConditionalWeakTable<NPC, Dictionary<Character, double>> seenPlayers = new ConditionalWeakTable<NPC, Dictionary<Character, double>>();
		internal const double SeenMemorySeconds = 300, SightRange = 18;

		/// <summary>Sphere's @NPCSeeNewPlayer, from the AI's tick: a monster not fighting looks round, and a player in sight it
		/// hasn't met in 5 minutes counts as new. A reaction that doesn't remember the player sees them new again next look.</summary>
		internal static void TrySeeNewPlayers(NPC self) {
			if (!HasAbilityDef(self, WormDropDef) && !HasAbilityDef(self, SandGuardianDef) && !HasAbilityDef(self, FanatikDef)) {
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(self);
			if (((ai != null) && (ai.Target != null)) || !self.IsAliveAndValid || IsSubdued(self)) {
				return;
			}
			Dictionary<Character, double> seen = seenPlayers.GetOrCreateValue(self);
			double now = Globals.TimeInSeconds;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, (int) SightRange)) {
				if (!(ch is Player player) || player.IsGM || !player.IsAliveAndValid || !self.CanSeeLOS(player).Allow) {
					continue;
				}
				if (seen.TryGetValue(player, out double until) && (until > now)) {
					continue;
				}
				if (SeeNewPlayer(self, player)) {
					seen[player] = now + SeenMemorySeconds;
				}
			}
		}

		/// <summary>One new player in sight. True: remember them.</summary>
		internal static bool SeeNewPlayer(NPC self, Player player) {
			if (HasAbilityDef(self, FanatikDef)) {
				self.Say("Nyni zemrete!");
			}
			WormDropAbilityDef worms = WormDropDef;
			if (HasAbilityDef(self, worms)) {
				Worms(self, worms.OnSight, player);
				NPCAiTimer.Attack(self, player);
			}
			if (HasAbilityDef(self, SandGuardianDef)) {
				if (Professions.HasProfession(player, Professions.Necromant)) {
					self.Say("Ucen strascu temnoty muze projit v pokoji");
				} else if (Professions.HasProfession(player, Professions.Priest)) {
					self.Say("Ha Prosebnicek Valar prisel rusit klid pravich synu Numeronu..  Manwe te mel varovat ze sem namas chodit.. ted zemres");
					NPCAiTimer.Attack(self, player);
				} else {
					AnimCalculator.PerformAnim(self, GenericAnim.Cast);
					SummonGroup(self, new[] { "c_pisecny_sumon_golem" }, self.X, self.Y, 1, 0, null, false, false);
				}
			}
			return true;
		}
		#endregion
	}

	/// <summary>i_osklivejsmazse: the monster (or a gate) is gone after its time, no death, no body, no loot.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BossVanishTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public BossVanishTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Thing thing) && !thing.IsDeleted) {
				thing.Delete();
			}
		}
	}
}
