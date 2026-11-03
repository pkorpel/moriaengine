using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the craftsman's vigour, miningfix.scp: with the counter on, a try at a fresh spot can
	/// find the next metal or wood up, if the skill is enough for it.</summary>
	public static class CraftsmanAbilities {
		private static readonly string[] metals = { "copper", "iron", "verite", "valorite", "obsidian", "adamantinum", "mithril" };

		[SteamFunction]
		public static void Vigour(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Craftsman)) {
				self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " nalezt lepsi materialy.", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>specability_craft: the counter's chance by level, and nastaveni_ability_craft_vigour per point.</summary>
		internal static bool FindsBetter(Character self, string ability) {
			Player player = self as Player;
			if ((player == null) || !Professions.HasProfession(player, Professions.Craftsman) || !ClassAbilities.CounterOn(player)) {
				return false;
			}
			int chance = CombatCalculator.CalculateClassAbility(player.EffectiveLevel) + (ClassAbilities.Points(player, ability) * ClassAbilitySettings.instance.craftVigour);
			return Globals.dice.Next(100001) < chance;
		}

		/// <summary>nexto_ and nextl_ of sphereregion.scp: o_copper to o_iron and so on, null at the top.</summary>
		internal static RegionResourceDef Next(RegionResourceDef resource) {
			if (resource == null) {
				return null;
			}
			string name = resource.Defname;
			if ((name.Length < 3) || (name[1] != '_') || ((name[0] != 'o') && (name[0] != 'l'))) {
				return null;
			}
			int at = Array.IndexOf(metals, name.Substring(2).ToLowerInvariant());
			if ((at < 0) || (at + 1 >= metals.Length)) {
				return null;
			}
			return RegionResourceDef.GetByDefname(name.Substring(0, 2) + metals[at + 1]);
		}
	}
}
