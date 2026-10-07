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

namespace SteamEngine.CompiledScripts.Dialogs {

	public class D_Firewall_BlockIP : CompiledGumpDef {
		internal static readonly TagKey ipToBlockTK = TagKey.Acquire("_ip_to_block_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string ip = TagMath.SGetTagNotNull(args, ipToBlockTK);

			int width = 500;
			int labels = 130;

			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);

			dialogHandler.CreateBackground(width);
			dialogHandler.SetLocation(200, 280);
			//prvni radek
			dialogHandler.AddTable(new GUTATable(1, width, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Pridej blokovanou IP").XPos((width - 130) / 2).Build();
			dialogHandler.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			//druhy radek IP adresa
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.Text("IP adresa:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(10).Text(ip).Build();
			dialogHandler.MakeLastTableTransparent();

			//treti radek Duvod
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Dùvod:").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Id(11).Build();
			dialogHandler.MakeLastTableTransparent();

			//ctvrty radek Kdo
			dialogHandler.AddTable(new GUTATable(1, 0, width - labels));
			dialogHandler.LastTable.RowHeight = ImprovedDialog.D_ROW_HEIGHT;
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Zablokoval:").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.Text(sendTo.Account.Name).Build();
			dialogHandler.MakeLastTableTransparent();

			//paty radek tlacitko OK
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).XPos(width / 2).Id(1).Build(); //exit button
			dialogHandler.MakeLastTableTransparent();

			dialogHandler.WriteOut();

		}
		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 1) { //OK button
				Firewall.AddBlockedIP(gr.GetTextResponse(10), gr.GetTextResponse(11), gi.Cont.Account);
				//zavolat stacklej dialog (if any)				
				DialogStacking.ShowPreviousDialog(gi);
			} else {
				//rovnou se vracime
				DialogStacking.ShowPreviousDialog(gi);
			}
		}

		[SteamFunction]
		public static void BlockIP(Character self, ScriptArgs sa) {
			if (sa != null) {
				DialogArgs newArgs = new DialogArgs();
				newArgs.SetTag(ipToBlockTK, sa.Argv[0]);
				self.Dialog(SingletonScript<D_Firewall_BlockIP>.Instance, newArgs);
			} else {
				self.Dialog(SingletonScript<D_Firewall_BlockIP>.Instance);
			}
		}
	}

}