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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Dialog for creating a new region rectangle</summary>
	public class D_New_Rectangle : CompiledGumpDef {
		private static int width = 450;

		/// <summary>
		/// Seznam parametru: 0 - list s rectanglama kam to pak pridame 
		/// 1-4 - pripadne predvyplnene souradnice (zobrazuji se jen pri chybach)
		/// </summary>
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string minX, minY, maxX, maxY; //predzadane hodnoty (if any)
			object[] argsArray = args.GetArgsArray();
			minX = string.Concat(argsArray[0]);
			minY = string.Concat(argsArray[1]);
			maxX = string.Concat(argsArray[2]);
			maxY = string.Concat(argsArray[3]);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(100, 100);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Vložení nového rectanglu").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//navod
			dlg.AddTable(new GUTATable(2, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("(MinX,MinY) - levy horni roh, (MaxX,MaxY) - pravy dolni roh").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextHeadline("Mapa v UO zacina vlevo nahore bodem (0,0) a konci vpravo dole!").Build();
			dlg.MakeLastTableTransparent();

			//textiky a editfieldy na zadani souradnic
			dlg.AddTable(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("MinX").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(31).Text(minX).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("MinY").Build();
			dlg.LastTable[0, 3] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(32).Text(minY).Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("MaxX").Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(33).Text(maxX).Build();
			dlg.LastTable[1, 2] = GUTAText.Builder.TextLabel("MaxY").Build();
			dlg.LastTable[1, 3] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(34).Text(maxY).Build();
			dlg.MakeLastTableTransparent();

			//send button
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("Uložit").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam rectanglu bereme z parametru (jsou to ty mutable)
			List<MutableRectangle> rectsList = (List<MutableRectangle>) args.GetTag(D_Region_Rectangles.rectsListTK);
			if (gr.PressedButton == 0) { //exit
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if (gr.PressedButton == 1) { //ulozit
				//precteme parametry a zkusime vytvorit rectangle
				MutableRectangle newRect = null;
				try {
					int startX = (int) gr.GetNumberResponse(31);
					int startY = (int) gr.GetNumberResponse(32);
					int endX = (int) gr.GetNumberResponse(33);
					int endY = (int) gr.GetNumberResponse(34);
					object[] argsArray = args.GetArgsArray();
					argsArray[0] = startX;
					argsArray[1] = startY;
					argsArray[2] = endX;
					argsArray[3] = endY;
					newRect = new MutableRectangle(startX, startY, endX, endY);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred levy horni, pak pravy dolni roh!)
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("MinX/Y ma byt levy horni roh, MaxX/Y ma byt pravy dolni");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				//vsef poradku tak hura zpatky (nezapomenout pridat do puvodniho seznamu!)
				rectsList.Add(newRect);
				Gump previousGi = DialogStacking.PopStackedDialog(gi);
				previousGi.InputArgs.SetTag(D_Region_Rectangles.rectsListTK, rectsList); //ulozime to do predchoziho dialogu
				DialogStacking.ResendAndRestackDialog(previousGi);
			}
		}
	}
}