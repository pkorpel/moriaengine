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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	//this is the class to be overriden in C# scripts
	//its subclasses (in scripts) are instantiated by ClassManager class

	public enum GumpButtonType {
		Page = 0,
		Reply = 1
	}

	public abstract class CompiledGumpDef : GumpDef {
		internal Gump gumpInstance; //this is to be used only in the abstract methods
		public Gump GumpInstance {
			get {
				return this.gumpInstance;
			}
		}

		protected CompiledGumpDef()
		{
		}

		protected CompiledGumpDef(string defName)
			: base(defName) {
		}

		protected override string InternalFirstGetDefname() {
			return this.GetType().Name;
		}

		//not sure why this was here... let's find out
		//public override void Unload() {
		//}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal override Gump InternalConstruct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Gump gi = new CompiledGump(this);

			if (args == null) {
				gi.InputArgs = new DialogArgs(); //empty arguments
			} else {
				gi.InputArgs = args; //store input arguments
			}

			this.gumpInstance = gi;
			try {
				this.Construct(focus, sendTo, args);
				if (this.gumpInstance != null) {
					gi = this.gumpInstance;
					this.gumpInstance = null;
					gi.FinishCompilingPacketData(focus, sendTo);
					return gi;
				}
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
			return null;
		}

		public abstract void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args);
		public abstract void OnResponse(Gump gi, GumpResponse gr, DialogArgs args);

		#region drawing interface
		//RunUO interface
		public void AddAlphaRegion(int x, int y, int width, int height) {
			this.GumpInstance.AddCheckerTrans(x, y, width, height);
		}

		//sphere-like interface
		public void CheckerTrans(int x, int y, int width, int height) {
			this.GumpInstance.AddCheckerTrans(x, y, width, height);
		}

		public void CheckerTrans() {
			this.GumpInstance.AddCheckerTrans();
		}

		public void AddBackground(int x, int y, int width, int height, int gumpId) {
			this.GumpInstance.AddResizePic(x, y, gumpId, width, height);
		}

		public void ResizePic(int x, int y, int gumpId, int width, int height) {
			this.GumpInstance.AddResizePic(x, y, gumpId, width, height);
		}

		public void AddButton(int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type, int param) {
			if (type == GumpButtonType.Reply) {
				this.GumpInstance.AddButton(x, y, normalId, pressedId, true, param, buttonId);
			} else {
				this.GumpInstance.AddButton(x, y, normalId, pressedId, false, param, buttonId);
			}
		}

		public void Button(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId) {
			this.GumpInstance.AddButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId);
		}

		public void AddImageTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			this.GumpInstance.AddTiledButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId, itemId, hue, width, height);
		}

		public void ImageTiledButton(int x, int y, int downGumpId, int upGumpId, bool isTrigger, int pageId, int triggerId, int itemId, int hue, int width, int height) {
			this.GumpInstance.AddTiledButton(x, y, downGumpId, upGumpId, isTrigger, pageId, triggerId, itemId, hue, width, height);
		}

		public void AddGroup(int group) {
			this.GumpInstance.AddGroup(group);
		}

		public void Group(int groupId) {
			this.GumpInstance.AddGroup(groupId);
		}

		public void AddHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar) {
			this.GumpInstance.AddHtmlGump(x, y, width, height, text, background, scrollbar);
		}

		public void HtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.GumpInstance.AddHtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}
		//99z+ interface
		public void HtmlGumpA(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.GumpInstance.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}
		//55ir interface
		public void DhtmlGump(int x, int y, int width, int height, string text, bool hasBoundBox, bool isScrollable) {
			this.GumpInstance.AddHtmlGump(x, y, width, height, text, hasBoundBox, isScrollable);
		}

		public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background, bool scrollbar) {
			this.GumpInstance.AddXmfhtmlGump(x, y, width, height, number, background, scrollbar);
		}

		public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background, bool scrollbar) {
			this.GumpInstance.AddXmfhtmlGumpColor(x, y, width, height, number, background, scrollbar, color);
		}

		public void XmfhtmlGump(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable) {
			this.GumpInstance.AddXmfhtmlGump(x, y, width, height, textId, hasBoundBox, isScrollable);
		}

		public void XmfhtmlGumpColor(int x, int y, int width, int height, int textId, bool hasBoundBox, bool isScrollable, int hue) {
			this.GumpInstance.AddXmfhtmlGumpColor(x, y, width, height, textId, hasBoundBox, isScrollable, hue);
		}

		public void AddCheck(int x, int y, int inactiveId, int activeId, bool initialState, int switchId) {
			this.GumpInstance.AddCheckBox(x, y, inactiveId, activeId, initialState, switchId);
		}

		public void CheckBox(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			this.GumpInstance.AddCheckBox(x, y, uncheckedGumpId, checkedGumpId, isChecked, id);
		}

		public void AddItem(int x, int y, int itemId) {
			this.GumpInstance.AddTilePic(x, y, itemId);
		}

		public void AddItem(int x, int y, int itemId, int hue) {
			this.GumpInstance.AddTilePicHue(x, y, itemId, hue);
		}

		public void TilePic(int x, int y, int tileId) {
			this.GumpInstance.AddTilePic(x, y, tileId);
		}

		public void TilePicHue(int x, int y, int tileId, int hue) {
			this.GumpInstance.AddTilePicHue(x, y, tileId, hue);
		}

		public void AddImage(int x, int y, int gumpId) {
			this.GumpInstance.AddGumpPic(x, y, gumpId);
		}

		public void AddImage(int x, int y, int gumpId, int hue) {
			this.GumpInstance.AddGumpPic(x, y, gumpId, hue);
		}

		public void AddImageTiled(int x, int y, int width, int height, int gumpId) {
			this.GumpInstance.AddGumpPicTiled(x, y, width, height, gumpId);
		}

		public void GumpPicTiled(int x, int y, int width, int height, int gumpId) {
			this.GumpInstance.AddGumpPicTiled(x, y, width, height, gumpId);
		}

		public void AddLabel(int x, int y, int hue, string text) {
			this.GumpInstance.AddText(x, y, hue, text);
		}

		public void Text(int x, int y, int hue, int textId) {
			this.GumpInstance.AddText(x, y, hue, textId);
		}
		//99z+ interface
		public void TextA(int x, int y, int hue, string text) {
			this.GumpInstance.AddText(x, y, hue, text);
		}
		//55ir+ interface. why can the idiots not unite? :P
		public void DText(int x, int y, int hue, string text) {
			this.GumpInstance.AddText(x, y, hue, text);
		}

		public void AddLabelCropped(int x, int y, int width, int height, int hue, string text) {
			this.GumpInstance.AddCroppedText(x, y, width, height, hue, text);
		}

		public void CroppedText(int x, int y, int width, int height, int hue, int textId) {
			this.GumpInstance.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void CroppedTextA(int x, int y, int width, int height, int hue, string textId) {
			this.GumpInstance.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void DCroppedText(int x, int y, int width, int height, int hue, string textId) {
			this.GumpInstance.AddCroppedText(x, y, width, height, hue, textId);
		}

		public void AddPage(int page) {
			this.GumpInstance.AddPage(page);
		}

		public void Page(int pageId) {
			this.GumpInstance.AddPage(pageId);
		}

		public void AddRadio(int x, int y, int inactiveId, int activeId, bool initialState, int switchId) {
			this.GumpInstance.AddRadio(x, y, inactiveId, activeId, initialState, switchId);
		}

		public void Radio(int x, int y, int uncheckedGumpId, int checkedGumpId, bool isChecked, int id) {
			this.GumpInstance.AddRadio(x, y, uncheckedGumpId, checkedGumpId, isChecked, id);
		}

		public void AddTextEntry(int x, int y, int width, int height, int hue, int entryId, string initialText) {
			this.GumpInstance.AddTextEntry(x, y, width, height, hue, entryId, initialText);
		}

		public void TextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			this.GumpInstance.AddTextEntry(x, y, widthPix, widthChars, hue, id, textId);
		}
		//99z+ interface
		public void TextEntryA(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			this.GumpInstance.AddTextEntry(x, y, widthPix, widthChars, hue, id, text);
		}
		//55ir interface
		public void DTextEntry(int x, int y, int widthPix, int widthChars, int hue, int id, string text) {
			this.GumpInstance.AddTextEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		public void NumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, int textId) {
			this.GumpInstance.AddNumberEntry(x, y, widthPix, widthChars, hue, id, textId);
		}
		//hypothetical 55ir interface
		public void DNumberEntry(int x, int y, int widthPix, int widthChars, int hue, int id, decimal text) {
			this.GumpInstance.AddNumberEntry(x, y, widthPix, widthChars, hue, id, text);
		}
		//hypothetical 99z+ interface
		public void NumberEntryA(int x, int y, int widthPix, int widthChars, int hue, int id, decimal text) {
			this.GumpInstance.AddNumberEntry(x, y, widthPix, widthChars, hue, id, text);
		}

		//RunUO interface
		public int X {
			get {
				return this.GumpInstance.X;
			}
			set {
				this.GumpInstance.X = value;
			}
		}
		//RunUO interface
		public int Y {
			get {
				return this.GumpInstance.Y;
			}
			set {
				this.GumpInstance.Y = value;
			}
		}
		//sphere interface
		public void SetLocation(int x, int y) {
			this.GumpInstance.X = x;
			this.GumpInstance.Y = y;
		}

		public void NoClose() {
			this.GumpInstance.closable = false;
		}

		public void NoMove() {
			this.GumpInstance.movable = false;
		}

		public void NoDispose() {
			this.GumpInstance.disposable = false;
		}

		public bool Closable {
			get {
				return this.GumpInstance.closable;
			}
			set {
				this.GumpInstance.closable = value;
			}
		}

		public bool Disposable {
			get {
				return this.GumpInstance.disposable;
			}
			set {
				this.GumpInstance.disposable = value;
			}
		}

		public bool Dragable {
			get {
				return this.GumpInstance.movable;
			}
			set {
				this.GumpInstance.movable = value;
			}
		}
		#endregion drawing interface

		public override string ToString() {
			return "CompiledGumpDef " + this.Defname;
		}
	}

	public sealed class CompiledGump : Gump {
		public CompiledGump(CompiledGumpDef def)
			: base(def) {
		}
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override void OnResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			CompiledGumpDef gdef = (CompiledGumpDef) this.Def;
			gdef.gumpInstance = this;
			try {
				gdef.OnResponse(this, new GumpResponse(pressedButton, selectedSwitches, responseTexts, responseNumbers), this.InputArgs);
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			gdef.gumpInstance = null;
		}
	}

	public class GumpResponse {
		private readonly int pressedButton;
		private readonly int[] selectedSwitches;
		private readonly ResponseText[] responseTexts;
		private readonly ResponseNumber[] responseNumbers;

		public GumpResponse(int pressedButton, int[] selectedSwitches, ResponseText[] responseTexts, ResponseNumber[] responseNumbers) {
			this.pressedButton = pressedButton;
			this.selectedSwitches = selectedSwitches;
			this.responseTexts = responseTexts;
			this.responseNumbers = responseNumbers;
		}

		public int PressedButton {
			get {
				return this.pressedButton;
			}
		}

		//public int[] SelectedSwitches {
		//    get {
		//        return selectedSwitches;
		//    }
		//}

		//public ResponseText[] ResponseTexts {
		//    get {
		//        return responseTexts;
		//    }
		//}

		//public ResponseNumber[] ResponseNumbers {
		//    get {
		//        return responseNumbers;
		//    }
		//} 

		public bool IsSwitched(int id) {
			for (int i = 0, n = this.selectedSwitches.Length; i < n; i++) {
				if (this.selectedSwitches[i] == id) {
					return true;
				}
			}
			return false;
		}

		public string GetTextResponse(int id) {
			for (int i = 0, n = this.responseTexts.Length; i < n; i++) {
				ResponseText rt = this.responseTexts[i];
				if (rt.Id == id) {
					return rt.Text;
				}
			}
			return "";
		}

		public decimal GetNumberResponse(int id) {
			for (int i = 0, n = this.responseNumbers.Length; i < n; i++) {
				ResponseNumber rn = this.responseNumbers[i];
				if ((rn != null) && (rn.Id == id)) {
					return rn.Number;
				}
			}
			return 0;
		}
	}
}