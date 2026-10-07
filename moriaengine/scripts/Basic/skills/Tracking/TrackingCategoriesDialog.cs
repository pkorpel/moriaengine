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

	/// <summary>Dialog that will display possible categories of characters to track (NPC's, Animals, Players)</summary>
	public class D_Tracking_Categories : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(180);
			dlg.SetLocation(80, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Co chceš stopovat?").Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			GUTATable picTable = new GUTATable(4, ImprovedDialog.ICON_WIDTH, 0, ButtonMetrics.D_BUTTON_WIDTH);
			picTable.RowHeight = 50; //dobra vejska pro obrazky
			picTable.InnerRowsDelimited = true;
			dlg.AddTable(picTable);
			dlg.LastTable[0, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Figurine_Llama).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Zvíøata").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Id(1).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[1, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Figurine_Ogre).Build();
			dlg.LastTable[1, 1] = GUTAText.Builder.TextLabel("Monstra").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[1, 2] = GUTAButton.Builder.Id(2).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[2, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Figurine_Man).Build();
			dlg.LastTable[2, 1] = GUTAText.Builder.TextLabel("Hráèe").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[2, 2] = GUTAButton.Builder.Id(3).Valign(DialogAlignment.Valign_Center).Build();

			dlg.LastTable[3, 0] = GUTAImage.Builder.NamedGump(GumpIDs.Figurine_NPC).Build();
			dlg.LastTable[3, 1] = GUTAText.Builder.TextLabel("NPC").Align(DialogAlignment.Align_Center).Valign(DialogAlignment.Valign_Center).Build();
			dlg.LastTable[3, 2] = GUTAButton.Builder.Id(4).Valign(DialogAlignment.Valign_Center).Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			SkillSequenceArgs skillSeqArgs = (SkillSequenceArgs) args[0];
			switch (gr.PressedButton) {
				case 0: //exit - finish tracking without selecting anything
					skillSeqArgs.PhaseAbort();
					break;
				//start the tracking skill in the first phase - looking for the characters around
				//as a parameter - set the category of characters to look for and the phase to start
				case 1: //animals chosen
					skillSeqArgs.Param1 = CharacterTypes.Animals;
					skillSeqArgs.Param2 = TrackingEnums.Phase_Characters_Seek; //looking for all nearby chars
					skillSeqArgs.PhaseStart();
					//newGi = self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(args.ArgsArray[0], ));
					//DialogStacking.EnstackDialog(gi, newGi);
					break;
				case 2: //monsters chosen
					skillSeqArgs.Param1 = CharacterTypes.Monsters;
					skillSeqArgs.Param2 = TrackingEnums.Phase_Characters_Seek; //looking for all nearby chars
					skillSeqArgs.PhaseStart();
					break;
				case 3: //players chosen
					skillSeqArgs.Param1 = CharacterTypes.Players;
					skillSeqArgs.Param2 = TrackingEnums.Phase_Characters_Seek; //looking for all nearby chars
					skillSeqArgs.PhaseStart();
					break;
				case 4: //NPCs chosen
					skillSeqArgs.Param1 = CharacterTypes.NPCs;
					skillSeqArgs.Param2 = TrackingEnums.Phase_Characters_Seek; //looking for all nearby chars
					skillSeqArgs.PhaseStart();
					break;
			}
		}
	}
}