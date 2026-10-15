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
	/// <summary>Parameters for D_OptionPicker (#162/#175) - a generic "pick one of these labeled
	/// options, laid out in two columns" gump, reused for the profession/race/nation steps of the
	/// race-selection flow rather than writing 3 near-identical gump classes.</summary>
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

	/// <summary>Two-column "pick one" gump (#175) - visually matches Erebor's own
	/// `D_RACEclass_classes`/`D_RACEclass_races` layout (`newbie_portals.scp`): background art
	/// 2200, up to 5 options per column as 2151/2152 checkbox-style buttons with an HTML label
	/// column beside each, rather than the single-column generic list the original #162 pass
	/// used (D_Info/GUTATable's "modern dialog" look, which the user didn't want here).</summary>
	public class D_OptionPicker : CompiledGumpDef {
		private const int FirstOptionButtonId = 1;
		private const int MaxPerColumn = 5;
		private const int Column1X = 170;
		private const int Column2X = 325;
		private const int FirstRowY = 210;
		private const int RowHeight = 36;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			OptionPickerArgs picker = (OptionPickerArgs) args[0];

			this.SetLocation(0, 0);
			this.AddImage(140, 200, 2200);
			this.AddHtml(170, 180, 280, 24, "<CENTER>" + picker.Title + "</CENTER>", false, false);

			for (int i = 0; i < picker.OptionLabels.Count; i++) {
				int column = i / MaxPerColumn;
				int row = i % MaxPerColumn;
				int x = (column == 0) ? Column1X : Column2X;
				int y = FirstRowY + (row * RowHeight);
				this.AddButton(x, y, 2151, 2152, FirstOptionButtonId + i, GumpButtonType.Reply, 0);

				// One AddHtml per row, at the same y as its button - a single batched HTML
				// block per column (<br>-joined) rendered each line shorter than RowHeight,
				// so text visibly drifted upward relative to its button row by row.
				int textX = (column == 0) ? Column1X + 40 : Column2X + 40;
				this.AddHtml(textX, y, 110, RowHeight, picker.OptionLabels[i], false, false);
			}
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
