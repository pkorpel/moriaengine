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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>Parameters for D_OptionPicker (#162) - a generic "pick one of these labeled
	/// options" gump, reused for the race/profession/nation steps of the race-selection flow
	/// rather than writing 3 near-identical gump classes.</summary>
	public class OptionPickerArgs {
		public readonly string Title;
		public readonly IReadOnlyList<string> OptionLabels;
		public readonly Action<Player, int> OnSelected;

		public OptionPickerArgs(string title, IReadOnlyList<string> optionLabels, Action<Player, int> onSelected) {
			this.Title = title;
			this.OptionLabels = optionLabels;
			this.OnSelected = onSelected;
		}
	}

	/// <summary>Generic single-column "pick one" gump (#162) - one row per option, a text
	/// label paired with a button, same layout convention D_Info/Input.cs already use
	/// (ImprovedDialog/GUTATable/GUTAButton). Button ids start at 10, matching D_Info's own
	/// "start counting buttons from 10" convention.</summary>
	public class D_OptionPicker : CompiledGumpDef {
		private const int FirstOptionButtonId = 10;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			OptionPickerArgs picker = (OptionPickerArgs) args[0];

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(300);
			dlg.SetLocation(300, 200);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline(picker.Title).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			for (int i = 0; i < picker.OptionLabels.Count; i++) {
				dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
				dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel(picker.OptionLabels[i]).Build();
				dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(FirstOptionButtonId + i).Build();
				dlg.MakeLastTableTransparent();
			}

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi);
				return;
			}
			OptionPickerArgs picker = (OptionPickerArgs) args[0];
			int index = gr.PressedButton - FirstOptionButtonId;
			if ((index >= 0) && (index < picker.OptionLabels.Count)) {
				picker.OnSelected((Player) gi.Cont, index);
			}
		}
	}
}
