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
	/// Dialog for dispalying the regions rectangles
	/// Seznam parametru: 0 - region
	/// 1 - paging
	/// 2 - rectangly v listu(je totiž možno pøidávat dynamicky)
	/// </summary>
	public class D_Region_Rectangles : CompiledGumpDef {
		internal static readonly TagKey regionTK = TagKey.Acquire("_region_with_rects_");
		internal static readonly TagKey rectsListTK = TagKey.Acquire("_rects_list_");


		private static int width = 450;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Region reg = (Region) args.GetTag(regionTK);
			List<MutableRectangle> rectList = (List<MutableRectangle>) args.GetTag(rectsListTK);
			if (rectList == null) {
				//vezmeme je z regionu
				rectList = MutableRectangle.TakeRectsFromRegion(reg);
				args.SetTag(rectsListTK, rectList); //ulozime to do argumentu dialogu
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rectList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(25, 25);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Pøehled rectanglù pro region " + reg.Name + "(" + reg.Defname + ") (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rectList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho rectanglu
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Pøidat rectangle").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu (Info, Height, Width, StartPoint, EndPoint, Tiles)
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 35, 35, 130, 0, 35));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Edit").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Šíøka").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Výška").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Poèáteèní bod").Build();
			dlg.LastTable[0, 5] = GUTAText.Builder.TextLabel("Koncový bod").Build();
			dlg.LastTable[0, 6] = GUTAText.Builder.TextLabel("Polí").Build();
			dlg.MakeLastTableTransparent();

			//seznam rectnaglu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				MutableRectangle rect = rectList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + (2 * i)).Build(); //editovat
				dlg.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(11 + (2 * i)).Build(); //smazat
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(rect.Width.ToString()).Build();
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(rect.Height.ToString()).Build();
				dlg.LastTable[rowCntr, 4] = GUTAText.Builder.Text("(" + rect.MinX + "," + rect.MinY + ")").Build();
				dlg.LastTable[rowCntr, 5] = GUTAText.Builder.Text("(" + rect.MaxX + "," + rect.MaxY + ")").Build();
				dlg.LastTable[rowCntr, 6] = GUTAText.Builder.Text(rect.TilesNumber.ToString()).Build();

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//send button
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(2).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("Uložit").Build();
			dlg.MakeLastTableTransparent();

			dlg.CreatePaging(rectList.Count, firstiVal, 1);//ted paging		
			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam rectanglu bereme z parametru (mohl byt nejaky pridan/smazan)
			StaticRegion reg = (StaticRegion) args.GetTag(regionTK);
			List<MutableRectangle> rectsList = (List<MutableRectangle>) args.GetTag(rectsListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy rectangle
						//nemazat rectangly - budeme do nich ukladat novy						
						DialogArgs newArgs = new DialogArgs(0, 0, 0, 0);//zakladni souradnice rectanglu
						newArgs.SetTag(rectsListTK, rectsList); //seznam budeme potrebovat
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_New_Rectangle>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //ulozit
						//nastavenim rectanglu dojde k reinicializaci vsech regionu
						if (reg.SetRectangles(rectsList)) {
							if (!reg.ContainsPoint(reg.P)) {//jeste zkoukneme pozici - mohla zmizet smazáním/resizem rectanglu
								D_Display_Text.ShowError("Home pozice je mimo region - je potøeba ji pøenastavit");
							} else {
								D_Display_Text.ShowInfo("Ukládání rectanglù bylo úspìšné");
							}
							//zobrazime info a zmizime (z infa bude navrat k predchozimu dlg neb tento nestackneme)
							break;
						} //nekde to neproslo
						Gump infoGi = D_Display_Text.ShowError("Ukládání rectanglù skonèilo s chybami - viz konzole!");
						DialogStacking.EnstackDialog(gi, infoGi); //vlozime dialog do stacku pro navrat						
						break;
					//pokud to nekde spadne, tak to uvidime v konzoli - to uz je zavazny problem a nesmi to jen tak projit! (=nechytam vyjimku)
					//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
					//break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, rectsList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime si radek a cudlik v nem
				int row = (gr.PressedButton - 10) / 2;
				int buttNo = (gr.PressedButton - 10) % 2;
				MutableRectangle rect = rectsList[row];
				Gump newGi;
				switch (buttNo) {
					case 0: //region rectangle info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(rect));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //smazat rectangle
						rectsList.Remove(rect); //remove z listu, list nenulujeme zejo .)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}
	}
}