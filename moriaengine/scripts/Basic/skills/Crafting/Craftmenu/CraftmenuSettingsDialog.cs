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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>
	/// Craftmenu for setting of the particular part of the craftmenu - allows setting of basic and most used properties 
	/// such as resources, skillmake or weight
	/// </summary>
	public class D_Craftmenu_Settings : CompiledGumpDef {
		private static int width = 600;
		private static readonly TagKey previousItemdefValsTK = TagKey.Acquire("_previous_itemdef_values_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			CraftmenuCategory cat = (CraftmenuCategory) args[0]; //this is a CraftmenuCategory for setting

			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK); //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, cat.Contents.Count); //nejvyssi index na strance

			int innerWidth = (width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE);

			dlg.CreateBackground(width);
			dlg.SetLocation(80, 50);

			//nadpis dialogu, cudliky na back a zruseni
			dlg.AddTable(new GUTATable(1, innerWidth - 3 * ButtonMetrics.D_BUTTON_WIDTH - 5 * ImprovedDialog.D_COL_SPACE, ButtonMetrics.D_BUTTON_WIDTH, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Nastavení kategorie výroby: " + cat.FullName).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonBack).Id(1).Build(); //one category back button
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(2).Build(); //craftmenu button
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dlg.MakeLastTableTransparent();

			//seznam polozek
			GUTATable mainTable = new GUTATable();
			dlg.AddTable(mainTable);
			dlg.MakeLastTableTransparent();

			int rowCntr = 0;
			//precompute some common values:
			int catRowHgth = Math.Max(ButtonMetrics.D_BUTTON_HEIGHT, GumpDimensions.Table[(int) GumpIDs.Pouch].Height + 2 * ImprovedDialog.D_ICON_SPACE);
			int offset = ImprovedDialog.TextLength("Weight:  "); //offset for the weight edit field
			int wghtLen = ImprovedDialog.TextLength("1000"); //just some number length (4 numbers) for the weight input field
			string resources = "";
			string skillmake = "";
			string weight = "";
			for (int i = firstiVal; i < imax; i++) { //pojedeme pres polozky kategorie ale jen v danem rozsahu co se vejde...
				ICraftmenuElement elem = cat.Contents[i];
				if (elem.IsCategory) { //for category element we will just add a category navigating link to the dlg.
					GUTARow oneRow = new GUTARow(1, ButtonMetrics.D_BUTTON_WIDTH, ImprovedDialog.ICON_WIDTH, 0);
					mainTable.AddRow(oneRow);
					oneRow.RowHeight = catRowHgth;
					oneRow[0, 0] = GUTAButton.Builder.Id(5 * i + 10).Valign(DialogAlignment.Valign_Center).Build();
					oneRow[0, 1] = GUTAImage.Builder.NamedGump(GumpIDs.Pouch).Build();
					oneRow[0, 2] = GUTAText.Builder.Text(elem.Name).Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Center).Build();
				} else {
					CraftmenuItem itm = (CraftmenuItem) elem;
					GUTARow oneRow = new GUTARow(1, ButtonMetrics.D_BUTTON_WIDTH, 80, 0);
					oneRow.InTableSeparated = false; //the row with item icon won't be separated from the rest of the item info
					mainTable.AddRow(oneRow);
					int itmPicRowHgth = Math.Max(ButtonMetrics.D_BUTTON_HEIGHT, GumpDimensions.Table[itm.itemDef.Model].Height + 2 * ImprovedDialog.D_ICON_SPACE);
					oneRow.RowHeight = itmPicRowHgth;
					oneRow[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(5 * i + 12).Valign(DialogAlignment.Valign_Top).Build(); //link to the itemdef info...
					oneRow[0, 1] = GUTAImage.Builder.Gump(itm.itemDef.Model).Color(itm.itemDef.Color).Build();
					oneRow[0, 2] = GUTAText.Builder.Text(itm.Name + "(" + itm.itemDef.PrettyDefname + ")   defname: " + itm.itemDef.Defname).Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Center).Build();
					GUTARow secRow = new GUTARow(3, ButtonMetrics.D_BUTTON_WIDTH, 80, 0);
					secRow.InnerRowsDelimited = true; //lines in the row will be separated
					mainTable.AddRow(secRow);
					resources = ObjectSaver.Save(itm.itemDef.Resources);
					skillmake = ObjectSaver.Save(itm.itemDef.SkillMake);
					weight = itm.itemDef.Weight.ToString();
					secRow[0, 2] = GUTAText.Builder.TextLabel("Váha:").Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Bottom).Build();
					secRow[0, 2] = GUTAText.Builder.Text(weight).Valign(DialogAlignment.Valign_Bottom).XPos(offset).Build();
					secRow[1, 1] = GUTAText.Builder.TextLabel("Resources").Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Bottom).Build();
					secRow[1, 2] = GUTAText.Builder.Text(resources).Build();
					secRow[2, 1] = GUTAText.Builder.TextLabel("Skillmake").Align(DialogAlignment.Align_Left).Valign(DialogAlignment.Valign_Bottom).Build();
					secRow[2, 2] = GUTAText.Builder.Text(skillmake).Build();
				}
				rowCntr++;
			}

			//ted paging
			dlg.CreatePaging(cat.Contents.Count, firstiVal, 1);

			//dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			//dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(3).Build(); //store changes
			//dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Nastavit").Build();
			//dlg.MakeLastTableTransparent();

			dlg.AddTable(new GUTATable(1, 0));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vlastnosti lze mìnit pomocí ikony v levé èásti dialogu").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			CraftmenuCategory cat = (CraftmenuCategory) args[0];
			int btnNo = gr.PressedButton;
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK); //prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, cat.Contents.Count); //nejvyssi index na strance

			if (btnNo < 10) {//basic buttons
				Gump newGi;
				switch (btnNo) {
					case 0: //exit
						//DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //one category back (if any)
						if (cat.Parent != null) {
							newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu_Settings>.Instance, new DialogArgs(cat.Parent));
							//DialogStacking.EnstackDialog(gi, newGi);
						} else {
							gi.Cont.SysMessage("Nelze pøejít na nadøazenou kategorii, souèasná je v hierarchii nejvýše!");
							DialogStacking.ResendAndRestackDialog(gi);
						}
						break;
					case 2: //back to craftmenu withe the selected category (no stacking)
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(cat));
						break;
					case 3: //store the changes on page
						for (int i = firstiVal; i < imax; i++) {

							ICraftmenuElement elem = cat.Contents[i];
							CraftmenuItem itm = elem as CraftmenuItem;
							if (itm != null) {//set this item's values (but only for items, leave categories)
								ResourcesList newRes = null;
								ResourcesList newSkillmake = null;
								decimal newWeight = 0;
								try {
									newWeight = gr.GetNumberResponse(5 * i + 11);
									//newRes = (ResourcesList) ObjectSaver.Load(gr.ResponseTexts[5 * i + 13].Text);
									//newSkillmake = (ResourcesList) ObjectSaver.Load(gr.ResponseTexts[5 * i + 14].Text);
								} catch { //any problem? - nothing will be set here !
									newRes = null;
									newSkillmake = null;
								}
								if (newRes != null && newSkillmake != null) {
									//set new values only all of them
									itm.itemDef.Weight = (float) newWeight;
									itm.itemDef.Resources = newRes;
									itm.itemDef.SkillMake = newSkillmake;
								}
							}
						}
						DialogStacking.ResendAndRestackDialog(gi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, cat.Contents.Count, 1)) {
			} else {
				int btnNumber = (btnNo - 10) % 5; //on one line we have numbers 10,11,12,13,14 next line is 15,16,17,18 etc.
				int line = (btnNo - (10 + btnNumber)) / 5;
				ICraftmenuElement elem = cat.Contents[line];
				Gump newGi = null;
				switch (btnNumber) {
					case 0://show the subcategory contents
						newGi = gi.Cont.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs((CraftmenuCategory) elem));
						//DialogStacking.EnstackDialog(gi, newGi);
						break;
					case 2://display an info dialog on the selected (craftmenu)item(def) - allows changes
						newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(((CraftmenuItem) elem).itemDef));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}
	}
}