using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Stoneskin and Steelskin, Sphere's armor spells: more armor, in a slot of their own.</summary>
	public sealed class SkinPotionKind : PotionKind {
		private const int Icon = 0x8CE;
		private readonly Func<string> numbers;
		private readonly string name;

		public SkinPotionKind(Func<string> numbers, string name) {
			this.numbers = numbers;
			this.name = name;
		}

		public override void Drink(Character drinker, Potion potion) {
			int[] values = Numbers(this.numbers());
			Sparkle(drinker);
			PotionEffectPlugin.Apply(drinker, PotionEffectPlugin.SkinSlot, PotionStat.Armor, Roll(values), values[0], this.name, Icon);
		}
	}

	public static class SkinPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_Stoneskin", new SkinPotionKind(() => s.stoneskin, "Stoneskin"));
			add("i_potion_Stoneskinelixir", new SkinPotionKind(() => s.stoneskinElixir, "Steelskin"));
		}
	}
}
