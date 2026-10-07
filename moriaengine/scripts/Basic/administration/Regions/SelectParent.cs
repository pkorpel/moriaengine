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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>
	/// Dialog listing all regions for selecting single one for parent dialog
	/// Seznam parametru: 0 - vyhledavaci retezec
	/// 1 - paging
	/// 2 - seznam regionu
	/// 3 - trideni
	/// </summary>
	public class D_SelectParent : CompiledGumpDef {
		private static int width = 450;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<StaticRegion> regionsList = (List<StaticRegion>) args.GetTag(D_Regions.regsListTK); //regionlist si posilame v argumentu (napriklad pri pagingu)
			if (regionsList == null) {
				//vzit seznam a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				regionsList = StaticRegion.FindByString(TagMath.SGetTag(args, D_Regions.regsSearchTK));
				args.SetTag(D_Regions.regsListTK, regionsList); //ulozime to do argumentu dialogu
			}

			object sorting = args.GetTag(D_Regions.regsSortingTK);
			if (sorting != null) {//mame cim tridit?
				this.SortBy(regionsList, (SortingCriteria) Convert.ToInt32(sorting));
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, regionsList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(200, 100);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Výbìr regionu (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + regionsList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu (Detail, Delete, Name, Defname, P)
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 150, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyber").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(3).Build(); //tridit podle jmena asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(4).Build(); //tridit podle jmena desc            
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jméno").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(5).Build(); //tridit dle defnamu asc
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(6).Build(); //tridit dle defnamu desc
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Defname").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.MakeLastTableTransparent();

			//seznam regionu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				StaticRegion reg = regionsList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Id(10 + i).Build(); //vyber tento
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(reg.Name).Build(); //nazev
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(reg.Defname).Build(); //defname

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(regionsList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam regionu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<StaticRegion> regionsList = (List<StaticRegion>) args.GetTag(D_Regions.regsListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 2: //vyhledavat / zuzit vyber
						string nameCriteria = gr.GetTextResponse(33);
						args.SetTag(D_Regions.regsSearchTK, nameCriteria);//uloz info o vyhledavacim kriteriu
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem												
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name asc						
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameAsc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //name desc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameDesc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //defname asc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.DefnameAsc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //defname desc
						args.RemoveTag(D_Regions.regsListTK);//vycistit soucasny odkaz na regionlist aby se mohl prenacist a pretridit
						args.SetTag(D_Regions.regsSortingTK, SortingCriteria.DefnameDesc);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, regionsList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
			} else {
				//zjistime si radek
				int row = gr.PressedButton - 10;
				StaticRegion region = regionsList[row];
				//vezmem z vybraneho regionu jeho defname a predame do predchoziho dialogu
				Gump previousGi = DialogStacking.PopStackedDialog(gi);
				previousGi.InputArgs.SetTag(D_New_Region.parentDefTK, region.Defname); //to je zalozeni noveho regionu (posleme si defname parenta)
				DialogStacking.ResendAndRestackDialog(previousGi);
			}
		}

		private void SortBy(List<StaticRegion> regList, SortingCriteria criterium) {
			switch (criterium) {
				case SortingCriteria.NameAsc:
					regList.Sort(RegionComparerByName.instance);
					break;
				case SortingCriteria.NameDesc:
					regList.Sort(RegionComparerByName.instance);
					regList.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					regList.Sort(RegionComparerByDefname.instance);
					break;
				case SortingCriteria.DefnameDesc:
					regList.Sort(RegionComparerByDefname.instance);
					regList.Reverse();
					break;
				default:
					break; //netridit
			}
		}
	}
}