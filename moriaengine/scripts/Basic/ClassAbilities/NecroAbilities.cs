using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the necromancer's darkpower and the mage's concentration, f_spelldone: with the counter on,
	/// a cast can give 85% of its mana back. And e_undeads_ability_life: the necromancer's undead are stronger.
	/// Prokleti and its time, and the summons' longer life, have points on Erebor and nothing that reads them.</summary>
	public static class NecroAbilities {
		[SteamFunction]
		public static void Darkpower(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Necromant)) {
				self.SysMessage("Nyni ti muze byt navracena spotrebovana mana.", ClassAbilities.ClassMessageHue);
			}
		}

		[SteamFunction]
		public static void Concentration(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Mage)) {
				self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " kouzlit za mene many.", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>f_spelldone, not for recall, mark and gate. A scroll gives back half.</summary>
		internal static void SpellDone(Player self, SpellDef spell, bool fromScroll) {
			bool mage = Professions.HasProfession(self, Professions.Mage);
			if ((!mage && !Professions.HasProfession(self, Professions.Necromant)) || (spell.Id == 32) || (spell.Id == 52) || (spell.Id == 45)) {
				return;
			}
			if (!ClassAbilities.CounterFires(self)) {
				return;
			}
			int back = (spell.ManaUse * ClassAbilitySettings.instance.manaBackPermille) / 1000;
			if (fromScroll) {
				back /= 2;
			}
			self.SysMessage(mage ? "setris manu!" : "mana navracena!", ClassAbilities.ClassMessageHue);
			self.Mana = (short) System.Math.Min(self.MaxMana, self.Mana + back);
		}

		/// <summary>e_undeads_ability_life: hits up 2% a point to 10, then (points / 2 + 15)%.</summary>
		internal static int UndeadHits(int hits, int points) {
			if (points < 11) {
				return (hits * 100 * ((points * 2) + 100)) / 10000;
			}
			return (hits * 100 * ((points / 2) + 115)) / 10000;
		}

		internal static void StrengthenUndead(NPC summon, Character master) {
			AbilityDef life = AbilityDef.GetByDefname("a_tevent_e_undeads_ability_life");
			int points = (master == null) ? 0 : ClassAbilities.Points(master, "undeads_zivoty");
			Ability ability;
			if ((life == null) || (points <= 0) || !summon.HasAbility(life, out ability)) {
				return;
			}
			master.SysMessage("Summon vyuziva tve schopnosti a zesiluje.");
			int hits = UndeadHits(summon.Hits, points);
			summon.MaxHits = (short) hits;
			summon.Hits = (short) hits;
		}
	}
}
