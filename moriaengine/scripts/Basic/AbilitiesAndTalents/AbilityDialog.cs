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

	/// <summary>Dialog listing all available abilities (ability defs)</summary>
	public class D_AbilitiesList : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Acquire("_ability_list_");
		internal static readonly TagKey criteriumTK = TagKey.Acquire("_ability_criterium_");
		internal static readonly TagKey sortingTK = TagKey.Acquire("_abilities_sorting_");

		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam abilit
			List<AbilityDef> abList = args.GetTag(listTK) as List<AbilityDef>;

			if (abList == null) {
				//vzit seznam abilit dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				abList = this.ListifyAbilities(AbilityDef.AllAbilities, TagMath.SGetTag(args, criteriumTK));
				this.SortAbilityDefs(abList, (SortingCriteria) TagMath.IGetTag(args, sortingTK));
				args.SetTag(listTK, abList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, abList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(70, 70);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam všech abilit (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + abList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0, 300));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle name asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle name desc				
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Název").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit dle defname asc
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit dle defname desc
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Defname").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.MakeLastTableTransparent();

			//seznam abilit
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				AbilityDef ad = abList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + i).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(ad.Name).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(ad.Defname).Build();

				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(abList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam abilitydefu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<AbilityDef> abList = (List<AbilityDef>) args.GetTag(listTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, abList.Count);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(criteriumTK, nameCriteria);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //name asc
						args.SetTag(sortingTK, SortingCriteria.NameAsc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //name desc
						args.SetTag(sortingTK, SortingCriteria.NameDesc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //defname asc
						args.SetTag(sortingTK, SortingCriteria.DefnameAsc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //defname desc
						args.SetTag(sortingTK, SortingCriteria.DefnameDesc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, abList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime si radek
				int row = (gr.PressedButton - 10);
				AbilityDef ad = abList[row];
				//a zobrazime info dialog
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ad));
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		/// <summary>Retreives the list of all existing abilities</summary>
		private List<AbilityDef> ListifyAbilities(IEnumerable<AbilityDef> abilities, string criteria) {
			List<AbilityDef> absList = new List<AbilityDef>();
			foreach (AbilityDef entry in abilities) {
				if (criteria == null || criteria.Equals("")) {
					absList.Add(entry);//bereme vse
				} else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
					absList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return absList;
		}

		/// <summary>Sorting of the abilitydefs list</summary>
		private void SortAbilityDefs(List<AbilityDef> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(AbilityDefsNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(AbilityDefsNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					list.Sort(AbilityDefsDefNameComparer.instance);
					break;
				case SortingCriteria.DefnameDesc:
					list.Sort(AbilityDefsDefNameComparer.instance);
					list.Reverse();
					break;
			}
		}

		/// <summary>
		/// Display a list of all abilities. Function accessible from the game.
		/// The function is designed to be triggered using .AllAbilities(criteria)
		/// </summary>
		[SteamFunction]
		public static void AllAbilities(Character self, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(sortingTK, SortingCriteria.NameAsc);//default sorting
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				self.Dialog(SingletonScript<D_AbilitiesList>.Instance, newArgs);
			} else {
				newArgs.SetTag(criteriumTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_AbilitiesList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting abilities by ability name asc</summary>
	public class AbilityDefsNameComparer : IComparer<AbilityDef> {
		public static readonly AbilityDefsNameComparer instance = new AbilityDefsNameComparer();

		private AbilityDefsNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are abilitydefs
		//otherwise this will crash on some ClassCastException -) -ker
		//not really. Compiler makes sure of that, this is no java :P -tar
		public int Compare(AbilityDef x, AbilityDef y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}
	}

	/// <summary>Comparer for sorting abstract defs defnames asc</summary>
	public class AbilityDefsDefNameComparer : IComparer<AbilityDef> {
		public static readonly AbilityDefsDefNameComparer instance = new AbilityDefsDefNameComparer();

		private AbilityDefsDefNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(AbilityDef x, AbilityDef y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Defname, y.Defname);
		}
	}
}