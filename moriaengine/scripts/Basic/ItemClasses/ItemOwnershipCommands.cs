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

namespace SteamEngine.CompiledScripts {
	/// <summary>#223 - minimal player-facing way to actually use attr_owned's owner-UID link.
	/// The issue only asked for the storage slot plus a sensible pickup check, not a full
	/// ownership-management UI - this target-cursor command (same shape as #221's
	/// .LockDown/.Secure) is the smallest useful way to exercise it for real.</summary>
	public static class ItemOwnershipCommands {
		[SteamFunction]
		public static void ClaimOwnership(Player self) {
			self.SysMessage("Target the item to claim as your own.");
			self.Target(SingletonScript<Targ_ClaimOwnership>.Instance);
		}
	}

	public class Targ_ClaimOwnership : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (item == null) {
				self.SysMessage("That is not an item.");
				return TargetResult.Done;
			}
			item.OwnedBy = self;
			item.SetAttribute(ItemAttributes.attr_owned, true);
			self.SysMessage("You claim this item as your own.");
			return TargetResult.Done;
		}
	}
}
