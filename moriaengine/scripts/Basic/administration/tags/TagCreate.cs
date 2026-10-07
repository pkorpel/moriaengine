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

using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>A new tag creating dialog</summary>
	public class D_NewTag : CompiledGumpDef {
		private static int width = 400;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			TagHolder th = (TagHolder) args.GetTag(D_TagList.holderTK); //na koho budeme tag ukladat?

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonMetrics.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Vložení nového tagu na " + th).Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build();//cudlik na info o hodnotach			
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//dialozek s inputama
			dlg.AddTable(new GUTATable(2, 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			//napred napisy 
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Název tagu").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("Hodnota").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(10).Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Id(11).Build();
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//a posledni radek s tlacitkem
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Potvrdit").Build();
			dlg.MakeLastTableTransparent(); //zpruhledni posledni radek

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			} else if (gr.PressedButton == 1) {
				//nacteme obsah input fieldu
				string tagName = gr.GetTextResponse(10);
				string tagValue = gr.GetTextResponse(11);
				//ziskame objektovou reprezentaci vlozene hodnoty. ocekava samozrejme prefixy pokud je potreba!
				object objectifiedValue = null;
				try {
					objectifiedValue = ObjectSaver.Load(tagValue);//kdyz to napsal blbe tak to spadne samozrejme...
				} catch {
					//zhucelo mu to, neco zadal blbe
					//stackneme a zobrazime chybu
					Gump newGi = D_Display_Text.ShowError("Chybne zadano, nerozpoznatelna hodnota: " + tagValue);
					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				TagHolder th = (TagHolder) args.GetTag(D_TagList.holderTK);
				th.SetTag(TagKey.Acquire(tagName), objectifiedValue);
				//vzit jeste predchozi dialog, musime smazat taglist aby se pregeneroval
				//a obsahoval ten novy tag
				Gump prevStacked = DialogStacking.PopStackedDialog(gi);
				//overovat netreba, proste odstranime tag se seznamem at uz existuje nebo ne
				//if(prevStacked.def.GetType().IsAssignableFrom(typeof(D_TagList))) {
				//prisli jsme z taglistu - mame zde seznam a muzeme ho smazat
				prevStacked.InputArgs.RemoveTag(D_TagList.tagListTK);
				//}
				DialogStacking.ResendAndRestackDialog(prevStacked);
			} else if (gr.PressedButton == 2) {
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
				DialogStacking.EnstackDialog(gi, newGi); //ulozime dialog do stacku
			}
		}
	}
}