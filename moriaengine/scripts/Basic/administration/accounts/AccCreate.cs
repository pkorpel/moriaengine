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

	/// <summary>An account creating dialog</summary>
	public class D_NewAccount : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs sa) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(500);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Vytvoøení nového hráèského úètu").Build();
			//cudlik na zavreni dialogu
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//dvousloupeckovy dialozek
			dlg.AddTable(new GUTATable(3, 0, 300)); //1.sl - napisy, 2.sl - editacni pole
			//napred napisy 
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Jméno úètu").Build();
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("Heslo").Build();
			dlg.LastTable[2, 0] = GUTAText.Builder.TextLabel("Registraèní e-mail").Build();

			//ted editacni pole
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(10).Build();
			dlg.LastTable[1, 1] = GUTAInput.Builder.Id(11).Build();
			dlg.LastTable[2, 1] = GUTAInput.Builder.Id(12).Build();
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//a posledni radek s tlacitkem
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Potvrdit").Build();
			dlg.MakeLastTableTransparent(); //zpruhledni posledni radek

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog				
			} else if (gr.PressedButton == 1) {
				//nacteme obsah input fieldu
				string accName = gr.GetTextResponse(10);
				string pass = gr.GetTextResponse(11);
				string email = gr.GetTextResponse(12);
				//zavolat metodu, ktera oznami uspech ci neuspech pri vytvoreni
				ScriptedAccount.CreateGameAccount(accName, pass, email);
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog			
			}
		}

		/// <summary>Create a new gm account using the dialog. Function accessible from the game</summary>
		[SteamFunction]
		public static void NewAcc(AbstractCharacter sender, ScriptArgs text) {
			sender.Dialog(SingletonScript<D_NewAccount>.Instance);
		}
	}
}