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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dialog that will display all colors examples - useful for determining which color we need.</summary>
	public class D_Colors : CompiledGumpDef {
		static readonly int lastColor = 2999;

		static readonly int columnsCnt = 10; //kolik sloupecku bude mit dialog?
		static readonly int dlgWidth = 850; //sirka dialogu

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int startingColor = Convert.ToInt32(args[0]); //cislo barvy od ktere (pocinaje) se zobrazi vsechny ostatni 
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance

			//maximalni index (20 radku mame) + hlidat konec seznamu...
			int imax = Math.Min(firstiVal + (ImprovedDialog.PAGE_ROWS * columnsCnt), lastColor);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(dlgWidth);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Colors dialog - ukázky barev v textu (poèínaje " + startingColor + ")").Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//input field pro vyber barvy
			dlg.AddTable(new GUTATable(1, 160, 40, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zadej poèáteèní barvu: ").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(10).Text(startingColor.ToString()).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//sloupecky - napred pripravime sirky
			int[] columns = new int[columnsCnt];
			for (int i = 0; i < columns.Length; i++) {
				//columns[i] = dlgWidth / columnsCnt;
				columns[i] = 80;
			}
			dlg.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS, columns));
			int colorCntr = firstiVal; //zacneme od te, ktera ma byt na strance prvni
			for (int i = 0; i < columnsCnt; i++) {//pro kazdy sloupecek
				for (int j = 0; j < ImprovedDialog.PAGE_ROWS && colorCntr <= lastColor; j++, colorCntr++) { //a v nem kazdy radek
					//vlozit priklad jedne pouzite barvy (dokud nedojdou barvy)
					//dlg.LastTable[j, i] = TextFactory.CreateText(colorCntr, "Color(" + String.Format("{0:X2}", colorCntr) + ")");
					dlg.LastTable[j, i] = GUTAText.Builder.Text("Color(" + colorCntr + ")").Hue(colorCntr).Build();
				}
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(lastColor, firstiVal, columnsCnt);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton < 10) { //ovladaci tlacitka (sorting, paging atd)
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog						
						break;
					case 1: //vybrat prvni barvu
						args[0] = (int) gr.GetNumberResponse(10); //vezmi zvolenou prvni barvu
						args.SetTag(ImprovedDialog.pagingIndexTK, (int) gr.GetNumberResponse(10)); //ta bude zaroven prvni na strance
						//args.ArgsArray[1] = (int)gr.GetNumberResponse(10);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, lastColor, columnsCnt)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[1] viz výše)
				//zde je sloupecku vice (columnsCnt, viz nahore)
			}
		}

		/// <summary>Prepare an array of colors to be displayed</summary>
		private int[] prepareColorList(int startingColor) {
			int[] retArr = new int[lastColor - startingColor + 1];
			for (int i = startingColor, j = 0; i <= lastColor; i++, j++) {
				retArr[j] = i;
			}
			return retArr;
		}

		/// <summary>
		/// Display a Colors dialog. Can be called without parameters (then the first displayed color will be the 0th)
		/// or with one parameter (number of color which will be taken as the first in the dialog)
		/// </summary>
		[SteamFunction]
		public static void ColorsDialog(AbstractCharacter sender, ScriptArgs text) {
			if (text == null || text.Args.Length == 0) {
				DialogArgs newArgs = new DialogArgs(0); //zaciname od 0. barvy
				newArgs.SetTag(ImprovedDialog.pagingIndexTK, 0); //prvni na strance bude ta 0.
				sender.Dialog(SingletonScript<D_Colors>.Instance, newArgs);
			} else {
				DialogArgs newArgs = new DialogArgs(Convert.ToInt32(text.Argv[0])); //zaciname od zvolene barvy
				newArgs.SetTag(ImprovedDialog.pagingIndexTK, Convert.ToInt32(text.Argv[0])); //prvni na strance bude ta zvolena
				sender.Dialog(SingletonScript<D_Colors>.Instance, newArgs);
			}
		}
	}
}