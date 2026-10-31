namespace SteamEngine.CompiledScripts {

	/// <summary>#927 - Erebor's drinking macros: the strongest one carried first.</summary>
	public static class PotionCommands {
		private static void Use(Character self, params string[] bestFirst) {
			foreach (string defname in bestFirst) {
				foreach (Item item in VarGate.Carried(self)) {
					if ((item is Potion potion) && VarGate.Matches(potion, defname, false)) {
						potion.On_DClick(self);
						return;
					}
				}
			}
		}

		[SteamFunction] public static void PotionCure(Character self) { Use(self, "i_potion_CureGreat", "i_potion_Cure", "i_potion_CureLess"); }
		[SteamFunction] public static void PotionInvis(Character self) { Use(self, "i_potion_Invisibility"); }
		[SteamFunction] public static void PotionReactive(Character self) { Use(self, "i_potion_reactive"); }
		[SteamFunction] public static void PotionReflection(Character self) { Use(self, "i_potion_reflection"); }
		[SteamFunction] public static void PotionReflex(Character self) { Use(self, "i_potion_reflection"); }
		[SteamFunction] public static void PotionNightsight(Character self) { Use(self, "i_potion_Nightsight"); }
		[SteamFunction] public static void PotionShrink(Character self) { Use(self, "i_potion_Shrink"); }
		[SteamFunction] public static void DragonResist(Character self) { Use(self, "i_potion_dragon_resist"); }
		[SteamFunction] public static void PotionHeal(Character self) { Use(self, "i_potion_HealGreat", "i_potion_Heal", "i_potion_HealLess"); }
		[SteamFunction] public static void ElixirHeal(Character self) { Use(self, "i_potion_HealElixir"); }
		[SteamFunction] public static void PotionRefresh(Character self) { Use(self, "i_potion_RefreshGreater", "i_potion_Refresh", "i_potion_RefreshLess"); }
		[SteamFunction] public static void ElixirRefresh(Character self) { Use(self, "i_potion_RefreshElixir"); }
		[SteamFunction] public static void PotionEnergy(Character self) { Use(self, "i_potion_EnergyGreat", "i_potion_Energy", "i_potion_EnergyLess"); }
		[SteamFunction] public static void ElixirEnergy(Character self) { Use(self, "i_potion_EnergyElixir"); }
		[SteamFunction] public static void PotionStrength(Character self) { Use(self, "i_potion_StrengthGreat", "i_potion_Strength", "i_potion_StrengthLess"); }
		[SteamFunction] public static void ElixirStrength(Character self) { Use(self, "i_potion_StrengthElixir"); }
		[SteamFunction] public static void PotionClever(Character self) { Use(self, "i_potion_CleverGreat", "i_potion_Clever", "i_potion_CleverLess"); }
		[SteamFunction] public static void ElixirClever(Character self) { Use(self, "i_potion_CleverElixir"); }
		[SteamFunction] public static void PotionAgility(Character self) { Use(self, "i_potion_AgilityGreat", "i_potion_Agility", "i_potion_AgilityLess"); }
		[SteamFunction] public static void ElixirAgility(Character self) { Use(self, "i_potion_AgilityElixir"); }
		[SteamFunction] public static void PotionMobility(Character self) { Use(self, "i_potion_mobility"); }
		[SteamFunction] public static void PotionSpellShield(Character self) { Use(self, "i_potion_Shield"); }
		[SteamFunction] public static void ElixirSpellShield(Character self) { Use(self, "i_potion_Shieldelixir"); }
		[SteamFunction] public static void PotionStoneskin(Character self) { Use(self, "i_potion_Stoneskin"); }
		[SteamFunction] public static void ElixirStoneskin(Character self) { Use(self, "i_potion_Stoneskinelixir"); }

		[SteamFunction]
		public static void ThrowExplosion(Character self) {
			if (self is Player player) {
				BombThrowing.Aim(player, false);
			}
		}

		[SteamFunction]
		public static void ThrowDispel(Character self) {
			if (self is Player player) {
				BombThrowing.Aim(player, true);
			}
		}
	}
}
