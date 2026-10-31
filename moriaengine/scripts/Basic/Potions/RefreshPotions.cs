using System;

namespace SteamEngine.CompiledScripts {

	public sealed class RefreshPotionKind : PotionKind {
		private readonly Func<int> permille;

		public RefreshPotionKind(Func<int> permille) {
			this.permille = permille;
		}

		public override void Drink(Character drinker, Potion potion) {
			drinker.AddStamina((this.permille() * drinker.MaxStam) / 1000);
		}
	}

	public static class RefreshPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_RefreshLess", new RefreshPotionKind(() => s.refreshLess));
			add("i_potion_Refresh", new RefreshPotionKind(() => s.refresh));
			add("i_potion_RefreshGreater", new RefreshPotionKind(() => s.refreshGreater));
			add("i_potion_RefreshElixir", new RefreshPotionKind(() => s.refreshElixir));
		}
	}
}
