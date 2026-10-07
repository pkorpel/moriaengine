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

namespace SteamEngine.CompiledScripts.Dialogs {
	public class D_Test : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int headerColor = 1;
			string header = "Carpentry Menu";
			//int contentColor = 1;
			string content = "blablub blöb";
			int width = 200;
			int height = 220;

			this.ResizePic(0, 0, 5054, width, height + 100);

			this.GumpPicTiled(10, 10, width - 20, 20, 2624);
			this.CheckerTrans(10, 10, width - 20, 20);
			this.TextA(10, 10, headerColor, header);

			this.GumpPicTiled(10, 40, width - 20, height - 80, 2624);
			this.CheckerTrans(10, 40, width - 20, height - 80);

			this.HtmlGumpA(10, 40, width - 20, height - 80, content, false, true);

			this.GumpPicTiled(10, height - 30, width - 20, 20, 2624);
			this.CheckerTrans(10, height - 30, width - 20, 20);
			this.Button(10, height - 30, 4005, 4007, true, 0, 1);
			this.CheckBox(10, height + 30, 210, 211, true, 786);
			this.TextEntryA(10, height + 50, 500, 100, 1, 762, "this is texentry ìšèøžýá");

			this.NumberEntryA(10, height + 70, 500, 100, 1, 763, 5.5m);

			this.XmfhtmlGumpColor(40, height - 30, 120, 20, 1011036, false, false, 32767); // OKAY
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Console.WriteLine("OnResponse");
			gi.Cont.SysMessage("OnResponse from " + gi);
			gi.Cont.SysMessage("button : " + gr.PressedButton);
			gi.Cont.SysMessage("checkbutton: " + gr.IsSwitched(786));
			gi.Cont.SysMessage("textentry: " + gr.GetTextResponse(762));
			gi.Cont.SysMessage("number textentry: " + gr.GetTextResponse(763));
			gi.Cont.SysMessage("numberentry: " + gr.GetNumberResponse(763));
		}
	}
}
