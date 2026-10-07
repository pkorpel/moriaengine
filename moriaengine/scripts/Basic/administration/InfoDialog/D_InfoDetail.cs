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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Class that will display the detail on the particular single IDataFieldView from the info dialog</summary>
	public class D_Info_Detail : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			IDataFieldView view = (IDataFieldView) args[0];//view to be displayed in detail
			object target = args[1]; //infoized target of the info dialog

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(600);
			dlg.SetLocation(50, 30);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			//the viewCls could be null ! - e.g. DataView does not exist
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Detail of " + target + ":" + view.GetName(target)).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//get the value to display
			object fieldValue = view.GetValue(target);
			Type fieldValueType = fieldValue.GetType();
			string text = "";
			if (ObjectSaver.IsSimpleSaveableType(fieldValueType)) {
				if (typeof(Enum).IsAssignableFrom(view.FieldType)) {
					text = Enum.GetName(view.FieldType, fieldValue);
				} else {
					text = view.GetStringValue(target);
				}
			} else {
				if (ObjectSaver.IsSimpleSaveableOrCoordinated(fieldValueType)) {
					text = view.GetStringValue(target);
				} else {
					//just informative label - this field should have a special button for displaying an info dialog
					//but this is accessible from the infodialog itself, not from here ! (i.e. no special buttons here...)
					text = view.GetName(target);
				}
			}

			dlg.AddTable(new GUTATable(1, 0));
			//decide if to make it editable or not (depends on the IDataFieldView...)
			if (view.ReadOnly) {
				dlg.LastTable[0, 0] = GUTAText.Builder.Text(text).Build();
			} else {
				dlg.LastTable[0, 0] = GUTAInput.Builder.Text(text).Id(2).Build();
				dlg.MakeLastTableTransparent();
				//and the send button
				dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
				dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
				dlg.LastTable[0, 1] = GUTAText.Builder.Text("Uložit").Build();
			}
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			IDataFieldView view = (IDataFieldView) args[0];//view
			object target = args[1];//target of info dialog

			switch (gr.PressedButton) {
				case 0: //exit
					DialogStacking.ShowPreviousDialog(gi); //zobrazit predchozi infodialog
					break;
				case 1: //store
					//simulate the dictionary of an edit field for the settings provider (see the Info Dialog response implementation)
					Dictionary<int, IDataFieldView> editFieldsPairing = new Dictionary<int, IDataFieldView>();
					editFieldsPairing[2] = view; //the edit field has number "2"

					List<SettingResult> reslist = SettingsProvider.AssertSettings(editFieldsPairing, gr, target);
					DialogStacking.ResendAndRestackDialog(gi);
					if (reslist.Count > 0) {
						//show the results dialog (if there is any change)
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_Settings_Result.resultsListTK, reslist); //list of settings resluts
						gi.Cont.Dialog(SingletonScript<D_Settings_Result>.Instance, newArgs);
					}
					break;
			}
		}
	}
}
