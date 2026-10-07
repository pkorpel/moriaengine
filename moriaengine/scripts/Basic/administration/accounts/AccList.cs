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

	/// <summary>Dialog listing all players accounts in the game</summary>
	public class D_AccList : CompiledGumpDef {
		public static TagKey searchStringTK = TagKey.Acquire("_search_string_");
		public static TagKey accListTK = TagKey.Acquire("_acc_list_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//seznam accountu vyhovujici zadanemu parametru, ulozit na dialog
			List<ScriptedAccount> accList = (List<ScriptedAccount>) args.GetTag(accListTK);//mame list v tagu, vytahneme ho
			if (accList == null) {//nemame zadny seznam
				accList = ScriptedAccount.RetreiveByStr(TagMath.SGetTag(args, searchStringTK));
				accList.Sort(AccountComparer.instance);//setridit, to da rozum			
			}
			args.SetTag(accListTK, accList);//ulozime to do argumentu dialogu
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			//maximalni index (20 radku mame) + hlidat konec seznamu...			
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, accList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(400);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam hráèských úètù (" + (firstiVal + 1) + "-" + imax + " z " + accList.Count + ")").Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 150, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho uctu
			dlg.AddTable(new GUTATable(1, 150, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Založit nový account").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(2).Build();
			dlg.MakeLastTableTransparent();

			//seznam uctu s tlacitkem pro detail (tolik radku, kolik se bude zobrazovat uctu)
			dlg.AddTable(new GUTATable(imax - firstiVal, ButtonMetrics.D_BUTTON_WIDTH, 0));
			//cudlik pro info
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Come").Build();
			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				AbstractAccount ga = accList[i];
				Hues nameColor = ga.IsOnline ? Hues.OnlineColor : Hues.OfflineColor;

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(i + 10).Build(); //account info
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(ga.Name).Hue(nameColor).Build(); //acc name

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(accList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam hracu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<ScriptedAccount> accList = (List<ScriptedAccount>) args.GetTag(accListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);

			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem
						args.SetTag(searchStringTK, nameCriteria);//uloz info o vyhledavacim kriteriu
						args.RemoveTag(accListTK);//vycistit soucasny odkaz
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //zalozit novy acc.
						//ulozime dialog pro navrat
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_NewAccount>.Instance);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, accList.Count, 1)) {//kliknuto na paging?
				//1 sloupecek
			} else { //skutecna talcitka z radku
				//zjistime kterej cudlik z kteryho radku byl zmacknut
				int row = gr.PressedButton - 10;
				int listIndex = firstOnPage + row;
				AbstractAccount ga = accList[row];
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ga));
				//ulozime dialog pro navrat
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		/// <summary>Display an account list. Function accessible from the game</summary>
		[SteamFunction]
		public static void AccList(AbstractCharacter sender, ScriptArgs text) {
			//zavolat dialog, pocatecni pismena
			//accountu vezmeme z args
			//vyhledavani
			DialogArgs newArgs = new DialogArgs();
			if (text.Argv == null || text.Argv.Length == 0) {
				sender.Dialog(SingletonScript<D_AccList>.Instance, newArgs);
			} else {
				newArgs.SetTag(searchStringTK, text.Args);
				sender.Dialog(SingletonScript<D_AccList>.Instance, newArgs);
			}
		}

		/// <summary>
		/// Display an account info. 
		/// Usage .x accinfo or .accinfo('accname')
		/// </summary>
		[SteamFunction]
		public static void AccInfo(AbstractCharacter target, ScriptArgs text) {
			if (text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(target.Account));
			} else {
				string accName = (string) text.Argv[0];
				AbstractAccount acc = AbstractAccount.GetByName(accName);
				if (acc == null) {
					Globals.SrcCharacter.SysMessage("Account se jménem " + accName + " neexistuje!", (int) Hues.Red);
					return;
				}
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(acc));
			}
		}
	}

	/// <summary>Comparer for sorting accounts by account name asc</summary>
	public class AccountComparer : IComparer<ScriptedAccount> {
		public static readonly AccountComparer instance = new AccountComparer();

		private AccountComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(ScriptedAccount x, ScriptedAccount y) {
			return string.Compare(x.Name, y.Name);
		}
	}
}