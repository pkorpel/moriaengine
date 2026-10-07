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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class GumpDef : AbstractScript {
		internal GumpDef()
		{
		}

		internal GumpDef(string name)
			: base(name) {
		}

		public new static GumpDef GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as GumpDef;
		}

		internal abstract Gump InternalConstruct(Thing focused, AbstractCharacter sendTo, DialogArgs args);
	}

	/// <summary>
	/// Dialog arguments holder. It can contain arguments as tags as well as an array of (e.g. hardcoded arguments)
	/// the array's length is unmodifiable so the only way to put args into it is to put them during constructor call.
	/// Arguments added in this way should be only the compulsory dialog arguments necessary in every case (for example 
	/// label and text in the Info/Error dialog-messages). Other args should be added as tags!
	/// </summary>
	public class DialogArgs : TagHolder {
		private object[] fldArgs;

		//public DialogArgs() {
		//    this.fldArgs = new object[0]; //aspon prazdny pole, ale ne null
		//}

		public DialogArgs(params object[] args) {
			this.fldArgs = args;
		}

		public object this[int i] {
			get {
				return this.fldArgs[i];
			}
			set {
				this.fldArgs[i] = value;
			}
		}

		public object[] GetArgsArray() {
			return this.fldArgs;
		}
	}

	public abstract class Gump {
		private static int uids;

		private readonly int uid;
		private readonly GumpDef def;
		private DialogArgs inputArgs;//arguments the gump is called with
		private AbstractCharacter cont;//the player who sees this instance (src)
		private Thing focus;//the thing this gump was "launched on"
		private int x;
		private int y;
		internal readonly StringBuilder layout = new StringBuilder();
		internal List<int> numEntryIDs;
		internal Dictionary<int, int> entryTextIds;

		//fields from former GumpBuilder
		internal bool movable = true;
		internal bool closable = true;
		internal bool disposable = true;
		//private List<string[]> elements = new List<string[]>();
		internal List<string> textsList;
		//internal int textsLengthsSum;

		internal Gump(GumpDef def) {
			this.def = def;
			this.uid = uids++;
		}

		public DialogArgs InputArgs {
			get {
				return this.inputArgs;
			}

			set {
				this.inputArgs = value;
			}
		}

		public int X {
			get {
				return this.x;
			}
			set {
				this.x = value;
			}
		}

		public int Y {
			get {
				return this.y;
			}
			set {
				this.y = value;
			}
		}

		public AbstractCharacter Cont {
			get {
				return this.cont;
			}
		}

		public Thing Focus {
			get {
				return this.focus;
			}
		}

		public int Uid {
			get {
				return this.uid;
			}
		}

		public GumpDef Def {
			get {
				return this.def;
			}
		}

		public abstract void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers);

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return (obj == this);
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture,
				"{0} {1} (uid {2})",
				Tools.TypeToString(this.GetType()), this.def.Defname, this.uid);
		}

		private void AddElement(string[] arr) {
			this.layout.Append("{").Append(string.Join(" ", arr)).Append("}");
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "focus"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "cont")]
		internal void FinishCompilingPacketData(Thing focus, AbstractCharacter cont) {
			if (!this.movable) {
				this.layout.Insert(0, "{nomove}");
			}
			if (!this.closable) {
				this.layout.Insert(0, "{noclose}");
			}
			if (!this.disposable) {//what does it really mean? :)
				this.layout.Insert(0, "{nodispose}");
			}
			this.focus = focus;
			this.cont = cont;
		}

		private void CreateTexts() {
			if (this.textsList == null) {
				this.textsList = new List<string>();
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			string[] arr = {
				"button", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				downGumpId.ToString(CultureInfo.InvariantCulture), 
				upGumpId.ToString(CultureInfo.InvariantCulture), 
				(isTrigger?"1": "0"), 
				pageId.ToString(CultureInfo.InvariantCulture),
				triggerId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			string[] arr = {
				"buttontileart", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				downGumpId.ToString(CultureInfo.InvariantCulture), 
				upGumpId.ToString(CultureInfo.InvariantCulture), 
				(isTrigger? "1": "0"), 
				pageId.ToString(CultureInfo.InvariantCulture),
				triggerId.ToString(CultureInfo.InvariantCulture),

				itemId.ToString(CultureInfo.InvariantCulture),
				hue.ToString(CultureInfo.InvariantCulture),
				width.ToString(CultureInfo.InvariantCulture),
				height.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = {
				"checkbox", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				uncheckedGumpId.ToString(CultureInfo.InvariantCulture), 
				checkedGumpId.ToString(CultureInfo.InvariantCulture), 
				(isChecked?"1": "0"), 
				id.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddRadio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			string[] arr = {
				"radio", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				uncheckedGumpId.ToString(CultureInfo.InvariantCulture), 
				checkedGumpId.ToString(CultureInfo.InvariantCulture), 
				(isChecked?"1": "0"), 
				id.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCheckerTrans(int x, int y, int width, int height) {
			string[] arr = {
				"checkertrans", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				width.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		//is it possible without args? sphere tables say it is...
		public void AddCheckerTrans() {
			string[] arr = { "checkertrans" };
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddText(int x, int y, int hue, int textId) {
			string[] arr = {
				"text", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				hue.ToString(CultureInfo.InvariantCulture), 
				textId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddText(int x, int y, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddText(x, y, hue, textId);
			return textId;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddCroppedText(int x, int y, int width, int height, int hue, int textId) {
			string[] arr = {
				"croppedtext", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				width.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture), 
				hue.ToString(CultureInfo.InvariantCulture), 
				textId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddCroppedText(int x, int y, int width, int height, int hue, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddCroppedText(x, y, width, height, hue, textId);
			return textId;
		}

		public void AddGroup(int groupId) {
			string[] arr = {
				"group", 
				groupId.ToString(CultureInfo.InvariantCulture) 
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPic(int x, int y, int gumpId) {
			string[] arr = {
				"gumppic", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				gumpId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPic(int x, int y, int gumpId, int hue) {
			if (hue == 0) {
				this.AddGumpPic(x, y, gumpId);
			} else {
				string[] arr = {
					"gumppic", 
					x.ToString(CultureInfo.InvariantCulture), 
					y.ToString(CultureInfo.InvariantCulture),
					gumpId.ToString(CultureInfo.InvariantCulture), 
					"hue="+hue.ToString(CultureInfo.InvariantCulture) 
				};
				this.AddElement(arr);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddGumpPicTiled(int x, int y, int width, int height, int gumpId) {
			string[] arr = {
				"gumppictiled", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				width.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture),
				gumpId.ToString(CultureInfo.InvariantCulture) 
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddHtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			string[] arr = {
				"htmlgump", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				width.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture), 
				textId.ToString(CultureInfo.InvariantCulture), 
				(hasBoundBox? "1": "0"), 
				(isScrollable? "1": "0")
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"),
		SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public int AddHtmlGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddHtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
			return textId;
		}

		public void AddPage(int pageId) {
			string[] arr = {
				"page",
				pageId.ToString(CultureInfo.InvariantCulture) 
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddResizePic(int x, int y, int gumpId, int width, int height) {
			string[] arr = {
				"resizepic", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				gumpId.ToString(CultureInfo.InvariantCulture), 
				width.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		//internal int AddString(string text) {
		//	Sanity.IfTrueThrow(text==null, "The text string can't be null");
		//	CreateTexts();
		//	return textsList.Add(text);
		//}

		public int AddTextLine(string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			return this.textsList.Count - 1;
		}

		public void AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if (id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID textoveho vstupu - zadano :" + id + " povoleny rozsah: " + short.MinValue + "-" + short.MaxValue));
			}
			string[] arr = {
				"textentry", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				widthPix.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture), 
				hue.ToString(CultureInfo.InvariantCulture), 
				id.ToString(CultureInfo.InvariantCulture),
				textId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
			if (this.entryTextIds == null) {
				this.entryTextIds = new Dictionary<int, int>();
			}
			this.entryTextIds[id] = textId;
		}

		public int AddTextEntry(int x, int y, int widthPix, int height, int hue, int id, string text) {
			Sanity.IfTrueThrow(text == null, "The text string can't be null");
			this.CreateTexts();
			this.textsList.Add(text);
			//textsLengthsSum += text.Length;
			int textId = this.textsList.Count - 1;
			this.AddTextEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		public void AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, int textId) {
			if (id < short.MinValue || id > short.MaxValue) {
				throw new SEException(LogStr.Error("Nepovolena hodnota ID ciselneho vstupu - zadano :" + id + " povoleny rozsah: " + short.MinValue + "-" + short.MaxValue));
			}
			string[] arr = {
				"textentry", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				widthPix.ToString(CultureInfo.InvariantCulture), 
				height.ToString(CultureInfo.InvariantCulture), 
				hue.ToString(CultureInfo.InvariantCulture), 
				id.ToString(CultureInfo.InvariantCulture),
				textId.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
			if (this.numEntryIDs == null) {
				this.numEntryIDs = new List<int>();
			}
			this.numEntryIDs.Add(id);

			if (this.entryTextIds == null) {
				this.entryTextIds = new Dictionary<int, int>();
			}
			this.entryTextIds[id] = textId;
		}

		public int AddNumberEntry(int x, int y, int widthPix, int height, int hue, int id, decimal text) {
			this.CreateTexts();
			string textStr = text.ToString(CultureInfo.InvariantCulture);
			this.textsList.Add(textStr);
			//textsLengthsSum += textStr.Length;
			int textId = this.textsList.Count - 1;
			this.AddNumberEntry(x, y, widthPix, height, hue, id, textId);
			return textId;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddTilePic(int x, int y, int model) {
			string[] arr = {
				"tilepic", 
				x.ToString(CultureInfo.InvariantCulture), 
				y.ToString(CultureInfo.InvariantCulture),
				model.ToString(CultureInfo.InvariantCulture)
			};
			this.AddElement(arr);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y")]
		public void AddTilePicHue(int x, int y, int model, int hue) {
			if (hue == 0) {
				this.AddTilePic(x, y, model);
			} else {
				string[] arr = {
					"tilepichue", 
					x.ToString(CultureInfo.InvariantCulture), 
					y.ToString(CultureInfo.InvariantCulture),
					model.ToString(CultureInfo.InvariantCulture),
					hue.ToString(CultureInfo.InvariantCulture)
				};
				this.AddElement(arr);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddXmfhtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.AddXmfhtmlGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, 0);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "y"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "x")]
		public void AddXmfhtmlGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			string[] arr;
			if (hue == 0) {
				arr = new[] {
					"xmfhtmlgump", 
					x.ToString(CultureInfo.InvariantCulture), 
					y.ToString(CultureInfo.InvariantCulture),
					width.ToString(CultureInfo.InvariantCulture), 
					height.ToString(CultureInfo.InvariantCulture), 
					textId.ToString(CultureInfo.InvariantCulture), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0")
				};
			} else {
				arr = new[] {
					"xmfhtmlgumpcolor", 
					x.ToString(CultureInfo.InvariantCulture), 
					y.ToString(CultureInfo.InvariantCulture),
					width.ToString(CultureInfo.InvariantCulture), 
					height.ToString(CultureInfo.InvariantCulture), 
					textId.ToString(CultureInfo.InvariantCulture), 
					(hasBoundBox? "1": "0"), 
					(isScrollable? "1": "0"),
					hue.ToString(CultureInfo.InvariantCulture)
				};
			}
			this.AddElement(arr);
		}
	}

	public class ResponseText {
		private readonly int id;
		private readonly string text;

		public ResponseText(int id, string text) {
			this.id = id;
			this.text = text;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public string Text {
			get {
				return this.text;
			}
		}
	}

	public class ResponseNumber {
		private readonly int id;
		private readonly decimal number;

		public ResponseNumber(int id, decimal number) {
			this.id = id;
			this.number = number;
		}

		public int Id {
			get {
				return this.id;
			}
		}

		public decimal Number {
			get {
				return this.number;
			}
		}
	}
}

//The 0xBF packet starts off with a cmd byte, followed by two bytes for the length.  After that is a two byte value which is a subcmd, and the message varies after that.
//General Info (5 bytes, plus specific message)
//� BYTE cmd
//� BYTE[2] len
//� BYTE[2] subcmd
//� BYTE[len-5] submessage
//
//Subcommand 4: "Close Generic GumpDef"
//
//    * BYTE[4] dialogID // which gump to destroy (second ID in 0xB0 packet)
//    * BYTE[4] buttonId // response buttonID for packet 0xB1