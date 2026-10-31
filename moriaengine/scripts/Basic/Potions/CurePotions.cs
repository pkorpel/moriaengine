using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Not in the archive beyond their strengths, nastaveni_potions_i_potion_Cure*. They lower a
	/// poison as the Cure spell does.</summary>
	public sealed class CurePotionKind : PotionKind {
		private readonly Func<int> strength;

		public CurePotionKind(Func<int> strength) {
			this.strength = strength;
		}

		public override void Drink(Character drinker, Potion potion) {
			Sparkle(drinker);
			ScriptHolder.GetFunction("CureEffect").TryRun(drinker, (double) this.strength());
		}
	}

	/// <summary>q006's potion: its worth is to the quest, which isn't ported.</summary>
	public sealed class PurificationPotionKind : PotionKind {
		public override void Drink(Character drinker, Potion potion) {
			drinker.SysMessage("Niceho zvlastniho jsi si nevsimnul.");
		}
	}

	public static class CurePotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_ocisteni", new PurificationPotionKind());
			add("i_potion_CureLess", new CurePotionKind(() => s.cureLess));
			add("i_potion_Cure", new CurePotionKind(() => s.cure));
			add("i_potion_CureGreat", new CurePotionKind(() => s.cureGreat));
		}
	}
}
