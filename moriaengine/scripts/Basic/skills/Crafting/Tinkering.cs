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
	public class TinkeringSkillDef : CraftingSkillDef {

		public TinkeringSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void DoStroke(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.Sound(0x241); //lockpicking sound
		}
	}
}
//502924	Which gem will you use to make the jewelry?
//502925	You don't have the resources required to make that item.
//502926	That is not proper material for tinkering items.
//502927	You already have a tinkering menu.
//502928	What materials would you like to work with?
//502929	There is not enough room in your backpack!  You do not assemble the sextant.
//502930	Use that on an axle to make an axle with gears.
//502931	Use that on an axle with gears to make clock parts.
//502932	Use that on an axle with gears to make sextant parts.
//502933	Use that on gears to make an axle with gears.
//502934	Use that on clock parts to make a clock.
//502935	Use that on springs to make clock parts, or a hinge to make sextant parts.
//502936	Use that on a clock frame to make a clock.

//502957	You don't have the resources required to make that item.
//502958	Use this on only one gem.
//502959	You don't have room for that item.
//502960	You fail to make the jewelry properly.
//502961	That's not a gem or jewel of the proper type.
//502962	Use raw material.
//502963	You decide you don't want to make anything.
//502964	You didn't select anything.

//1044039	You need a tinker's toolkit to make that.

//1044043	You failed to create the item, and some of your materials are lost.

//1044627	You don't have enough sand to make that.
//1044628	You must be near a forge to blow glass.


//1044633	You haven't learned masonry.  Perhaps you need to study a book!
//1044634	You haven't learned glassblowing.  Perhaps studying a book would help!
//1044635	Requires masonry (carpentry specialization)
//1044636	Requires glassblowing (alchemy specialization)