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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>
	/// Dialog that will display a desired text with a desired label (e.g. displaying larger texts 
	/// in pages dialog etc.
	/// </summary>
	public class D_Display_Text : CompiledGumpDef {
		private string label;
		private string dispText;
		private Hues textColor;

		internal static TagKey textHueTK = TagKey.Acquire("_text_hue_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.label = string.Concat(args[0]); //the gump's label
			this.dispText = string.Concat(args[1]); //the text to be displayed

			object hue = args.GetTag(textHueTK);
			if (hue != null) {
				this.textColor = (Hues) Convert.ToInt32(hue); //barva titulku volitelna
			} else {
				this.textColor = Hues.HeadlineColor; //normalni nadpisek
			}
			this.ShowDialog();
		}

		/// <summary>Simply display the labeled text.</summary>
		private void ShowDialog() {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			//create the background GUTAMatrix and set its size an transparency            
			dialogHandler.CreateBackground(400);
			dialogHandler.SetLocation(400, 300);

			//first row - the label of the dialog
			dialogHandler.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.Text(this.label).Hue(this.textColor).Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dialogHandler.MakeLastTableTransparent();

			//at least three rows of a button height (scrollbar has some demands)
			dialogHandler.AddTable(new GUTATable(3, 0));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			//unbounded, scrollable html text area
			dialogHandler.LastTable[0, 0] = GUTAHTMLText.Builder.Text(this.dispText).Scrollable(true).Build();
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			switch (gr.PressedButton) {
				case 0: //exit
					//look if some dialog is not stored in the dialogs stack and possibly display it
					DialogStacking.ShowPreviousDialog(gi);
					break;
			}
		}

		/// <summary>Zobrazí dialog s volitelným labelem a textem v nìm</summary>
		[SteamFunction]
		public static void DisplayText(Thing self, ScriptArgs args) {
			if (args != null && args.Args.Length != 2) {
				self.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs(args.Args[0], args.Args[1]));
			} else {
				Globals.SrcCharacter.Message("DisplayText musí být volána se dvìma parametry - label + text", (int) Hues.Red);
			}
		}

		/// <summary>Zobrazí dialog s nadpisem CHYBA a textovým popisem chyby</summary>
		[SteamFunction]
		public static void ShowError(Thing self, ScriptArgs args) {
			if (args != null && args.Args.Length != 1) {
				DialogArgs newArgs = new DialogArgs("CHYBA", args.Argv[0]);
				newArgs.SetTag(textHueTK, Hues.Red);
				self.Dialog(SingletonScript<D_Display_Text>.Instance, newArgs);
			} else {
				Globals.SrcCharacter.Message("ShowError musí být volána s parametrem - text chyby", (int) Hues.Red);
			}
		}

		/// <summary>Obdoba show erroru použitlená jendoduše z C# - vraci GumpInstanci (napriklad pro stacknuti)</summary>
		public static Gump ShowError(string text) {
			DialogArgs newArgs = new DialogArgs("CHYBA", text);
			newArgs.SetTag(textHueTK, Hues.Red);
			return Globals.SrcCharacter.Dialog(SingletonScript<D_Display_Text>.Instance, newArgs);
		}

		/// <summary>Zobrazení infa použitlené jendoduše z C# - vraci GumpInstanci (napriklad pro stacknuti)</summary>
		public static Gump ShowInfo(string text) {
			return Globals.SrcCharacter.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("INFO", text));
		}
	}
}