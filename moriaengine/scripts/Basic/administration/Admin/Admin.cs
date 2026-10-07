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
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dialog that will display the admin dialog</summary>
	public class D_Admin : CompiledGumpDef {
		internal static readonly TagKey playersListTK = TagKey.Acquire("_players_list_");
		internal static readonly TagKey plrListSortTK = TagKey.Acquire("_players_list_sorting_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//seznam lidi z parametru (if any)
			ArrayList playersList = (ArrayList) args.GetTag(playersListTK);
			if (playersList == null) {
				playersList = ScriptUtil.ArrayListFromEnumerable(GameServer.GetAllPlayers());
				args.SetTag(playersListTK, playersList);//ulozime do parametru dialogu
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, playersList.Count);

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dialogHandler.CreateBackground(800);
			dialogHandler.SetLocation(50, 50);

			//nadpis
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Admin dialog - seznam pøipojených klientù (" + (firstiVal + 1) + "-" + imax + " z " + playersList.Count + ")").Build();
			//cudlik na zavreni dialogu
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dialogHandler.MakeLastTableTransparent();

			//popis sloupecku
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 180, 180, 180, 0));
			//cudlik pro privolani hrace
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Come").Build();

			//Accounts
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(1).Build(); //tridit podle accountu asc
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit podle accountu desc			
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Account").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();

			//Jméno
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle hráèù asc
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit podle hráèù desc			
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel("Jméno").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();

			//Lokace
			dialogHandler.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit dle lokaci asc
			dialogHandler.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(6).Build(); //tridit podle lokaci desc			
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel("Lokace").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();

			//Akce
			dialogHandler.LastTable[0, 4] = GUTAText.Builder.TextLabel("Action").Build();
			dialogHandler.MakeLastTableTransparent(); //zpruhledni nadpisovy radek

			//vlastni seznam lidi
			dialogHandler.AddTable(new GUTATable(imax - firstiVal));
			dialogHandler.CopyColsFromLastTable();

			//sorting = 0th position
			switch ((SortingCriteria) Convert.ToInt32(args[0])) {
				case SortingCriteria.NameAsc:
					playersList.Sort(CharComparerByName<Player>.instance);
					break;
				case SortingCriteria.NameDesc:
					playersList.Sort(CharComparerByName<Player>.instance);
					playersList.Reverse();
					break;
				case SortingCriteria.LocationAsc:
					playersList.Sort(CharComparerByLocation.instance);
					break;
				case SortingCriteria.LocationDesc:
					playersList.Sort(CharComparerByLocation.instance);
					playersList.Reverse();
					break;
				case SortingCriteria.AccountAsc:
					playersList.Sort(CharComparerByAccount.instance);
					break;
				case SortingCriteria.AccountDesc:
					playersList.Sort(CharComparerByAccount.instance);
					playersList.Reverse();
					break;
				default:
					break; //netridit
			}

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				Player plr = (Player) playersList[i];
				Hues plrColor = Hues.PlayerColor;
				//TODO - barveni dle prislusnosti
				dialogHandler.LastTable[rowCntr, 0] = GUTAButton.Builder.Id((4 * i) + 10).Build(); //player come
				dialogHandler.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id((4 * i) + 11).Build(); //account detail
				dialogHandler.LastTable[rowCntr, 1] = GUTAText.Builder.Text(plr.Account.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Hue(plrColor).Build(); //acc name
				dialogHandler.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id((4 * i) + 12).Build(); //player info
				dialogHandler.LastTable[rowCntr, 2] = GUTAText.Builder.Text(plr.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Hue(plrColor).Build(); //plr name
				dialogHandler.LastTable[rowCntr, 3] = GUTAButton.Builder.Id((4 * i) + 13).Build(); //goto location
				dialogHandler.LastTable[rowCntr, 3] = GUTAText.Builder.Text(plr.Region.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Hue(plrColor).Build(); //region name
				dialogHandler.LastTable[rowCntr, 4] = GUTAText.Builder.Text(plr.CurrentSkillName.ToString()).Hue(plrColor).Build(); //action name

				rowCntr++;
			}
			dialogHandler.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dialogHandler.CreatePaging(playersList.Count, firstiVal, 1);

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam hracu bereme z kontextu (mohl byt jiz trideny atd)
			ArrayList playersList = (ArrayList) args.GetTag(playersListTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
						break;
					case 1: //acc tøídit asc
						args[0] = SortingCriteria.AccountAsc;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //hráèi tøídit asc
						args[0] = SortingCriteria.NameAsc;
						//args.SetTag(D_Admin.plrListSortTK, SortingCriteria.NameAsc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //lokace tøídit asc
						args[0] = SortingCriteria.LocationAsc;
						//args.SetTag(D_Admin.plrListSortTK, SortingCriteria.LocationAsc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //acc tøídit desc
						args[0] = SortingCriteria.AccountDesc;
						//args.SetTag(D_Admin.plrListSortTK, SortingCriteria.AccountDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //hráèi tøídit desc
						args[0] = SortingCriteria.NameDesc;
						//args.SetTag(D_Admin.plrListSortTK, SortingCriteria.NameDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //lokace tøídit desc
						args[0] = SortingCriteria.LocationDesc;
						//args.SetTag(D_Admin.plrListSortTK, SortingCriteria.LocationDesc);//uprav info o sortovani
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, playersList.Count, 1)) {//posledni 1 - pocet sloupecku v dialogu				
			} else { //skutecna adminovaci tlacitka z radku
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (gr.PressedButton - 10) / 4;
				int buttNum = (gr.PressedButton - 10) % 4;
				Player plr = (Player) playersList[row];
				Gump newGi;
				switch (buttNum) {
					case 0: //player come
						plr.Go(gi.Cont);
						break;
					case 1: //acc info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(plr.Account));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //player info						
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(plr));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //goto location
						((Character) gi.Cont).Go(plr);
						break;
				}
			}
		}
	}

	/// <summary>Comparer for sorting players (chars) by name asc</summary>
	public class CharComparerByName<T> : IComparer<T>, IComparer where T : AbstractCharacter {
		public static readonly CharComparerByName<T> instance = new CharComparerByName<T>();

		public int Compare(object a, object b) {
			return this.Compare((T) a, (T) b);
		}

		public int Compare(T x, T y) {
			return string.Compare(x.Name, y.Name);
		}
	}

	/// <summary>Comparer for sorting players (chars) by location name asc</summary>
	public class CharComparerByLocation : IComparer<Character>, IComparer {
		public static readonly CharComparerByLocation instance = new CharComparerByLocation();

		public int Compare(object a, object b) {
			return this.Compare((Character) a, (Character) b);
		}

		public int Compare(Character x, Character y) {
			return string.Compare(x.Region == null ? "" : x.Region.Name,
				y.Region == null ? "" : y.Region.Name);
		}
	}

	/// <summary>Comparer for sorting players (chars) by account name asc</summary>
	public class CharComparerByAccount : IComparer<Character>, IComparer {
		public static readonly CharComparerByAccount instance = new CharComparerByAccount();

		public int Compare(object a, object b) {
			return this.Compare((Character) a, (Character) b);
		}

		public int Compare(Character x, Character y) {
			return string.Compare(x.Account == null ? "" : x.Account.Name,
				y.Account == null ? "" : y.Account.Name);
		}
	}
}