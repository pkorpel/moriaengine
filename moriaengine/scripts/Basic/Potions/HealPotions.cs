using System;

namespace SteamEngine.CompiledScripts {

	public sealed class HealPotionKind : PotionKind {
		private readonly Func<int> amount;

		public HealPotionKind(Func<int> amount) {
			this.amount = amount;
		}

		/// <summary>drinkHealPotion.</summary>
		public override void Drink(Character drinker, Potion potion) {
			if (drinker.Hits > 0) {
				int before = drinker.Hits;
				drinker.AddHits(this.amount());
				HpSight.ShowChange(drinker, drinker.Hits - before, true);
			}
		}
	}

	public static class HealPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_HealLess", new HealPotionKind(() => s.healLess));
			add("i_potion_Heal", new HealPotionKind(() => s.heal));
			add("i_potion_HealGreat", new HealPotionKind(() => s.healGreat));
			add("i_potion_HealElixir", new HealPotionKind(() => s.healElixir));
		}
	}
}
