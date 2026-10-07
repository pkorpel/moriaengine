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
	public class BlacksmithySkillDef : CraftingSkillDef {

		public BlacksmithySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		//do the "hammer on anvil" sound
		protected override void DoStroke(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.Sound(0x2a);
		}
	}
}
//500418	This item is of imported material, probably bought at a store, and does not yield much metal.
//500419	Some metal seems to have lost its color due to the heat.
//500420	You are not near a forge.
//500421	You cannot use that.
//500422	You can't work on that item.
//500423	That is already in full repair.
//500424	You destroyed the item.
//500425	You repair the item.
//500426	You can't repair that.
//500427	You cannot smith that item!
//500428	This strange metal is beyond your capability--you have no idea how to work it!
//500429	You cannot melt something that is being worn or held.
//500430	You feel compelled to not destroy this shield.
//500431	You may only melt items which are in your backpack.
//500432	You may only melt items which are in your backpack.
//500433	This item cannot be melted.
//500434	That is being used by someone else.
//500435	Select your choice from the menu above.
//500436	Select item to repair.
//500437	Select the item you wish to melt.
//500438	Target the metal you would like to use.
//500439	There is not enough metal there to make that item.
//500440	You cannot place the item into your backpack! Construction stopped.
//500441	You lost some metal.
//500442	You create the item and put it in your backpack.
//500443	In the process of hammering out the blade, you lose the distinctive color of the source metal.
//500444	Due to your exceptional skill, it's quality is higher than average.
//500445	You were barely able to make this item.  Its quality is below average.
//500446	That is too far away.
//500447	That is not accessible.

//501969	That ore belongs to someone else.
//501970	Someone is using that ore.
//501971	Select the forge on which to smelt the ore, or another pile of ore with which to combine it.
//501972	Select another pile of ore with which to combine this.
//501973	You cannot use that.
//501974	You are dead.
//501975	That is too far away.
//501976	The ore is too far away.
//501977	You can't see that.
//501978	The weight is too great to combine in a container.
//501979	You cannot combine ores of different metals.
//501980	You cannot combine ores of different metals.
//501981	You cannot combine ores of different metals.
//501982	You cannot combine ores of different metals.
//501983	You cannot combine ores of different metals.
//501984	You cannot combine ores of different metals.
//501985	You cannot combine ores of different metals.
//501986	You have no idea how to smelt this strange ore!
//501987	There is not enough metal-bearing ore in this pile to make an ingot.
//501988	You smelt the ore removing the impurities and put the metal in your backpack.
//501989	You burn away the impurities but are left with no useable metal.
//501990	You burn away the impurities but are left with less useable metal.
//501991	You are too fatigued to even lift a finger.

//502966	You must use metal to craft that item.

//1010015	You already have a blacksmithing menu.
//1010016	You did not select anything to make.

//1044035	You must wait a few moments before crafting another item.
//1044037	You do not have sufficient metal to make that.

//1044038	You have worn out your tool! 
//1044043	You failed to create the item, and some of your materials are lost.

//1044261	You have worn out your tongs.
//1044262	You have worn out your hammer.
//1044263	The tool must be on your person to use.

//1044265	You must be near a forge
//1044266	You must be near an anvil
//1044267	You must be near an anvil and a forge to smith items.
//1044268	You cannot work this strange and unusual metal.
//1044269	You have no idea how to work this metal.
//1044270	You melt the item down into ingots.
//1044271	You can't melt down the tool you are working with!
//1044272	You can't melt that down into ingots.
//1044273	Target an item to recycle.
//1044274	The item must be in your backpack to recycle it.
//1044275	The item must be in your backpack to repair it.
//1044276	Target an item to repair.
//1044277	That item cannot be repaired.
//1044278	That item has been repaired many times, and will break if repairs are attempted again.
//1044279	You repair the item.
//1044280	You fail to repair the item.
//1044281	That item is in full repair
//1044282	You must be near a forge and and anvil to repair items.
//1044283	You cannot repair that.