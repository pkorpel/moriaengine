using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1070 - d_dyi_vendor: the portrait, the top buttons always, the page's own buttons below the top, the page's
	/// offers, the intro at the top and a page's text elsewhere. Where the player is in the tree travels in the dialog's args,
	/// Erebor's diy_vendor_adresa and diy_vendor_text tags.</summary>
	public class D_DiyVendor : CompiledGumpDef {
		internal const int BackButton = 10, FirstSubButton = 70, FirstOfferButton = 80;
		private const int ButtonHue = 1152, OfferHue = 50;
		private static readonly int[,] buttonSpots = { { 160, 330 }, { 325, 330 }, { 490, 330 }, { 160, 360 }, { 325, 360 }, { 490, 360 } };

		internal static void Show(Player player, NPC vendor, int address, string text) {
			vendor.Dialog(player, SingletonScript<D_DiyVendor>.Instance, new DialogArgs(vendor, address, text ?? ""));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1];
			string text = (string) args[2];
			Thing store = DiyVendor.Store(vendor);
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, DiyVendor.Portrait(store));
			for (int slot = 1; slot <= DiyVendor.Buttons; slot++) {
				if (DiyVendor.HasButton(store, 0, slot)) {
					this.Button(slot, slot, DiyVendor.ButtonLabel(store, 0, slot));
				}
			}
			if (address > 0) {
				for (int slot = 1; slot <= DiyVendor.Buttons; slot++) {
					if (DiyVendor.HasButton(store, address, slot)) {
						this.Button(slot, FirstSubButton + slot, DiyVendor.ButtonLabel(store, address, slot));
					}
				}
			}
			for (int slot = 1; slot <= DiyVendor.Offers; slot++) {
				if (DiyVendor.HasOffer(store, address, slot)) {
					int x = (slot <= 5) ? 30 : 325, y = 400 + (((slot - 1) % 5) * 25);
					this.AddButton(x, y, 0xfa5, 0xfa7, FirstOfferButton + slot, GumpButtonType.Reply, 0);
					this.AddLabel(x + 30, y, OfferHue, DiyVendor.OfferName(store, address, slot));
				}
			}
			if (address == 0) {
				string intro = DiyVendor.Intro(store);
				if (intro.Length > 0) {
					this.AddHtml(170, 230, 440, 100, intro, false, true);
				}
			} else {
				if (text.Length > 0) {
					this.AddHtml(170, 230, 440, 100, text, false, true);
				}
				this.AddButton(630, 525, 0x5780, 0x5781, BackButton, GumpButtonType.Reply, 0);
			}
		}

		private void Button(int slot, int id, string label) {
			int x = buttonSpots[slot - 1, 0], y = buttonSpots[slot - 1, 1];
			this.AddButton(x, y, 0x988, 0x988, id, GumpButtonType.Reply, 0);
			this.AddLabel(x + 8, y + 2, ButtonHue, label);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1];
			Player player = (Player) gi.Cont;
			Thing store = DiyVendor.Store(vendor);
			int button = gr.PressedButton;
			if (button == BackButton) {
				Show(player, vendor, 0, null);
				return;
			}
			if ((button == 0) || vendor.IsDeleted || (Point2D.GetSimpleDistance(vendor, player) >= DiyVendor.DialogReach)) {
				return;
			}
			if ((button >= 1) && (button <= DiyVendor.Buttons)) {
				Show(player, vendor, DiyVendor.ButtonChild(store, 0, button), DiyVendor.ButtonText(store, 0, button));
			} else if ((button > FirstSubButton) && (button <= FirstSubButton + DiyVendor.Buttons)) {
				int slot = button - FirstSubButton;
				Show(player, vendor, DiyVendor.ButtonChild(store, address, slot), DiyVendor.ButtonText(store, address, slot));
			} else if ((button > FirstOfferButton) && (button <= FirstOfferButton + DiyVendor.Offers)) {
				int slot = button - FirstOfferButton;
				if (DiyVendor.HasOffer(store, address, slot)) {
					D_DiyOffer.Show(player, vendor, address, slot);
				}
				Show(player, vendor, address, (string) args[2]);
			}
		}
	}

	/// <summary>d_dyi_vendor_nabidka: the goods of one offer, what they cost, what the player has or can afford, the most one
	/// deal may take; a line's button opens the contract.</summary>
	public class D_DiyOffer : CompiledGumpDef {
		internal const int FirstLineButton = 10;
		private const int Row = 19, Edge = 13;

		internal static void Show(Player player, NPC vendor, int address, int slot) {
			vendor.Dialog(player, SingletonScript<D_DiyOffer>.Instance, new DialogArgs(vendor, address, slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Player player = (Player) sendTo;
			Thing store = DiyVendor.Store(vendor);
			int count = DiyVendor.GoodsCount(store, address, slot);
			bool vykup = DiyVendor.OfferKind(store, address, slot) == DiyVendor.Vykup;
			int height = (count + 4) * 21;
			this.SetLocation(285, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 800, height + 30, 2620);
			this.AddLabel(300, 12, 400, DiyVendor.OfferName(store, address, slot));
			this.AddLabel(50, 34, 20, vykup ? "prodat" : "koupit");
			this.AddLabel(470, 34, 20, "Cena ks/gp");
			this.AddLabel(590, 34, 20, vykup ? "mas na prodej" : "muzes si dovolit");
			this.AddLabel(710, 34, 20, vykup ? "ke koupi" : "na prodej");
			long gold = GoldUtils.CountGoldOn(player);
			for (int k = 1; k <= count; k++) {
				DiyGoods goods = DiyVendor.GoodsForShow(store, address, slot, k);
				int y = (k + 2) * Row;
				this.AddButton(Edge, y, 0xfa5, 0xfa7, FirstLineButton + k, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, 100, goods.Name);
				this.AddLabel(470, y, 100, goods.Price.ToString(CultureInfo.InvariantCulture));
				string third;
				if (vykup) {
					ThingDef def = ThingDef.GetByDefname(goods.Defname);
					third = ((def == null) ? 0 : DiyVendor.CountIn(player.Backpack, def)) + " ks";
				} else {
					third = (goods.Price == 0) ? "INF." : ((gold / goods.Price) + " ks");
				}
				this.AddLabel(vykup || (goods.Price == 0) ? 600 : 580, y, 100, third);
				this.AddLabel(720, y, 100, goods.Current.ToString(CultureInfo.InvariantCulture));
			}
			this.AddButton(Edge, height + 5, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(Edge + 32, height + 5, 42, "zavrit");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Player player = (Player) gi.Cont;
			Thing store = DiyVendor.Store(vendor);
			int k = gr.PressedButton - FirstLineButton;
			if ((k < 1) || (k > DiyVendor.GoodsCount(store, address, slot)) || vendor.IsDeleted ||
					(Point2D.GetSimpleDistance(vendor, player) >= DiyVendor.DialogReach)) {
				return;
			}
			Show(player, vendor, address, slot);
			D_DiyContract.Show(player, vendor, DiyVendor.OfferKind(store, address, slot), DiyVendor.Goods(store, address, slot, k));
		}
	}

	/// <summary>d_dyi_vendor_smlouva: how many, then the deal, on the line as it was when the contract opened.</summary>
	public class D_DiyContract : CompiledGumpDef {
		internal const int OkButton = 1, CountEntry = 10;

		internal static void Show(Player player, NPC vendor, int kind, DiyGoods goods) {
			vendor.Dialog(player, SingletonScript<D_DiyContract>.Instance, new DialogArgs(vendor, kind, goods));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int kind = (int) args[1];
			DiyGoods goods = (DiyGoods) args[2];
			this.SetLocation(385, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 350, 115, 2620);
			this.AddLabel(140, 14, 400, "SMLOUVA");
			this.AddLabel(20, 34, 400, ((kind == DiyVendor.Vykup) ? "Smlouva o prodeji \"" : "Smlouva o nakupu \"") + goods.Name + "\"");
			this.AddLabel(20, 58, 400, "POCET");
			this.AddImage(85, 57, 0x98d);
			this.AddTextEntry(88, 58, 220, 19, 120, CountEntry, "0");
			this.AddButton(13, 80, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			this.AddButton(310, 80, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton != OkButton) {
				return;
			}
			NPC vendor = (NPC) args[0];
			if (!vendor.IsDeleted) {
				DiyVendor.Deal(vendor, (Player) gi.Cont, (int) args[1], (DiyGoods) args[2], gr.GetTextResponse(CountEntry));
			}
		}
	}
}
