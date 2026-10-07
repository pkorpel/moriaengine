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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>
	/// Dialog listing all object(tagholder)'s timers
	/// Seznam parametru: 0 - thing jehoz timery zobrazujeme, 
	/// 	1 - index ze seznamu timeru ktery bude na strance jako prvni 
	/// 	2 - vyhledavaci kriterium pro jmena timeru(timerkeye)
	/// 	3 - ulozeny timerlist pro pripadnou navigaci v dialogu
	/// </summary>
	public class D_TimerList : CompiledGumpDef {
		internal static readonly TagKey holderTK = TagKey.Acquire("_timer_holder_");
		internal static readonly TagKey timerListTK = TagKey.Acquire("_timer_list_");
		internal static readonly TagKey timerCriteriumTK = TagKey.Acquire("_timer_criterium_");

		private static int width = 500;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam timeru z tagholdera (char nebo item) prisleho v parametru dialogu
			TagHolder th = (TagHolder) args.GetTag(holderTK); //z koho budeme timery brat?
			List<KeyValuePair<TimerKey, BoundTimer>> timerList = args.GetTag(timerListTK) as List<KeyValuePair<TimerKey, BoundTimer>>;
			if (timerList == null) {
				//vzit seznam timeru dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				timerList = this.ListifyTimers(th.GetAllTimers(), TagMath.SGetTag(args, timerCriteriumTK));
				timerList.Sort(TimersComparer.instance);
				args.SetTag(timerListTK, timerList); //ulozime to do argumentu dialogu
			}
			//zjistit zda bude paging, najit maximalni index na strance
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);   //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, timerList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, innerWidth - 2 * ButtonMetrics.D_BUTTON_WIDTH - ImprovedDialog.D_COL_SPACE, 0, ButtonMetrics.D_BUTTON_WIDTH));
			//dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam všech timerù na " + th + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + timerList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(3).Build();//cudlik na refresh dialogu
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik na zalozeni noveho timeru
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Založit nový timer").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(2).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 200, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zruš").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jméno timeru").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Zbývající èas").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Uprav").Build();
			dlg.MakeLastTableTransparent();

			//seznam timeru
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				KeyValuePair<TimerKey, BoundTimer> de = timerList[i];
				BoundTimer tmr = de.Value;

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id((2 * i) + 10).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(de.Key.Name).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(tmr.DueInSeconds.ToString()).Build(); //hodnota tagu, vcetne prefixu oznacujicim typ
				dlg.LastTable[rowCntr, 3] = GUTAButton.Builder.Id((2 * i) + 11).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//now handle the paging 
			dlg.CreatePaging(timerList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam timeru bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<KeyValuePair<TimerKey, BoundTimer>> timerList = (List<KeyValuePair<TimerKey, BoundTimer>>) args.GetTag(timerListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(timerCriteriumTK, nameCriteria);//uloz info o vyhledavacim kriteriu
						args.RemoveTag(timerListTK);//vycistit soucasny odkaz na timerlist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //zalozit novy timer - TOTO ZATIM DELAT NEBUDEME
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //refresh
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, timerList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (gr.PressedButton - 10) / 2;
				int buttNum = (gr.PressedButton - 10) % 2;
				KeyValuePair<TimerKey, BoundTimer> de = timerList[row];
				switch (buttNum) {
					case 0: //smazat timer
						TagHolder timerOwner = (TagHolder) args.GetTag(holderTK);
						timerOwner.RemoveTimer(de.Key);
						//na zaver smazat timerlist (musi se reloadnout)
						args.RemoveTag(timerListTK);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 1: //upravit timer
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_EditTimer.editedTimerTK, de.Value);//editovany timer
						newArgs.SetTag(holderTK, (TagHolder) args.GetTag(holderTK));//majitel timeru
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_EditTimer>.Instance, newArgs); //posleme si parametr toho typka na nemz editujeme timer a taky timer sam
						//uložit info o dialogu pro návrat						
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		/// <summary>Retreives the list of all timers the given TagHolder has</summary>
		private List<KeyValuePair<TimerKey, BoundTimer>> ListifyTimers(IEnumerable<KeyValuePair<TimerKey, BoundTimer>> tags, string criteria) {
			List<KeyValuePair<TimerKey, BoundTimer>> timersList = new List<KeyValuePair<TimerKey, BoundTimer>>();
			foreach (KeyValuePair<TimerKey, BoundTimer> entry in tags) {
				//entry in this hashtable is TimerKey and its Timer value
				if (criteria == null || criteria.Equals("")) {
					timersList.Add(entry);//bereme vse
				} else if (entry.Key.Name.ToUpper().Contains(criteria.ToUpper())) {
					timersList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu timeru
				}
			}
			return timersList;
		}

		/// <summary>
		/// Display a timers ist. Function accessible from the game.
		/// The function is designed to be triggered using .x TimersList(criteria)
		/// but it can be used also normally .TimerList(criteria) to display runner's own timers
		/// </summary>
		[SteamFunction]
		public static void TimerList(TagHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz timery chceme zobrazit
			//0 - zacneme od prvniho tagu co ma
			//treti parametr vyhledavani dle parametru, if any...
			//ctvrty parametr = volny jeden prvek pole pro seznam timeru, pouzito az v dialogu
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(holderTK, self); //na sobe budeme zobrazovat timery
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TimerList>.Instance, newArgs);
			} else {
				newArgs.SetTag(timerCriteriumTK, text.Args);//vyhl. kriterium				
				Globals.SrcCharacter.Dialog(SingletonScript<D_TimerList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting timer dictionary entries by timers TimerKeys asc</summary>
	public class TimersComparer : IComparer<KeyValuePair<TimerKey, BoundTimer>> {
		public static readonly TimersComparer instance = new TimersComparer();

		private TimersComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(KeyValuePair<TimerKey, BoundTimer> x, KeyValuePair<TimerKey, BoundTimer> y) {
			TimerKey a = (x.Key);
			TimerKey b = (y.Key);
			return string.Compare(a.Name, b.Name);
		}
	}
}