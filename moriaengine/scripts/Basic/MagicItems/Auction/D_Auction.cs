using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1070 - the auction's list: own realm's listings (a GM sees all), ten to a page, soonest end first. Pages are
	/// sent anew, no in-place redraw, for the 3.0.6m clients.</summary>
	public class D_Auction : CompiledGumpDef {
		internal const int PrevButton = 1, NextButton = 2, FirstLine = 100, PerPage = 10;
		private const int LabelHue = 2301, ValueHue = 52;

		internal static void Show(Player player, int page) {
			player.Dialog(player, SingletonScript<D_Auction>.Instance, new DialogArgs(page, AuctionHouse.Listings(player)));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int page = (int) args[0];
			List<Item> listings = (List<Item>) args[1];
			Player player = (Player) sendTo;
			this.SetLocation(50, 50);
			this.AddPage(0);
			this.AddBackground(0, 0, 620, 330, 2620);
			this.AddLabel(16, 12, 42, "Aukce magickych predmetu" + (player.IsGM ? " (GM: vsechny realmy)" : ""));
			this.AddLabel(16, 36, LabelHue, "Predmet");
			this.AddLabel(300, 36, LabelHue, "Nabidka");
			this.AddLabel(400, 36, LabelHue, "Vykup");
			this.AddLabel(500, 36, LabelHue, "Konci za");
			if (listings.Count == 0) {
				this.AddLabel(16, 62, LabelHue, "V aukci nic neni.");
			}
			for (int i = page * PerPage; (i < listings.Count) && (i < (page + 1) * PerPage); i++) {
				Item item = listings[i];
				int y = 62 + ((i - (page * PerPage)) * 22);
				bool bid = AuctionHouse.Bidder(item) != null;
				int buyout = AuctionHouse.Buyout(item);
				this.AddButton(16, y, 0xfa5, 0xfa7, FirstLine + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, ValueHue, AuctionHouse.ShownName(item));
				this.AddLabel(300, y, ValueHue, Number(bid ? AuctionHouse.Bid(item) : AuctionHouse.Start(item)) + (bid ? "" : " (od)"));
				this.AddLabel(400, y, ValueHue, (buyout > 0) ? Number(buyout) : "-");
				this.AddLabel(500, y, ValueHue, AuctionHouse.TimeLeft(item));
			}
			if (page > 0) {
				this.AddButton(16, 295, 0xfae, 0xfb0, PrevButton, GumpButtonType.Reply, 0);
				this.AddLabel(50, 295, LabelHue, "Predchozi");
			}
			if ((page + 1) * PerPage < listings.Count) {
				this.AddButton(500, 295, 0xfa5, 0xfa7, NextButton, GumpButtonType.Reply, 0);
				this.AddLabel(534, 295, LabelHue, "Dalsi");
			}
		}

		internal static string Number(int n) {
			return n.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int page = (int) args[0];
			List<Item> listings = (List<Item>) args[1];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (button == PrevButton) {
				Show(player, page - 1);
			} else if (button == NextButton) {
				Show(player, page + 1);
			} else if ((button >= FirstLine) && (button - FirstLine < listings.Count)) {
				Item item = listings[button - FirstLine];
				if (AuctionHouse.IsListed(item)) {
					D_AuctionItem.Show(player, item);
				} else {
					player.SysMessage("Tato aukce uz skoncila.");
					Show(player, page);
				}
			}
		}
	}

	/// <summary>#1070 - one listing: what it is, who sells it, the bid, the buyout, the time; Prihodit, Koupit, Stahnout.</summary>
	public class D_AuctionItem : CompiledGumpDef {
		internal const int BidButton = 1, BuyButton = 2, WithdrawButton = 3, BackButton = 4, BidEntry = 10;
		private const int LabelHue = 2301, ValueHue = 52;

		internal static void Show(Player player, Item item) {
			player.Dialog(player, SingletonScript<D_AuctionItem>.Instance, new DialogArgs(item));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item item = (Item) args[0];
			Player player = (Player) sendTo;
			List<string> info = MagicItems.InfoLines(item);
			Character seller = AuctionHouse.Seller(item), bidder = AuctionHouse.Bidder(item);
			int buyout = AuctionHouse.Buyout(item);
			int height = 252 + (info.Count * 20);
			this.SetLocation(80, 80);
			this.AddPage(0);
			this.AddBackground(0, 0, 420, height, 2620);
			this.AddLabel(16, 12, 42, item.Name);
			int y = 36;
			foreach (string line in info) {
				this.AddLabel(16, y, ValueHue, line);
				y += 20;
			}
			y += 6;
			string material = AuctionHouse.MaterialOf(item);
			if (material != null) {
				this.Row(ref y, "Material:", material);
			}
			this.Row(ref y, "Prodava:", (seller == null) ? "?" : (seller.Name + " (" + AuctionHouse.RealmOf(item) + ")"));
			this.Row(ref y, "Nabidka:", (bidder == null) ? ("zadna, od " + D_Auction.Number(AuctionHouse.Start(item))) :
				(D_Auction.Number(AuctionHouse.Bid(item)) + ((bidder == player) ? " (tvoje)" : "")));
			this.Row(ref y, "Vykupni cena:", (buyout > 0) ? D_Auction.Number(buyout) : "bez vykupu");
			this.Row(ref y, "Konci za:", AuctionHouse.TimeLeft(item));
			int min = AuctionHouse.MinBid(item);
			this.AddLabel(16, y, LabelHue, "Prihodit:");
			this.AddImage(130, y - 1, 0x98d);
			this.AddTextEntry(134, y, 100, 20, ValueHue, BidEntry, min.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(250, y, LabelHue, "(nejmene " + D_Auction.Number(min) + ")");
			y += 30;
			this.AddButton(16, y, 0xfa5, 0xfa7, BidButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, LabelHue, "Prihodit");
			if (buyout > 0) {
				this.AddButton(130, y, 0xfa5, 0xfa7, BuyButton, GumpButtonType.Reply, 0);
				this.AddLabel(164, y, LabelHue, "Koupit");
			}
			if (player.IsGM || ((seller == player) && (bidder == null))) {
				this.AddButton(230, y, 0xfa5, 0xfa7, WithdrawButton, GumpButtonType.Reply, 0);
				this.AddLabel(264, y, LabelHue, "Stahnout");
			}
			this.AddButton(330, y, 0xfa5, 0xfa7, BackButton, GumpButtonType.Reply, 0);
			this.AddLabel(364, y, LabelHue, "Zpet");
		}

		private void Row(ref int y, string label, string value) {
			this.AddLabel(16, y, LabelHue, label);
			this.AddLabel(130, y, ValueHue, value);
			y += 22;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Item item = (Item) args[0];
			Player player = (Player) gi.Cont;
			switch (gr.PressedButton) {
				case BidButton:
					int.TryParse((gr.GetTextResponse(BidEntry) ?? "").Trim().Replace(".", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount);
					AuctionHouse.PlaceBid(player, item, amount);
					break;
				case BuyButton:
					AuctionHouse.BuyOut(player, item);
					break;
				case WithdrawButton:
					AuctionHouse.Withdraw(player, item);
					break;
				case BackButton:
					D_Auction.Show(player, 0);
					break;
			}
		}
	}
}
