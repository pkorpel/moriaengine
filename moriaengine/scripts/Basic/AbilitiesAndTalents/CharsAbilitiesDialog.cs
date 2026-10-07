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

	/// <summary>Dialog listing all character's abilities</summary>
	public class D_CharsAbilitiesList : CompiledGumpDef {
		internal static readonly TagKey listTK = TagKey.Acquire("_abilities_set_");
		internal static readonly TagKey criteriumTK = TagKey.Acquire("_abilities_criterium_");
		internal static readonly TagKey sortingTK = TagKey.Acquire("_abilities_sorting_");
		internal static readonly TagKey abiliterTK = TagKey.Acquire("_abiliter_");

		private static int width = 800;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam abilit (je-li)
			List<Ability> abList = args.GetTag(listTK) as List<Ability>;

			if (abList == null) {
				//vzit seznam abilit z focusa dle vyhledavaciho kriteria
				Character whose = (Character) focus;
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				abList = this.ListifyAbilities(whose.Abilities, TagMath.SGetTag(args, criteriumTK));
				this.SortAbilities(abList, (SortingCriteria) TagMath.IGetTag(args, sortingTK));
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
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam abilit co má " + focus.Name + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + abList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 140, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//cudlik na pridani nove ability
			dlg.AddTable(new GUTATable(1, 120, 120, 100, 120, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Pøidat abilitu").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Ability defname").Build();
			dlg.LastTable[0, 2] = GUTAInput.Builder.Id(6).Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Poèet bodù").Build();
			dlg.LastTable[0, 4] = GUTAInput.Builder.Id(7).Type(LeafComponentTypes.InputNumber).Build();
			dlg.LastTable[0, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(8).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 250, 70, 150, ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(2).Build(); //tridit podle name asc
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(3).Build(); //tridit podle name desc				
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Název").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Pts/Max pts").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Last usage").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Run").Build();
			dlg.LastTable[0, 5] = GUTAText.Builder.TextLabel("Stop").Build();
			dlg.LastTable[0, 6] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(4).Build(); //tridit podle roledefname asc
			dlg.LastTable[0, 6] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(5).Build(); //tridit podle abilitydefname desc				
			dlg.LastTable[0, 6] = GUTAText.Builder.TextLabel("Abilitydef").XPos(ButtonMetrics.D_SORTBUTTON_COL_OFFSET).Build();
			dlg.LastTable[0, 7] = GUTAText.Builder.TextLabel("Def info").Build();
			dlg.MakeLastTableTransparent();

			//seznam abilit
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			TimeSpan now = Globals.TimeAsSpan;
			for (int i = firstiVal; i < imax; i++) {
				Ability ab = abList[i];

				//infodialog
				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id((5 * i) + 10).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(ab.Name).Build();
				dlg.LastTable[rowCntr, 2] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((5 * i) + 11).Width(30).Text("" + ab.ModifiedPoints).Build();
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text("/" + ab.MaxPoints).XPos(30).Build();

				TimeSpan ago = now - ab.LastUsage;
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(Math.Round(ago.TotalSeconds, 1) + " secs ago").Build();
				AbilityDef adef = ab.Def;
				if (adef is PassiveAbilityDef) { //PassiveAbilityDef ability nebude mit vubec nic na mackani
					dlg.LastTable[rowCntr, 4] = GUTAText.Builder.Text("").Build();
					dlg.LastTable[rowCntr, 5] = GUTAText.Builder.Text("").Build();
					//dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonNoOperation).Active(false).Id((5 * i) + 12).Build();
					//dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonNoOperation).Active(false).Id((5 * i) + 13).Build();
				} else if (adef is ActivableAbilityDef) { //activable ability budou mit tlacitka na zapnuti i vypnuti
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Active(!ab.Running).Id((5 * i) + 12).Build();
					dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Active(ab.Running).Id((5 * i) + 13).Build();
				} else if (adef is ImmediateAbilityDef) { //immediate ability def bude mit jen zapinaci tlacitko
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id((5 * i) + 12).Build();
					dlg.LastTable[rowCntr, 5] = GUTAText.Builder.Text("").Build();
					//dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonNoOperation).Active(false).Id((5 * i) + 13).Build();
				}
				dlg.LastTable[rowCntr, 6] = GUTAText.Builder.Text(ab.Def.Defname).Build();
				//abilitydef info dialog
				dlg.LastTable[rowCntr, 7] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id((5 * i) + 14).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//cudlik na ulozeni zmen v hodnotach
			dlg.AddTable(new GUTATable(1, 130, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Uložit zmìny").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(9).Build();
			dlg.MakeLastTableTransparent();

			//ted paging
			dlg.CreatePaging(abList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam abilit bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<Ability> abList = (List<Ability>) args.GetTag(listTK);
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
					case 4: //abilitydefname asc
						args.SetTag(sortingTK, SortingCriteria.DefnameAsc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 5: //abilitydefname desc
						args.SetTag(sortingTK, SortingCriteria.DefnameDesc);
						args.RemoveTag(listTK);//vycistit soucasny odkaz na list aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 8: //pridat abilitu
						//nacteme obsah obou input fieldu
						string abilityDefname = gr.GetTextResponse(6);
						decimal abilityPoints = gr.GetNumberResponse(7);
						AbilityDef abDef = AbilityDef.GetByDefname(abilityDefname);
						if (abDef == null) {
							//zadal neexistujici abilitydefname
							Gump newGi = D_Display_Text.ShowError("Chybnì zadáno, neznámý abilitydefname: " + abilityDefname);
							DialogStacking.EnstackDialog(gi, newGi);
							return;
						}
						Character abiliter = (Character) gi.Focus;
						abiliter.SetRealAbilityPoints(abDef, (int) abilityPoints); //zalozi novou / zmodifikuje hodnotu existujici ability

						args.RemoveTag(listTK); //promazeme seznam pro prenacteni
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 9: //Ulozit zmeny
						abiliter = (Character) gi.Focus;
						string result = "Výsledek zmìn hodnot abilit: <br>";
						for (int abId = firstOnPage; abId < imax; abId++) {
							int inptID = 5 * abId + 11;
							Ability chgdAbility = abList[abId];
							int newAbilityValue = (int) gr.GetNumberResponse(inptID);
							int oldAbilityValue = abiliter.GetAbility(chgdAbility.Def);
							if (oldAbilityValue != newAbilityValue) {
								result = result + "Abilita '" + chgdAbility.Name + "' zmìnìna z " + oldAbilityValue + " na " + newAbilityValue + "<br>";
								abiliter.SetRealAbilityPoints(chgdAbility.Def, newAbilityValue);
							}
						}
						DialogStacking.ResendAndRestackDialog(gi);//hned znovuotevrit
						D_Display_Text.ShowInfo(result);//a zobrazit vysledek (neni nutno stackovat je to jen pro info)
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, abList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime kterej cudlik z radku byl zmacknut
				int row = (gr.PressedButton - 10) / 5;
				int buttNum = (gr.PressedButton - 10) % 5;
				Ability ab = abList[row];
				Gump newGi;
				switch (buttNum) {
					case 0: //ability info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ab));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2: //activate ability - pro Activable / Immediate Ability
						ab.Def.Activate((Character) gi.Focus);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3: //de-activate ability (dostupne jen kdyz abilita bezela) - pro Activable Ability
						((ActivableAbilityDef) ab.Def).Deactivate((Character) gi.Focus);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 4: //abilitydef info
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(ab.Def));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		/// <summary>Retreives the list of all chars abilities</summary>
		private List<Ability> ListifyAbilities(IEnumerable<Ability> abilities, string criteria) {
			List<Ability> absList = new List<Ability>();
			foreach (Ability entry in abilities) {
				if (criteria == null || criteria.Equals("")) {
					absList.Add(entry);//bereme vse
				} else if (entry.Name.ToUpper().Contains(criteria.ToUpper())) {
					absList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu ability
				}
			}
			return absList;
		}

		/// <summary>Sorting of the abilities list</summary>
		private void SortAbilities(List<Ability> list, SortingCriteria criteria) {
			switch (criteria) {
				case SortingCriteria.NameAsc:
					list.Sort(AbilitiesNameComparer.instance);
					break;
				case SortingCriteria.NameDesc:
					list.Sort(AbilitiesNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.DefnameAsc:
					list.Sort(AbilitiesAbilityDefsDefNameComparer.instance);
					break;
				case SortingCriteria.DefnameDesc:
					list.Sort(AbilitiesAbilityDefsDefNameComparer.instance);
					list.Reverse();
					break;
				case SortingCriteria.RunningAsc:
					list.Sort(AbilitiesRunningStateComparer.instance);
					break;
				case SortingCriteria.RunningDesc:
					list.Sort(AbilitiesRunningStateComparer.instance);
					list.Reverse();
					break;
			}
		}

		/// <summary>
		/// Display a list of roles on a given character. Function accessible from the game.
		/// The function is designed to be triggered using .x AbilitiessList(criteria)
		/// </summary>
		[SteamFunction]
		public static void AbilitiesList(Character self, ScriptArgs text) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(sortingTK, SortingCriteria.NameAsc);//trideni
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				self.Dialog(SingletonScript<D_CharsAbilitiesList>.Instance, newArgs);
			} else {
				newArgs.SetTag(criteriumTK, text.Args);//vyhl. kriterium
				self.Dialog(SingletonScript<D_CharsAbilitiesList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting abilities by name asc</summary>
	public class AbilitiesNameComparer : IComparer<Ability> {
		public static readonly AbilitiesNameComparer instance = new AbilitiesNameComparer();

		private AbilitiesNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}
	}

	/// <summary>Comparer for sorting abilities by their abilitydefs defname asc</summary>
	public class AbilitiesAbilityDefsDefNameComparer : IComparer<Ability> {
		public static readonly AbilitiesAbilityDefsDefNameComparer instance = new AbilitiesAbilityDefsDefNameComparer();

		private AbilitiesAbilityDefsDefNameComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Def.Defname, y.Def.Defname);
		}
	}

	/// <summary>Comparer for sorting abilitiys by running status</summary>
	public class AbilitiesRunningStateComparer : IComparer<Ability> {
		public static readonly AbilitiesRunningStateComparer instance = new AbilitiesRunningStateComparer();

		private AbilitiesRunningStateComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(Ability x, Ability y) {
			if (x.Running)
				return 1; //x running => always larger or equals to y
			return (y.Running ? -1 : 1); //x not running => if y is running x is less. otherwise is equal
		}
	}
}