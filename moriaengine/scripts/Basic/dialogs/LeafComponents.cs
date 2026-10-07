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

using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {
	public abstract class Builder<T> where T : LeafGUTAComponent {
		public abstract T Build();
	}

	/// <summary>Leaf GUTA components cannot have any children, these are e.g buttons, inputs, texts etc.</summary>
	public abstract class LeafGUTAComponent : GUTAComponent {
		/// <summary>This is the ID many gump items have - buttons number, input entries number...</summary>
		protected int id;

		/// <summary>Which row in the column this component lies in?</summary>
		protected int columnRow;

		/// <summary>Adding any children to the leaf is prohibited...</summary>
		internal sealed override void AddComponent(GUTAComponent child) {
			throw new GUTAComponentCannotBeExtendedException("GUTAcomponent " + this.GetType() + " cannot have any children");
		}
	}

	/// <summary>A staic class holding all necessary button constants</summary>
	public static class ButtonMetrics {
		public const int D_BUTTON_WIDTH = 31;
		public const int D_BUTTON_HEIGHT = 22;
		public const int D_CHECKBOX_WIDTH = 19;
		public const int D_CHECKBOX_HEIGHT = 20;
		public const int D_RADIO_WIDTH = 21;
		public const int D_RADIO_HEIGHT = 21;
		public const int D_BUTTON_PREVNEXT_WIDTH = 16;
		public const int D_BUTTON_PREVNEXT_HEIGHT = 21;

		/// <summary>Number of pixels to move the button in the line so it is in the middle</summary>
		public const int D_SORTBUTTON_LINE_OFFSET = 9;
		/// <summary>Number of pixels to move the text to the right so it is readable next to the sort buttons</summary>
		public const int D_SORTBUTTON_COL_OFFSET = 11;

		//not used and not necessary so far...
		///// <summary>The TiledButton component class - it handles the tiled button writing to the client</summary>
		//public class TiledButton : LeafGUTAComponent {
		//    protected static string stringDescription = "Tiled Button";

		//    private ButtonGump gumps;
		//    private int page = 0;
		//    private bool active = true;

		//    private int itemID, hue;

		//    /// <summary>Button vertical alignment</summary>
		//    protected DialogAlignment valign = DialogAlignment.Valign_Top;
		//    /// <summary>Button horizontal alignment</summary>
		//    protected DialogAlignment align = DialogAlignment.Align_Left;

		//    internal TiledButton(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page, DialogAlignment valign, int itemID, int hue, int width, int height) {
		//        this.id = id;
		//        this.xPos = xPos;
		//        this.yPos = yPos;
		//        this.gumps = gumps;
		//        this.active = active;
		//        this.page = page;
		//        this.valign = valign;

		//        this.itemID = itemID;
		//        this.width = width;
		//        this.height = height;
		//        this.hue = hue;
		//    }

		//    /// <summary>Basic constructor awaiting the buttons ID and string value of UP and DOWN gump graphic ids</summary>
		//    internal TiledButton(int id, int xPos, int yPos, ButtonGump gumps, bool active, int page, DialogAlignment valign, GumpIDs itemID, Hues hue, int width, int height)
		//        :
		//        this(id, xPos, yPos, gumps, active, page, valign, (int) itemID, (int) hue, width, height) {
		//    }

		//    <summary>When added, we must recompute the TiledButtons absolute position in the dialog (we " +
		//            " were provided only relative positions")]
		//    protected override void OnBeforeWrite(GUTAComponent parent) {
		//        //set the level
		//        level = parent.Level + 1;

		//        //get the grandparent (GUTARow) (parent is GUTAColumn!)
		//        GUTARow grandpa = (GUTARow) parent.Parent;
		//        //set the column row (counted from the relative position and the grandpa's inner-row height)
		//        columnRow = xPos / grandpa.RowHeight;

		//        int valignOffset = 0;
		//        switch (valign) {
		//            case DialogAlignment.Valign_Center:
		//                valignOffset = grandpa.RowHeight / 2 - height / 2; //moves the button to the middle of the column
		//                break;
		//            case DialogAlignment.Valign_Bottom:
		//                valignOffset = grandpa.RowHeight - height; //moves the button to the bottom
		//                break;
		//        }
		//        //no space here, the used button gumps have themselves some space...
		//        xPos += parent.XPos;
		//        yPos += parent.YPos + valignOffset;
		//    }

		//    /// <summary>Simply write the tiled button (send the method request to the underlaying gump)</summary>
		//    internal override void WriteComponent() {
		//        gump.AddTiledButton(xPos, yPos, gumps.GumpDown, gumps.GumpUp, active, page, id, itemID, hue, width, height);
		//    }

		//    public override string ToString() {
		//        string linesTabsOffset = "\r\n"; //at least one row
		//        //add as much rows as is the row which this item lies in
		//        for (int i = 0; i < columnRow; i++) {
		//            linesTabsOffset += "\r\n";
		//        }
		//        for (int i = 0; i < level; i++) {
		//            linesTabsOffset += "\t";
		//        }
		//        return linesTabsOffset + "->" + stringDescription;
		//    }
		//}
	}

	/// <summary>The Button component class - it handles the button writing to the client</summary>
	public class GUTAButton : LeafGUTAComponent {
		protected static string stringDescription = "Button";

		protected static Dictionary<LeafComponentTypes, ButtonGump> buttonGumps = new Dictionary<LeafComponentTypes, ButtonGump>();

		static GUTAButton() {
			//0fb1, 0fb3 Cross button
			buttonGumps.Add(LeafComponentTypes.ButtonCross, new ButtonGump(4017, 4019));
			//0fb7, 0fb9 OK button
			buttonGumps.Add(LeafComponentTypes.ButtonOK, new ButtonGump(4023, 4025));
			//0fa5, 0fa7 Tick button
			buttonGumps.Add(LeafComponentTypes.ButtonTick, new ButtonGump(4005, 4007));
			//0fae, 0fb0 Back button
			buttonGumps.Add(LeafComponentTypes.ButtonBack, new ButtonGump(4014, 4016));
			//0fab, 0fad Paper button
			buttonGumps.Add(LeafComponentTypes.ButtonPaper, new ButtonGump(4011, 4013));
			//0fbd, 0fbf Send button
			buttonGumps.Add(LeafComponentTypes.ButtonSend, new ButtonGump(4029, 4031));
			//0fa, 0fb Previous page button
			buttonGumps.Add(LeafComponentTypes.ButtonPrev, new ButtonGump(250, 251));
			//0fc, 0fd Next page button
			buttonGumps.Add(LeafComponentTypes.ButtonNext, new ButtonGump(252, 253));
			//0fa8, 0faa People button
			buttonGumps.Add(LeafComponentTypes.ButtonPeople, new ButtonGump(4008, 4010));
			//0983, 0984 Sort up button
			buttonGumps.Add(LeafComponentTypes.ButtonSortUp, new ButtonGump(2435, 2436));
			//0985, 0986 Sort down button
			buttonGumps.Add(LeafComponentTypes.ButtonSortDown, new ButtonGump(2437, 2438));
			//0fb4, 0fb6 Crossed circle button 
			buttonGumps.Add(LeafComponentTypes.ButtonNoOperation, new ButtonGump(4020, 4022));
			//9905,9904 Triangle button pointing right
			buttonGumps.Add(LeafComponentTypes.ButtonTriangle, new ButtonGump(9905, 9904));

			//0d2, 0d3 Checkbox (unchecked, checked)
			buttonGumps.Add(LeafComponentTypes.CheckBox, new ButtonGump(210, 211));
			//0d0, 0d1 Radiobutton (unselected, selected)
			buttonGumps.Add(LeafComponentTypes.RadioButton, new ButtonGump(208, 209));
		}

		/// <summary>
		/// Flyweight struct carrying info about the two button gumps (pressed and released)
		/// it will be used when building the dialog buttons for storing info about the gumps.
		/// </summary>
		protected struct ButtonGump {
			private int gumpUp, //also unchecked checkbox and unselected radiobutton
						gumpDown; //also checked checkbox and selected radiobutton

			internal ButtonGump(int gumpUp, int gumpDown) {
				this.gumpUp = gumpUp;
				this.gumpDown = gumpDown;
			}

			internal int GumpUp {
				get {
					return this.gumpUp;
				}
			}

			internal int GumpDown {
				get {
					return this.gumpDown;
				}
			}
		}

		protected ButtonGump gumps;
		private int page;
		private bool active = true;

		/// <summary>Button vertical alignment</summary>
		protected DialogAlignment valign = DialogAlignment.Valign_Top;

		public static ButtonBuilder Builder {
			get {
				return new ButtonBuilder();
			}
		}

		/// <summary>Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class ButtonBuilder : Builder<GUTAButton> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal int id;
			internal DialogAlignment valign = DialogAlignment.Valign_Top;
			internal bool active = true;
			internal int page;
			internal LeafComponentTypes type = LeafComponentTypes.ButtonTick;

			internal ButtonBuilder() {
			}

			/// <summary>Set the button's relative X position</summary>
			public ButtonBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the button's relative Y position</summary>
			public ButtonBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the button's ID</summary>
			public ButtonBuilder Id(int val) {
				this.id = val;
				return this;
			}

			/// <summary>Set the button's state (active = clickable)</summary>
			public ButtonBuilder Active(bool val) {
				this.active = val;
				return this;
			}

			/// <summary>Set the button's referenced page</summary>
			public ButtonBuilder Page(int val) {
				this.page = val;
				return this;
			}

			/// <summary>Set the button's up and down graphics (using enumeration of types)</summary>
			public ButtonBuilder Type(LeafComponentTypes val) {
				this.type = val;
				return this;
			}

			/// <summary>Set the button's vertical algiment</summary>
			public ButtonBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAButton field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Create the GUTAButton instance</summary>
			public override GUTAButton Build() {
				GUTAButton retVal = new GUTAButton(this);
				return retVal;
			}
		}

		protected GUTAButton() {
			//constructor for children
		}

		private GUTAButton(ButtonBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.id = builder.id;
			this.gumps = buttonGumps[builder.type];
			this.active = builder.active;
			this.page = builder.page;
			this.valign = builder.valign;
		}

		/// <summary>
		/// When added, we must recompute the Buttons absolute position in the dialog (we 
		/// were provided only relative positions
		/// </summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			int valignOffset = 0;
			switch (this.valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_BUTTON_HEIGHT / 2; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_BUTTON_HEIGHT; //moves the button to the bottom
					break;
			}
			//no space here, the used button gumps have themselves some space...
			this.xPos += parent.XPos;
			this.yPos += parent.YPos + valignOffset;
		}

		/// <summary>Simply write the button (send the method request to the underlaying gump)</summary>
		internal override void WriteComponent() {
			this.gump.AddButton(this.xPos, this.yPos, this.gumps.GumpDown, this.gumps.GumpUp, this.active, 0, this.id);
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n"; //at least one row
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < this.columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < this.level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->" + stringDescription;
		}
	}

	/// <summary>Create a nice small checkbox</summary>
	public class GUTACheckBox : GUTAButton {
		protected new static string stringDescription = "CheckBox";

		private bool isChecked;
		/// <summary>Checkbox's horizontal alignment</summary>
		private DialogAlignment align = DialogAlignment.Align_Center;

		public new static CheckBoxBuilder Builder {
			get {
				return new CheckBoxBuilder();
			}
		}

		/// <summary>Builder class for the Checkbox LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class CheckBoxBuilder : Builder<GUTACheckBox> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal int id;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal bool isChecked;
			internal LeafComponentTypes type = LeafComponentTypes.CheckBox;

			internal CheckBoxBuilder() {
			}

			/// <summary>Set the checkbox's relative X position</summary>
			public CheckBoxBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the checkbox's relative Y position</summary>
			public CheckBoxBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the checkbox's ID</summary>
			public CheckBoxBuilder Id(int val) {
				this.id = val;
				return this;
			}

			/// <summary>Set the checkbox's state (checked/unchecked)</summary>
			public CheckBoxBuilder Checked(bool val) {
				this.isChecked = val;
				return this;
			}

			/// <summary>Set the checkbox's up and down graphics (using enumeration of types)</summary>
			public CheckBoxBuilder Type(LeafComponentTypes val) {
				this.type = val;
				return this;
			}

			/// <summary>Set the checkbox's vertical algiment</summary>
			public CheckBoxBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTACheckBox field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Set the checkbox's horizontal algiment</summary>
			public CheckBoxBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTACheckBox field: " + val));
				}
				this.align = val;
				return this;
			}

			/// <summary>Create the GUTACheckBox instance</summary>
			public override GUTACheckBox Build() {
				GUTACheckBox retVal = new GUTACheckBox(this);
				return retVal;
			}
		}

		private GUTACheckBox(CheckBoxBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.gumps = buttonGumps[builder.type];
			this.isChecked = builder.isChecked;
			this.id = builder.id;
			this.valign = builder.valign;
			this.align = builder.align;
		}

		/// <summary>
		/// When added, we must recompute the Checkbox's absolute position in the dialog (we 
		/// were provided only relative positions
		/// </summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			int valignOffset = 0;
			int alignOffset = 0;

			switch (this.valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_CHECKBOX_HEIGHT / 2 + 1; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_CHECKBOX_HEIGHT; //moves the button to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (this.align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - ButtonMetrics.D_CHECKBOX_WIDTH / 2; //moves the text to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - ButtonMetrics.D_CHECKBOX_WIDTH;
					break;
			}
			//no space here, the used button gumps have themselves some space...
			this.xPos += parent.XPos + alignOffset;
			this.yPos += parent.YPos + valignOffset;
		}

		/// <summary>Simply call the gumps method for writing the checkbox</summary>
		internal override void WriteComponent() {
			//unchecked!!!,    checked !!!
			this.gump.AddCheckBox(this.xPos, this.yPos, this.gumps.GumpUp, this.gumps.GumpDown, this.isChecked, this.id);
		}
	}

	/// <summary>A class representing the radio button</summary>
	public class GUTARadioButton : GUTAButton {
		protected new static string stringDescription = "Radio";

		/// <summary>Radiobutton's horizontal alignment</summary>
		private DialogAlignment align = DialogAlignment.Align_Center;
		private bool isChecked;

		public new static RadioBuilder Builder {
			get {
				return new RadioBuilder();
			}
		}

		/// <summary>Builder class for the Radiobutton LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class RadioBuilder : Builder<GUTARadioButton> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal int id;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal bool isChecked;
			internal LeafComponentTypes type = LeafComponentTypes.RadioButton;

			internal RadioBuilder() {
			}

			/// <summary>Set the radiobutton's relative X position</summary>
			public RadioBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the radiobutton's relative Y position</summary>
			public RadioBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the radiobutton's ID</summary>
			public RadioBuilder Id(int val) {
				this.id = val;
				return this;
			}

			/// <summary>Set the radiobutton's state (checked/unchecked)</summary>
			public RadioBuilder Checked(bool val) {
				this.isChecked = val;
				return this;
			}

			/// <summary>Set the radiobutton's checked/unchecked graphics (using enumeration of types)</summary>
			public RadioBuilder Type(LeafComponentTypes val) {
				this.type = val;
				return this;
			}

			/// <summary>Set the radiobutton's vertical algiment</summary>
			public RadioBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTARadioButton field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Set the radiobutton's horizontal algiment</summary>
			public RadioBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTARadioButton field: " + val));
				}
				this.align = val;
				return this;
			}

			/// <summary>Create the GUTARadioButton instance</summary>
			public override GUTARadioButton Build() {
				GUTARadioButton retVal = new GUTARadioButton(this);
				return retVal;
			}
		}

		private GUTARadioButton(RadioBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.gumps = buttonGumps[builder.type];
			this.isChecked = builder.isChecked;
			this.id = builder.id;
			this.valign = builder.valign;
			this.align = builder.align;
		}

		/// <summary>
		/// When added, we must recompute the Radiobutton's absolute position in the dialog (we 
		/// were provided only relative positions
		/// </summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			int valignOffset = 0;
			int alignOffset = 0;

			switch (this.valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - ButtonMetrics.D_RADIO_HEIGHT / 2 + 1; //moves the button to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - ButtonMetrics.D_RADIO_HEIGHT; //moves the button to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (this.align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - ButtonMetrics.D_RADIO_WIDTH / 2; //moves the text to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - ButtonMetrics.D_RADIO_WIDTH;
					break;
			}
			//no space here, the used button gumps have themselves some space...
			this.xPos += parent.XPos + alignOffset;
			this.yPos += parent.YPos + valignOffset;
		}

		/// <summary>Simply call the gumps method for writing the radiobutton</summary>
		internal override void WriteComponent() {
			//unselected!!!, selected!!!
			this.gump.AddRadio(this.xPos, this.yPos, this.gumps.GumpUp, this.gumps.GumpDown, this.isChecked, this.id);
		}
	}

	/// <summary>The Button component class - it handles the button writing to the client</summary>
	public class GUTAInput : LeafGUTAComponent {
		private LeafComponentTypes type;
		/// <summary>We have either ID of the used (pre-)text, or the text string itself</summary>
		private int textId;
		private string text;
		/// <summary>The text hue in the input field - if not specified, the default will be used.</summary>
		private int textHue;

		/// <summary>Input field vertical alignment</summary>
		protected DialogAlignment valign = DialogAlignment.Valign_Top;
		/// <summary>Input field horizontal alignment</summary>
		protected DialogAlignment align = DialogAlignment.Align_Left;

		public static InputBuilder Builder {
			get {
				return new InputBuilder();
			}
		}

		/// <summary>Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class InputBuilder : Builder<GUTAInput> {
			//prepare the default values
			internal LeafComponentTypes type = LeafComponentTypes.InputText;
			internal int xPos;
			internal int yPos;
			internal int width;
			internal int id = 100; //some default ID (it will be usually specified as it is necessary for Response implementation...)
			internal int height = ButtonMetrics.D_BUTTON_HEIGHT; //default height is to fit to the rows with buttons (majority of rows use this)
			internal int hue = (int) Hues.WriteColor;
			internal DialogAlignment align = DialogAlignment.Align_Left;
			internal DialogAlignment valign = DialogAlignment.Valign_Bottom;
			internal string text = "";
			internal int textId;

			internal InputBuilder() {
			}

			/// <summary>Set the input field's relative X position</summary>
			public InputBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the input field's relative Y position</summary>
			public InputBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the input field's ID for inside-Response recognition</summary>
			public InputBuilder Id(int val) {
				this.id = val;
				return this;
			}

			/// <summary>Set the input field's width</summary>
			public InputBuilder Width(int val) {
				this.width = val;
				return this;
			}

			/// <summary>Set the input field's height</summary>
			public InputBuilder Height(int val) {
				this.height = val;
				return this;
			}

			/// <summary>Set the input type</summary>
			public InputBuilder Type(LeafComponentTypes val) {
				this.type = val;
				return this;
			}

			/// <summary>Set the input field's text hue</summary>
			public InputBuilder Hue(int val) {
				this.hue = val;
				return this;
			}

			/// <summary>Set the input field's text hue usign enum</summary>
			public InputBuilder Hue(Hues val) {
				this.hue = (int) val;
				return this;
			}

			/// <summary>Set the input field's value</summary>
			public InputBuilder Text(string val) {
				this.text = val;
				return this;
			}

			/// <summary>Set the input field's text id (for prepared texts)</summary>
			public InputBuilder TextId(int val) {
				this.textId = val;
				return this;
			}

			/// <summary>Set the input field's text's horizontal algiment</summary>
			public InputBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAInput field: " + val));
				}
				this.align = val;
				return this;
			}

			/// <summary>Set the input field's text's vertical algiment</summary>
			public InputBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAInput field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Create the GUTAInput instance</summary>
			public override GUTAInput Build() {
				GUTAInput retVal = new GUTAInput(this);
				return retVal;
			}
		}

		private GUTAInput(InputBuilder builder) {
			this.type = builder.type;
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.id = builder.id;
			this.width = builder.width;
			this.height = builder.height;
			this.textHue = builder.hue;
			this.align = builder.align;
			this.valign = builder.valign;
			this.text = builder.text;
			this.textId = builder.textId;
		}

		/// <summary>
		/// When added, we must recompute the Input Field's absolute position in the dialog (we 
		///  were provided only relative positions
		///  </summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			this.xPos += parent.XPos;
			this.yPos += parent.YPos;
			if (this.width == 0) {
				//no width - get it from the parent
				this.width = parent.Width;
				this.width -= ImprovedDialog.D_COL_SPACE; //put it between the borders of the column with a little spaces
				//substract also the space from the xPos adjustment of this field (it can be shorter to fit to the column)
				//this makes  sense, if the input field is not at the beginning pos. of the column... - it will shorten it 
				//of the space it is indented from the left border
				this.width -= (this.xPos - parent.XPos);
			}

			int valignOffset = 0;
			int alignOffset = 0;
			switch (this.valign) {
				case DialogAlignment.Valign_Center:
					valignOffset = grandpa.RowHeight / 2 - this.height / 2 + 1; //moves the field to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset = grandpa.RowHeight - this.height + 1; //moves the field to the bottom
					break;
			}
			int parentWidth = parent.Width;
			switch (this.align) {
				case DialogAlignment.Align_Center:
					alignOffset = parentWidth / 2 - this.width / 2; //moves the field to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset = parentWidth - this.width;
					break;
			}
			this.xPos += alignOffset;
			this.yPos += valignOffset;
		}

		/// <summary>
		/// Simply write the input (send the method request to the underlaying gump) 
		/// it will determine also what parameters to send
		/// </summary>
		internal override void WriteComponent() {
			//first of all add a different background
			this.gump.AddGumpPicTiled(this.xPos, this.yPos, this.width, this.height, ImprovedDialog.D_DEFAULT_INPUT_BACKGROUND);
			//and make it immediately transparent
			this.gump.AddCheckerTrans(this.xPos, this.yPos, this.width, this.height);
			switch (this.type) {
				case LeafComponentTypes.InputText: {
						if (this.textId == 0) {//no text ID was specified, use the text version
							this.gump.AddTextEntry(this.xPos, this.yPos, this.width, this.height, this.textHue, this.id, this.text);
						} else {
							this.gump.AddTextEntry(this.xPos, this.yPos, this.width, this.height, this.textHue, this.id, this.textId);
						}
						break;
					}
				case LeafComponentTypes.InputNumber: {
						if (this.textId == 0) {//no text ID was specified, use the text version (but send it as double!)
							//if the text is empty (the input field will be empty), then display zero
							decimal textToDisp = string.IsNullOrWhiteSpace(this.text) ? default(decimal) : decimal.Parse(this.text);
							this.gump.AddNumberEntry(this.xPos, this.yPos, this.width, this.height, this.textHue, this.id, textToDisp);
						} else {
							this.gump.AddNumberEntry(this.xPos, this.yPos, this.width, this.height, this.textHue, this.id, this.textId);
						}
						break;
					}
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < this.columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < this.level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->Input";
		}
	}

	/// <summary>The text component class - it handles the text writing to the underlaying gump</summary>
	public class GUTAText : LeafGUTAComponent {
		/// <summary>The text hue in the input field - if not specified, the default will be used.</summary>
		private int textHue;
		/// <summary>We have either ID of the used text, or the text string itself</summary>
		private int textId;
		private string text;

		/// <summary>Text horizontal alignment</summary>
		private DialogAlignment align;
		/// <summary>Text vertical alignment</summary>
		private DialogAlignment valign;

		public static TextBuilder Builder {
			get {
				return new TextBuilder();
			}
		}

		/// <summary>Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class TextBuilder : Builder<GUTAText> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal int hue = (int) Hues.WriteColor;
			internal DialogAlignment align = DialogAlignment.Align_Left;
			internal DialogAlignment valign = DialogAlignment.Valign_Top;
			internal string text = "";
			internal int textId;

			internal TextBuilder() {
			}

			/// <summary>Set the text's relative X position</summary>
			public TextBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the text's relative Y position</summary>
			public TextBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the text's hue</summary>
			public TextBuilder Hue(int val) {
				this.hue = val;
				return this;
			}

			/// <summary>Set the text's hue usign enum</summary>
			public TextBuilder Hue(Hues val) {
				this.hue = (int) val;
				return this;
			}

			/// <summary>Create the text as label (set the hue also)</summary>
			public TextBuilder TextLabel(string val) {
				this.text = val;
				this.hue = (int) Hues.LabelColor;
				return this;
			}

			/// <summary>Create the text as headline (set the hue also)</summary>
			public TextBuilder TextHeadline(string val) {
				this.text = val;
				this.hue = (int) Hues.HeadlineColor;
				return this;
			}

			/// <summary>Set the text value</summary>
			public TextBuilder Text(string val) {
				this.text = val;
				return this;
			}

			/// <summary>Set the text id (for prepared texts)</summary>
			public TextBuilder TextId(int val) {
				this.textId = val;
				return this;
			}

			/// <summary>Set the text's horizontal algiment</summary>
			public TextBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAText field: " + val));
				}
				this.align = val;
				return this;
			}

			/// <summary>Set the text's vertical algiment</summary>
			public TextBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAText field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Create the GUTAText instance</summary>
			public override GUTAText Build() {
				GUTAText retVal = new GUTAText(this);
				return retVal;
			}
		}

		private GUTAText(TextBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.textHue = builder.hue;
			this.align = builder.align;
			this.valign = builder.valign;
			this.text = builder.text;
			this.textId = builder.textId;
		}

		/// <summary>When added to the column we have to specify the position (count the absolute)</summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			int alignOffset = 0;
			int valignOffset = 0;
			if (this.text != null) { //we are not using the ID of the text, we can do some alignment computings if necessary
				int parentWidth = parent.Width;
				int textWidth = ImprovedDialog.TextLength(this.text);
				switch (this.align) {
					case DialogAlignment.Align_Center:
						alignOffset = parentWidth / 2 - textWidth / 2; //moves the text to the middle of the column
						break;
					case DialogAlignment.Align_Right:
						alignOffset = parentWidth - textWidth - 1; //moves the text to the right (1 pix added - it is the border)
						break;
				}
				switch (this.valign) {
					case DialogAlignment.Valign_Center:
						valignOffset = grandpa.RowHeight / 2 - ImprovedDialog.D_TEXT_HEIGHT / 2; //moves the text to the middle of the column
						break;
					case DialogAlignment.Valign_Bottom:
						valignOffset = grandpa.RowHeight - ImprovedDialog.D_CHARACTER_HEIGHT; //moves the text to the bottom
						break;
				}
			}
			this.xPos += parent.XPos + alignOffset;
			this.yPos += parent.YPos + valignOffset;

			if (this.text == null) {
				this.text = "null"; //we cannot display null so stringify it
			}
		}

		/// <summary>Call the underlaying gump istance's methods</summary>
		internal override void WriteComponent() {
			if (this.textId == 0) { //no text ID was specified, use the text version
				this.gump.AddText(this.xPos, this.yPos, this.textHue, this.text);
			} else {
				this.gump.AddText(this.xPos, this.yPos, this.textHue, this.textId);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < this.columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < this.level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->GUTAText(" + this.text + ")";
		}
	}


	/// <summary>The HTML text component class - allows making the text scrollable</summary>
	public class GUTAHTMLText : LeafGUTAComponent {
		private bool isScrollable, hasBoundBox;
		/// <summary>The text is either specified or passed as a text id</summary>
		private string text;
		private int textId;

		public static HTMLTextBuilder Builder {
			get {
				return new HTMLTextBuilder();
			}
		}

		/// <summary>Builder class for the Text LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class HTMLTextBuilder : Builder<GUTAHTMLText> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal bool isScrollable;
			internal bool hasBoundBox;
			internal string text = "";
			internal int textId;

			internal HTMLTextBuilder() {
			}

			/// <summary>Set the text's relative X position</summary>
			public HTMLTextBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the text's relative Y position</summary>
			public HTMLTextBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the text's scrollable state</summary>
			public HTMLTextBuilder Scrollable(bool val) {
				this.isScrollable = val;
				return this;
			}

			/// <summary>Set the text's scrollable state</summary>
			public HTMLTextBuilder HasBoundBox(bool val) {
				this.hasBoundBox = val;
				return this;
			}

			/// <summary>Set the text value</summary>
			public HTMLTextBuilder Text(string val) {
				this.text = val;
				return this;
			}

			/// <summary>Set the text id (for prepared texts)</summary>
			public HTMLTextBuilder TextId(int val) {
				this.textId = val;
				return this;
			}

			/// <summary>Create the GUTAText instance</summary>
			public override GUTAHTMLText Build() {
				GUTAHTMLText retVal = new GUTAHTMLText(this);
				return retVal;
			}
		}

		private GUTAHTMLText(HTMLTextBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.hasBoundBox = builder.hasBoundBox;
			this.isScrollable = builder.isScrollable;
			this.text = builder.text;
			this.textId = builder.textId;
		}


		private GUTAHTMLText(int x, int y, int width, int height, bool hasBoundBox, bool isScrollable) {
			this.xPos = x;
			this.yPos = y;
			this.width = width;
			this.height = height;
			this.hasBoundBox = hasBoundBox;
			this.isScrollable = isScrollable;
		}

		/// <summary>When added to the column we have to specify the position (count the absolute)</summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			//dont use spaces here or the text is glued to the bottom of the line on the single lined inputs
			this.xPos += parent.XPos;
			this.yPos += parent.YPos;
			//if not specified, take the size from the parent
			if (this.height == 0) {
				this.height = parent.Height;
			}
			if (this.width == 0) {
				this.width = parent.Width;
			}
		}

		/// <summary>Call the underlaying gump istance's methods</summary>
		internal override void WriteComponent() {
			if (this.textId == 0) { //no text ID was specified, use the text version
				this.gump.AddHtmlGump(this.xPos, this.yPos, this.width, this.height, this.text, this.hasBoundBox, this.isScrollable);
			} else {
				this.gump.AddHtmlGump(this.xPos, this.yPos, this.width, this.height, this.textId, this.hasBoundBox, this.isScrollable);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < this.columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < this.level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->HTMLText(" + this.text + ")";
		}
	}

	public class GUTAImage : LeafGUTAComponent {
		private int gumpId;
		private int color;

		/// <summary>Image horizontal alignment</summary>
		private DialogAlignment align;
		/// <summary>Image vertical alignment</summary>
		private DialogAlignment valign;

		public static ImageBuilder Builder {
			get {
				return new ImageBuilder();
			}
		}

		/// <summary>Builder class for the GUTAImage LeafGUTAComponent. Allows to set some or all necessary parameters via methods</summary>
		public class ImageBuilder : Builder<GUTAImage> {
			//prepare the default values
			internal int xPos;
			internal int yPos;
			internal DialogAlignment align = DialogAlignment.Align_Center;
			internal DialogAlignment valign = DialogAlignment.Valign_Center;
			internal int gumpId;
			internal int color;

			internal ImageBuilder() {
			}

			/// <summary>Set the image's relative X position</summary>
			public ImageBuilder XPos(int val) {
				this.xPos = val;
				return this;
			}

			/// <summary>Set the image's relative Y position</summary>
			public ImageBuilder YPos(int val) {
				this.yPos = val;
				return this;
			}

			/// <summary>Set the image's horizontal alignment</summary>
			public ImageBuilder Align(DialogAlignment val) {
				if (val != DialogAlignment.Align_Center && val != DialogAlignment.Align_Left && val != DialogAlignment.Align_Right) {
					throw new SEException(LogStr.Error("Wrong align used for GUTAImage field: " + val));
				}
				this.align = val;
				return this;
			}

			/// <summary>Set the image's vertical alignment</summary>
			public ImageBuilder Valign(DialogAlignment val) {
				if (val != DialogAlignment.Valign_Bottom && val != DialogAlignment.Valign_Center && val != DialogAlignment.Valign_Top) {
					throw new SEException(LogStr.Error("Wrong valign used for GUTAImage field: " + val));
				}
				this.valign = val;
				return this;
			}

			/// <summary>Set the image's gump</summary>
			public ImageBuilder Gump(int val) {
				this.gumpId = val;
				return this;
			}

			/// <summary>Set the image's gump using enumeration</summary>
			public ImageBuilder NamedGump(GumpIDs val) {
				this.gumpId = (int) val;
				return this;
			}

			/// <summary>Set the image's hue (color)</summary>
			public ImageBuilder Hue(int val) {
				this.color = val;
				return this;
			}

			/// <summary>Set the image's hue (color)</summary>
			public ImageBuilder Color(int val) {
				this.color = val;
				return this;
			}

			/// <summary>Create the GUTAImage instance</summary>
			public override GUTAImage Build() {
				GUTAImage retVal = new GUTAImage(this);
				return retVal;
			}
		}

		private GUTAImage(ImageBuilder builder) {
			this.xPos = builder.xPos;
			this.yPos = builder.yPos;
			this.align = builder.align;
			this.valign = builder.valign;
			this.gumpId = builder.gumpId;
			this.color = builder.color;
		}

		/// <summary>When added to the column we have to specify the position (count the absolute)</summary>
		protected override void OnBeforeWrite(GUTAComponent parent) {
			//set the level
			this.level = parent.Level + 1;

			//get the grandparent (GUTARow) (parent is GUTAColumn!)
			GUTARow grandpa = (GUTARow) parent.Parent;
			//set the column row (counted from the relative position and the grandpa's inner-row height)
			this.columnRow = this.xPos / grandpa.RowHeight;

			GumpArtDimension picDim = GumpDimensions.Table[this.gumpId];

			int alignOffset = -picDim.X; //at least...
			int valignOffset = -picDim.Y; //at least...

			switch (this.align) {
				case DialogAlignment.Align_Center:
					alignOffset += parent.Width / 2 - picDim.Width / 2; //moves the image to the middle of the column
					break;
				case DialogAlignment.Align_Right:
					alignOffset += parent.Width - picDim.Width - 1; //moves the image to the right (1 pix added - it is the border)
					break;
			}
			switch (this.valign) {
				case DialogAlignment.Valign_Center:
					valignOffset += grandpa.RowHeight / 2 - picDim.Height / 2; //moves the image to the middle of the column
					break;
				case DialogAlignment.Valign_Bottom:
					valignOffset += grandpa.RowHeight - picDim.Height; //moves the image to the bottom
					break;
			}
			this.xPos += parent.XPos + alignOffset;
			this.yPos += parent.YPos + valignOffset;
		}

		/// <summary>Call the underlaying gump istance's methods</summary>
		internal override void WriteComponent() {
			if (this.color == 0) {
				this.gump.AddTilePic(this.xPos, this.yPos, this.gumpId);
			} else {
				this.gump.AddTilePicHue(this.xPos, this.yPos, this.gumpId, this.color);
			}
		}

		public override string ToString() {
			string linesTabsOffset = "\r\n";
			//add as much rows as is the row which this item lies in
			for (int i = 0; i < this.columnRow; i++) {
				linesTabsOffset += "\r\n";
			}
			for (int i = 0; i < this.level; i++) {
				linesTabsOffset += "\t";
			}
			return linesTabsOffset + "->Image(" + this.gumpId + ")";
		}
	}
}