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

	/// <summary>Dialog listing all available roles (role defs)</summary>
	public class D_RolesList : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Acquire("_roles_list_");
		internal static readonly TagKey criteriumTK = TagKey.Acquire("_roles_criterium_");
		internal static readonly TagKey sortingTK = TagKey.Acquire("_roles_sorting_");

		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam roli
			List<RoleDef> rlList = args.GetTag(listTK) as List<RoleDef>;
			if (rlList == null) {
				//vzit seznam roli dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				rlList = this.ListifyRoles(RoleDef.AllRoles, TagMath.SGetTag(args, criteriumTK));
				this.SortRoleDefs(rlList, (SortingCriteria) TagMath.IGetTag(args, sortingTK));
				args.SetTag(listTK, rlList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, rlList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(70, 70);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam všech rolí (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rlList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, 300, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle name asc
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle name desc				
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Název").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit dle defname asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit dle defname desc
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Defname").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.MakeLastTableTransparent();

			//seznam roli
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				RoleDef rd = rlList[i];

				dlg.LastTable[rowCntr, 0] = GUTAText.Builder.TextLabel(rd.Defname).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.TextLabel(rd.Defname).Build();
				//a na zaver odkaz do infodialogu
				dlg.LastTable[rowCntr, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + i).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(rlList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam roledefu bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<RoleDef> rlList = (List<RoleDef>) args.GetTag(listTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, rlList.Count);
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
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, rlList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime si radek
				int row = (gr.PressedButton - 10);
				RoleDef ad = rlList[row];
				//a zobrazime info dialog
				Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ad));
				DialogStacking.EnstackDialog(gi, newGi);
			}
		}

		/// <summary>Retreives the list of all existing roledefs</summary>
		private List<RoleDef> ListifyRoles(IEnumerable<RoleDef> roles, string criteria) {
			List<RoleDef> rlsList = new List<RoleDef>();
			foreach (RoleDef entry in roles) {
				if (criteria == null || criteria.Equals("")) {
					rlsList.Add(entry);//bereme vse
				} else if (entry.Defname.ToUpper().Contains(criteria.ToUpper())) {
					rlsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return rlsList;
		}

		/// <summary>Sorting of the roledefs list</summary>
		private void SortRoleDefs(List<RoleDef> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(RoleDefsNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(RoleDefsNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					list.Sort(RoleDefsDefNameComparer.instance);
					break;
				case SortingCriteria.DefnameDesc:
					list.Sort(RoleDefsDefNameComparer.instance);
					list.Reverse();
					break;
			}
		}

		/// <summary>
		/// Display a list of all roles. Function accessible from the game.
		/// The function is designed to be triggered using .AllRoles(criteria)
		/// </summary>
		[SteamFunction]
		public static void AllRoles(Character self, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(sortingTK, SortingCriteria.NameAsc);//default sorting
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				self.Dialog(SingletonScript<D_RolesList>.Instance, newArgs);
			} else {
				newArgs.SetTag(criteriumTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_RolesList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting roledefs by name asc</summary>
	public class RoleDefsNameComparer : IComparer<RoleDef> {
		public static readonly RoleDefsNameComparer instance = new RoleDefsNameComparer();

		private RoleDefsNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(RoleDef x, RoleDef y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Defname, y.Defname);
		}
	}

	/// <summary>Comparer for sorting roledefs by defnames asc</summary>
	public class RoleDefsDefNameComparer : IComparer<RoleDef> {
		public static readonly RoleDefsDefNameComparer instance = new RoleDefsDefNameComparer();

		private RoleDefsDefNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(RoleDef x, RoleDef y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Defname, y.Defname);
		}
	}
}