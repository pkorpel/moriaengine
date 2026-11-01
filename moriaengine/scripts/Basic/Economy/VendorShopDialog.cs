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
	/// <summary>
	/// The "buy" shop list for a basic NPC vendor (enhancements Phase 14.5, issue #36) - one row
	/// per NpcVendorStock entry (icon, name, price, buy button). Modeled after D_Craftmenu
	/// (scripts/Basic/skills/Crafting/Craftmenu/CraftmenuDialog.cs) but much simpler - no paging,
	/// no GM-only columns, since the stock list is small and fixed.
	/// </summary>
	public class D_VendorShop : CompiledGumpDef {
		private const int width = 320;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(width);
			dlg.SetLocation(80, 50);

			int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

			dlg.AddTable(new GUTATable(1, innerWidth - ButtonMetrics.D_BUTTON_WIDTH, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Shop").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit
			dlg.MakeLastTableTransparent();

			var entries = NpcVendorStock.Entries;
			dlg.AddTable(new GUTATable(entries.Count, ImprovedDialog.ICON_WIDTH, 0, 60, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable.RowHeight = ImprovedDialog.ICON_HEIGHT;
			dlg.LastTable.InnerRowsDelimited = true;

			for (int i = 0; i < entries.Count; i++) {
				NpcVendorStock.Entry entry = entries[i];
				dlg.LastTable[i, 0] = GUTAImage.Builder.Gump(entry.Def.Model).Build();
				dlg.LastTable[i, 1] = GUTAText.Builder.Text(entry.Def.Name).Valign(DialogAlignment.Valign_Center).Build();
				dlg.LastTable[i, 2] = GUTAText.Builder.Text(entry.BuyPrice + " gold").Valign(DialogAlignment.Valign_Center).Build();
				dlg.LastTable[i, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(10 + i).Valign(DialogAlignment.Valign_Center).Build();
			}
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int btnNo = gr.PressedButton;
			if (btnNo < 10) {
				return; //exit button, nothing else to do
			}

			int index = btnNo - 10;
			var entries = NpcVendorStock.Entries;
			if (index < 0 || index >= entries.Count) {
				return;
			}

			NpcVendorStock.Entry entry = entries[index];
			Player buyer = (Player) gi.Cont;
			if (!GoldUtils.Pay(buyer, entry.BuyPrice, notEnough: "Thou dost not have enough gold.")) {
				return;
			}

			AbstractItem bought = (AbstractItem) entry.Def.Create(buyer.Backpack);
			buyer.SysMessage("Thou hast bought " + bought.Name + ".");
		}
	}
}
