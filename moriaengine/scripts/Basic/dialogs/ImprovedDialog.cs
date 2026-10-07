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
	/// <summary>Wrapper class used to manage and create dialogs easily.</summary>
	public class ImprovedDialog {
		private static Dictionary<char, int> charsLength = new Dictionary<char, int>();

		/// <summary>Map of all main characters and their pixel length</summary>
		static ImprovedDialog() {
			charsLength.Add('a', 6); charsLength.Add('A', 8); charsLength.Add('1', 4);
			charsLength.Add('b', 6); charsLength.Add('B', 8); charsLength.Add('2', 8);
			charsLength.Add('c', 6); charsLength.Add('C', 8); charsLength.Add('3', 8);
			charsLength.Add('d', 6); charsLength.Add('D', 8); charsLength.Add('4', 8);
			charsLength.Add('e', 6); charsLength.Add('E', 7); charsLength.Add('5', 8);
			charsLength.Add('f', 6); charsLength.Add('F', 7); charsLength.Add('6', 8);
			charsLength.Add('g', 6); charsLength.Add('G', 8); charsLength.Add('7', 8);
			charsLength.Add('h', 6); charsLength.Add('H', 8); charsLength.Add('8', 8);
			charsLength.Add('i', 3); charsLength.Add('I', 3); charsLength.Add('9', 8);
			charsLength.Add('j', 6); charsLength.Add('J', 8); charsLength.Add('0', 8);
			charsLength.Add('k', 6); charsLength.Add('K', 8); charsLength.Add('!', 3);
			charsLength.Add('l', 3); charsLength.Add('L', 7); charsLength.Add('?', 7);
			charsLength.Add('m', 9); charsLength.Add('M', 10); charsLength.Add('(', 4);
			charsLength.Add('n', 6); charsLength.Add('N', 8); charsLength.Add(')', 4);
			charsLength.Add('o', 6); charsLength.Add('O', 8); charsLength.Add(' ', 8);
			charsLength.Add('p', 6); charsLength.Add('P', 8); charsLength.Add('_', 13);
			charsLength.Add('q', 6); charsLength.Add('Q', 9);
			charsLength.Add('r', 6); charsLength.Add('R', 8);
			charsLength.Add('s', 6); charsLength.Add('S', 8);
			charsLength.Add('t', 6); charsLength.Add('T', 7);
			charsLength.Add('u', 6); charsLength.Add('U', 8);
			charsLength.Add('v', 6); charsLength.Add('V', 8);
			charsLength.Add('w', 8); charsLength.Add('W', 12);
			charsLength.Add('x', 6); charsLength.Add('X', 8);
			charsLength.Add('y', 6); charsLength.Add('Y', 9);
			charsLength.Add('z', 6); charsLength.Add('Z', 8);
		}

		/// <summary>Compute the length of the text (mainly for dialog purposes)</summary>
		public static int TextLength(string text) {
			int retVal = 0;
			int val;
			for (int i = 0; i < text.Length; i++) {
				if (charsLength.TryGetValue(text[i], out val)) {
					retVal += val;
				} else {//default approx value
					retVal += D_CHARACTER_WIDTH;
				}
			}
			return retVal + 1; //last letter ends with its boundary (1 pixel)
		}

		/// <summary>The gump instance to send gump creating method calls to</summary>
		protected Gump instance;

		/// <summary>The deepest background of the dialog (the first GUTAMatrix where all other GumpComponents are</summary>
		private GUTAMatrix background;
		/// <summary>
		/// Last added table to the background GUTAMatrix - all GumpColumns will be added to this row until
		///  the next GUTATable is added
		/// </summary>
		protected GUTATable lastTable;
		/// <summary>
		/// Last added column - the column is placed automatically to the lastTable, all LeafGumpComponents
		///  will be added to this column until the next GUTAColumn is added
		/// </summary>
		private GUTAColumn lastColumn;

		/// <summary>Getter for the lastcolumn - may be needed from LScript if we have to operate with the sies or positions</summary>
		public GUTAColumn LastColumn {
			get {
				return this.lastColumn;
			}
		}

		/// <summary>
		/// Getter for the lasttable - may be needed from LScript if we have to operate with the sizes or positions or 
		/// if we want to add component directly to the desired column or row
		/// </summary>
		public GUTATable LastTable {
			get {
				return this.lastTable;
			}
		}

		/// <summary>The getter for the background table - usable for manual creating of the dialog structure</summary>
		public GUTAMatrix Background {
			get {
				return this.background;
			}
		}

		/// <summary>Create the wrapper instance and prepare set the instance variable for receiving dialog method calls</summary>
		public ImprovedDialog(Gump dialogInstance) {
			this.instance = dialogInstance;
		}

		/// <summary>Create the dialog background and set its size</summary>
		public void CreateBackground(int width) {
			this.background = new GUTAMatrix(this.instance, width);
		}

		/// <summary>
		/// Set the main table's position (all underlaying components will 
		/// be moved too). It is up to the scripter to make sure that this method is called on the correctly 
		/// set background
		/// </summary>
		public void SetLocation(int newX, int newY) {
			this.background.AdjustPosition(newX - this.background.XPos, newY - this.background.YPos);
		}

		/// <summary>
		/// Getter allowing us to access the underlaying GUTATable by the specified index 
		/// (usage: this.Table[i])
		/// </summary>
		public List<GUTAComponent> Table {
			get {
				return this.background.Components;
			}
		}

		/// <summary>Add a single GUTATable to the dialog and set is as 'last'</summary>
		public GUTATable AddTable(GUTATable table) {
			this.background.AddComponent(table);
			this.lastTable = table;
			return table;
		}

		private void AddLastColumn(GUTAColumn col) {
			if (this.lastTable == null || this.lastTable.Components.Count == 0) {
				throw new SEException("Cannot add a last column into the row which either does not exist or is empty");
			}
			//get the lastly added column
			//GUTAColumn lastCol = (GUTAColumn) lastTable.Components[lastTable.Components.Count-1];
			//the column will be added from the right side...
			col.IsLast = true;

			//GUTATable headingTable = (GUTATable)background.Components[background.Components.IndexOf(lastTable) - 1];
			//GUTAColumn newHeadsCol = new GUTAColumn(col.Width);
			//mark thew new paging columns as "last" 
			//newHeadsCol.IsLast = true;
			//col.IsLast = true;
			//add the paging column to the headings and to the data area
			//headingTable.Components[0].AddComponent(newHeadsCol);
			this.lastTable.Components[0].AddComponent(col);
			//unmark the previous "last" columns
			//((GUTAColumn) headingTable.Components[0].Components[headingTable.Components[0].Components.IndexOf(newHeadsCol) - 1]).IsLast = false;
			//((GUTAColumn) lastTable.Components[0].Components[lastTable.Components[0].Components.IndexOf(col) - 1]).IsLast = false;
			this.lastColumn = col;
		}

		/// <summary>
		/// Take the columns in the last row and copy their structure to the new row.
		/// They will take the new tables's rowCount. No underlaying children will be copied!
		/// </summary>
		public void CopyColsFromLastTable() {
			//take the last row (count-1 = this, new, row; count-2 = previous row)
			this.CopyColsFromTable(this.background.Components.Count - 2);
		}

		/// <summary>
		/// Take the columns from the specified row (start counting from 0) - 0th, 1st, 2nd etc.
		/// and copy their structure to the new row. They will get the new row's rowCount. 
		/// No underlaying columns children will be copied!
		/// </summary>
		public void CopyColsFromTable(int tableNumber) {
			GUTATable theTable = (GUTATable) this.background.Components[tableNumber];
			foreach (GUTAColumn col in theTable.Components[0].Components) { //use the first (mainly only) virtual Row
				//copy every column to the newly added (now empty) row
				GUTAColumn newCol = new GUTAColumn(col.Width);
				newCol.IsLast = col.IsLast;
				this.lastTable.Components[0].AddComponent(newCol);
			}
		}

		/// <summary>Take the last table, iterate through the columns and make them all transparent</summary>
		public void MakeLastTableTransparent() {
			this.lastTable.Transparent = true;
		}

		/// <summary>Last method to be called - it prints out the whole dialog</summary>
		public void WriteOut() {
			this.background.WriteComponent();
		}

		/// <summary>
		/// Takes care for the whole paging - gets number of items and the number of the 
		/// topmost Item on the current page (0 for first page, other for another pages). 
		/// We also specify the number of columns - not only single is now available for paging
		/// </summary>
		public void CreatePaging(int itemsCount, int firstNumber, int columnsCount) {
			if (itemsCount <= PAGE_ROWS * columnsCount) {//do we need paging at all...?
				//...no
				return;
			}
			int lastNumber = Math.Min(firstNumber + PAGE_ROWS * columnsCount, itemsCount);
			int pagesCount = (int) Math.Ceiling((double) itemsCount / (PAGE_ROWS * columnsCount));
			//first index on the page is a multiple of number of rows per page...
			int actualPage = (firstNumber / (PAGE_ROWS * columnsCount)) + 1;

			bool prevNextColumnAdded = false; //indicator of navigating column
			if (actualPage > 1) {
				this.AddLastColumn(new GUTAColumn(ButtonMetrics.D_BUTTON_PREVNEXT_WIDTH));
				this.lastColumn.AddComponent(GUTAButton.Builder.Type(LeafComponentTypes.ButtonPrev).Id(ID_PREV_BUTTON).Build()); //prev
				prevNextColumnAdded = true; //the column has been created				
			}
			if (actualPage < pagesCount) { //there will be next page
				if (!prevNextColumnAdded) { //the navigating column does not exist (e.g. we are on the 1st page)
					this.AddLastColumn(new GUTAColumn(ButtonMetrics.D_BUTTON_PREVNEXT_WIDTH));
				}
				this.lastColumn.AddComponent(GUTAButton.Builder.Type(LeafComponentTypes.ButtonNext).YPos(this.lastColumn.Height - 21).Id(ID_NEXT_BUTTON).Build()); //next
			}
			this.MakeLastTableTransparent(); //the row where we added the navigating column
			//add a navigating bar to the bottom (editable field for jumping to the selected page)
			//it looks like this: "Stránka |__| / 23. <GOPAGE>  where |__| is editable field
			//and <GOPAGE> is confirming button that jumps to the written page.
			GUTATable storedLastTable = this.lastTable; //store these two things :)
			GUTAColumn storedLastColumn = this.lastColumn;
			this.AddTable(new GUTATable(1, 0));
			this.lastTable[0, 0] = GUTAText.Builder.TextLabel("Stránka").Build();
			//type if input,x,y,ID, width, height, prescribed text
			this.lastTable[0, 0] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).XPos(65).Id(ID_PAGE_NO_INPUT).Width(30).Text(actualPage.ToString()).Build();
			this.lastTable[0, 0] = GUTAText.Builder.TextLabel("/" + pagesCount).XPos(95).Build();
			this.lastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).XPos(135).Id(ID_JUMP_PAGE_BUTTON).Build();
			this.MakeLastTableTransparent(); //newly created row
			//restore the last components
			this.lastTable = storedLastTable;
			this.lastColumn = storedLastColumn;
		}

		/// <summary>Tag key using for holding information about paging actual item index for dialogs.</summary>
		public static readonly TagKey pagingIndexTK = TagKey.Acquire("_paging_index_");

		/// <summary>
		/// Look if the paging buttons has been pressed and if so, handle the actions as 
		/// a normal OnResponse method, otherwise return to the dialog OnResponse method 
		/// and continue there. 
		/// gi - Gump from the OnResponse method 
		/// gr - GumpResponse object from the OnResponse method 
		/// columnsCount - number of columns per page (each containing PAGES_ROWS number of rows) 
		/// return true or false if the button was one of the paging buttons or not
		/// </summary>
		public static bool PagingButtonsHandled(Gump gi, GumpResponse gr, int itemsCount, int columnsCount) {
			//stacked dialog item (it is necessary to have it here so it must be set in the 
			//dialog construct method!)
			DialogArgs args = gi.InputArgs;//arguments of the dialog			
			bool pagingHandled = false; //indicator if the pressed btton was the paging one.
			switch (gr.PressedButton) {
				case ID_PREV_BUTTON:
					args.SetTag(pagingIndexTK, TagMath.IGetTag(args, pagingIndexTK) - (PAGE_ROWS * columnsCount));
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
				case ID_NEXT_BUTTON:
					args.SetTag(pagingIndexTK, TagMath.IGetTag(args, pagingIndexTK) + (PAGE_ROWS * columnsCount));
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
				case ID_JUMP_PAGE_BUTTON:
					//get the selected page number (absolute value - make it a bit idiot proof :) )
					int selectedPage = (int) gr.GetNumberResponse(ID_PAGE_NO_INPUT);
					if (selectedPage < 1) {
						//idiot proof adjustment
						gi.Cont.WriteLine("Nepovolené èíslo stránky - povoleny jen kladné hodnoty");
						selectedPage = 1;
					}
					//count the index of the first item
					int countedFirstIndex = (selectedPage - 1) * (PAGE_ROWS * columnsCount);
					if (countedFirstIndex > itemsCount) { //get the last page
						int lastPage = (itemsCount / (PAGE_ROWS * columnsCount)) + 1; //(int) casted last page number
						countedFirstIndex = (lastPage - 1) * (PAGE_ROWS * columnsCount); //counted fist item on the last page
					} //otherwise it is properly set to the first item on the page
					args.SetTag(pagingIndexTK, countedFirstIndex);//set the index of the first item
					DialogStacking.ResendAndRestackDialog(gi);
					pagingHandled = true;
					break;
			}
			return pagingHandled;
		}

		/// <summary>
		/// This is the paging handler used in LScript where no GumpResponse is present
		/// sentTo - player who has seen the dialog; buttNo - pressed button; selPageInpt - filled 
		/// number of page to jump to (if any, used only when 'jump page button' was pressed, 
		/// "otherwise is null); 
		/// pagingArgumentNo - index to the paramaeters field where the paging info is stored; 
		/// columnsCount - number of columns per page (each containing PAGES_ROWS number of rows) 
		/// itemsCount - total count of diplayed items in the list
		/// </summary>
		public static bool PagingButtonsHandled(Gump actualGi, int buttNo, int selPageInpt, int itemsCount, int columnsCount) {
			//stacked dialog item (it is necessary to have it here so it must be set in the 
			bool pagingHandled = false; //indicator if the pressed btton was the paging one.
			DialogArgs args = actualGi.InputArgs;
			switch (buttNo) {
				case ID_PREV_BUTTON:
					args.SetTag(pagingIndexTK, TagMath.IGetTag(args, pagingIndexTK) - (PAGE_ROWS * columnsCount));
					DialogStacking.ResendAndRestackDialog(actualGi);
					pagingHandled = true;
					break;
				case ID_NEXT_BUTTON:
					args.SetTag(pagingIndexTK, TagMath.IGetTag(args, pagingIndexTK) + (PAGE_ROWS * columnsCount));
					DialogStacking.ResendAndRestackDialog(actualGi);
					pagingHandled = true;
					break;
				case ID_JUMP_PAGE_BUTTON:
					//get the selected page number (absolute value - make it a bit idiot proof :) )
					int selectedPage = selPageInpt;
					if (selectedPage < 1) {
						//idiot proof adjustment
						actualGi.Cont.WriteLine("Nepovolené èíslo stránky - povoleny jen kladné hodnoty");
						selectedPage = 1;
					}
					//count the index of the first item
					int countedFirstIndex = (selectedPage - 1) * (PAGE_ROWS * columnsCount);
					if (countedFirstIndex > itemsCount) { //get the last page
						int lastPage = (itemsCount / (PAGE_ROWS * columnsCount)) + 1; //(int) casted last page number
						countedFirstIndex = (lastPage - 1) * (PAGE_ROWS * columnsCount); //counted fist item on the last page
					} //otherwise it is properly set to the first item on the page
					args.SetTag(pagingIndexTK, countedFirstIndex);
					DialogStacking.ResendAndRestackDialog(actualGi);
					pagingHandled = true;
					break;
			}
			return pagingHandled;
		}

		/// <summary>Dialog constants</summary>
		public const int D_DEFAULT_DIALOG_BORDERS = 9250; //grey borders
		public const int D_DEFAULT_DIALOG_BACKGROUND = 9354; //beige background

		public const int D_DEFAULT_ROW_BORDERS = 9254; //grey background
		public const int D_DEFAULT_ROW_BACKGROUND = 9354; //beige background

		public const int D_DEFAULT_COL_BACKGROUND = 9254; //grey background

		public const int D_DEFAULT_INPUT_BORDERS = 9270; //dark grey borders
		public const int D_DEFAULT_INPUT_BACKGROUND = 9274; //dark grey background

		public const int D_ROW_HEIGHT = 19;
		public const int D_SPACE = 3;
		/// <summary>Space for delimiting the rows one from the above one</summary>
		public const int D_ROW_SPACE = 2;
		/// <summary>Space for delimiting the columns in the inner row</summary>
		public const int D_COL_SPACE = 1;
		public const int D_BORDER = 10;
		public const int D_OFFSET = 5;

		public const int D_CHARACTER_WIDTH = 6; //approximate width of the normal character
		public const int D_CHARACTER_HEIGHT = 18; //approximate height of the normal character
		public const int D_TEXT_HEIGHT = 22; //approx height of the text field (character + some automatical offset)

		/// <summary>Number of pixels of which the label from the label-value fields will be indented from the left</summary>
		public static int ITEM_INDENT = 20;
		/// <summary>
		/// Number of pixels of which the editbox from the label-value fields will be indented from the left 
		/// (so there is enough space for the label
		/// </summary>
		public static int INPUT_INDENT = 75;

		/// <summary>Number of normal rows on the various dialog pages (when paging is used)</summary>
		public const int PAGE_ROWS = 20;

		/// <summary>Empirically determined icon dimensions (mostly can be used)</summary>
		public const int ICON_WIDTH = 43;
		public const int ICON_HEIGHT = 50;

		/// <summary>Good delmiting space for the icon in one row (from the top and bottom row border)</summary>
		public const int D_ICON_SPACE = 2;

		/// <summary>Page navigating buttons (constant IDs, different enough from those common used :))</summary>
		public const int ID_PREV_BUTTON = 98765;
		public const int ID_NEXT_BUTTON = 98764;
		public const int ID_JUMP_PAGE_BUTTON = 98763;
		public const int ID_PAGE_NO_INPUT = 28762;

	}
}