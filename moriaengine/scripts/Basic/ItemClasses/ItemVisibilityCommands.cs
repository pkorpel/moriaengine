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
	/// <summary>#224 - minimal way to actually use attr_invis's per-viewer SeenBy exception
	/// list. GMs already see every invisible item unconditionally (Character.
	/// CanSeeVisibility(Item) special-cases IsGM), so this command targets an item THEN a
	/// character to grant - matches real Sphere's own "TAG.SeenBy_[CharUID]" grant mechanic,
	/// and the issue's own "characters explicitly granted visibility" wording - granting
	/// visibility to some OTHER character, not just the GM's own already-unrestricted view.</summary>
	public static class ItemVisibilityCommands {
		[SteamFunction]
		public static void Reveal(Player self) {
			self.SysMessage("Target the invisible item to reveal.");
			self.Target(SingletonScript<Targ_RevealItem>.Instance);
		}
	}

	public class Targ_RevealItem : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (item == null) {
				self.SysMessage("That is not an item.");
				return TargetResult.Done;
			}
			self.SysMessage("Now target the character to reveal it to.");
			self.Target(SingletonScript<Targ_RevealTarget>.Instance, item);
			return TargetResult.Done;
		}
	}

	public class Targ_RevealTarget : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = (Item) parameter;
			Character viewer = targetted as Character;
			if (viewer == null) {
				self.SysMessage("That is not a character.");
				return TargetResult.Done;
			}
			item.GrantVisibilityTo(viewer);
			self.SysMessage("Visibility granted.");
			return TargetResult.Done;
		}
	}
}
