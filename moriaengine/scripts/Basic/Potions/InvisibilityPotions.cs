using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>The spell's effect, and like any potion it takes the potion slot.</summary>
	public sealed class InvisibilityPotionKind : PotionKind {
		public override void Drink(Character drinker, Potion potion) {
			drinker.DeletePlugin(PotionEffectPlugin.MainSlot);
			InvisibilitySpellDef.MakeInvisible(drinker, PotionSettings.instance.invisibilitySeconds);
		}
	}

	public static class InvisibilityPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_Invisibility", new InvisibilityPotionKind());
		}
	}
}
