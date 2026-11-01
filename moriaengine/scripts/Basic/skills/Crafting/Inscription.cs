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

using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class InscriptionSkillDef : CraftingSkillDef {

		public InscriptionSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string FailMessage(Character self) {
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
			return self.IsFemale ? texts.SpoiltScrollFemale : texts.SpoiltScrollMale;
		}

		private const int SphereSpells = 64;

		//a failure costs the scroll's whole recipe on top of Sphere's part, as Erebor's @Fail
		protected override void ConsumeFailed(ItemDef iDef, Character self) {
			base.ConsumeFailed(iDef, self);
			IResourceListEntry missing;
			if ((iDef.Resources != null) && iDef.Resources.HasResourcesPresent(self, ResourcesLocality.Backpack, out missing)) {
				iDef.Resources.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
			}
		}

		protected override bool CanStartMaking(ItemDef iDef, Character self) {
			return base.CanStartMaking(iDef, self) && CanCast(SpellOf(iDef), self);
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			TriggerResult result = base.On_Start(skillSeqArgs);
			if (result != TriggerResult.Cancel) {
				Character self = skillSeqArgs.Self;
				self.Sound(0x249); //inscribing the scroll
				SpellDef spell = SpellOf((ItemDef) skillSeqArgs.Param1);
				if ((spell != null) && !self.IsGM) {
					//Sphere takes it all for spells 1 to 64 only, Erebor's makethis settles it at half
					int half = spell.ManaUse / 2;
					self.Mana = (short) Math.Max(0, self.Mana - ((spell.Id <= SphereSpells) ? (spell.ManaUse - half) : half));
				}
			}
			return result;
		}

		private static SpellDef SpellOf(ItemDef iDef) {
			SpellScrollDef scroll = iDef as SpellScrollDef;
			return (scroll == null) ? null : scroll.SpellDef;
		}

		/// <summary>Erebor's canmakethis for a scroll: can cast it now, and it's on the class's scribe_ list. Any other item passes.</summary>
		internal static bool CanScribe(ItemDef iDef, Character chr) {
			SpellDef spell = SpellOf(iDef);
			return (!(iDef is SpellScrollDef)) || (CanCast(spell, chr) && ClassScribes(spell, chr));
		}

		//Sphere's Spell_CanCast from one's own book
		private static bool CanCast(SpellDef spell, Character chr) {
			if (spell == null) {
				return false;
			}
			IResourceListEntry missing;
			if ((chr.Mana < spell.ManaUse) || !chr.CheckAliveWithMessage()) {
				return false;
			}
			if ((spell.Requirements != null) && !spell.Requirements.HasResourcesPresent(chr, ResourcesLocality.BackpackAndLayers, out missing)) {
				return false;
			}
			if ((spell.Resources != null) && !spell.Resources.HasResourcesPresent(chr, ResourcesLocality.Backpack, out missing)) {
				return false;
			}
			return HasBookWith(spell, chr);
		}

		private static bool HasBookWith(SpellDef spell, Character chr) {
			SpellBook inHand = chr.FindLayer(LayerNames.Hand1) as SpellBook;
			if ((inHand != null) && inHand.HasSpell(spell)) {
				return true;
			}
			foreach (AbstractItem item in chr.Backpack) {
				SpellBook book = item as SpellBook;
				if ((book != null) && book.HasSpell(spell)) {
					return true;
				}
			}
			return false;
		}

		//spells_scribe_<class>_0_<word>: a bit per spell, 32 to a word
		private static bool ClassScribes(SpellDef spell, Character chr) {
			int bit = spell.Id - 1;
			Constant word = Constant.GetByName("spells_scribe_" + AlchemySkillDef.EreborClass(chr) + "_0_" + Math.Min(bit / 32, 2));
			return (word != null) && ((Convert.ToInt64(word.Value, System.Globalization.CultureInfo.InvariantCulture) >> (bit % 32)) & 1) != 0;
		}
	}
}