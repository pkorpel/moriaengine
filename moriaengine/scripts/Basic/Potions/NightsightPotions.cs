using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Its own slot, so it stays beside a stat potion.</summary>
	public sealed class NightsightPotionKind : PotionKind {
		private const int Icon = 0x8C5;

		public override void Drink(Character drinker, Potion potion) {
			Sparkle(drinker);
			PotionEffectPlugin.Apply(drinker, PotionEffectPlugin.NightSightSlot, PotionStat.NightSight, 0, PotionSettings.instance.nightsightSeconds, "Nightsight", Icon);
		}
	}

	public static class NightsightPotions {
		internal static bool HasNightSight(Character ch) {
			return ch.GetPlugin(PotionEffectPlugin.NightSightSlot) != null;
		}

		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_Nightsight", new NightsightPotionKind());
		}
	}
}
