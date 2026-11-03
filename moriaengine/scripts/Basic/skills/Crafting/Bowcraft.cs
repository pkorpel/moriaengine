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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#962 - Erebor's bowcrafting_targon_item: a knife on wood, ingots or ammunition opens the menu.</summary>
	public class Targ_BowcraftKnife : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			if (CorpseCarving.TryBlade(self, parameter as Item, targetted)) { //#1070 - a corpse, or the shaman's dagger on a head
				return TargetResult.Done;
			}
			if (CraftingSkillDef.IsOfType(targetted as Item, "t_log", "t_ingot", "t_weapon_bolt", "t_weapon_bolt_jagged", "t_weapon_arrow")) {
				CraftingSkillDef.OpenMenu(self, SkillName.Fletching);
			}
			return TargetResult.Done;
		}
	}

	[ViewableClass]
	public class BowcraftSkillDef : CraftingSkillDef {

		public BowcraftSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string FailMessage(Character self) {
			return SpoiltProduct(self);
		}

		//#962 - every stage, as Sphere's Skill_Bowcraft
		protected override void DoEveryStage(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.Sound(0x55);
			AnimCalculator.PerformAnim(skillSeqArgs.Self, GenericAnim.Salute);
		}
	}
}
//501778	You make an arrow and put it in your backpack.
//501779	You make some arrows and put them in your backpack.
//501780	You make a bolt and put it in your backpack.
//501781	You make some bolts and put them in your backpack.

//1044043	You failed to create the item, and some of your materials are lost.