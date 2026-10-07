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

	/// <summary>Brief information about one single item in the craftmenu (for players)</summary>
	public class D_Craftmenu_ItemInfo : CompiledGumpDef {
		private static int width = 600;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			ItemDef itm = (ItemDef) args[0];

			GumpArtDimension picDim = GumpDimensions.Table[itm.Model];

			dlg.CreateBackground(width);
			dlg.SetLocation(100, 70);

			//jmeno, ikona, cudlik na zruseni
			GUTATable hdrTable = new GUTATable(1, 85, 120, 0, ButtonMetrics.D_BUTTON_WIDTH);
			//add 2 pixels from both top and bottom corners (but minimal rowheight stays... - due to button icons))
			hdrTable.RowHeight = Math.Max(ImprovedDialog.D_ROW_HEIGHT, picDim.Height + 2 * ImprovedDialog.D_ICON_SPACE);
			dlg.AddTable(hdrTable);
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Název:").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text(itm.Name).Build();
			dlg.LastTable[0, 2] = GUTAImage.Builder.Gump(itm.Model).Color(itm.Color).Build();
			dlg.LastTable[0, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build(); //exit button
			dlg.MakeLastTableTransparent();

			int otherRows = 2;
			if (itm.IsWeaponDef) {
				otherRows += 2; //attack vsM and vsP
			}
			if (itm.IsWearableDef) {
				otherRows += 2; //armor vsP and vsM
			}
			if (itm.IsDestroyableDef) {
				otherRows += 1; //max durability
			}
			dlg.AddTable(new GUTATable(otherRows, 85, 0));
			//resources
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Materiál").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.Text((itm.Resources != null) ? itm.Resources.ToString() : "").Build();
			//type
			dlg.LastTable[1, 0] = GUTAText.Builder.TextLabel("Typ").Build();
			dlg.LastTable[1, 1] = GUTAText.Builder.Text(ItemTypeNames.GetPrettyName(itm.Type)).Build();
			//weapon
			if (itm.IsWeaponDef) {
				dlg.LastTable[2, 0] = GUTAText.Builder.TextLabel("UC vs M").Build();
				dlg.LastTable[2, 1] = GUTAText.Builder.Text(((WeaponDef) itm).AttackVsM.ToString()).Build();
				dlg.LastTable[3, 0] = GUTAText.Builder.TextLabel("UC vs P").Build();
				dlg.LastTable[3, 1] = GUTAText.Builder.Text(((WeaponDef) itm).AttackVsP.ToString()).Build();
			}
			if (itm.IsWearableDef) {
				dlg.LastTable[2, 0] = GUTAText.Builder.TextLabel("Armor vs M").Build();
				dlg.LastTable[2, 1] = GUTAText.Builder.Text(((WearableDef) itm).ArmorVsM.ToString()).Build();
				dlg.LastTable[3, 0] = GUTAText.Builder.TextLabel("Armor vs P").Build();
				dlg.LastTable[3, 1] = GUTAText.Builder.Text(((WearableDef) itm).ArmorVsP.ToString()).Build();
			}
			if (itm.IsDestroyableDef) {
				dlg.LastTable[4, 0] = GUTAText.Builder.TextLabel("Výdrž").Build();
				dlg.LastTable[4, 1] = GUTAText.Builder.Text(((DestroyableDef) itm).MaxDurability.ToString()).Build();
			}

			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) {
				DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
			}
		}
	}
}