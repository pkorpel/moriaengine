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

	/// <summary>A new triggergrouup adding dialog</summary>
	public class D_NewTriggerGroup : CompiledGumpDef {
		private static int width = 400;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		private static readonly TagKey prefilledDefnameTK = TagKey.Acquire("_trigger_group_to_add_defname_");


		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			PluginHolder ph = (PluginHolder) args.GetTag(D_PluginList.holderTK); //na koho budeme tg ukladat?
			//zkusime se mrknout jestli uz nemame defname predvyplneno (napriklad pri neuspesnem zadani - preklep apd.)
			string filledDefname = TagMath.SGetTag(args, prefilledDefnameTK);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Vložení nové trigger groupy na " + ph).Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//dialozek s inputama
			dlg.AddTable(new GUTATable(1, 0, 275)); //1.sl - label, 2sl. - defname pro vyhledani
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Defname").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(10).Text(filledDefname == null ? "" : filledDefname).Build();
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
				string tgDefname = gr.GetTextResponse(10);
				//zkusime getnout TriggerGroupu
				TriggerGroup tg = TriggerGroup.GetByDefname(tgDefname);
				if (tg == null) {
					//zobrait chybovou hlasku
					Gump newGi = D_Display_Text.ShowError("Trigger group s defnamem '" + tgDefname + "' nenalezena!");
					//ulozime do tagu vlozene defname
					args.SetTag(prefilledDefnameTK, tgDefname);

					DialogStacking.EnstackDialog(gi, newGi);
					return;
				}
				//ulozit a vubec
				PluginHolder ph = (PluginHolder) args.GetTag(D_PluginList.holderTK); //na koho budeme tg ukladat?
				ph.AddTriggerGroup(tg);
				//vzit jeste predchozi dialog, musime smazat tglist aby se pregeneroval
				//a obsahoval tu nove pridanou trigger groupu
				Gump prevStacked = DialogStacking.PopStackedDialog(gi);
				//overovat netreba, proste odstranime tag se seznamem at uz existuje nebo ne
				prevStacked.InputArgs.RemoveTag(D_TriggerGroupsList.tgListTK);
				DialogStacking.ResendAndRestackDialog(prevStacked);
			}
		}
	}
}