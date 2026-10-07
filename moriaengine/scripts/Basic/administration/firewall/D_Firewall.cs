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
using System.ComponentModel;

namespace SteamEngine.CompiledScripts.Dialogs {

	public class D_Firewall : CompiledGumpDef {
		//internal static readonly TagKey ipSortingTK = TagKey.Acquire("_blocked_ips_sorting_");
		//internal static readonly TagKey ipsListTK = TagKey.Acquire("_blocked_ips_list_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int IPwidth = 120; //velikost okna pro IP adresu 
			int duvod = 350;
			int kdo = 110;
			int width = IPwidth * 2 + duvod + kdo + 2 * ButtonMetrics.D_BUTTON_WIDTH;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			args.SetDataComparerIfNeededLScript<FirewallEntry>("LowerBound.GetAddressBytes()");

			List<FirewallEntry> entries = args.GetDataList<FirewallEntry>();
			if (entries == null) {
				entries = Firewall.GetAllEntries();
				args.SetDataList(entries);
				args.SortDataList<FirewallEntry>();
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, entries.Count);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(40, 30);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Administrace blokovaných IP (" + (firstiVal + 1) + "-" + imax + " z " + entries.Count + ")").Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, IPwidth, IPwidth, duvod, kdo));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Del").Build();
			//cudlik s IP
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(1).Build(); //tridit dle blokovane IP vzestupne
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(2).Build(); //tridit dle blokovane IP sestupne     
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Rozsah IP od").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//cudlik s IP
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel("Rozsah IP do").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//duvod
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Dùvod").Build();
			//kdo blokoval
			dialogHandler.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit dle GM - Blokare vzestupne (Blocked by)
			dialogHandler.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit dle GM - Blokare sestupne(Blocked by)  
			dialogHandler.LastTable[0, 4] = GUTAText.Builder.TextLabel("Zablokoval").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();

			dialogHandler.MakeLastTableTransparent();

			//seznam blokovaných IP adres
			dialogHandler.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS));
			dialogHandler.CopyColsFromLastTable();

			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++, rowCntr++) {
				FirewallEntry entry = entries[i];
				dialogHandler.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(i + 10).Build();
				dialogHandler.LastTable[rowCntr, 1] = GUTAText.Builder.Text(entry.LowerBound.ToString()).Build();
				dialogHandler.LastTable[rowCntr, 2] = GUTAText.Builder.Text(entry.UpperBound.ToString()).Build();
				dialogHandler.LastTable[rowCntr, 3] = GUTAText.Builder.Text(entry.Reason).Build();
				dialogHandler.LastTable[rowCntr, 4] = GUTAText.Builder.Text(entry.BlockedBy.Name).Build();
			}
			dialogHandler.MakeLastTableTransparent();

			//ted paging
			dialogHandler.CreatePaging(entries.Count, firstiVal, 1);

			// nadpis pro blokaci novych
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, width / 2 - ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(5).Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Pøidat novì blokovanou IP").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(6).Build();
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Pøidat novì blokovaný rozsah IP").Build();

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			List<FirewallEntry> entries = args.GetDataList<FirewallEntry>();

			if (gr.PressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)				
				switch (gr.PressedButton) {
					case 0: //rmouseButton anebo zavreni tlacitkem v rohu
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
						return;
					case 1: //sort ipup
						args.SortDataListUsingLscriptExpression<FirewallEntry>("LowerBound.GetAddressBytes()", ListSortDirection.Ascending);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 2: // sort ipdown
						args.SortDataListUsingLscriptExpression<FirewallEntry>("LowerBound.GetAddressBytes()", ListSortDirection.Descending);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 3: // sort accountup
						args.SortDataListUsingLscriptExpression<FirewallEntry>("BlockedBy.Name", ListSortDirection.Ascending);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 4: // sort accountdown
						args.SortDataListUsingLscriptExpression<FirewallEntry>("BlockedBy.Name", ListSortDirection.Descending);
						DialogStacking.ResendAndRestackDialog(gi);
						return;
					case 5: // block single ip
						//stackneme aktualni dialog pro navrat
						args.RemoveDataList();
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Firewall_BlockIP>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);
						return;
					case 6: // block iprange
						//stackneme aktualni dialog pro navrat
						args.RemoveDataList();
						newGi = gi.Cont.Dialog(SingletonScript<D_Firewall_BlockIPRange>.Instance);//a zobrazime novy
						DialogStacking.EnstackDialog(gi, newGi);
						return;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, entries.Count, 1)) {//kliknuto na paging?
			} else {
				//porad jsme zde - klikalo se na tlacitko primo u bloknute IP
				//zjistime kterej cudlik z radku byl zmacknut
				int row = gr.PressedButton - 10;
				FirewallEntry entry = entries[row];
				if (entry.IsSingleIPEntry) {
					Firewall.RemoveBlockedIP(entry.LowerBound);
					//znovuzavolat dialog
					DialogStacking.ResendAndRestackDialog(gi);
				} else {
					Firewall.RemoveBlockedIPRange(entry.LowerBound, entry.UpperBound);
					//znovuzavolat dialog
					DialogStacking.ResendAndRestackDialog(gi);
				}
			}
		}

		[SteamFunction]
		public static void BlockedIPs(Character self) {
			self.Dialog(SingletonScript<D_Firewall>.Instance);
		}
	}

}