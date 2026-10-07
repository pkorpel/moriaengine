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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class PlayerVendor {

		public override void On_Create() {
			base.On_Create();


		}

		//public void TradeOperationStarted(Player player) {
		//    //TODO: Ensure initialisation?
		//}

		public bool CanTradeWithMessage(Player player) {
			//TODO: check realm, etc.

			return true;
		}

		/// <summary>
		/// Determines whether the specified player can control this vendor (i.e. is the owner). If not, sends deny message.
		/// </summary>
		public bool CanVendorBeControlledByWithMessage(Player player) {
			//TODO? override pet system?

			//does player own the vendor?
			if (!this.CanVendorBeControlledBy(player)) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.VendorIsntYours);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines whether the specified player can control this vendor (i.e. is the owner).
		/// </summary>
		public bool CanVendorBeControlledBy(Player player) {
			return this.IsPetOf(player);
		}

		/// <summary>
		/// Determines whether the specified player can interact with the vendor.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <returns>
		///   <c>true</c> if this instance [can interact with vendor message] the specified player; otherwise, <c>false</c>.
		/// </returns>
		public bool CanInteractWithVendorMessage(Player player) {

			if (!this.IsOperational) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.VendorOffline);
				return false;
			}

			//is player close to vendor?
			if (!player.CanReachWithMessage(this)) {
				return false;
			}

			return true;
		}

		public bool IsOperational {
			get { return this.isOperational; }
			set { this.isOperational = value; }
		}

		#region Stock methods
		public Container StockRoot {
			get {
				var def = PlayerVendorDef.StockContainerDef;
				var root = this.FindLayer(def.Layer);

				if (root == null) {
					def.Create(this);
				}

				return (Container) root;
			}
		}

		/// <summary>
		/// Determines whether the specified player can stock the specified item within this vendor.
		/// This is the aggregate method, checking everything needed including vendor ownership and reachable state.
		/// In the negative fall, sends a sysmessage to the player.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="thingBeingStocked">The thing being stocked.</param>
		/// <returns>
		///   <c>true</c> if the specified player can stock the specified item within this vendor; otherwise, <c>false</c>.
		/// </returns>
		public bool CanStockWithMessage(Player player, Thing thingBeingStocked) {

			if (!this.CanVendorBeControlledByWithMessage(player) || !this.CanInteractWithVendorMessage(player)) {
				return false;
			}

			var asItem = thingBeingStocked as Item;

			if (asItem != null) {
				//selling item. Check allowed amount and type
				if (asItem.Type is t_gold) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.YouCantStockMoney);
					return false;
				}
				if (asItem.RecursiveCount > 500) {
					player.WriteLineLoc<PlayerVendorLoc>(l => l.StockedContainerTooFull);
					return false;
				}

				//most important check - whether the item is in players reach/possession
				return player.CanPickUpWithMessage(asItem);

			}
			//pet being sold - check ownership
			var pet = (Character) thingBeingStocked;

			if (!pet.IsPetOf(player)) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.ThisNpcIsntYours);
				return false;
			}

			if (!pet.IsAnimal) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.CanOnlySellAnimals);
				return false;
			}

			//is the pet close enough
			return player.CanReachWithMessage(pet);
		}

		/// <summary>
		/// Adds new stock section.
		/// </summary>
		/// <param name="parentSection">The parent section.</param>
		/// <param name="description">The description.</param>
		/// <param name="model">The model.</param>
		/// <param name="color">The color.</param>
		public void AddNewStockSection(Container parentSection, string description, int model, int color) {
			Sanity.IfTrueThrow(parentSection.TopObj() != this, "parentSection.TopObj() != this");

			var section = PlayerVendorDef.StockContainerDef.Create(parentSection);
			section.Name = description;
			section.Model = model;
			section.Color = color;
		}

		/// <summary>
		/// Stocks a new thing - i.e. puts it into vendor to be sold. CanStockWithMessage or equivalent is supposed to have been already called on the thing.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="thingToStock">The thing to stock.</param>
		/// <param name="parentCategory">The parent category.</param>
		/// <param name="description">The description.</param>
		/// <param name="price">The price.</param>
		public void StockThing(Player player, Thing thingToStock, Container parentCategory, string description, decimal price) {
			var itemToStock = thingToStock as Item;

			var entry = (PlayerVendorStockEntry) PlayerVendorDef.StockEntryDef.Create(parentCategory);
			if (itemToStock != null) {
				try {
					entry.origDescription = description;
					entry.Name = description;
					entry.soldByUnits = false;
					entry.price = price;
					entry.Model = itemToStock.Model;
					entry.Color = itemToStock.Color;

					//we make the player pick up the item and put it down into the entry container already in the vendor, so that all logs and stuff are consistent. We don't use the "Try" methods since CanPickup was supposedly already run
					player.PickupItem(itemToStock, itemToStock.Amount);
					int x, y;
					entry.GetRandomXYInside(out x, out y);
					player.PutItemInItem(entry, x, y, false);

				} catch {
					if (entry.Count == 0) {
						entry.Delete();
					}
					throw;
				}

			} else {
				try {
					var petToStock = (Character) thingToStock;
					petToStock.Disconnect();

					entry.Link = petToStock;
					entry.soldByUnits = false;
					entry.Name = description;
					entry.origDescription = description;
					entry.price = price;
					entry.Model = petToStock.TypeDef.Icon;
					entry.Color = petToStock.Color;

				} catch {
					if (entry.Link == null) {
						entry.Delete();
					}
				}
			}
		}

		/// <summary>
		/// Stocks a new container of things to be sold by unit. "CanPickup" or similar checks are not being run here, are supposed to be run already.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="units">The units.</param>
		/// <param name="parentCategory">The parent category.</param>
		/// <param name="description">The description.</param>
		/// <param name="price">The price.</param>
		public void StockThingSoldByUnit(Player player, Container units, Container parentCategory, string description, decimal price) {
			int model = -1;
			int color = -1;
			int counter = 0;

			List<Item> approvedItems = new List<Item>(units.Count);

			foreach (Item unit in units) {
				if (player.CanPickup(unit).Allow) {
					approvedItems.Add(unit);

					if (approvedItems.Count == 1) {
						model = unit.Model;
						color = unit.Color;
					}

					counter += unit.Amount;
				}
			}

			if (approvedItems.Count == 0) {
				player.WriteLineLoc<PlayerVendorLoc>(l => l.NoUnitAvailable);
				return;
			}

			var entry = (PlayerVendorStockEntry) PlayerVendorDef.StockEntryDef.Create(parentCategory);
			try {
				entry.origDescription = description;
				entry.Name = SoldByUnitEntryName(counter, description);
				entry.soldByUnits = true;
				entry.price = price;
				entry.Model = model;
				entry.Color = color;

				foreach (var approved in approvedItems) {
					//we make the player pick up every item and put it down into the entry container already in the vendor, so that all logs and stuff are consistent. We don't use the "Try" methods since we already ran CanPickup
					player.PickupItem(approved, approved.Amount);
					int x, y;
					entry.GetRandomXYInside(out x, out y);
					player.PutItemInItem(entry, x, y, false);
				}

			} catch {
				if (entry.Count == 0) {
					entry.Delete();
				}
				throw;
			}
		}

		private static string SoldByUnitEntryName(int counter, string description) {
			return string.Concat(
				counter.ToString(CultureInfo.InvariantCulture),
				"x ", description);
		}
		#endregion

		internal void RecallEntry(Player player, PlayerVendorStockEntry entry, int units) {
			this.BuyOrRecallImpl(player, entry, units, false);
		}

		internal bool TryBuyEntry(Player player, PlayerVendorStockEntry entry, int units) {
			return this.BuyOrRecallImpl(player, entry, units, true);
		}

		private bool BuyOrRecallImpl(Player player, PlayerVendorStockEntry entry, int units, bool buying) {
			if (entry.soldByUnits) {

				var totalUnits = entry.Aggregate(0, (a, i) => i.Amount + a); //countamount
				units = Math.Min(units, totalUnits);

				if (buying) {
					decimal priceTotal;
					try {
						priceTotal = entry.price * units;
					} catch {
						player.WriteLineLoc<PlayerVendorLoc>(l => l.PriceCalculationError);
						return false;
					}

					if (!player.Pay(priceTotal)) {
						return false;
					}
				}

				var toMoveTotal = units;
				var backpack = player.Backpack;

				while (toMoveTotal > 0) {
					var item = entry.FindCont(0);

					var toMove = Math.Min(item.Amount, toMoveTotal);

					player.PickupItem(item, toMove);
					int x, y;
					entry.GetRandomXYInside(out x, out y);
					player.PutItemInItem(backpack, x, y, true);

					toMoveTotal -= toMove;
				}

			} else {
				if (buying && !player.Pay(entry.price)) {
					return false;
				}

				var asChar = (Character) entry.Link;
				if (asChar != null) {
					asChar.P(player.P());
					asChar.Reconnect();
					entry.Delete();
					return true;
				}
				var item = entry.FindCont(0);

				player.PickupItem(item, item.Amount);
				int x, y;
				entry.GetRandomXYInside(out x, out y);
				player.PutItemInItem(player.Backpack, x, y, true);
			}


			if (entry.Count == 0) {
				entry.Delete();
			}
			return true;
		}
	}

	[ViewableClass]
	public partial class PlayerVendorDef {

		private static ContainerDef i_playervendor_stock_container;
		public static ContainerDef StockContainerDef {
			get {
				if (i_playervendor_stock_container == null) {
					i_playervendor_stock_container = (ContainerDef) GetByDefname("i_playervendor_stock_container");
				}
				return i_playervendor_stock_container;
			}
		}

		private static PlayerVendorStockEntryDef i_playervendor_stock_entry;
		public static PlayerVendorStockEntryDef StockEntryDef {
			get {
				if (i_playervendor_stock_entry == null) {
					i_playervendor_stock_entry = (PlayerVendorStockEntryDef) GetByDefname("i_playervendor_stock_entry");
				}
				return i_playervendor_stock_entry;
			}
		}
	}

	public class PlayerVendorLoc : CompiledLocStringCollection<PlayerVendorLoc> {
		public string YouCantStockMoney = "Nemùžeš prodávat hotovost";
		public string StockedContainerTooFull = "Nemùžeš prodávat kontejner s více jak 500ks zboží";

		public string NoUnitAvailable = "Kontejner pro jednotkový prodej musíš mít otevøený a v nìm alespoò 1 pøedmìt.";

		public string VendorOffline = "Tento prodejce má zavøeno";
		public string VendorIsntYours = "Tento prodejce ti není podøízen.";

		public string ThisNpcIsntYours = "Tento tvor ti nepatøí.";
		public string CanOnlySellAnimals = "Prodávat lze jen zvíøata";

		public string PriceCalculationError = "Cenu nelze vypoèítat.";
	}
}