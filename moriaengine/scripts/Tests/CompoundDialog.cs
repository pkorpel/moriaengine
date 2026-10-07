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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dialog testing the tables in columns</summary>
	public class D_CompoundDlg : CompiledGumpDef {

		/// <summary>Instance of the D_TagList, for possible access from other dialogs etc.</summary>
		private static D_CompoundDlg instance;
		public static D_CompoundDlg Instance {
			get {
				return instance;
			}
		}
		/// <summary>Set the static reference to the instance of this dialog</summary>
		public D_CompoundDlg() {
			instance = this;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs sa) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(450);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Testovací skládaný dialog").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//telo
			dlg.AddTable(new GUTATable(ImprovedDialog.PAGE_ROWS, 200, 0));

			GUTATable innerTable1 = new GUTATable(ImprovedDialog.PAGE_ROWS, 0, 100);
			//innerTable1[0,0] = TextFactory.CreateLabel("prvni vnitrni tabulecka");
			//innerTable1[0, 1] = TextFactory.CreateHeadline("druhej sloupecek");
			innerTable1.Transparent = true;
			innerTable1.NoWrite = true;
			GUTATable innerTable2 = new GUTATable(ImprovedDialog.PAGE_ROWS, 0, 100);
			//innerTable2[0,0] = TextFactory.CreateLabel("druha vnitrni tabulecka");
			//innerTable2[0, 1] = TextFactory.CreateHeadline("taky druhej sloupecek");
			innerTable2.Transparent = true;
			innerTable2.NoWrite = true;

			dlg.LastTable.Components[0].AddComponent(innerTable1);
			dlg.LastTable.Components[1].AddComponent(innerTable2);

			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) { //ovladaci tlacitka (exit, new, vyhledej)								
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog				
			}
		}

		[SteamFunction]
		public static void TestDlg(TagHolder self, ScriptArgs text) {
			Globals.SrcCharacter.Dialog(Instance);
		}
	}
}