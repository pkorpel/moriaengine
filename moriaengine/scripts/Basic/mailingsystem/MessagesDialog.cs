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
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Dialog that will display the list of clients delayed messages</summary>
	public class D_DelayedMessages : CompiledGumpDef {
		internal static readonly TagKey msgsSortingTK = TagKey.Acquire("_messages_sorting_");
		internal static readonly TagKey msgsListTK = TagKey.Acquire("_messages_list_");

		/// <summary>Display the list of the messages</summary>
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<DelayedMsg> messagesList = (List<DelayedMsg>) args.GetTag(msgsListTK); //seznam msgi si posilame v argumentu (napriklad pri pagingu)
			if (messagesList == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				messagesList = MsgsBoard.GetClientsMessages((Character) sendTo);
				//setrid zpravy (neni li specifikaovano trideni, pouzije se prirozene trideni dle casu)
				messagesList = MsgsBoard.GetSortedBy(messagesList, (SortingCriteria) TagMath.IGetTag(args, msgsSortingTK));
				args.SetTag(msgsListTK, messagesList); //ulozime mezi parametry dialogu
			}

			int unreadCnt = MsgsBoard.CountUnread((Character) sendTo);
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, messagesList.Count);

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(40, 30);

			//nadpis
			dialogHandler.AddTable(new GUTATable(1, 300, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam zpráv (" + (firstiVal + 1) + "-" + imax + " z " + messagesList.Count + ")").Build();

			//cudliky na trideni dle neprectenych (i s popiskem)
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(5).Build();//tridit dle neprectenych (neprectene nahoru)
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(6).Build();//tridit dle neprectenych (neprectene dolu)
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Tøídit dle nepøeètených (nepøeètených " + unreadCnt + ")").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//cudlik na zavreni dialogu			
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dialogHandler.MakeLastTableTransparent();

			//popis sloupecku
			dialogHandler.AddTable(new GUTATable(1, 180, 160, ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 0)); //radek na nadpisy            
			//cas
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(1).Build(); //tridit podle casu asc
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(2).Build(); //tridit podle casu desc            
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Èas odeslání").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//cudlik s odesilatelem
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit dle jmena sendera asc
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit dle jmena sendera desc
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Odesilatel").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			//cudlik pro cteni
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel("Èíst").Build();
			//cudlik pro mazani   
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Del").Build();
			//text
			dialogHandler.LastTable[0, 4] = GUTAText.Builder.TextLabel("Text zprávy").Build();

			dialogHandler.MakeLastTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam zprav
			dialogHandler.AddTable(new GUTATable(imax - firstiVal));
			dialogHandler.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				DelayedMsg msg = messagesList[i];
				Hues msgColor = msg.color;
				dialogHandler.LastTable[rowCntr, 0] = GUTAText.Builder.Text(msg.time.ToString()).Hue(msgColor).Build();//cas odeslani
				dialogHandler.LastTable[rowCntr, 1] = GUTAText.Builder.Text(msg.sender == null ? MsgsBoard.NO_SENDER : msg.sender.Name).Hue(msgColor).Build(); //odesilatel
				dialogHandler.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id((2 * i) + 10).Build(); //èíst
				dialogHandler.LastTable[rowCntr, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id((2 * i) + 11).Build(); //smazat
				dialogHandler.LastTable[rowCntr, 4] = GUTAText.Builder.Text(msg.text).Hue(msgColor).Build(); //text zpravy				

				rowCntr++;
			}
			dialogHandler.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(messagesList.Count, firstiVal, 1);

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam zprav z kontextu (mohl jiz byt trideny apd.)
			ArrayList messagesList = (ArrayList) args.GetTag(msgsListTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //tridit dle casu asc						
						args.SetTag(msgsSortingTK, SortingCriteria.TimeAsc);
						args.RemoveTag(msgsListTK);//odstranit seznm, bude prenacten a jinak setriden
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //tridit dle casu desc
						args.SetTag(msgsSortingTK, SortingCriteria.TimeDesc);
						args.RemoveTag(msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //tridit dle sendera asc
						args.SetTag(msgsSortingTK, SortingCriteria.NameAsc);
						args.RemoveTag(msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //tridit dle sendera desc
						args.SetTag(msgsSortingTK, SortingCriteria.NameDesc);
						args.RemoveTag(msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //tridit dle neprectenych asc
						args.SetTag(msgsSortingTK, SortingCriteria.UnreadAsc);
						args.RemoveTag(msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //tridit dle neprectenych desc
						args.SetTag(msgsSortingTK, SortingCriteria.UnreadDesc);
						args.RemoveTag(msgsListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, messagesList.Count, 1)) {//kliknuto na paging?
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (gr.PressedButton - 10) / 2;
				int buttNum = (gr.PressedButton - 10) % 2;
				DelayedMsg msg = (DelayedMsg) messagesList[row];
				switch (buttNum) {
					case 0: //èíst
						if (!msg.read) {
							//store the info about the message read status and change its color a bit
							msg.read = true;
							msg.color = msg.color + 3;//trosku ztmavit barvu
						}
						//zobrazit tex zprávy (první parametr je nadpis, druhý je zobrazný text)
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("Text zprávy", msg.text));
						//stacknout messageslist pro navrat
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat
						MsgsBoard.DeleteMessage((Character) gi.Cont, msg);
						//znovuzavolat dialog
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		[SteamFunction]
		public static void Messages(AbstractCharacter sender, ScriptArgs args) {
			//ten poslendi null - misto pro seznam messagi
			DialogArgs newArgs = new DialogArgs();
			if (args.Args.Length == 0) {
				newArgs.SetTag(msgsSortingTK, SortingCriteria.TimeAsc);
				sender.Dialog(SingletonScript<D_DelayedMessages>.Instance, newArgs);//default sorting, beginning from the first message
			} else {
				newArgs.SetTag(msgsSortingTK, (SortingCriteria) args.Args[0]);
				sender.Dialog(SingletonScript<D_DelayedMessages>.Instance, newArgs); //we expect a sorting criterion !, listing from the first message
			}
		}
	}
}