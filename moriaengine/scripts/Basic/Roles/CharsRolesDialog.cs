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

	/// <summary>Dialog listing all character's roles</summary>
	public class D_CharsRolesList : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Acquire("_roles_set_");
		internal static readonly TagKey criteriumTK = TagKey.Acquire("_roles_criterium_");
		internal static readonly TagKey sortingTK = TagKey.Acquire("_roles_sorting_");

		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam roli
			List<Role> rlList = args.GetTag(listTK) as List<Role>;

			if (rlList == null) {
				//vzit seznam roli z focusa dle vyhledavaciho kriteria
				Character whose = (Character) focus;
				ICollection<Role> rolesSet = RolesManagement.GetCharactersRoles(whose);
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				rlList = this.ListifyRoles(rolesSet, TagMath.SGetTag(args, criteriumTK));
				this.SortRoles(rlList, (SortingCriteria) TagMath.IGetTag(args, sortingTK));
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
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam rolí co má " + focus.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + rlList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 300, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle name asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle name desc				
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Název").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit podle roledefname asc
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit podle roledefname desc				
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Roledef").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Roledef info").Build();
			dlg.MakeLastTableTransparent();

			//seznam roli
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				Role rl = rlList[i];
				//infodialog
				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + 2 * i).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(rl.Key.Name).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(rl.RoleDef.Defname).Build();
				//roledef info dialog
				dlg.LastTable[rowCntr, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(11 + 2 * i).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(rlList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam roli bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<Role> rlList = (List<Role>) args.GetTag(listTK);
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
						args.RemoveTag(listTK);//vycistit soucasny odkaz na taglist aby se mohl prenacist
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
					case 4: //roledefname asc
						args.SetTag(sortingTK, SortingCriteria.DefnameAsc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //roledefname desc
						args.SetTag(sortingTK, SortingCriteria.DefnameDesc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, rlList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (gr.PressedButton - 10) / 2;
				int buttNum = (gr.PressedButton - 10) % 2;
				Role rl = rlList[row];
				Gump newGi;
				switch (buttNum) {
					case 0: //role info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(rl));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1: //role def info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(rl.RoleDef));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		/// <summary>Retreives the list of all chars roles</summary>
		private List<Role> ListifyRoles(IEnumerable<Role> roles, string criteria) {
			List<Role> rlsList = new List<Role>();
			foreach (Role entry in roles) {
				if (criteria == null || criteria.Equals("")) {
					rlsList.Add(entry);//bereme vse
				} else if (entry.Key.Name.ToUpper().Contains(criteria.ToUpper())) {
					rlsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return rlsList;
		}

		/// <summary>Sorting of the roles list</summary>
		private void SortRoles(List<Role> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(RolesNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(RolesNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					list.Sort(RolesNameComparer.instance);
					break;
				case SortingCriteria.DefnameDesc:
					list.Sort(RolesNameComparer.instance);
					list.Reverse();
					break;
			}
		}

		/// <summary>
		/// Display a list of roles on a given character. Function accessible from the game.
		/// The function is designed to be triggered using .x RolesList(criteria)
		/// </summary>
		[SteamFunction]
		public static void RolesList(Character self, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(sortingTK, SortingCriteria.NameAsc);//trideni
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				self.Dialog(SingletonScript<D_CharsRolesList>.Instance, newArgs);
			} else {
				newArgs.SetTag(criteriumTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_CharsRolesList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting roles by name asc</summary>
	public class RolesNameComparer : IComparer<Role> {
		public static readonly RolesNameComparer instance = new RolesNameComparer();

		private RolesNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Role x, Role y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Key.Name, y.Key.Name);
		}
	}

	/// <summary>Comparer for sorting roles by their roledefs defname asc</summary>
	public class RolesRoleDefsDefNameComparer : IComparer<Role> {
		public static readonly RolesRoleDefsDefNameComparer instance = new RolesRoleDefsDefNameComparer();

		private RolesRoleDefsDefNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Role x, Role y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.RoleDef.Defname, y.RoleDef.Defname);
		}
	}
}