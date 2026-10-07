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

	/// <summary>Dialog listing all object(pluginholder)'s trigger groups</summary>
	public class D_TriggerGroupsList : CompiledGumpDef {
		internal static readonly TagKey tgListTK = TagKey.Acquire("_tg_list_");
		internal static readonly TagKey tgCriteriumTK = TagKey.Acquire("_tg_criterium_");

		private static int width = 500;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam tagu z tagholdera (char nebo item) prisleho v parametru dialogu
			PluginHolder ph = (PluginHolder) args.GetTag(D_PluginList.holderTK); //z koho budeme triggergroupy brat?
			List<TriggerGroup> tgList = args.GetTag(tgListTK) as List<TriggerGroup>;
			if (tgList == null) {
				//vzit seznam triggergroup dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				tgList = this.ListifyTriggerGroups(ph.GetAllTriggerGroups(), TagMath.SGetTag(args, tgCriteriumTK));
				tgList.Sort(TriggerGroupsComparer.instance); //setridit podle abecedy
				args.SetTag(tgListTK, tgList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, tgList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam všech trigger group na " + ph + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + tgList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik na pridani nove triggergroupy
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Pøidat trigger group").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(2).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 200, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jméno Trigger groupy").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Defname").Build();
			dlg.MakeLastTableTransparent();

			//seznam tgcek
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				TriggerGroup tg = tgList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + (i)).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(tg.PrettyDefname).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(tg.Defname).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(tgList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			PluginHolder tgOwner = (PluginHolder) args.GetTag(D_PluginList.holderTK); //z koho budeme tg brat?				
			//seznam plugin bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<TriggerGroup> tgList = (List<TriggerGroup>) args.GetTag(tgListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, tgList.Count);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(tgCriteriumTK, nameCriteria);
						args.RemoveTag(tgListTK);//vycistit soucasny odkaz na tglist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //pridat novou trigger groupu
						DialogArgs newArgs = new DialogArgs();
						newArgs.SetTag(D_PluginList.holderTK, tgOwner);
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_NewTriggerGroup>.Instance, newArgs);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, tgList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime si radek
				int row = gr.PressedButton - 10;
				int buttNo = (gr.PressedButton - 10) % 1; //vzdy nula, ale pokud budem chtit pridat cudlik do radku, tak to jen zmenim na 2 :)
				TriggerGroup tg = tgList[row];
				switch (buttNo) {
					case 0: //smazat						
						tgOwner.RemoveTriggerGroup(tg);
						args.RemoveTag(tgListTK);//na zaver smazat tglist (musi se reloadnout)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		/// <summary>Retreives the list of all trigger groups the given PluginHolder has</summary>
		private List<TriggerGroup> ListifyTriggerGroups(IEnumerable<TriggerGroup> tgs, string criteria) {
			List<TriggerGroup> tgList = new List<TriggerGroup>();
			foreach (TriggerGroup entry in tgs) {
				if (criteria == null || criteria.Equals("")) {
					tgList.Add(entry);//bereme vse
				} else if (entry.Defname.ToUpper().Contains(criteria.ToUpper())) {
					tgList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return tgList;
		}

		/// <summary>
		/// Display a trigger group list. Function accessible from the game.
		/// The function is designed to be triggered using .x TriggerGroupList(criteria)" +
		/// but it can be used also normally .TriggerGroupList(criteria) to display runner's own plugins
		/// </summary>
		[SteamFunction]
		public static void TriggerGroupList(PluginHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvni pluginy co ma
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_PluginList.holderTK, self); //na kom budeme zobrazovat tagy
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_TriggerGroupsList>.Instance, newArgs);
			} else {
				newArgs.SetTag(tgCriteriumTK, text.Args);//vyhl. kriterium
				Globals.SrcCharacter.Dialog(SingletonScript<D_TriggerGroupsList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting triggergroups by name</summary>
	public class TriggerGroupsComparer : IComparer<TriggerGroup> {
		public static readonly TriggerGroupsComparer instance = new TriggerGroupsComparer();

		private TriggerGroupsComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(TriggerGroup x, TriggerGroup y) {
			string nameA = x.Defname;
			string nameB = y.Defname;
			return StringComparer.OrdinalIgnoreCase.Compare(nameA, nameB);
		}
	}
}