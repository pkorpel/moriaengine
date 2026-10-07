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
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Dialog for creating a new region</summary>
	public class D_New_Region : CompiledGumpDef {
		private static readonly TagKey defNameTK = TagKey.Acquire("_new_region_defname_");
		private static readonly TagKey nameTK = TagKey.Acquire("_new_region_name_");
		private static readonly TagKey homeposTK = TagKey.Acquire("_new_region_homepos_");
		public static readonly TagKey parentDefTK = TagKey.Acquire("_new_region_parent_defname_");
		private static int width = 450;

		/// <summary>V argumentech (args) mohou prijit parametry pro dialogove editfieldy</summary>
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string minX, minY, maxX, maxY, name, defname, home, parent; //predzadane hodnoty (if any)

			object[] argsArray = args.GetArgsArray();
			minX = string.Concat(argsArray[0]);
			minY = string.Concat(argsArray[1]);
			maxX = string.Concat(argsArray[2]);
			maxY = string.Concat(argsArray[3]);
			name = TagMath.SGetTagNotNull(args, nameTK);
			defname = TagMath.SGetTagNotNull(args, defNameTK);
			home = TagMath.SGetTagNotNull(args, homeposTK);
			parent = TagMath.SGetTagNotNull(args, parentDefTK);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(150, 150);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Založení nového regionu").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//navod
			dlg.AddTable(new GUTATable(3, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Vyplò vše vèetnì jednoho rectanglu (další lze pøidat pozdìji)").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextHeadline("(MinX,MinY) - levy horni roh, (MaxX,MaxY) - pravy dolni roh.").Build();
			dlg.MakeLastTableTransparent();

			//textiky a editfieldy na zadani vseho a souradnic rectanglu
			dlg.AddTable(new GUTATable(2, 100, 100, 0, 100));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Name").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(21).Text(name).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Defname").Build();
			dlg.LastTable[0, 3] = GUTAInput.Builder.Id(22).Text(defname).Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("Home (4D)").Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Id(23).Text(home).Build();
			dlg.LastTable[1, 2] = GUTAText.Builder.TextLabel("Parent").Build();
			dlg.LastTable[1, 3] = GUTAInput.Builder.Id(24).Text(parent).Build();
			dlg.LastTable[1, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).XPos(100 - ButtonMetrics.D_BUTTON_WIDTH).Id(2).Build();
			dlg.MakeLastTableTransparent();
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
			dlg.LastTable[0, 1] = GUTAText.Builder.Text("Vytvoøit").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) { //exit
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if (gr.PressedButton == 1) { //ulozit				
				//zkusime precist vsechny parametry
				int startX = (int) gr.GetNumberResponse(31);
				int startY = (int) gr.GetNumberResponse(32);
				int endX = (int) gr.GetNumberResponse(33);
				int endY = (int) gr.GetNumberResponse(34);
				object[] argsArray = args.GetArgsArray();
				argsArray[0] = startX;
				argsArray[1] = startY;
				argsArray[2] = endX;
				argsArray[3] = endY;
				args.SetTag(nameTK, gr.GetTextResponse(21)); //name
				args.SetTag(defNameTK, gr.GetTextResponse(22)); //defname
				args.SetTag(homeposTK, gr.GetTextResponse(23)); //home pozice
				args.SetTag(parentDefTK, gr.GetTextResponse(24)); //parentuv defname				

				string name, defname;
				name = gr.GetTextResponse(21);
				defname = gr.GetTextResponse(22);
				//precteme parametry a zkusime vytvorit rectangle
				MutableRectangle newRect = null;
				try {
					newRect = new MutableRectangle(startX, startY, endX, endY);
				} catch {
					//tady se octneme pokud zadal blbe ty souradnice (napred levy horni, pak pravy dolni roh!)
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("MinX/Y ma byt levy horni roh, MaxX/Y ma byt pravy dolni");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				Point4D home = null;
				try {
					home = (Point4D) ObjectSaver.OptimizedLoad_SimpleType(gr.GetTextResponse(23), typeof(Point4D));//homepos
				} catch {
					//podelal homepos
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Chybne zadana home position - ocekavano '(4D)x,y,z,m'");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				StaticRegion parent = StaticRegion.GetByDefname(gr.GetTextResponse(24));//parent defname
				if (name == null || name.Equals("") || defname == null || defname.Equals("")) {
					//neco blbe s namem nebo defnamem
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Name i defname musi byt zadano");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				if (!newRect.Contains(home)) {
					//homepos by nesedla
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Home pozice (" + gr.GetTextResponse(23) + ") musi lezet v zadanem rectanglu (" + args[0] + "," + args[1] + ")-(" + args[2] + "," + args[3] + ")");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				if (StaticRegion.GetByName(name) != null) {
					//jmeno uz existuje
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Region se jmenem " + name + " uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				if (StaticRegion.GetByDefname(defname) != null) {
					//defname uz existuje
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Region s defnamem " + defname + " uz existuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				if (parent == null) {
					//parent je povinnost!
					Gump newGi = D_Display_Text.ShowError("Rodièovský region " + gr.GetTextResponse(24) + " neexistuje");
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}

				List<MutableRectangle> oneRectList = new List<MutableRectangle>();
				oneRectList.Add(newRect);
				//vsef poradku tak hura vytvorit novy region
				StaticRegion newRegion = new FlaggedRegion(defname, parent);
				if (newRegion.InitializeNewRegion(name, home, oneRectList)) {
					D_Display_Text.ShowInfo("Vytvoøení regionu bylo úspìšné");
				} else {
					D_Display_Text.ShowError("Pøi vytávøení regionu došlo k problémùm - viz konzole");
				}
				//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if (gr.PressedButton == 2) {//vyber regionu parenta                         dialog,vyhledavani,prvni index, seznam regionu, trideni
				//zkusime precist vsechyn parametry abychom je kdyztak meli
				object[] argsArray = args.GetArgsArray();
				argsArray[0] = (int) gr.GetNumberResponse(31);
				argsArray[1] = (int) gr.GetNumberResponse(32);
				argsArray[2] = (int) gr.GetNumberResponse(33);
				argsArray[3] = (int) gr.GetNumberResponse(34);
				args.SetTag(nameTK, gr.GetTextResponse(21)); //name
				args.SetTag(defNameTK, gr.GetTextResponse(22)); //defname
				args.SetTag(homeposTK, gr.GetTextResponse(23)); //home pozice
				args.SetTag(parentDefTK, gr.GetTextResponse(24)); //parentuv defname

				DialogArgs newArgs = new DialogArgs();
				newArgs.SetTag(D_Regions.regsSortingTK, SortingCriteria.NameAsc);//zakladni trideni			
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_SelectParent>.Instance, newArgs);
				DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku				
			}
		}

		/// <summary>
		/// Create a new region. Function accessible from the game.
		/// The function is designed to be triggered using .NewRegion"
		/// but it can be also called from other dialogs - such as regions list...
		/// </summary>
		[SteamFunction]
		public static void NewRegion(Thing self, ScriptArgs text) {
			//Parametry dialogu: - jen 4 zakladni souradnice rectanglu
			self.Dialog(SingletonScript<D_New_Region>.Instance, new DialogArgs(0, 0, 0, 0));
		}
	}
}