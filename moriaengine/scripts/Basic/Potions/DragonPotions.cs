using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>All break if put down. The three cures have nothing to cure until the fire dragons'
	/// i_fire_dragon_efect_* are ported.</summary>
	public class DragonCurePotionKind : PotionKind {
		public override void Drink(Character drinker, Potion potion) {
		}

		public override void On_PutOnGround(Character by, Potion potion) {
			by?.SysMessage("Ups, rozbila se...");
			potion.AddTimer(BrokenBottleTimer.Key, new BrokenBottleTimer()).DueInSeconds = 0;
		}
	}

	public sealed class DragonResistPotionKind : DragonCurePotionKind {
		private const int Icon = 0x8EA;

		public override void Drink(Character drinker, Potion potion) {
			int[] values = Numbers(PotionSettings.instance.dragonResist);
			Sparkle(drinker);
			PotionEffectPlugin.Apply(drinker, PotionEffectPlugin.MainSlot, PotionStat.DragonResist, Roll(values), values[0], "Dragon Protect", Icon);
		}
	}

	public static class DragonPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_dragon_poison", new DragonCurePotionKind());
			add("i_potion_dragon_fire", new DragonCurePotionKind());
			add("i_potion_dragon_white", new DragonCurePotionKind());
			add("i_potion_dragon_resist", new DragonResistPotionKind());
		}
	}
}
