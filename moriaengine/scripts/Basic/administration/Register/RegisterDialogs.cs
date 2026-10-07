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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>
	/// Dialog listing all accounts notes
	/// Seznam parametru: 0 - account jehoz noty zobrazujeme, 
	/// 1 - tridici kriterium
	/// 2 - index ze seznamu notu ktery bude na strance jako prvni
	/// 3 - ulozeny noteslist pro pripadnou navigaci v dialogu
	/// </summary>
	public class D_AccountNotes : CompiledGumpDef {
		internal static readonly TagKey accountTK = TagKey.Acquire("_scripted_account_tag_");
		internal static readonly TagKey issuesListTK = TagKey.Acquire("_acc_issues_list_"); //used for notes, crimes
		internal static readonly TagKey issuesSortingTK = TagKey.Acquire("_acc_issues_sorting_"); //used the smae way
		private static int width = 800;


		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ScriptedAccount acc = (ScriptedAccount) args.GetTag(accountTK); //vzit seznam notu z accountu prisleho v parametru dialogu

			List<AccountNote> notesList = (List<AccountNote>) args.GetTag(issuesListTK); //taglist si posilame v argumentu (napriklad pri pagingu)
			if (notesList == null) {
				//vzit seznam notu a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				notesList = AccountRegister.GetNotes(acc, (SortingCriteria) TagMath.IGetTag(args, issuesSortingTK));
				args.SetTag(issuesListTK, notesList); //ulozime to do argumentu dialogu
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, notesList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Poznámky k accountu " + acc.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + notesList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni nove pozmamky
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Nová poznámka").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, 145, 120, 350, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle casu asc
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle casu desc            
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Èas").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit dle refcharu asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit dle refcharu desc
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Postava").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Text poznámky").Build();
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(6).Build(); //tridit dle issuera asc
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(7).Build(); //tridit dle issuera desc
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Autor").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.MakeLastTableTransparent();

			//seznam poznamek
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				AccountNote note = notesList[i];

				dlg.LastTable[rowCntr, 0] = GUTAText.Builder.Text(note.time.ToString("hh:mm:ss dd.MM.yyyy")).Build();
				if (note.referredChar != null) {
					dlg.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + (4 * i)).Build(); //info o postave
					dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(note.referredChar.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //postava
				} else {
					dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(AccountRegister.ALL_CHARS).Build(); //tyka se vsech postav na acc
				}
				dlg.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(11 + (4 * i)).Build(); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(note.text).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //text zpravy
				dlg.LastTable[rowCntr, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(12 + (4 * i)).Build(); //info o issuerovi
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(note.issuer.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //issuer

				if (sendTo == note.issuer || sendTo.Plevel > note.issuer.Plevel) {
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(13 + (4 * i)).Build(); //smaz
				} else {
					//pokud ten co se diva neni ten kdo zpravu postnul a ani nema vyssi plevel, pak nesmi poznamku smazat!
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonNoOperation).Active(false).Id(9999).Build();
				}
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(notesList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam poznamek bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AccountNote> notesList = (List<AccountNote>) args.GetTag(issuesListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novou poznamku
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_New_AccountNote.isCrimeTK, false); //poznamka
						newArgs.SetTag(accountTK, args.GetTag(accountTK));//account (char nepotrebujeme)
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_New_AccountNote>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //time asc
						args.SetTag(issuesSortingTK, SortingCriteria.TimeAsc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //time desc
						args.SetTag(issuesSortingTK, SortingCriteria.TimeDesc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //refcar asc
						args.SetTag(issuesSortingTK, SortingCriteria.RefCharAsc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //refchar desc
						args.SetTag(issuesSortingTK, SortingCriteria.RefCharDesc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //issuer asc
						args.SetTag(issuesSortingTK, SortingCriteria.IssuerAsc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //issuer desc
						args.SetTag(issuesSortingTK, SortingCriteria.IssuerDesc);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, notesList.Count, 1)) {//kliknuto na paging?
				//1 sloupecek
			} else {
				//zjistime si radek
				int row = (gr.PressedButton - 10) / 4;
				int buttNo = (gr.PressedButton - 10) % 4;
				AccountNote note = notesList[row];
				Gump newGi;
				switch (buttNo) {
					case 0: //char info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(note.referredChar));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //text zpravy
						//zobrazit tex zprávy (první parametr je nadpis, druhý je zobrazný text)
						newGi = gi.Cont.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("Text poznámky", note.text));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //issuer info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(note.issuer));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //smazat poznamku (to muze jen jeji autor nebo clovek s vyssim plevelem)
						//ScriptedAccount acc = (ScriptedAccount)args[0];
						ScriptedAccount acc = (ScriptedAccount) args.GetTag(accountTK);
						acc.RemoveNote(note);
						args.RemoveTag(issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		/// <summary>
		/// Display an account notes. Function accessible from the game.
		/// The function is designed to be triggered using .x AccNotes or .AccNotes
		/// but it can be also called from other dialogs - such as info...
		/// Default sorting is by Time, desc.
		/// Another way to use is: .AccNotes('acc_name'[,'sorting']).
		/// </summary>
		[SteamFunction]
		public static void AccNotes(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			//1. parametr - account
			//2. param - trideni dle...
			//3. od kolikate poznamky zaciname (0), 3. prostor pro potreby dialogu
			DialogArgs newArgs = new DialogArgs();
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				AbstractCharacter refChar = self as AbstractCharacter;
				if (refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer hrace", (int) Hues.Red);
					return;
				}
				if (refChar.IsPlayer) {
					newArgs.SetTag(accountTK, refChar.Account);//nastavit account
					newArgs.SetTag(issuesSortingTK, SortingCriteria.TimeDesc);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, newArgs);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				ScriptedAccount acc = (ScriptedAccount) AbstractAccount.GetByName(text.Argv[0].ToString());
				if (acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem " + text.Argv[0] + " neexistuje.", (int) Hues.Red);
					return;
				}
				newArgs.SetTag(accountTK, acc);//nastavit account					
				if (text.Argv.Length == 1) { //mame jen nazev accountu
					newArgs.SetTag(issuesSortingTK, SortingCriteria.TimeDesc);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, newArgs);
				} else { //mame i trideni
					newArgs.SetTag(issuesSortingTK, (SortingCriteria) text.Argv[1]);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountNotes>.Instance, newArgs);
				}
			}
		}
	}

	/// <summary>
	/// Dialog listing all account's crimes
	/// Seznam parametru: 0 - account jehoz crimy zobrazujeme, 
	/// 	1 - tridici kriterium
	/// 	2 - index ze seznamu notu ktery bude na strance jako prvni
	/// 	3 - ulozeny noteslist pro pripadnou navigaci v dialogu
	/// </summary>
	public class D_AccountCrimes : CompiledGumpDef {
		private static int width = 900;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ScriptedAccount acc = (ScriptedAccount) args.GetTag(D_AccountNotes.accountTK);

			List<AccountCrime> crimesList = (List<AccountCrime>) args.GetTag(D_AccountNotes.issuesListTK);	//taglist si posilame v argumentu (napriklad pri pagingu)
			if (crimesList == null) {
				//vzit seznam crimu a pripadne ho setridit...
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				crimesList = AccountRegister.GetCrimes(acc, (SortingCriteria) TagMath.IGetTag(args, D_AccountNotes.issuesSortingTK));
				args.SetTag(D_AccountNotes.issuesListTK, crimesList); //ulozime to do argumentu dialogu								
			}

			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, crimesList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Prohøešky accountu " + acc.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + crimesList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho trestu
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Nový prohøešek").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, 145, 120, 225, 225, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle casu asc
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle casu desc            
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Èas").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit dle refcharu asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit dle refcharu desc
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Postava").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Popis prohøešku").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Popis trestu").Build();
			dlg.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(6).Build(); //tridit dle issuera asc
			dlg.LastTable[0, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(7).Build(); //tridit dle issuera desc
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Autor").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 5] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.MakeLastTableTransparent();

			//seznam trestu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				AccountCrime crime = crimesList[i];

				dlg.LastTable[rowCntr, 0] = GUTAText.Builder.Text(crime.time.ToString("hh:mm:ss dd.MM.yyyy")).Build();
				if (crime.referredChar != null) {
					dlg.LastTable[rowCntr, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + (5 * i)).Build(); //info o postave
					dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(crime.referredChar.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //postava
				} else {
					dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(AccountRegister.ALL_CHARS).Build(); //tyka se vsech postav na acc
				}
				dlg.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(11 + (5 * i)).Build(); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(crime.text).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //text prohresku
				dlg.LastTable[rowCntr, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(12 + (5 * i)).Build(); //zobrazit text zpravy
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(crime.punishment).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //text trestu

				dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(13 + (5 * i)).Build(); //info o issuerovi
				dlg.LastTable[rowCntr, 4] = GUTAText.Builder.Text(crime.issuer.Name).XPos(ButtonMetrics.D_BUTTON_WIDTH).Build(); //issuer

				if (sendTo == crime.issuer || sendTo.Plevel > crime.issuer.Plevel) {
					dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(14 + (5 * i)).Build(); //smaz
				} else {
					//pokud ten co se diva neni ten kdo zpravu postnul a ani nema vyssi plevel, pak nesmi trest smazat!
					dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonNoOperation).Active(false).Id(9999).Build();
				}
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu
			dlg.CreatePaging(crimesList.Count, firstiVal, 1);//ted paging			
			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam crimu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AccountCrime> crimesList = (List<AccountCrime>) args.GetTag(D_AccountNotes.issuesListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, tridit)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //zalozit novy trest
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_New_AccountNote.isCrimeTK, true); //trest
						newArgs.SetTag(D_AccountNotes.accountTK, args.GetTag(D_AccountNotes.accountTK));//account (char nepotrebujeme)
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_New_AccountNote>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi); //vlozime napred dialog do stacku
						break;
					case 2: //time asc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.TimeAsc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //time desc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.TimeDesc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //refcar asc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.RefCharAsc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //refchar desc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.RefCharDesc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //issuer asc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.IssuerAsc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //issuer desc
						args.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.IssuerDesc);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, crimesList.Count, 1)) {//kliknuto na paging? (1 = index parametru nesoucim info o pagingu (zde dsi.Args[2] viz výše)
				//1 sloupecek
			} else {
				//zjistime si radek
				int row = (gr.PressedButton - 10) / 5;
				int buttNo = (gr.PressedButton - 10) % 5;
				AccountCrime crime = crimesList[row];
				Gump newGi;
				switch (buttNo) {
					case 0: //char info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(crime.referredChar));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //text prohresku
						//první parametr je nadpis, druhy je zobrazeny text)
						newGi = gi.Cont.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("Popis prohøešku", crime.text));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //text trestu
						//první parametr je nadpis, druhy je zobrazeny text)
						newGi = gi.Cont.Dialog(SingletonScript<D_Display_Text>.Instance, new DialogArgs("Popis trestu", crime.punishment));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //issuer info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(crime.issuer));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 4: //smazat trest (to muze jen jeji autor nebo clovek s vyssim plevelem)
						ScriptedAccount acc = (ScriptedAccount) args.GetTag(D_AccountNotes.accountTK);
						acc.RemoveCrime(crime);
						args.RemoveTag(D_AccountNotes.issuesListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		/// <summary>
		/// Display an account crimes. Function accessible from the game.
		/// The function is designed to be triggered using .x AccCrimes or .AccCrimes
		/// but it can be also called from other dialogs - such as info...
		/// Default sorting is by Time, desc.
		/// Another way to use is: .AccCrimes('acc_name'[,'sorting']).
		/// </summary>
		[SteamFunction]
		public static void AccCrimes(Thing self, ScriptArgs text) {
			//Parametry dialogu:
			//1. parametr - account
			//2. param - trideni dle...
			//3. od kolikate poznamky zaciname (0), 3. prostor pro potreby dialogu
			DialogArgs newArgs = new DialogArgs();
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				AbstractCharacter refChar = self as AbstractCharacter;
				if (refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer hrace", (int) Hues.Red);
					return;
				}
				if (refChar.IsPlayer) {
					newArgs.SetTag(D_AccountNotes.accountTK, refChar.Account);
					newArgs.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.TimeDesc);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, newArgs);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				ScriptedAccount acc = (ScriptedAccount) AbstractAccount.GetByName(text.Argv[0].ToString());
				if (acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem " + text.Argv[0] + " neexistuje.", (int) Hues.Red);
					return;
				}
				newArgs.SetTag(D_AccountNotes.accountTK, acc);
				if (text.Argv.Length == 1) { //mame jen nazev accountu
					newArgs.SetTag(D_AccountNotes.issuesSortingTK, SortingCriteria.TimeDesc);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, newArgs);
				} else { //mame i trideni
					newArgs.SetTag(D_AccountNotes.issuesSortingTK, (SortingCriteria) text.Argv[1]);
					Globals.SrcCharacter.Dialog(SingletonScript<D_AccountCrimes>.Instance, newArgs);
				}
			}
		}
	}

	public class D_New_AccountNote : CompiledGumpDef {
		internal static readonly TagKey isCrimeTK = TagKey.Acquire("_is_crime_issue_");
		internal static readonly TagKey issuedCharTK = TagKey.Acquire("_issued_char_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			bool isCrime = ConvertTools.ToBoolean(args.GetTag(isCrimeTK)); //true - crime note, false - normal note
			AbstractCharacter refChar = args.GetTag(issuedCharTK) as AbstractCharacter; //if not present the note is for the whole account
			ScriptedAccount acc = args.GetTag(D_AccountNotes.accountTK) as ScriptedAccount; //the account could or might not have arrived... :]

			string dlgHeadline; //crime / note
			string textFldLabel; //crime desc / record
			string target; //who is this note for?
			if (refChar != null) { //we have player, we dont care for the account any more
				target = "hráèe " + refChar.Name;
			} else {
				if (acc != null) {
					target = "account " + acc.Name;
				} else {
					target = "account";
				}
			}
			if (isCrime) {
				dlgHeadline = "Nový záznam trestu pro " + target;
				textFldLabel = "Prohøešek";
			} else {
				dlgHeadline = "Nová poznámka pro " + target;
				textFldLabel = "Poznámka";
			}

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(400);
			dlg.SetLocation(50, 50);

			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline(dlgHeadline).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable((isCrime ? 3 : 2), 0, 275)); //1.sl - edit nazev, 2.sl - edit hodnota
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel(textFldLabel).Build();
			if (refChar == null) {
				dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Account").Build();
				if (acc == null) { //account didnt come					
					dlg.LastTable[0, 1] = GUTAInput.Builder.Id(10).Build();
				} else {
					dlg.LastTable[0, 1] = GUTAText.Builder.Text(acc.Name).Build();
				}
			} else {//we have player
				dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Postava").Build();
				dlg.LastTable[0, 1] = GUTAText.Builder.Text(refChar.Name).Build();
			}
			dlg.LastTable[1, 1] = GUTAInput.Builder.Id(11).Build();
			if (isCrime) { //crimes have more fields...
				dlg.LastTable[2, 0] = GUTAText.Builder.TextLabel("Trest").Build();
				dlg.LastTable[2, 1] = GUTAInput.Builder.Id(12).Build();
			}
			dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Uložit").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			bool isCrime = ConvertTools.ToBoolean(args.GetTag(isCrimeTK)); //true - crime note, false - normal note
			AbstractCharacter refChar = args.GetTag(issuedCharTK) as AbstractCharacter; //if not present the note is for the whole account
			ScriptedAccount acc = args.GetTag(D_AccountNotes.accountTK) as ScriptedAccount; //the account could or might not have arrived... :]

			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi);
			} else if (gr.PressedButton == 1) {
				if (refChar != null) {//creating note for the player
					acc = (ScriptedAccount) refChar.Account; //get the account
				} else {
					if (acc == null) {//try to load the account
						string accName = gr.GetTextResponse(10);
						acc = (ScriptedAccount) AbstractAccount.GetByName(accName);
						if (acc == null) {//failed to find the acc
							gi.Cont.SysMessage("Account se jménem " + accName + " neexistuje!", (int) Hues.Red);
							DialogStacking.ResendAndRestackDialog(gi);
						}
					}
				}
				//and now the note itself
				string noteDesc = gr.GetTextResponse(11);
				if (isCrime) {
					string punishmentDesc = gr.GetTextResponse(12);
					AccountCrime newCrime = new AccountCrime(gi.Cont, refChar, punishmentDesc, noteDesc);
					acc.AddCrime(newCrime);
				} else {
					AccountNote newNote = new AccountNote(gi.Cont, refChar, noteDesc);
					acc.AddNote(newNote);
				}
				//if the previous dialog is the accnotes or acccrimes list, we have to clear the list in the stacked instance
				//so it can be reread again with the newly created note
				Gump prevStacked = DialogStacking.PopStackedDialog(gi);
				if (prevStacked != null) {
					//uz neni treba kontrolovat, odstranujeme jenom tag...
					//if(prevStacked.def.GetType().IsAssignableFrom(typeof(D_AccountNotes)) ||
					//  prevStacked.def.GetType().IsAssignableFrom(typeof(D_AccountCrimes))) {
					prevStacked.InputArgs.RemoveTag(D_AccountNotes.issuesListTK);
					//}
					DialogStacking.ResendAndRestackDialog(prevStacked);
				}
			}
		}

		/// <summary>
		/// Display the dialog for creating a new AccountNote.
		/// Is called directly by .x NewAccNote on the target player
		/// </summary>
		[SteamFunction]
		public static void NewAccNote(Thing self, ScriptArgs text) {
			//dialog parameters: 
			//1 - true (isCrime) / false (isNote)
			//2 - referred character
			//3 - referred account (not necessary)
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				//no player, no account specified - we will use the "self" as the target player
				AbstractCharacter refChar = self as AbstractCharacter;
				if (refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer playera", (int) Hues.Red);
				}
				if (refChar.IsPlayer) {
					DialogArgs newArgs = new DialogArgs();
					newArgs.SetTag(isCrimeTK, false);
					newArgs.SetTag(issuedCharTK, self);
					Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, newArgs);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				//we dont support parameters here!
				Globals.SrcCharacter.SysMessage("Pouziti: .NewAccNote - nova poznamka na sebe; .x NewAccNote - nova poznamka na zamereneho hrace");
			}
		}

		/// <summary>
		/// Display the dialog for creating a new AccountCrime.
		/// Is called directly by .x NewAccCrime on the target player
		/// </summary>
		[SteamFunction]
		public static void NewAccCrime(Thing self, ScriptArgs text) {
			//dialog parameters: 
			//1 - true (isCrime) / false (isNote)
			//2 - referred character
			//3 - referred account (not necessary)
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				//no player, no account specified - we will use the "self" as the target player
				AbstractCharacter refChar = self as AbstractCharacter;
				if (refChar == null) {
					Globals.SrcCharacter.SysMessage("Chybne zamereni, zamer playera", (int) Hues.Red);
				}
				if (refChar.IsPlayer) {
					DialogArgs newArgs = new DialogArgs();
					newArgs.SetTag(isCrimeTK, true);
					newArgs.SetTag(issuedCharTK, self);
					Globals.SrcCharacter.Dialog(SingletonScript<D_New_AccountNote>.Instance, newArgs);
				} else {
					Globals.SrcCharacter.SysMessage("Zameruj hrace!");
				}
			} else {
				//we dont support parameters here!
				Globals.SrcCharacter.SysMessage("Pouziti: .NewAccCrime - novy trest pro sebe :); .x NewAccCrime - novy trest na zamereneho hrace");
			}
		}
	}
}
