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

	/// <summary>Craftmenu for the specified crafting skill</summary>
	public class D_Craftmenu : CompiledGumpDef {
		//public static readonly TagKey tkCraftmenuLastpos = TagKey.Get("_cm_lastPosition_");
		private const string tkCraftmenuLastposPrefix = "_cm_lastPosition_";
		private static TagKey tkInputIds = TagKey.Acquire("_cm_input_ids_");
		private static int width = 600;

		private static TagKey tkLastCat;
		public static TagKey TkLastCat {
			get {
				if (tkLastCat == null) {
					tkLastCat = TagKey.Acquire(tkCraftmenuLastposPrefix);
				}
				return tkLastCat;
			}
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			sendTo.SysMessage("Co chceš vyrobit?");
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			CraftmenuCategory cat = (CraftmenuCategory) args[0];

			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK); //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, cat.Contents.Count); //nejvyssi index na strance

			int innerWidth = (width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE);

			dlg.CreateBackground(width);
			dlg.SetLocation(80, 50);

			//nadpis dialogu, cudliky na back a zruseni

			dlg.AddTable(new GUTATable(1, innerWidth - 3 * ButtonMetrics.D_BUTTON_WIDTH - 5 * ImprovedDialog.D_COL_SPACE, ButtonMetrics.D_BUTTON_WIDTH, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Kategorie výroby: " + cat.FullName).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonBack).Id(1).Build(); //one category back button
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(7).Build(); //settings button
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dlg.MakeLastTableTransparent();

			bool forGM = ((Player) sendTo).IsGM;
			int resourcesColumn = 6;
			//"new subcategory" and "add items" button	
			if (forGM) {//only for GMs
				resourcesColumn = 7; //lastcolumn with graphical list of resources

				dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 203, ButtonMetrics.D_BUTTON_WIDTH, 0));
				dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(3).Build(); //new item(s) btn
				dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Pøidat pøedmìt(y)").Build();
				dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build(); //new subcategory btn
				dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Pøidat podkategorii").Build();
				dlg.MakeLastTableTransparent();

				//headlines row
				//make | howmuch | icon | name | move up/down | info | smaz | resources
				//allowed players see also the delete button
				dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH, ImprovedDialog.ICON_WIDTH, 150, 10, ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH, 0));
				dlg.LastTable[0, 6] = GUTAText.Builder.TextLabel("Del").Build();
				dlg.LastTable[0, 7] = GUTAText.Builder.TextLabel("Suroviny").Build();
			} else {
				//headlines row
				//make | howmuch | icon | name | move up/down | info | resources
				dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH, ImprovedDialog.ICON_WIDTH, 150, 10, ButtonMetrics.D_BUTTON_WIDTH, 0));
				dlg.LastTable[0, 6] = GUTAText.Builder.TextLabel("Suroviny").Build();
			}
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Kolik").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Jméno").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Top).Build();
			dlg.LastTable[0, 5] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.MakeLastTableTransparent();

			//konecne seznam polozek
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.LastTable.RowHeight = ImprovedDialog.ICON_HEIGHT; //it'll contain pictures...
			dlg.CopyColsFromLastTable();
			dlg.LastTable.InnerRowsDelimited = true;

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			int maxIndex = cat.Contents.Count - 1; //maximal index to access (in case of omitting...)
			CraftmenuItem oneItm = null;
			CraftmenuCategory oneCat = null;
			List<int> inputIds = new List<int>(); //list for storing indexes to input fields with items count
			for (int i = firstiVal; i < imax; i++) {
				ICraftmenuElement elem = cat.Contents[i];
				if (elem.IsCategory) {
					oneCat = (CraftmenuCategory) elem;
					dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Id(6 * i + 10).Valign(DialogAlignment.Valign_Center).Build();
					dlg.LastTable[rowCntr, 2] = GUTAImage.Builder.NamedGump(GumpIDs.Pouch).Build();
				} else {
					oneItm = (CraftmenuItem) elem;
					if (!CraftingSkillDef.CanBeMade(oneItm.itemDef, (Character) focus)) {
						//cannot be made, do not display, step over and continue
						if (imax < maxIndex) {
							imax++; //we can iterate additional one step 
						}
						continue;
					}
					dlg.LastTable[rowCntr, 0] = GUTACheckBox.Builder.Id(6 * i + 10).Build();
					dlg.LastTable[rowCntr, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(6 * i + 15).Valign(DialogAlignment.Valign_Center).Text("0").Build(); //how many to make of this item (if any)
					inputIds.Add(6 * i + 15);//store the index to the list
					dlg.LastTable[rowCntr, 2] = GUTAImage.Builder.Gump(oneItm.itemDef.Model).Color(oneItm.itemDef.Color).Build();
					dlg.LastTable[rowCntr, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(6 * i + 13).Valign(DialogAlignment.Valign_Center).Build();//display info

					//now the list of resources
					ResourcesList reses = oneItm.itemDef.Resources;
					if (reses != null) {//only if we have any resources...
						int spaceLength = ImprovedDialog.TextLength(" ");
						int lastColPos = spaceLength; //relative position in the resources column (beginning one space from the border)
						foreach (IResourceListEntry_ItemCounter rlItm in reses.MultiplicablesSublist) {
							ItemResource itmRes = rlItm as ItemResource;
							if (itmRes != null) {//add count + item picture
								string textToShow = (lastColPos > spaceLength) ? "  " : ""; //second and more items will be separated by 2spaces
								textToShow += itmRes.DesiredCount + " ";
								dlg.LastTable[rowCntr, resourcesColumn] = GUTAText.Builder.Text(textToShow).XPos(lastColPos).Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Center).Build();
								int countLength = ImprovedDialog.TextLength(textToShow); //length of the text with number (count of items needed) plus the separating space
								dlg.LastTable[rowCntr, resourcesColumn] = GUTAImage.Builder.Gump(itmRes.ItemDef.Model).Color(itmRes.ItemDef.Color).XPos(lastColPos + countLength).Align(DialogAlignment.Align_Left).Build();
								//prepare next offset:
								GumpArtDimension gad = GumpDimensions.Table[itmRes.ItemDef.Model];
								lastColPos += spaceLength + countLength + gad.Width; //first offset, counted length of the number text including separating space, width of the icon, space after the icon
							} else {//not an item => can be a typedef... (t_fruit etc.)
								TriggerGroupResource tgrRes = rlItm as TriggerGroupResource;
								if (tgrRes != null) {
									string textToShow = (lastColPos > spaceLength) ? "  " : ""; //second and more items will be separated by 2 spaces
									textToShow += tgrRes.DesiredCount + " " + ItemTypeNames.GetPrettyName(tgrRes.triggerGroup);
									dlg.LastTable[rowCntr, resourcesColumn] = GUTAText.Builder.Text(textToShow).XPos(lastColPos).Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Center).Build();
									//prepare next offset:
									lastColPos += spaceLength + ImprovedDialog.TextLength(textToShow);
								}
							}
						}
					}
				}
				dlg.LastTable[rowCntr, 3] = GUTAText.Builder.Text(elem.Name).Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
				if (forGM) {//delete button
					dlg.LastTable[rowCntr, 6] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(6 * i + 14).Valign(DialogAlignment.Valign_Center).Build(); //remove from the list
				}
				if (i > 0) {//item can be moved up only if it is not the first one
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortUp).Id(6 * i + 11).Valign(DialogAlignment.Valign_Center).Build(); //v seznamu posunout nahoru
				}
				if (i < maxIndex) {//similarly with the last one
					dlg.LastTable[rowCntr, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSortDown).YPos(ButtonMetrics.D_SORTBUTTON_LINE_OFFSET).Id(6 * i + 12).Valign(DialogAlignment.Valign_Center).Build(); //v seznamu posunout dolu
				}
				rowCntr++;
			}
			if (dlg.LastTable.RowCount > rowCntr) {//there were less rows displayed than was prepared to display
				dlg.LastTable.RowCount = rowCntr; //(this can occur when displaying the last page of the dialog and not all items are to be shown)
			}

			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			args.SetTag(tkInputIds, inputIds);//store the id info

			//ted paging
			dlg.CreatePaging(cat.Contents.Count, firstiVal, 1);

			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH + 160, ButtonMetrics.D_BUTTON_WIDTH, 150, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(4).Build(); //start making
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Vyrobit vybrané").Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonBack).Id(5).Build(); //next time open here
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Pøíštì otevøít zde").Build();
			dlg.LastTable[0, 4] = GUTAText.Builder.TextLabel("Zrušit otevírání").Build();
			dlg.LastTable[0, 5] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(6).Build(); //discard saved position
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			CraftmenuCategory cat = (CraftmenuCategory) args[0];
			int btnNo = gr.PressedButton;
			if (btnNo < 10) {//basic buttons
				Gump newGi;
				Dictionary<CraftingSkillDef, CraftmenuCategory> lastPosDict;
				switch (btnNo) {
					case 0: //exit
						//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //one category back (if any)
						if (cat.Parent != null) {
							newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(cat.Parent));
							//DialogStacking.EnstackDialog(gi, newGi);
						} else {
							//gi.Cont.SysMessage("Nelze pøejít na nadøazenou kategorii, souèasná je v hierarchii nejvýše!");
							//DialogStacking.ResendAndRestackDialog(gi);
							//neni-li parent, prechazime na craftmenu categorie
							newGi = gi.Cont.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
						}
						break;
					case 2: //new subcategory
						var inputDialog = new CompiledInputDef("Nová subkategtorie", "Jméno", (sentTo, focus, filledText) => {
							CraftmenuCategory newCat = new CraftmenuCategory(filledText);
							cat.Contents.Add(newCat);
							newCat.Parent = cat;
						});

						newGi = gi.Cont.Dialog(inputDialog);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 3: //new items to add (target)
						lastPosDict = gi.Cont.GetTag(TkLastCat) as Dictionary<CraftingSkillDef, CraftmenuCategory>;
						if (lastPosDict == null) {
							lastPosDict = new Dictionary<CraftingSkillDef, CraftmenuCategory>();
							gi.Cont.SetTag(TkLastCat, lastPosDict);
						}
						lastPosDict[cat.CategorySkill] = cat; //store the category associated to this crafting skill
						((Player) gi.Cont).Target(SingletonScript<Targ_Craftmenu>.Instance, cat);
						DialogStacking.ClearDialogStack(gi.Cont); //dont show any dialogs now
						break;
					case 4: //start making
						//first we will prepare the list of Item-Count pairs to be made
						List<int> inputIds = (List<int>) args.GetTag(tkInputIds);//get the ids info
						SimpleQueue<CraftingSelection> selectionQueue = new SimpleQueue<CraftingSelection>();
						foreach (int id in inputIds) {
							int requestedCount = (int) gr.GetNumberResponse(id);//always integer number
							if (requestedCount > 0) {//non zero request for making, parse the line number
								int line = (id - 15) / 6; //input fields have IDs as 6*i + 15
								CraftmenuItem cmItm = (CraftmenuItem) cat.Contents[line];
								selectionQueue.Enqueue(new CraftingSelection(cmItm.itemDef, requestedCount));
							}
						}
						if (selectionQueue.Count > 0) {
							CraftingProcessPlugin.StartCrafting((Character) gi.Cont, new CraftingOrder(cat.CategorySkill, selectionQueue));
						}
						break;
					case 5: //openhere
						lastPosDict = gi.Cont.GetTag(TkLastCat) as Dictionary<CraftingSkillDef, CraftmenuCategory>;
						if (lastPosDict == null) {
							lastPosDict = new Dictionary<CraftingSkillDef, CraftmenuCategory>();
							gi.Cont.SetTag(TkLastCat, lastPosDict);
						}
						lastPosDict[cat.CategorySkill] = cat; //store the category associated to one crafting skill
						gi.Cont.SysMessage("Pozice výrobního menu pro skill " + cat.CategorySkill.Key + " nastavena na kategorii " + cat.FullName);
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 6: //stop opening here
						lastPosDict = gi.Cont.GetTag(TkLastCat) as Dictionary<CraftingSkillDef, CraftmenuCategory>;
						if (lastPosDict != null) {
							lastPosDict.Remove(cat.CategorySkill);
							if (lastPosDict.Count == 0) {
								gi.Cont.RemoveTag(TkLastCat); //no more last positions left
							}
						}
						gi.Cont.SysMessage("Nastavení poslední pozice výrobního menu pro skill " + cat.CategorySkill.Key + " zrušeno");
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 7: //go to settings page with this category
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu_Settings>.Instance, new DialogArgs(cat));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, cat.Contents.Count, 1)) {
			} else {
				int btnNumber = (btnNo - 10) % 6; //on one line we have numbers 10,11,12,13,14,15 next line is 16,17,18,19,20,21 etc.
				int line = (btnNo - (10 + btnNumber)) / 6; //e.g. 12 - (10+2) / 6 = 0; 21 - (10+3) / 6 = 8/6 = 1; 15 - (10 + 1) / 6 = 0 etc...
				ICraftmenuElement elem = cat.Contents[line];
				Gump newGi = null;
				switch (btnNumber) {
					case 0://show the subcategory contents
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs((CraftmenuCategory) elem));
						//DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 1://move element one position up in the list
						ICraftmenuElement prevSibling = cat.Contents[line - 1];
						cat.Contents[line - 1] = elem;
						cat.Contents[line] = prevSibling;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2://move element one position down
						ICraftmenuElement nextSibling = cat.Contents[line + 1];
						cat.Contents[line + 1] = elem;
						cat.Contents[line] = nextSibling;
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 3://show the Item Info
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu_ItemInfo>.Instance, new DialogArgs(((CraftmenuItem) elem).itemDef));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 4://remove from the list
						//put the craftmenu element to the user's backpack and remove from the list
						elem.Bounce(((Player) gi.Cont).Backpack);
						elem.Remove();
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			}
		}

		/// <summary>
		/// Display the craftmenu categories dialog. You can use some of the crafting skills SkillName as 
		/// a parameter to display directly the particular skill Craftmenu
		/// </summary>
		[SteamFunction]
		public static void Craftmenu(Character self, ScriptArgs args) {
			CraftingSkillDef sklDef;
			string skillName;
			int skillId;
			if (args == null || args.Argv == null || args.Argv.Length == 0) {
				Craftmenu(self);
			} else if ((sklDef = args.Argv[0] as CraftingSkillDef) != null) { //check if the parameter was a skill name
				Craftmenu(self, sklDef);
			} else if ((skillName = args.Argv[0] as string) != null) {
				Craftmenu(self, skillName);
			} else if (ConvertTools.TryConvertToInt32(args.Argv[0], out skillId)) {
				Craftmenu(self, (SkillName) skillId);
			}
		}

		public static void Craftmenu(Character self, CraftingSkillDef skill) {
			TagKey tkKey = TagKey.Acquire(tkCraftmenuLastposPrefix);
			Dictionary<CraftingSkillDef, CraftmenuCategory> lastPosDict = (Dictionary<CraftingSkillDef, CraftmenuCategory>) self.GetTag(tkKey);
			CraftmenuCategory prevCat = null;
			if (lastPosDict != null && skill != null) {
				prevCat = lastPosDict[skill];
			}
			if (prevCat != null) {//some bookmark for this skill exist... use it
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(prevCat));
			} else if (skill != null) {//no bookmark, open the main skill category
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[(SkillName) skill.Id]));
			} else { //default craftmenu opening (selection of skills)
				self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
			}
		}

		public static void Craftmenu(Character self, string skillName) {
			CraftingSkillDef sklDef = (CraftingSkillDef) AbstractSkillDef.GetByKey(skillName);
			Craftmenu(self, sklDef);
		}

		public static void Craftmenu(Character self, SkillName skillName) {
			CraftingSkillDef sklDef = (CraftingSkillDef) AbstractSkillDef.GetById((int) skillName);
			Craftmenu(self, sklDef);
		}

		public static void Craftmenu(Character self) {
			Craftmenu(self, (CraftingSkillDef) null);
		}
	}

	/// <summary>Dialog listing all available craftmenu categories (one for every crafting skill)</summary>
	public class D_CraftmenuCategories : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(240);
			dlg.SetLocation(70, 70);

			//nadpis tabulky
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Kategorie výroby").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dlg.MakeLastTableTransparent();

			GUTATable picTable = new GUTATable(8, ImprovedDialog.ICON_WIDTH, 0, ButtonMetrics.D_BUTTON_WIDTH);
			picTable.RowHeight = 40;
			picTable.InnerRowsDelimited = true;
			dlg.AddTable(picTable);
			dlg.LastTable[0, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Mortar).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Alchemy").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Id(1).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[1, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Anvil).Build();
			dlg.LastTable[1, 1] = GUTAText.Builder.TextLabel("Blacksmithing").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[1, 2] = GUTAButton.Builder.Id(2).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[2, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Bow).Build();
			dlg.LastTable[2, 1] = GUTAText.Builder.TextLabel("Bowcraft").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[2, 2] = GUTAButton.Builder.Id(3).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[3, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Saw).Build();
			dlg.LastTable[3, 1] = GUTAText.Builder.TextLabel("Carpentry").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[3, 2] = GUTAButton.Builder.Id(4).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[4, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Cake).Build();
			dlg.LastTable[4, 1] = GUTAText.Builder.TextLabel("Cooking").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[4, 2] = GUTAButton.Builder.Id(5).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[5, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Scroll).Build();
			dlg.LastTable[5, 1] = GUTAText.Builder.TextLabel("Inscription").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[5, 2] = GUTAButton.Builder.Id(6).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[6, 0] = GUTAImage.Builder.NamedGump(GumpIDs.SewingKit).Build();
			dlg.LastTable[6, 1] = GUTAText.Builder.TextLabel("Tailoring").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[6, 2] = GUTAButton.Builder.Id(7).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[7, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Tools).Build();
			dlg.LastTable[7, 1] = GUTAText.Builder.TextLabel("Tinkering").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[7, 2] = GUTAButton.Builder.Id(8).Valign(DialogAlignment.Valign_Center).Build();

			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Gump newGi = null;
			switch (gr.PressedButton) {
				case 0://exit
					DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
					break;
				case 1://alchemy
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Alchemy]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 2://blacksmithy
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Blacksmith]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 3://bowcraft
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Fletching]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 4://carpentry
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Carpentry]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 5://cooking
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Cooking]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 6://inscription
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Inscribe]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 7://tailoring
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Tailoring]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 8://tinkering
					newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[SkillName.Tinkering]));
					DialogStacking.EnstackDialog(gi, newGi);
					break;
			}
		}
	}
}