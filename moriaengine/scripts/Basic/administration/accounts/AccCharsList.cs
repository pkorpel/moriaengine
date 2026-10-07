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

	/// <summary>Dialog listing all characters of the account</summary>
	public class D_Acc_Characters : CompiledGumpDef {
		private static readonly TagKey accountTK = TagKey.Acquire("_account_with_chars_");
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			AbstractAccount acc = AbstractAccount.GetByName((string) args[0]); //jmeno accountu
			if (acc == null) {
				Globals.SrcCharacter.SysMessage("Account se jménem " + args[0] + " neexistuje!", (int) Hues.Red);
				return;
			}
			//mame-li ho, ulozme si ho do parametru pro pozdejsi pouziti
			args.SetTag(accountTK, acc);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(600);
			dlg.SetLocation(50, 600);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam postav na accountu " + acc.Name).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//seznam charu s tlacitkem pro info
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 160, 130, 110, 40, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jméno").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Profese").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Rasa").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Level").Build();
			dlg.LastTable[0, 5] = GUTAText.Builder.TextLabel("Pozice").Build();
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(AbstractAccount.maxCharactersPerGameAccount));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			foreach (AbstractCharacter oneChar in acc.Characters) {
				if (oneChar == null) {
					continue;
				}
				Player castChar = (Player) oneChar;
				// TODO poresit barvy podle prislusnosti ke strane!
				Hues color = Hues.WriteColor;
				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(rowCntr + 10).Build(); //char info
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(castChar.Name).Hue(color).Build(); //plr name
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text("TODO-profese").Hue(color).Build(); //profese
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text("TODO-rasa").Hue(color).Build(); //rasa
				dlg.LastTable[rowCntr, 4] = GUTAText.Builder.Text("TODO-level").Hue(color).Build(); //level
				dlg.LastTable[rowCntr, 5] = GUTAText.Builder.Text(castChar.P().ToNormalString()).Hue(color).Build(); //pozice

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			AbstractAccount acc = (AbstractAccount) args.GetTag(accountTK);

			if (gr.PressedButton < 10) { //ovladaci tlacitka		
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
				}
			} else { //skutecna tlacitka z radku
				//zjistime kterej cudlik z kteryho radku byl zmacknut
				int row = gr.PressedButton - 10;
				Character oneChar = (Character) acc.Characters[row];
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(oneChar));
				//ulozime dialog pro navrat
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		/// <summary>
		/// Display a list of characters on the account list. 
		/// Usage - .x AccChars. or .AccChars('accname')
		/// </summary>
		[SteamFunction]
		public static void AccChars(AbstractCharacter target, ScriptArgs text) {
			if (text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_Acc_Characters>.Instance, new DialogArgs(target.Account.Name));
			} else {
				string accName = (string) text.Argv[0];
				//overime zda existuje (uz ted)
				AbstractAccount acc = AbstractAccount.GetByName(accName);
				if (acc == null) {
					D_Display_Text.ShowError("Account se jménem " + accName + " neexistuje!");
					return;
				}
				Globals.SrcCharacter.Dialog(SingletonScript<D_Acc_Characters>.Instance, new DialogArgs(accName));
			}
		}
	}
}