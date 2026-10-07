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

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Dialog showing the results after storing the info or settigns dialog changes</summary>
	public class D_Settings_Result : CompiledGumpDef {
		internal static readonly TagKey resultsListTK = TagKey.Acquire("_settings_results_list_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//field containing the results for display
			List<SettingResult> setResults = (List<SettingResult>) args.GetTag(resultsListTK);
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK); //first index on the page (0 if not present)

			//max index (20 lines) + check the list end !
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, setResults.Count);

			int resultsOK = SettingsProvider.CountSuccessfulSettings(setResults);
			int resultsNOK = SettingsProvider.CountUnSuccessfulSettings(setResults);
			int allFields = setResults.Count;

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(700);
			dlg.SetLocation(50, 590);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Výsledky nastavení (celkem:" + allFields + ", pøenastaveno: " + resultsOK + ", chybnì zadáno: " + resultsNOK + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, 175, 175, 175, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Název").Build();//name of the datafield
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Souèasná hodnota").Build();//after setting
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Pùvodní hodnota").Build();//filled when successfully changed
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Chybná hodnota").Build();//filled on erroneous attempt to store the change
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(imax - firstiVal)); //as much lines as many results there is on the page (maximally ROW_COUNT)
			dlg.CopyColsFromLastTable();

			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				SettingResult sres = setResults[i];
				Hues color = SettingsProvider.ResultColor(sres);
				dlg.LastTable[rowCntr, 0] = GUTAText.Builder.Text(sres.Name).Hue(color).Build(); //nam of the editable field
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(sres.CurrentValue).Hue(color).Build(); //actual value from the field
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(sres.FormerValue).Hue(color).Build(); //former value of the field (filled only if the value has changed)														//
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(sres.ErroneousValue).Hue(color).Build(); //value that was attempted to be filled but which ended by some error (filled only on error)
				rowCntr++;
			}
			dlg.MakeLastTableTransparent();

			//ted paging, klasika
			dlg.CreatePaging(setResults.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			List<SettingResult> setResults = (List<SettingResult>) args.GetTag(resultsListTK);
			if (gr.PressedButton == 0) { //end				
				//dont redirect to any dialog - former info/settings dialog is already open
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, setResults.Count, 1)) {//kliknuto na paging?
				//1 sloupecek
			}
		}
	}
}