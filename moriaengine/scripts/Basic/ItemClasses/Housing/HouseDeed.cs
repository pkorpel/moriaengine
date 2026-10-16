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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// A deed that places a House (enhancements #19). The original converted content already
	/// had house deeds (scripts/converted/item_deed.def, all commented out) that referenced the
	/// target house via a RESOURCES line shared with crafting's ingredient-list syntax - that's
	/// consumption-list semantics (check/take items from a backpack), not what a deed needs, so
	/// this uses a dedicated "housedef" property instead when restoring that content.
	/// </summary>
	[ViewableClass]
	public class HouseDeedDef : ItemDef {
		public HouseDeedDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		private static readonly TagKey houseDefTK = TagKey.Acquire("_houseDeedTarget_");

		public HouseDef HouseDef {
			get {
				return this.GetTag(houseDefTK) as HouseDef;
			}
			set {
				this.SetTag(houseDefTK, value);
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param == "housedef") {
				this.HouseDef = GetByDefname(args) as HouseDef;
				if (this.HouseDef == null) {
					Logger.WriteWarning(filename, line, "housedef '" + args + "' is not a known HouseDef.");
				}
				return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}
	}

	public class t_deed_house : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return;
			}
			DenyResult canPickup = player.CanPickup(self);
			if (!canPickup.Allow) {
				canPickup.SendDenyMessage(player);
				return;
			}
			HouseDeedDef def = self.TypeDef as HouseDeedDef;
			if ((def == null) || (def.HouseDef == null)) {
				player.SysMessage("This deed is not linked to any house.");
				return;
			}
			player.SysMessage("Where would you like to place your house?");
			player.Target(SingletonScript<Targ_PlaceHouse>.Instance, new object[] { self, def.HouseDef });
		}
	}

	public class Targ_PlaceHouse : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			object[] arr = (object[]) parameter;
			Item deed = (Item) arr[0];
			HouseDef houseDef = (HouseDef) arr[1];

			if (!self.CanInteractWith(targetted).Allow) {
				self.SysMessage("You can't place a house there.");
				return TargetResult.RestartTargetting;
			}

			Map map = self.GetMap();
			int landTile = map.GetTileId(targetted.X, targetted.Y);
			if (TileData.HasFlag(TileData.GetTileFlags(landTile), TileFlag.Impassable)) {
				self.SysMessage("The ground there won't support a house.");
				return TargetResult.RestartTargetting;
			}

			House house = (House) houseDef.Create(targetted.X, targetted.Y, targetted.Z, self.M);
			if (!house.IsRegionPlaced) {
				//MultiItem.InitMultiRegion (ran during Create, via MultiItemDef.On_Create) already did the
				//real footprint-overlap check against every other multi's region - this is what actually
				//catches "there's already a house/ship here", not a single-point check.
				house.Delete();
				self.SysMessage("There is not enough room for a house there.");
				return TargetResult.RestartTargetting;
			}

			house.Owner = self;
			deed.Delete();
			self.SysMessage("You place the house.");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("You can't place a house there.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You can't place a house there.");
			return TargetResult.RestartTargetting;
		}
	}

	/// <summary>Minimal house management - full friend/co-owner/ban list editing UI is a follow-up, not built here.</summary>
	public static class HouseCommands {
		[SteamFunction]
		public static void ReleaseHouse(Character self, ScriptArgs sa) {
			Player player = self as Player;
			if (player == null) {
				return;
			}
			House house = FindOwnedHouseNearby(player);
			if (house == null) {
				player.SysMessage("You must target or stand in a house you own to release it.");
				return;
			}
			house.Delete();
			player.SysMessage("You release the house.");
		}

		private static House FindOwnedHouseNearby(Player self) {
			Region region = self.Region;
			MultiRegion multiRegion = region as MultiRegion;
			if (multiRegion != null) {
				House house = multiRegion.multiItem as House;
				if ((house != null) && (house.Owner == self)) {
					return house;
				}
			}
			return null;
		}

		/// <summary>#221 - owner/co-owner-facing (not GM-only, unlike D_AttrList's own checkbox
		/// toggle for the same flag) way to actually lock an item down, per the issue's explicit
		/// request for real player-facing functionality. Toggles rather than being a one-way
		/// lock - a real player needs to be able to undo this without GM help.</summary>
		[SteamFunction]
		public static void LockDown(Player self) {
			self.SysMessage("Target the item to lock down or release.");
			self.Target(SingletonScript<Targ_LockDown>.Instance);
		}

		/// <summary>#221 - same shape as LockDown, for attr_secure. Container-only (a non-
		/// container has nothing for "access restriction" to mean anything for).</summary>
		[SteamFunction]
		public static void Secure(Player self) {
			self.SysMessage("Target the container to secure or release.");
			self.Target(SingletonScript<Targ_Secure>.Instance);
		}
	}

	public class Targ_LockDown : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (item == null) {
				self.SysMessage("That is not an item.");
				return TargetResult.Done;
			}
			House house = House.FindHouseAt(item);
			if (house == null) {
				self.SysMessage("That is not inside a house.");
				return TargetResult.Done;
			}
			if (!house.CanMoveLockedItems(self)) {
				self.SysMessage("You do not have permission to lock down items in this house.");
				return TargetResult.Done;
			}
			bool newState = !item.HasAttribute(ItemAttributes.attr_lockeddown);
			item.SetAttribute(ItemAttributes.attr_lockeddown, newState);
			self.SysMessage(newState ? "The item is now locked down." : "The item is released.");
			return TargetResult.Done;
		}
	}

	public class Targ_Secure : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (item == null) {
				self.SysMessage("That is not an item.");
				return TargetResult.Done;
			}
			if (!item.IsContainer) {
				self.SysMessage("Only containers can be made secure.");
				return TargetResult.Done;
			}
			House house = House.FindHouseAt(item);
			if (house == null) {
				self.SysMessage("That is not inside a house.");
				return TargetResult.Done;
			}
			if (!house.CanMoveLockedItems(self)) {
				self.SysMessage("You do not have permission to secure items in this house.");
				return TargetResult.Done;
			}
			bool newState = !item.HasAttribute(ItemAttributes.attr_secure);
			item.SetAttribute(ItemAttributes.attr_secure, newState);
			self.SysMessage(newState ? "The container is now secure." : "The container is no longer secure.");
			return TargetResult.Done;
		}
	}
}
