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

namespace SteamEngine {
	public enum LayerNames {
		None = 0,			//non-equippable items have this.

		Hand1 = 1,			//one-handed weapons or tools
		Hand2 = 2,			//two-handed weapons or tools, or shields or one-handed accessory items (like torches)
		Shoes = 3,
		Pants = 4,			//Also some kinds of armor
		Shirt = 5,
		Helmet = 6,			//or hats, etc
		Gloves = 7,
		Ring = 8,			//only one, oddly
		Light = 9,  		//Apparently this is where you put i_light_source or i_dark_source. Does i_dark_source work?
		Collar = 10,		//Necklace, Gorget, Mempo, etc.
		Gorget = 10,
		Hair = 11,
		HalfApron = 12,
		Chest = 13,			//Primarily for armor
		Bracelet = 14,			//Bracelets.
		Hidden = 15,		//Apparently you can equip animation items (i_fx_*) here - that would probably give you an endlessly repeating animation.
		Beard = 16,
		Tunic = 17,
		Earrings = 18,			//Earrings
		Arms = 19,			//For various armor's arms
		Cape = 20,			//Cape/Cloak/Etc
		Pack = 21,			//For the backpack.
		Robe = 22,			//Robe, death shroud, hooded robe, etc
		Skirt = 23,
		Leggins = 24,			//Platemail in particular
		Mount = 25,			//Or any mount
		VendorStock = 26,	//Items we restock automatically, and sell. (Price, amount, amount when fully stocked)
		VendorExtra = 27,	//Items given or sold to us by players, which we will re-sell (Price, amount)
		VendorBuys = 28,	//Examples of items that we will buy. (Price, max amount we want to have?)
		Bankbox = 29,
		Special = 30,		//Used in sphere for memory items, timer items, etc.
		Dragging = 31		//Used when dragging stuff around.
	}
}