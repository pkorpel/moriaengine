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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	public class D_PlayerVendor_ListStock : CompiledGumpDef {
		public const int width = 640;

		const int buttonId_NewStock = 3;
		const int buttonId_DeleteSection = 4;
		const int buttonId_ReverseOrder = 5;
		const int buttonId_ChangeIcon = 6;

		const int buttonId_Rows_Offset = 100;
		const int Rows_ButtonCount = 3;
		const int buttonId_Rows_Detail = 0;
		const int buttonId_Rows_MoveUp = 1;
		const int buttonId_Rows_MoveTop = 2;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {

			var section = (Container) focus;
			var vendor = (PlayerVendor) focus.TopObj();
			var player = (Player) sendTo;
			var loc = Loc<Loc_PlayerVendor_ListStock>.Get(player.Language);

			var dialogHandler = new ImprovedDialog(this.GumpInstance);

			int firstIndex = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int iMax = Math.Min(firstIndex + ImprovedDialog.PAGE_ROWS, section.Count);
			int rows = iMax - firstIndex;

			//create the background GUTAMatrix and set its size       
			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(10, 10);

			//header
			string header = section.Name;
			var parentSection = section.Cont;
			while (parentSection != vendor) {
				header = string.Concat(parentSection.Name, " - ", header);
				parentSection = parentSection.Cont;
			}
			var t = dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			t.AddToCell(0, 0, GUTAText.Builder.TextHeadline(header).Build());
			t.AddToCell(0, 1, GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build());
			t.Transparent = true;

			t = dialogHandler.AddTable(new GUTATable(1, 20 + ImprovedDialog.D_COL_SPACE + 40, 450, 0));
			t.AddToCell(0, 0, GUTAText.Builder.TextHeadline(loc.Buy).Build());
			t.AddToCell(0, 1, GUTAText.Builder.TextHeadline(loc.Description).Build());
			t.AddToCell(0, 2, GUTAText.Builder.TextHeadline(loc.PriceInGp).Build());
			t.Transparent = true;


			//rows with item descriptions
			t = dialogHandler.AddTable(new GUTATable(rows, 20, 40, 450, 0));

			bool isMyVendor = vendor.CanVendorBeControlledBy(player);

			int index = -1;
			int row = 0;
			foreach (var item in section) {
				index++;

				if (index < firstIndex) {
					continue;
				}
				if (index >= iMax) {
					break;
				}

				if (isMyVendor) {
					t.AddToCell(row, 3, GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(index * Rows_ButtonCount + buttonId_Rows_Offset + buttonId_Rows_MoveUp)
						.Valign(DialogAlignment.Valign_Top).XPos(-35).Build()); //
					t.AddToCell(row, 3, GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(index * Rows_ButtonCount + buttonId_Rows_Offset + buttonId_Rows_MoveTop)
						.Valign(DialogAlignment.Valign_Top).XPos(-20).Build()); //
					t.AddToCell(row, 3, GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(index * Rows_ButtonCount + buttonId_Rows_Offset + buttonId_Rows_MoveTop)
						.Valign(DialogAlignment.Valign_Top).YPos(8).XPos(-20).Build()); //
				}

				t.AddToCell(row, 0, GUTAButton.Builder.Type(LeafComponentTypes.ButtonTriangle).Id(index * Rows_ButtonCount + buttonId_Rows_Offset + buttonId_Rows_Detail).Build());

				t.AddToCell(row, 1, GUTAImage.Builder.Gump(item.Model).Color(item.Color).Build());
				t.AddToCell(row, 2, GUTAText.Builder.Text(item.Name).Build());

				var asEntry = item as PlayerVendorStockEntry;
				if (asEntry != null) {
					t.AddToCell(row, 3, GUTAText.Builder.Text(asEntry.price.ToString(CultureInfo.InvariantCulture)).Build());
				}

				row++;
			}

			t.Transparent = true;

			dialogHandler.CreatePaging(section.Count, firstIndex, 1);

			if (isMyVendor) {

				t = dialogHandler.AddTable(new GUTATable(1, 170, 170, 170, 0));

				t.AddToCell(0, 0, GUTAButton.Builder.Type(LeafComponentTypes.ButtonTriangle).Id(buttonId_NewStock).Build());
				t.AddToCell(0, 0, GUTAText.Builder.TextHeadline(loc.NewStock).XPos(30).Build());

				//empty section can be deleted
				if (section.Count == 0) {
					t.AddToCell(0, 1, GUTAButton.Builder.Type(LeafComponentTypes.ButtonTriangle).Id(buttonId_DeleteSection).Build());
					t.AddToCell(0, 1, GUTAText.Builder.TextHeadline(loc.DeleteSection).XPos(30).Build());
				}

				t.AddToCell(0, 2, GUTAButton.Builder.Type(LeafComponentTypes.ButtonTriangle).Id(buttonId_ReverseOrder).Build());
				t.AddToCell(0, 2, GUTAText.Builder.TextHeadline(loc.ReverseOrder).XPos(30).Build());

				//if we're not the uppermost section, we can change icon
				if (section.Cont is Container) {
					t.AddToCell(0, 3, GUTAButton.Builder.Type(LeafComponentTypes.ButtonTriangle).Id(buttonId_ChangeIcon).Build());
					t.AddToCell(0, 3, GUTAText.Builder.TextHeadline(loc.ChangeIcon).XPos(30).Build());
				}

				t.Transparent = true;

			}


			dialogHandler.WriteOut();

			#region orig. spherescript
			//SetLocation=10,10
			//argo.tag(firsti,<eval argv(0)>)//<eval argv(1)>
			//if (argo.tag(firsti)<0)
			// argo.tag(firsti,0)
			//endif
			//argo.cv_radku(<rescount>)
			//argo.tag(sirka,<cv_dialog_sirka>)
			//argo.tag(vyska,<eval (3*<d_def_radek_vyska>)+(argo.tag(radku)*cv_dialog_radek_vyska)+(4*<d_def_skvira>)+(2*<d_def_okraj>)>)

			//argo.dialog_prvni
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)

			//var(d_def_radek_vyska,<cv_dialog_radek_vyska>)
			//var(sloupec_noback[0],1)
			//argo.dialog_pozadi(<argo.tag(nexty)>,<argo.tag(radku)>,20,40,450)
			//var(sloupec_noback[0],"")
			//var(d_def_radek_vyska,"")
			//argo.dialog_pozadi(<argo.tag(nexty)>,1)
			//var(d_def_radek_vyska,<cv_dialog_radek_vyska>)
			//argo.dialog_zpruhledni

			//if (cont) //if not, its some error
			// arg(hobj,<uid>)
			// while (<eval arg(hobj).cont>!=<eval arg(hobj).topobj>)
			//  arg(urovne_uid,"<safe urovne_uid><arg(hobj)>,")
			//  arg(hobj,<arg(hobj).cont>)
			// endwhile
			// arg(urovne_uid,"<safe urovne_uid><arg(hobj)>")
			// argo.texta(<argo.dialog_textpos(0,0)>,2301,<cm_UIDs_list(<arg(urovne_uid)>)>)
			//endif


			//argo.texta(<argo.dialog_textpos(1,0,1)>,2301,kup)
			//argo.texta(<argo.dialog_textpos(1,2)>,2301,Nazev zbozi)
			//argo.texta(<argo.dialog_textpos(1,3)>,2301,Cena/gp)

			//button((<argo.tag(sirka)>-<d_def_okraj>)-33,<argo.tag(obj_y[0])>,0fb1,0fb3,1,0,9)//cancel

			//if (argo.tag(firsti))
			// argo.button(<argo.tag(sirka)>-(<d_def_okraj>+<d_def_skvira>+16),<argo.tag(obj_y[2])>+1,0fa,0fb,1,0,1) // prev
			//endif
			//arg(imax,<argo.tag(firsti)>+<cv_dialog_maxradku>)
			//if (imax>rescount)
			// arg(imax,<rescount>)
			//else
			// argo.button(<argo.tag(sirka)>-(<d_def_okraj>+<d_def_skvira>+16),<argo.tag(obj_y[3])>-24,0fc,0fd,1,0,2) // next
			//endif

			//arg(ypos,<argo.tag(obj_y[2])>)
			//arg(index,<argo.tag(firsti)>)
			//while (index<imax)
			// if (topobj.ismypet)
			//  argo.button(<argo.tag(sloupec_x[3])>-35,ypos,0984,0983,1,0,(index*3)+101) 
			//  argo.button(<argo.tag(sloupec_x[3])>-20,ypos,0984,0984,1,0,(index*3)+102)
			//  argo.button(<argo.tag(sloupec_x[3])>-20,ypos+8,0983,0983,1,0,(index*3)+102)
			// endif
			// arg(item,<findcont(index)>)
			// argo.button(<argo.tag(sloupec_x[0])>,ypos,9905,9904,1,0,(index*3)+103)
			// argo.tilepic(<argo.tag(sloupec_x[1])>,ypos,<arg(item).dispiddec)>)
			// argo.textA(<argo.tag(sloupec_x[2])>+<d_def_odsazeni>,ypos,2301,"<arg(item).tag(description)>")
			// argo.textA(<argo.tag(sloupec_x[3])>+<d_def_odsazeni>,ypos,2301,"<arg(item).tag(price)>")
			// arg(ypos,#+<d_def_radek_vyska>)
			// arg(index,#+1)
			//endwhile

			//if (topobj.ismypet)//ovladaci tlacitka
			// argo.dialog_textpos(3,0,1)
			// argo.button(lastxpos,lastypos-1,9905,9904,1,0,3)
			// argo.textA(lastxpos+30,lastypos,2301,Pridat dalsi zbozi)
			// if !(rescount)
			//  argo.button(lastxpos+200,lastypos-1,9905,9904,1,0,4)
			//  argo.textA(lastxpos+230,lastypos,2301,Odebrat sekci)
			// endif
			// argo.button(lastxpos+400,lastypos-1,9905,9904,1,0,5)
			// argo.textA(lastxpos+430,lastypos,2301,Prevratit nabidku)
			// if (topobj!=cont)//vlozena sekce
			//   argo.button(lastxpos+560,lastypos-1,9905,9904,1,0,6)
			//   argo.textA(lastxpos+580,lastypos,2301,Ikona)
			// endif
			//else
			// argo.texta(<argo.dialog_textpos(3,0)>,2301,Vase konto: <eval src.findlayer(21).rescount(t_gold)+src.bankbalance> gp)
			//endif
			//var(d_def_radek_vyska,"") 
			#endregion
		}


		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			var section = (Container) gi.Focus;
			var vendor = (PlayerVendor) section.TopObj();
			var player = (Player) gi.Cont;

			int buttonId = gr.PressedButton;

			switch (buttonId) {
				case 0:
					DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
					return;

				case buttonId_NewStock:
					if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player)) {
						player.Target(StockTargetDef, section);
						//show previous dialog after, somehow?
					}
					return;
				case buttonId_DeleteSection:
					if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player) && section.Count == 0) {
						section.Delete();
						DialogStacking.ShowPreviousDialog(gi); //previous should be the upper section
					}
					return;
				case buttonId_ReverseOrder:
					if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player)) {
						ReverseOrderInContainer(section, vendor.Backpack);
						DialogStacking.ResendAndRestackDialog(gi);
					}
					return;
				case buttonId_ChangeIcon:
					if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player) && (section.Cont is Container)) {
						var prevGi = DialogStacking.PopStackedDialog(gi);
						player.Target(SingletonScript<Targ_PlayerVendor_CopyIcon>.Instance,
							Tuple.Create(section, prevGi));
					}
					return;
				default:
					if (ImprovedDialog.PagingButtonsHandled(gi, gr, section.Count, 1)) {
						return;
					}

					var index = (buttonId - buttonId_Rows_Offset) / Rows_ButtonCount;
					var modulo = (buttonId - buttonId_Rows_Offset) % Rows_ButtonCount;

					var item = section.FindCont(index);

					switch (modulo) {
						case buttonId_Rows_Detail:
							var asEntry = item as PlayerVendorStockEntry;
							if (asEntry != null) {
								var newGi = asEntry.Dialog(player, SingletonScript<D_PlayerVendor_StockItemDetail>.Instance);
								DialogStacking.EnstackDialog(gi, newGi);
							} else { //section
								var newGi = item.Dialog(player, this);
								DialogStacking.EnstackDialog(gi, newGi);
							}
							return;

						case buttonId_Rows_MoveUp:
							if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player)) {
								MoveUpInContainer(section, index, item, vendor.Backpack);
								DialogStacking.ResendAndRestackDialog(gi);
							}
							return;

						case buttonId_Rows_MoveTop:
							if (vendor.CanVendorBeControlledByWithMessage(player) && vendor.CanInteractWithVendorMessage(player)) {
								MoveToTopInContainer(section, item, vendor.Backpack);
								DialogStacking.ResendAndRestackDialog(gi);
							}
							return;
					}
					return;
			}
		}


		public static void MoveUpInContainer(Container section, int index, AbstractItem entry, Container tempCont) {
			var list = new List<AbstractItem>(index);
			for (int i = 0; i < index - 1; i++) {
				var e = section.FindCont(0);
				list.Add(e);
				e.Cont = tempCont;
			}
			list.Add(entry);
			entry.Cont = tempCont;
			for (int i = list.Count - 1; i >= 0; i--) {
				list[i].Cont = section;
			}
		}

		public static void ReverseOrderInContainer(Container section, Container tempCont) {
			var n = section.Count;
			var list = new List<AbstractItem>(n);
			foreach (var e in section) {
				list.Add(e);
				e.Cont = tempCont;
			}
			for (int i = 0; i < n; i++) {
				list[i].Cont = section;
			}
		}

		public static void MoveToTopInContainer(Container section, AbstractItem entry, Container tempCont) {
			entry.Cont = tempCont;
			entry.Cont = section;
		}

		private static AbstractTargetDef targ_playerVendor_stock;
		public static AbstractTargetDef StockTargetDef {
			get {
				if (targ_playerVendor_stock == null) {
					targ_playerVendor_stock = AbstractTargetDef.GetByDefname("targ_playerVendor_stock");
				}
				return targ_playerVendor_stock;
			}
		}
	}

	public class Targ_PlayerVendor_CopyIcon : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.WriteLineLoc<Loc_PlayerVendor_ListStock>(l => l.TargetIconSource);

			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			var t = (Tuple<Container, Gump>) parameter;

			Container section = t.Item1;
			if (section != null) {
				PlayerVendor vendor = section.TopObj() as PlayerVendor;

				if (vendor != null && vendor.CanVendorBeControlledBy(self)) {
					var asChar = targetted as Character;
					if (asChar == null) {
						section.Model = targetted.Model;
					} else {
						section.Model = asChar.TypeDef.Icon;
					}

					DialogStacking.ResendAndRestackDialog(t.Item2);
				}
			}

			return TargetResult.Done;
		}
	}

	public class Loc_PlayerVendor_ListStock : CompiledLocStringCollection<Loc_PlayerVendor_ListStock> {
		public string Buy = "kup";
		public string Description = "Název zboží";
		public string PriceInGp = "Cena (gp)";

		public string NewStock = "Pøidat dalši zboží";
		public string DeleteSection = "Odebrat sekci";
		public string ReverseOrder = "Pøevrátit nabídku";
		public string ChangeIcon = "Ikona";
		public string YourAccount = "Vaše konto: {0} gp";

		public string TargetIconSource = "Target something to copy icon from.";
	}
}
