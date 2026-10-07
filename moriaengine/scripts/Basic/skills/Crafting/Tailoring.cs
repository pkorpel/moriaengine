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
	[ViewableClass]
	public class TailoringSkillDef : CraftingSkillDef {

		public TailoringSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void DoStroke(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.Sound(0x248); //scissors and knitting
		}
	}
}

//502767	What would you like to make?
//502768	You create the item and put it in your backpack.
//502769	You create the item and put it at your feet.
//502770	Please select the cloth you would like to use.
//502771	You cannot reach that.
//502772	That cloth belongs to someone else.
//502773	Someone else is using that cloth.
//502774	There's not enough material on that.
//502775	That cloth belongs to someone else.
//502776	Someone else is using that cloth.
//502777	There's not enough of the right type of material on that.
//502778	That's not the proper material.
//502779	You do not have enough leather to make this item.
//502780	You don't have room for the item in your pack, so you stop working on it.
//502781	You don't have room for the item and leftovers in your pack, so you stop working on it.
//502782	You place the left-over cloth pieces into your backpack
//502783	You place the left-over cloth pieces into your backpack
//502784	Due to your exceptional skill, its quality is higher than average.
//502785	You were barely able to make this item.  It's quality is below average.
//502786	You create the item and put it in your backpack.
//502787	You create the item and put it at your feet.
//502788	You throw the useless pieces away.

//1044043	You failed to create the item, and some of your materials are lost.

//1044454	You don’t have a bolt of cloth.
//1044456	You don't have any ready cloth.
//1044463	You do not have sufficient leather to make that item.