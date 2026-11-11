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

	/// <summary>Erebor's def_poison, in its order.</summary>
	public enum PoisonKind {
		AbsoluteDamage, PercentOfHits, Paralyze, Fizzle, Stamina, Feeblemind, Weaken, Clumsy
	}

	[HasSavedMembers]
	[SaveableClass]
	[ViewableClass]
	public class PoisonEffectSettings : SettingsMetaCategory {
		[SavedMember]
		public static PoisonEffectSettings instance = new PoisonEffectSettings();

		[LoadingInitializer]
		public PoisonEffectSettings() {
		}

		/// <summary>Seconds between hits: nastaveni_poison_poison_*. Paralysis is the only one that differs.</summary>
		[SaveableData]
		public double secondsBetweenHits = 5;

		[SaveableData]
		public double secondsBetweenParalyses = 11;

		/// <summary>nastaveni_poison_pvm_coefficient, percent: a poisoned blade on a monster.</summary>
		[SaveableData]
		public int percentOnMonsters = 1200;

		/// <summary>nastaveni_poison_pvp_coefficient, the Poison spell on a player. The default, it isn't in Erebor's save.</summary>
		[SaveableData]
		public int percentOnPlayers = 100;

		/// <summary>nastaveni_poison_spell_poisoning_effect and _magery_effect: their defaults likewise.</summary>
		[SaveableData]
		public int spellPoisoningEffect = 205;

		[SaveableData]
		public int spellMageryEffect = 82;

		/// <summary>nastaveni_poison_mvm_coefficient, per mille of the monster's Poisoning, on another monster.</summary>
		[SaveableData]
		public int monsterOnMonster = 100;

		/// <summary>nastaveni_sip_jed, percent of the bottle's strength on arrows.</summary>
		[SaveableData]
		public int percentOnArrows = 75;

		internal double SecondsFor(PoisonKind kind) {
			return (kind == PoisonKind.Paralyze) ? this.secondsBetweenParalyses : this.secondsBetweenHits;
		}
	}

	/// <summary>#962 - Erebor's f_poison_doPoison and the poison manager, the only way anything gets poisoned: one running
	/// poison of each kind, a stronger dose replacing the weaker, each hit a tenth weaker, cure taking them all. Monsters'
	/// poison and the Poison spell come here too. #1422 - a running poison is its plugin, DamagingPoisonEffectPlugin
	/// (p_poison, Erebor's i_memory_jed).</summary>
	public static class Poison {
		private const int CursedEffectSpeed = 6, CursedEffectLength = 15, DefaultSound = 221, FullResist = 1000;
		private const int DeadlyAbove = 15, GreaterAbove = 10, NormalAbove = 5, ParalysisSeconds = 1;
		private static readonly TagKey immuneUntilTK = TagKey.Acquire("_poisonImmuneUntil_");
		private static readonly PluginKey paralysisPK = PluginKey.Acquire("_poisonParalysis_");
		private static readonly PoisonKind[] kinds = (PoisonKind[]) Enum.GetValues(typeof(PoisonKind));

		/// <summary>poison_c and the like, as the bottle's def names them.</summary>
		public static PoisonKind ParseKind(string name) {
			switch ((name ?? "").ToLowerInvariant()) {
				case "poison_para":
					return PoisonKind.Paralyze;
				case "poison_fizz":
					return PoisonKind.Fizzle;
				case "poison_stamina":
					return PoisonKind.Stamina;
				case "poison_feeblemind":
					return PoisonKind.Feeblemind;
				case "poison_weaken":
					return PoisonKind.Weaken;
				case "poison_clumsy":
					return PoisonKind.Clumsy;
				case "poison_absolutedmg":
					return PoisonKind.AbsoluteDamage;
				default:
					return PoisonKind.PercentOfHits;
			}
		}

		public static bool IsPoisoned(Character ch) {
			return Array.Exists(kinds, kind => ch.GetPlugin(DamagingPoisonEffectPlugin.KeyOf(kind)) != null);
		}

		/// <summary>The running poison of that kind, or null.</summary>
		public static DamagingPoisonEffectPlugin Running(Character ch, PoisonKind kind) {
			return ch.GetPlugin(DamagingPoisonEffectPlugin.KeyOf(kind)) as DamagingPoisonEffectPlugin;
		}

		/// <summary>f_pm_isImmune: from a cure, or from resist.</summary>
		public static bool IsImmune(Character ch) {
			return ((ch.GetTag(immuneUntilTK) is double until) && (until > Globals.TimeInSeconds)) || (ch.ResistPoison >= FullResist);
		}

		public static void SetImmunity(Character ch, double seconds) {
			double until = Globals.TimeInSeconds + seconds;
			if (!(ch.GetTag(immuneUntilTK) is double already) || (already < until)) {
				ch.SetTag(immuneUntilTK, until);
			}
		}

		/// <summary>f_poison_doPoison. False when it didn't take.</summary>
		public static bool Apply(Character target, Character dealer, int strength, double seconds, PoisonKind kind, bool quiet = false, int sound = 0) {
			if (IsImmune(target) || target.Flag_Dead) {
				return false;
			}
			FightMemory.Record(target, dealer);
			PoisonLoc texts = Loc<PoisonLoc>.Get(target.Language);
			DamagingPoisonEffectPlugin running = Running(target, kind);
			if (running != null) {
				if (running.Strength < strength) {
					running.Renew(dealer, strength);
					target.SysMessage(target.IsFemale ? texts.AnotherDoseFemale : texts.AnotherDoseMale);
				}
			} else {
				DamagingPoisonEffectPlugin poison = (DamagingPoisonEffectPlugin) DamagingPoisonEffectPluginDef.Instance.Create();
				target.AddPlugin(DamagingPoisonEffectPlugin.KeyOf(kind), poison);
				poison.Begin(dealer, kind, strength, seconds);
				target.SysMessage(target.IsFemale ? texts.PoisonedFemale : texts.PoisonedMale);
			}
			if (!quiet) {
				EffectFactory.StationaryEffect(target, ThingDef.GetByDefname("i_fx_curse").Model, CursedEffectSpeed, CursedEffectLength);
			}
			target.Sound((sound == 0) ? DefaultSound : sound);
			return true;
		}

		/// <summary>f_pm_removeAll: what a cure does. A death ends them too (EffectDurationPlugin.On_Death).</summary>
		public static void RemoveAll(Character ch) {
			foreach (PoisonKind kind in kinds) {
				ch.DeletePlugin(DamagingPoisonEffectPlugin.KeyOf(kind));
			}
		}

		[SteamFunction]
		public static void CurePoison(Character self) {
			RemoveAll(self);
		}

		/// <summary>The hit itself. Nothing left of it ends the poison.</summary>
		internal static int Hit(Character self, DamagingPoisonEffectPlugin poison) {
			WeeklyQuests.OnPoisonTick(self, poison.Dealer, poison.Strength); //#1090 - i_memory_jed's tick
			switch (poison.Kind) {
				case PoisonKind.AbsoluteDamage:
					return Damage(self, poison.Strength);
				case PoisonKind.PercentOfHits:
					return Damage(self, (self.MaxHits * poison.Strength) / 100);
				case PoisonKind.Paralyze:
					if (self.ResistParalyse > poison.Strength) {
						return 0;
					}
					ParalyzeEffectPluginDef.Paralyze(self, paralysisPK, TimeSpan.FromSeconds(ParalysisSeconds));
					return 1;
				case PoisonKind.Fizzle:
					SkillSequenceArgs.AbortSkill(self);
					HidingSkillDef.Reveal(self);
					ScriptedSkills.Stop(self);
					DamageManager.CauseDamage(null, self, DamageType.MagicPoison, 1);
					return 1;
				case PoisonKind.Stamina:
					int lost = (self.MaxStam * poison.Strength) / 100;
					self.Stam = (short) Math.Max(self.Stam - lost, 0);
					return lost;
				default:
					StatPoison.Apply(self, poison);
					return 1;
			}
		}

		private static int Damage(Character self, int amount) {
			int done = DamageManager.CauseDamage(null, self, DamageType.MagicPoison, amount); //damage_noresponse: credit is by FightMemory
			PoisonLoc texts = Loc<PoisonLoc>.Get(self.Language);
			if (self is Player) {
				if (done > DeadlyAbove) {
					self.SysMessage(self.IsFemale ? texts.DeadlyFemale : texts.DeadlyMale);
				} else if (done > GreaterAbove) {
					self.SysMessage(self.IsFemale ? texts.GravelyFemale : texts.GravelyMale);
				} else if (done > NormalAbove) {
					self.SysMessage(self.IsFemale ? texts.PoisonedPlainFemale : texts.PoisonedPlainMale);
				} else if (done > 0) {
					self.SysMessage(self.IsFemale ? texts.SlightlyFemale : texts.SlightlyMale);
				} else {
					self.SysMessage(texts.NoEffect);
				}
			}
			return done;
		}

		/// <summary>f_poisoning_Trigger: a monster's blow, 1 in 1000 for each point of its skill.</summary>
		internal static void FromMonster(NPC self, Character target) {
			int poisoning = self.GetSkill(SkillName.Poisoning);
			if ((poisoning <= 0) || (Globals.dice.Next(1, 1001) > poisoning)) {
				return;
			}
			PoisonEffectSettings settings = PoisonEffectSettings.instance;
			int strength = (target is NPC) ? (poisoning * settings.monsterOnMonster) / 1000 : poisoning / 100;
			Apply(target, self, strength, settings.secondsBetweenHits, PoisonKind.AbsoluteDamage);
		}
	}

	/// <summary>f_eqrune: the same curse as the spell's, renewed at every hit. A monster of 110 or more loses a share, not
	/// a number. Nothing goes under 10.</summary>
	internal static class StatPoison {
		private const int ShareFrom = 110, Least = 10;

		internal static void Apply(Character self, DamagingPoisonEffectPlugin poison) {
			string spell = (poison.Kind == PoisonKind.Feeblemind) ? "s_feeblemind" : (poison.Kind == PoisonKind.Weaken) ? "s_weaken" : "s_clumsy";
			int stat = (poison.Kind == PoisonKind.Feeblemind) ? self.Int : (poison.Kind == PoisonKind.Weaken) ? self.Str : self.Dex;
			int lower = (self.IsPlayer || (stat < ShareFrom)) ? Math.Min(poison.Strength, stat - Least) : (stat * poison.Strength) / 100;
			DurableCharEffectSpellDef def = (DurableCharEffectSpellDef) SpellDef.GetByDefname(spell);
			self.DeletePlugin(def.EffectPluginKey_Potion);
			EffectDurationPlugin plugin = (EffectDurationPlugin) def.EffectPluginDef.Create();
			plugin.Init(poison.Dealer, EffectFlag.HarmfulEffect | EffectFlag.FromPotion, -Math.Max(lower, 0),
				TimeSpan.FromSeconds(PoisonEffectSettings.instance.SecondsFor(poison.Kind)), def);
			self.AddPlugin(def.EffectPluginKey_Potion, plugin);
		}
	}

	internal class PoisonLoc : CompiledLocStringCollection<PoisonLoc> {
		public string Poisoned = "Jed";
		public string PoisonedMale = "Byl jsi otraven!";
		public string PoisonedFemale = "Byla jsi otravena!";
		public string AnotherDoseMale = "Dostal jsi dalsi davku jedu!";
		public string AnotherDoseFemale = "Dostala jsi dalsi davku jedu!";
		public string DeadlyMale = "Jsi smrtelne otraven!";
		public string DeadlyFemale = "Jsi smrtelne otravena!";
		public string GravelyMale = "Jsi tezce otraven!";
		public string GravelyFemale = "Jsi tezce otravena!";
		public string PoisonedPlainMale = "Jsi otraven!";
		public string PoisonedPlainFemale = "Jsi otravena!";
		public string SlightlyMale = "Jsi lehce otraven!";
		public string SlightlyFemale = "Jsi lehce otravena!";
		public string NoEffect = "Otrava na tebe nema zadny ucinek.";
	}
}
