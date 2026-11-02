using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>What a spell does for an NPC deciding whether it's worth casting.</summary>
	internal enum NpcSpellKind {
		Unknown,
		Heal,
		Cure,
		Buff,
		Damage,
		Poison,
		Paralyze,
		Curse,
		Debuff,
		ManaDrain,
		Dispel,
		Summon,
		Field,
	}

	/// <summary>
	/// #1033 - which of the spells an NPC could cast right now is worth it. Support spells count
	/// only when they'd do something. Offensive ones follow Erebor's ai_returnspell: dispel a
	/// summon, attack a paralyzed target, paralyze one time in 3, curse a target at full health,
	/// the weakest damage against reflection, as it comes back at the caster, poison one time in 4, and otherwise damage, the more
	/// of it after the target's resists the better. Mana Drain goes for a mage with over half its mana. A score of 0 means don't cast it; with no spell
	/// above 0 the NPC just fights. Left out: Erebor's "cast fast when attacked", NPC casts here go
	/// off at once and can't be interrupted, and tag.lecitel healers, who heal friends.
	/// </summary>
	internal static class NpcSpellChoice {
		/// <summary>GM-level, not from Magery: NPCs have no meaningful skill set up for this.</summary>
		internal const int SpellPower = 700;

		private const double DispelSummonScore = 0.9;
		private const double CureScore = 0.85;
		private const double ParalyzeScore = 0.8;
		private const double ReflectionBreakerScore = 0.8;
		private const double FreshTargetCurseScore = 0.78;
		private const double PoisonScore = 0.77;
		private const double BuffScore = 0.72;
		private const double SummonScore = 0.6;
		private const double ManaDrainScore = 0.79;
		private const double MinorDebuffScore = 0.5;
		private const double FieldScore = 0.45;
		private const double WastedOnReflectionScore = 0.2;
		//the strongest damage spell gets 0.75, one half as good 0.65, a paralyzed target adds 0.1
		private const double DamageFloorScore = 0.55;
		private const double DamageRangeScore = 0.2;
		private const double ParalyzedTargetBonus = 0.1;
		//a heal grows with how hurt the NPC is: half dead, it beats everything but a dispel
		private const double HealBaseScore = 0.4;
		private const double NoisePerMille = 30;
		private const double MaxPoisonShare = 0.25;

		internal static NpcSpellKind KindOf(SpellDef spell) {
			if (spell is HealingSpellDef) {
				return NpcSpellKind.Heal;
			}
			if (spell is ManaDrainSpellDef) {
				return NpcSpellKind.ManaDrain;
			}
			if (spell is DispellDef) {
				return NpcSpellKind.Dispel;
			}
			if (spell is SummonSpellDef) {
				return NpcSpellKind.Summon;
			}
			if (spell is WallSpellDef) {
				return NpcSpellKind.Field;
			}
			if (spell is PoisonSpellDef) {
				return NpcSpellKind.Poison;
			}
			if (spell is DamageSpellDef) {
				return NpcSpellKind.Damage;
			}
			if ((spell == SpellDef.GetByDefname("s_cure")) || (spell == SpellDef.GetByDefname("s_archcure"))) {
				return NpcSpellKind.Cure;
			}
			if (spell is DurableCharEffectSpellDef) {
				if ((spell.Flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial) {
					return NpcSpellKind.Buff;
				}
				if (spell == SpellDef.GetByDefname("s_paralyze")) {
					return NpcSpellKind.Paralyze;
				}
				if (IsCurse(spell)) {
					return NpcSpellKind.Curse;
				}
				return NpcSpellKind.Debuff;
			}
			return NpcSpellKind.Unknown;
		}

		internal static SpellDef Choose(NPC self, Character target, List<SpellDef> spells) {
			Situation now = new Situation(self, target, spells);
			SpellDef best = null;
			double bestScore = 0;
			foreach (SpellDef spell in spells) {
				double score = Score(self, target, spell, now);
				if (score > bestScore) {
					bestScore = score;
					best = spell;
				}
			}
			return best;
		}

		private static double Score(NPC self, Character target, SpellDef spell, Situation now) {
			switch (KindOf(spell)) {
				case NpcSpellKind.Heal:
					return ScoreHeal(self, spell);
				case NpcSpellKind.Cure:
					return Poison.IsPoisoned(self) ? CureScore : 0;
				case NpcSpellKind.Buff:
					return HasEffect(self, (DurableCharEffectSpellDef) spell) ? 0 : BuffScore;
				case NpcSpellKind.Damage:
					return ScoreDamage(self, target, (DamageSpellDef) spell, now);
				case NpcSpellKind.Poison:
					return ScorePoison(target, now);
				case NpcSpellKind.Paralyze:
					if (now.reflects || now.paralyzed) {
						return 0;
					}
					return (now.rollParalyze || (now.bestDamage <= 0)) ? ParalyzeScore : 0;
				case NpcSpellKind.Curse:
					if (now.reflects || IsCursed(target)) {
						return 0;
					}
					return (target.Hits >= target.MaxHits) ? FreshTargetCurseScore : MinorDebuffScore;
				case NpcSpellKind.Debuff:
					return (now.reflects || HasEffect(target, (DurableCharEffectSpellDef) spell)) ? 0 : MinorDebuffScore;
				case NpcSpellKind.ManaDrain:
					return (!now.reflects && (target.Mana * 2 > target.MaxMana) && (target.GetSkill(SkillName.Magery) > 0)) ? ManaDrainScore : 0;
				case NpcSpellKind.Dispel:
					return (((spell.Flags & SpellFlag.CanEffectChar) == SpellFlag.CanEffectChar) && SummonSpellDef.IsSummonedCreature(target)) ? DispelSummonScore : 0;
				case NpcSpellKind.Summon:
					return (SummonSpellDef.CountSummonsNear(self) < NpcAggroSettings.instance.npcMaxSummons) ? SummonScore + Noise() : 0;
				case NpcSpellKind.Field:
					return now.rollField ? FieldScore : 0;
			}
			return 0; //nothing says it would help, and a cast it doesn't need is what this class stops
		}

		private static double ScoreHeal(NPC self, SpellDef spell) {
			if (self.MaxHits <= 0) {
				return 0;
			}
			double share = self.Hits / (double) self.MaxHits;
			if ((share * 100) >= NpcAggroSettings.instance.npcHealBelowPercent) {
				return 0;
			}
			if ((self.MaxHits - self.Hits) < spell.GetEffectForValue(SpellPower)) {
				return 0;
			}
			return HealBaseScore + (1 - share);
		}

		private static double ScoreDamage(NPC self, Character target, DamageSpellDef spell, Situation now) {
			double damage;
			if (!now.damage.TryGetValue(spell, out damage)) {
				return 0;
			}
			if (now.reflects) {
				return (damage <= now.weakestDamage) ? ReflectionBreakerScore : WastedOnReflectionScore;
			}
			if (damage <= 0) {
				return 0;
			}
			double score = DamageFloorScore + (DamageRangeScore * damage / now.bestDamage) + Noise();
			return now.paralyzed ? score + ParalyzedTargetBonus : score;
		}

		private static double ScorePoison(Character target, Situation now) {
			if (now.reflects || Poison.IsPoisoned(target) || Poison.IsImmune(target) ||
					(DamageManager.GetResistModifier(target, DamageType.MagicPoison) <= MaxPoisonShare)) {
				return 0;
			}
			return (now.rollPoison || (now.bestDamage <= 0)) ? PoisonScore : 0;
		}

		private static bool HasEffect(Character ch, DurableCharEffectSpellDef spell) {
			return (ch.GetPlugin(spell.EffectPluginKey_Spell) != null) || (ch.GetPlugin(spell.EffectPluginKey_Potion) != null);
		}

		private static bool IsCurse(SpellDef spell) {
			return (spell == SpellDef.GetByDefname("s_curse")) || (spell == SpellDef.GetByDefname("s_mass_curse"));
		}

		private static bool IsCursed(Character ch) {
			return HasEffect(ch, (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_curse")) ||
				HasEffect(ch, (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_mass_curse"));
		}

		private static double Noise() {
			return Globals.dice.Next((int) NoisePerMille) / 1000.0;
		}

		/// <summary>What's looked up once for one decision.</summary>
		private sealed class Situation {
			internal readonly bool reflects;
			internal readonly bool paralyzed;
			internal readonly bool rollParalyze = Globals.dice.Next(3) == 0;
			internal readonly bool rollPoison = Globals.dice.Next(4) == 0;
			internal readonly bool rollField = Globals.dice.Next(4) == 0;
			internal readonly Dictionary<SpellDef, double> damage = new Dictionary<SpellDef, double>();
			internal readonly double bestDamage;
			internal readonly double weakestDamage = double.MaxValue;

			internal Situation(NPC self, Character target, List<SpellDef> spells) {
				this.reflects = MagicReflectionPluginDef.GetMagicReflectionPlugin(target) != null;
				this.paralyzed = ParalyzeEffectPluginDef.IsParalyzed(target);
				foreach (SpellDef spell in spells) {
					DamageSpellDef attack = spell as DamageSpellDef;
					if (attack != null) {
						double estimate = attack.EstimateDamage(self, target, SpellPower);
						this.damage[attack] = estimate;
						this.bestDamage = System.Math.Max(this.bestDamage, estimate);
						this.weakestDamage = System.Math.Min(this.weakestDamage, estimate);
					}
				}
			}
		}
	}
}
