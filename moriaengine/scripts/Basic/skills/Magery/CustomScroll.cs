/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's t_customspell_scroll: a scroll of a spell past Sphere's numbers. It's for the professions
	/// tagged on its def, and customspellcast checks mana and skill before the cursor comes up.</summary>
	public class t_customspell_scroll : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				Use(player, self);
			}
			return TriggerResult.Cancel;
		}

		private static void Use(Player self, Item scroll) {
			CustomScrollLoc texts = Loc<CustomScrollLoc>.Get(self.Language);
			if ((scroll.TopObj() != self) || (scroll.Cont == self.FindLayer(LayerNames.Bankbox))) {
				self.SysMessage(texts.MustCarry);
				return;
			}
			ProfessionDef profession = self.Profession;
			if ((profession == null) || (scroll.Def.GetTag(TagKey.Acquire(profession.PrettyDefname)) == null)) {
				self.RedMessage(texts.WrongProfession);
				return;
			}
			if (scroll is SpellScroll spellScroll) {
				SpellDef spell = spellScroll.SpellDef;
				if ((spell != null) && !self.IsGM) {
					if (self.Mana < spell.ManaUse) {
						self.SysMessage(texts.NoMana);
						return;
					}
					ResourcesList req = spell.Requirements;
					if ((req != null) && !req.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out IResourceListEntry missing)) {
						self.SysMessage(texts.TooHard);
						return;
					}
				}
				MagerySkillDef.TryCastSpellFromScroll(self, spellScroll);
			} else {
				self.ClilocMessage(502345); // This spell has been temporarily disabled.
			}
		}
	}

	internal class CustomScrollLoc : CompiledLocStringCollection<CustomScrollLoc> {
		public string MustCarry = "Svitek musis mit u sebe!";
		public string WrongProfession = "Pro toto kouzlo nemas vhodne povolani";
		public string NoMana = "Na toto kouzlo ted nemas dost many.";
		public string TooHard = "Toto kouzlo je na tebe prilis slozite.";
	}
}
