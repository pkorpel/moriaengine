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


using System.Linq;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {


	/// <summary>
	/// Detail of an entry already in stock - either to be sold to a customer or to be edited by the vendor owner. 
	/// It is somewhat similar to the "_Stock" dialog, but "focus" object is the Entry, not the item/char itself
	/// </summary>
	public class D_PlayerVendor_StockItemDetail : CompiledGumpDef {
		const int inputId_Price = 1;
		const int inputId_Units = 2;

		const int buttonId_ChangePrice = 1;
		const int buttonId_BuyOrRecall = 2;
		const int buttonId_AddMoreUnits = 3;
		const int buttonId_Examine = 4;


		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			var entry = (PlayerVendorStockEntry) focus;
			var item = (Item) entry.FindCont(0);
			var vendor = (PlayerVendor) entry.TopObj();
			var player = (Player) sendTo;

			var isMyVendor = vendor.CanVendorBeControlledBy(player);

			var loc = Loc<Loc_D_PlayerVendor_StockItemDetail>.Get(sendTo.Language);

			string header;
			int amount;
			if (entry.soldByUnits) {
				amount = entry.Aggregate(0, (a, i) => i.Amount + a); //countamount
				header = item.Name;
			} else {
				amount = item.Amount;
				header = string.Concat(amount.ToString(), " ", item.Name);
			}


			//var asItem = focus as Item;
			//if (asItem != null) {
			//    amount = asItem.Amount;

			//    enableSoldByUnit = asItem.IsContainer;

			//    header = string.Format(Loc<Loc_D_PlayerVendor_StockItemDetail>.Get(sendTo.Language).Header_Item,
			//        amount, asItem.Name);
			//    if (amount == 1) {
			//        description = asItem.Name;
			//    } else {
			//        description = string.Concat(amount, " ", asItem.Name);
			//    }
			//} else {
			//    header = string.Format(Loc<Loc_D_PlayerVendor_StockItemDetail>.Get(sendTo.Language).Header_Char,
			//        focus.Def.Name);
			//    description = focus.Def.Name;
			//}


			var dialogHandler = new ImprovedDialog(this.GumpInstance);

			//create the background GUTAMatrix and set its size       
			dialogHandler.CreateBackground(300);
			dialogHandler.SetLocation(100, 100);



			//header
			var t = dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			t.AddToCell(0, 0, GUTAText.Builder.TextHeadline(header).Build());
			t.AddToCell(0, 1, GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build());
			t.Transparent = true;

			//htmlgump - description
			t = dialogHandler.AddTable(new GUTATable(3, 0));
			t.AddToCell(0, 0, GUTAHTMLText.Builder.Text(entry.Name).Build());
			t.Transparent = true;

			//color
			t = dialogHandler.AddTable(new GUTATable(1, 80, 0));
			t.AddToCell(0, 0, GUTAText.Builder.Text(loc.Label_Color).Build());
			t.AddToCell(0, 1, GUTAText.Builder.Hue(item.Color - 1).Text("||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||").Build());
			t.Transparent = true;

			//price (entry if my vendor)
			if (isMyVendor) {
				t = dialogHandler.AddTable(new GUTATable(1, 80, 100, 0));
				t.AddToCell(0, 1, GUTAInput.Builder.Id(inputId_Price).Text(entry.price.ToString()).Build());
				//change price button
				t.AddToCell(0, 2, GUTAButton.Builder.Id(buttonId_ChangePrice).Build());
				t.AddToCell(0, 2, GUTAText.Builder.Text(loc.Label_Change).XPos(30).Build());
			} else {
				t = dialogHandler.AddTable(new GUTATable(1, 80, 0));
				t.AddToCell(0, 1, GUTAText.Builder.Text(entry.price.ToString()).Build());
			}
			t.AddToCell(0, 0, GUTAText.Builder.Text(loc.Label_Price).Build());
			t.Transparent = true;

			//units
			if (entry.soldByUnits) {
				t = dialogHandler.AddTable(new GUTATable(1, 80, 0));
				t.AddToCell(0, 0, GUTAText.Builder.Text(loc.Label_Units).Build());
				string entryText = isMyVendor ? "" : "1";

				t.AddToCell(0, 1, GUTAInput.Builder.Id(inputId_Units).Text(entryText).Build());
				t.AddToCell(0, 1, GUTAText.Builder.Text(amount.ToString()).XPos(100).Build());

				t.Transparent = true;
			}

			//last row with buttons
			t = dialogHandler.AddTable(new GUTATable(1, 100, 90, 0));
			t.AddToCell(0, 0, GUTAButton.Builder.Id(buttonId_BuyOrRecall).Type(LeafComponentTypes.ButtonTick).Build());
			string buyOrRecall = isMyVendor ? loc.Label_Recall : loc.Label_Buy;
			t.AddToCell(0, 0, GUTAText.Builder.Text(buyOrRecall).XPos(35).Build());
			if (entry.soldByUnits) {
				t.AddToCell(0, 1, GUTAText.Builder.Text(loc.Label_Add).Build());
				t.AddToCell(0, 1, GUTAButton.Builder.Id(buttonId_AddMoreUnits).XPos(35).Build());
			}
			t.AddToCell(0, 2, GUTAButton.Builder.Id(buttonId_Examine).Build());
			t.AddToCell(0, 2, GUTAText.Builder.Text(loc.Label_Examine).XPos(35).Build());

			t.Transparent = true;

			dialogHandler.WriteOut();

			#region orig. spherescript
			//[DIALOG d_vendor_item]
			//SetLocation=100,100
			//argo.tag(sirka,300)
			//argo.tag(vyska,<eval (8*d_def_radek_vyska)+(7*d_def_skvira)+(2*d_def_okraj)>)
			//argo.dialog_prvni
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//argo.dialog_pozadi(<argo.tag(nexty)>,3)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1,80)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1,80)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1,80)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//argo.dialog_zpruhledni

			//button((<argo.tag(sirka)>-<d_def_okraj>)-33,<argo.tag(obj_y[0])>,0fb1,0fb3,1,0,0)
			//argo.settext(100,<tag(description)>)//(d_Def_odsazeni(2*(d_def_okraj+d_def_skvira)))
			//argo.HTMLGUMP(<argo.dialog_textpos(1,0)>,argo.tag(sirka)-(d_Def_odsazeni+(2*(d_def_okraj+d_def_skvira))),3*d_def_radek_vyska,100,0,1)

			//argo.texta(<argo.dialog_textpos(0,0)>,0481,<amount> <name>)
			//argo.texta(<argo.dialog_textpos(2,0)>,0481,Barva)
			//argo.texta(<argo.dialog_textpos(2,1)>,<EVAL <color>-1>,"||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||")
			//argo.texta(<argo.dialog_textpos(3,0)>,0481,Cena)
			//settext(105,<tag.PRICE>)
			//if (topobj.ismypet)
			// argo.textentry(<argo.dialog_textpos(3,1)>,100,d_def_radek_vyska,0481,1,105)
			// argo.button(lastxpos+100,lastypos,4005,4007,1,0,4)//zmenit cenu
			// argo.texta(lastxpos+135,lastypos,0481,Zmenit)//zmenit cenu 
			//else
			// argo.text(<argo.dialog_textpos(3,1)>,0481,105) 
			//endif
			//if (tag(jednot))
			// argo.texta(<argo.dialog_textpos(4,0)>,0481,Jednotek?)
			// if (topobj.ismypet)
			//  argo.dialog_textpos(5,0,1)
			//  argo.button(lastxpos+100,lastypos,4005,4007,1,0,3)//
			//  argo.texta(lastxpos+135,lastypos,0481,Pridat)//
			//  argo.settext(110,)
			// else
			//  argo.settext(110,1)
			// endif
			// argo.textentry(<argo.dialog_textpos(4,1)>,100,d_def_radek_vyska,0481,0,110)
			// argo.texta(lastxpos+100,lastypos,0481,<link.countamount>)
			//endif

			//argo.button(<argo.dialog_textpos(5,0,1)>,4005,4007,1,0,1)//koupit / stahnout
			//if (topobj.ismypet)
			// argo.texta(lastxpos+35,lastypos,0481,Stahnout)
			//else
			// argo.texta(lastxpos+35,lastypos,0481,Koupit)
			//endif
			//if (link.type==t_container)&&(safe link.rescount) && !(link.id==i_truhla_heslo)//
			// argo.button(lastxpos+190,lastypos,4005,4007,1,0,2)//
			// argo.texta(lastxpos+225,lastypos,0481,Projit)//
			// return -2
			// break
			//endif

			//if (link.cont.baseid==i_pouch)
			// if (link.cont.rescount==1) && !(link.id==i_truhla_heslo)//if (cont.baseid==i_pouch)&&(cont.rescount==1)
			//  argo.button(lastxpos+190,lastypos,4005,4007,1,0,5)//
			//  argo.texta(lastxpos+221,lastypos,0481,Prohledni)//
			// endif
			//endif
			#endregion
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//try {
			var button = gr.PressedButton;

			if (button == 0) {
				return;
			}

			var entry = (PlayerVendorStockEntry) gi.Focus;
			var item = (Item) entry.FindCont(0);
			var vendor = (PlayerVendor) entry.TopObj();
			var player = (Player) gi.Cont;

			if (!vendor.CanInteractWithVendorMessage(player)) {
				return;
			}

			var isMyVendor = vendor.CanVendorBeControlledBy(player);

			switch (button) {
				case buttonId_ChangePrice:
					if (isMyVendor) {
						var price = gr.GetNumberResponse(inputId_Price);
						if (!(price > 0)) {
							player.WriteLineLoc<Loc_PlayerVendor_Stock>(l => l.InvalidPrice);
							DialogStacking.ResendAndRestackDialog(gi);
							return;
						}

						entry.price = price;
					}
					return;

				case buttonId_BuyOrRecall:
					var units = 1;
					if (entry.soldByUnits) {
						units = (int) gr.GetNumberResponse(inputId_Units);
					}

					if (isMyVendor) {
						vendor.RecallEntry(player, entry, units);
					} else {
						vendor.TryBuyEntry(player, entry, units);
					}

					return;

				case buttonId_AddMoreUnits:
					if (isMyVendor) {

					}
					return;

				case buttonId_Examine:
					return;
			}



			//var description = gr.GetTextResponse(inputId_Description);

			//if (button == buttonId_NewSection) {

			//    //new section, we need no hardcore checks because nothing from the outside world is actually being manipulated
			//    int sectionModel = focus.Model;
			//    if (focus.IsChar) {
			//        sectionModel = ((Character) focus).TypeDef.Icon;
			//    }
			//    vendor.AddNewStockSection(stockSection, description, sectionModel, focus.Color);
			//} else {

			//    //we're actually stocking something, need all possible checks
			//    if (!vendor.CanStockWithMessage(player, focus)) {
			//        return;
			//    }



			//    if (button == buttonId_Sell) {
			//        vendor.StockThing(player, focus, stockSection, description, price);
			//    } else if (button == buttonId_SellByUnit) {
			//        vendor.StockThingSoldByUnit(player, (Container) focus, stockSection, description, price);
			//    }
			//}
			//} finally {
			//    DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			//}
		}
	}

	public class Loc_D_PlayerVendor_StockItemDetail : CompiledLocStringCollection<Loc_D_PlayerVendor_StockItemDetail> {
		public string Label_Price = "Cena";
		public string Label_Color = "Barva";
		public string Label_Change = "Zmìnit";
		public string Label_Units = "Jednotek?";
		public string Label_Add = "Pøidat";
		public string Label_Recall = "Stáhnout";
		public string Label_Buy = "Koupit";
		public string Label_Examine = "Projít";

		//public string InvalidPrice = "Cena musí být vìtší než 0";
	}
}
