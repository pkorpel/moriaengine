using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Reflection and Reactive armor are the spell's effect at full power: SRC.SPELLEFFECT n 1000.</summary>
	public sealed class SpellEffectPotionKind : PotionKind {
		private const int FullPower = 1000;
		private readonly int spellId;

		public SpellEffectPotionKind(int spellId) {
			this.spellId = spellId;
		}

		public override void Drink(Character drinker, Potion potion) {
			Sparkle(drinker);
			SpellDef.GetById(this.spellId).ApplyEffectChar(drinker, drinker, FullPower, EffectFlag.FromPotion | EffectFlag.BeneficialEffect);
		}
	}

	public static class SpellEffectPotions {
		private const int ReactiveArmor = 7, MagicReflection = 36;

		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_reflection", new SpellEffectPotionKind(MagicReflection));
			add("i_potion_reactive", new SpellEffectPotionKind(ReactiveArmor));
		}
	}
}
