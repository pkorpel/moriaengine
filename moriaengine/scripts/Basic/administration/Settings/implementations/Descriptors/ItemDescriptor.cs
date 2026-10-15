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


namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(Item), "Item")]
	public static class ItemDescriptor {
		[Button("FixWeight")]
		public static void FixWeight(object target) {
			((Item) target).FixWeight();
		}

		//#215/#217 - opens the dedicated attribute checkbox dialog (D_AttrList), same "button
		//in .info opens a separate dialog" pattern TagHolderDescriptor already uses for
		//Taglist/Timerlist, per explicit user request for a consistent shape.
		[Button("Attributes")]
		public static void Attributes(object target) {
			D_AttrList.ShowFor((Player) Globals.SrcCharacter, (Item) target);
		}
	}

	[ViewDescriptor(typeof(Container), "Container")]
	public static class ContainerDescriptor {

		[Button("EmptyCont")]
		public static void EmptyCont(object target) {
			((Container) target).EmptyCont();
		}

		[GetMethod("Edit", typeof(ContainerView.ThingAsContainer))]
		public static object Edit(object target) {
			return new ContainerView.ThingAsContainer((Thing) target);
		}
	}

	[ViewDescriptor(typeof(ItemDispidInfo), "ItemDispidInfo")]
	public static class ItemDispidInfoDescriptor {

	}
}