using System;

namespace SteamEngine.CompiledScripts {

	public sealed class HpSightPotionKind : PotionKind {
		private readonly Func<double> seconds;

		public HpSightPotionKind(Func<double> seconds) {
			this.seconds = seconds;
		}

		public override void Drink(Character drinker, Potion potion) {
			HpSight.Add(drinker, this.seconds());
		}
	}

	public static class HpSightPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_HPsightLess", new HpSightPotionKind(() => s.hpSightLessSeconds));
			add("i_potion_HPsight", new HpSightPotionKind(() => s.hpSightSeconds));
			add("i_potion_HPsightGreat", new HpSightPotionKind(() => s.hpSightGreatSeconds));
			add("i_potion_HPsightElixir", new HpSightPotionKind(() => s.hpSightElixirSeconds));
		}
	}
}
