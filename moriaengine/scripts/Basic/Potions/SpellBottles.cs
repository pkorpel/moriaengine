using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Sphere's plain T_POTION bottles: a spell on the drinker, at a power rolled per bottle.</summary>
	public sealed class SpellBottleKind : PotionKind {
		private readonly string spell;
		private readonly int minPower, maxPower;

		public SpellBottleKind(string spell, int minPower, int maxPower) {
			this.spell = spell;
			this.minPower = minPower;
			this.maxPower = maxPower;
		}

		public override void Drink(Character drinker, Potion potion) {
			SpellDef def = SpellDef.GetByDefname(this.spell) as SpellDef;
			if (def != null) {
				EffectFlag good = def.Flags.HasFlag(SpellFlag.IsHarmful) ? EffectFlag.HarmfulEffect : EffectFlag.BeneficialEffect;
				def.ApplyEffectChar(drinker, drinker, Globals.dice.Next(this.minPower, this.maxPower + 1), EffectFlag.FromPotion | good);
			}
		}
	}

	public static class SpellBottles {
		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_bottle_black", new NightsightPotionKind());
			add("i_bottle_orange", new SpellBottleKind("s_cure", 300, 900));
			add("i_bottle_blue", new SpellBottleKind("s_agility", 300, 900));
			add("i_bottle_white", new SpellBottleKind("s_strength", 300, 900));
			add("i_bottle_green", new SpellBottleKind("s_poison", 300, 900));
			add("i_bottle_red", new SpellBottleKind("s_refresh", 300, 900));
			add("i_bottle_yellow", new SpellBottleKind("s_heal", 300, 900));
			add("i_bottle_purple", new SpellBottleKind("s_explosion", 300, 800));
		}
	}
}
