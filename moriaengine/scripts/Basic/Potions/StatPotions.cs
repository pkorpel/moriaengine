using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Agility, Strength, Cleverness, Energy and Spell Shield: a raised stat, in the potion slot.</summary>
	public sealed class StatPotionKind : PotionKind {
		private readonly Func<string> numbers;
		private readonly PotionStat stat;
		private readonly string name;
		private readonly int icon;

		public StatPotionKind(Func<string> numbers, PotionStat stat, string name, int icon) {
			this.numbers = numbers;
			this.stat = stat;
			this.name = name;
			this.icon = icon;
		}

		public override void Drink(Character drinker, Potion potion) {
			int[] values = Numbers(this.numbers());
			Sparkle(drinker);
			PotionEffectPlugin.Apply(drinker, PotionEffectPlugin.MainSlot, this.stat, Roll(values), values[0], this.name, this.icon);
		}
	}

	public static class StatPotions {
		private const int AgilityIcon = 0x8C8, CunningIcon = 0x8C9, StrengthIcon = 0x8CF, EnergyIcon = 0x8C3, ShieldIcon = 0x5422;

		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_AgilityLess", new StatPotionKind(() => s.agilityLess, PotionStat.Dexterity, "Agility", AgilityIcon));
			add("i_potion_Agility", new StatPotionKind(() => s.agility, PotionStat.Dexterity, "Agility", AgilityIcon));
			add("i_potion_AgilityGreat", new StatPotionKind(() => s.agilityGreat, PotionStat.Dexterity, "Agility", AgilityIcon));
			add("i_potion_AgilityElixir", new StatPotionKind(() => s.agilityElixir, PotionStat.Dexterity, "Agility", AgilityIcon));
			add("i_potion_StrengthLess", new StatPotionKind(() => s.strengthLess, PotionStat.Strength, "Strength", StrengthIcon));
			add("i_potion_Strength", new StatPotionKind(() => s.strength, PotionStat.Strength, "Strength", StrengthIcon));
			add("i_potion_StrengthGreat", new StatPotionKind(() => s.strengthGreat, PotionStat.Strength, "Strength", StrengthIcon));
			add("i_potion_StrengthElixir", new StatPotionKind(() => s.strengthElixir, PotionStat.Strength, "Strength", StrengthIcon));
			add("i_potion_CleverLess", new StatPotionKind(() => s.cleverLess, PotionStat.Intelligence, "Cleverness", CunningIcon));
			add("i_potion_Clever", new StatPotionKind(() => s.clever, PotionStat.Intelligence, "Cleverness", CunningIcon));
			add("i_potion_CleverGreat", new StatPotionKind(() => s.cleverGreat, PotionStat.Intelligence, "Cleverness", CunningIcon));
			add("i_potion_CleverElixir", new StatPotionKind(() => s.cleverElixir, PotionStat.Intelligence, "Cleverness", CunningIcon));
			add("i_potion_EnergyLess", new StatPotionKind(() => s.energyLess, PotionStat.Vitality, "Energy", EnergyIcon));
			add("i_potion_Energy", new StatPotionKind(() => s.energy, PotionStat.Vitality, "Energy", EnergyIcon));
			add("i_potion_EnergyGreat", new StatPotionKind(() => s.energyGreat, PotionStat.Vitality, "Energy", EnergyIcon));
			add("i_potion_EnergyElixir", new StatPotionKind(() => s.energyElixir, PotionStat.Vitality, "Energy", EnergyIcon));
			add("i_potion_Shield", new StatPotionKind(() => s.shield, PotionStat.MagicResist, "Spell Shield", ShieldIcon));
			add("i_potion_Shieldelixir", new StatPotionKind(() => s.shieldElixir, PotionStat.MagicResist, "Spell Shield", ShieldIcon));
		}
	}
}
