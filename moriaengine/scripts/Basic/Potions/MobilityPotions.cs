using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Protects from paralysis, briefly.</summary>
	public static class MobilityPotions {
		private const int Icon = 0x8E5;

		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_mobility", new StatPotionKind(() => PotionSettings.instance.mobility, PotionStat.ResistParalyse, "Mobility", Icon));
		}
	}
}
