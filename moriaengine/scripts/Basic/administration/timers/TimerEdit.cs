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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>A timer editing dialog</summary>
	public class D_EditTimer : CompiledGumpDef {
		internal static readonly TagKey editedTimerTK = TagKey.Acquire("_timer_edited_");
		private static int width = 400;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			TagHolder th = (TagHolder) args.GetTag(D_TimerList.holderTK); //na koho budeme timer ukladat?
			Timer tm = (Timer) args.GetTag(editedTimerTK); //timer ktery editujeme

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Úprava timeru " + tm + " na " + th).Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//tabulka s inputem
			dlg.AddTable(new GUTATable(2, 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			//napred napisy 
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Název timeru").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Èas [s]").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.Text(tm.ToString()).Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(11).Text(tm.DueInSeconds.ToString()).Build();
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
				int timerTime = Convert.ToInt32(gr.GetNumberResponse(11));
				Timer tm = (Timer) args.GetTag(editedTimerTK);
				tm.DueInSeconds = timerTime;
				Gump prevStacked = DialogStacking.PopStackedDialog(gi);
				if (prevStacked.Def is D_TimerList) {
					//prisli jsme z timerlistu - mame zde seznam a muzeme ho smazat
					prevStacked.InputArgs.RemoveTag(D_TimerList.timerListTK);
				}
				DialogStacking.ResendAndRestackDialog(prevStacked);
			}
		}
	}
}