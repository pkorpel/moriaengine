using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_npc_alchymista, its pages and Erebor's texts. Koupit reads the counts on every page.</summary>
	public class D_Alchemist : CompiledGumpDef {
		internal const int BuyButton = 1, FirstCount = 1;
		private const int TextHue = 1152, ListHue = 152, Row = 25;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC alchemist = (NPC) args[0];
			Player player = (Player) sendTo;
			bool met = AlchemistShop.HaveMet(player, alchemist);
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, 0x3116);
			this.AddLabel(220, 210, 52, alchemist.Name + ", Nejvyssi Akolyta");
			this.AddButton(160, 330, 0x988, 0x988, 0, GumpButtonType.Page, 2);
			this.AddLabel(met ? 182 : 215, 332, TextHue, met ? "Jak jdou obchody?" : "Kdo Jsi?");
			this.AddButton(325, 330, 0x988, 0x988, 0, GumpButtonType.Page, 3);
			this.AddLabel(360, 332, TextHue, "Mohu Pomoci?");
			this.AddButton(490, 330, 0x988, 0x988, 0, GumpButtonType.Page, 4);
			this.AddLabel(538, 332, TextHue, "Obchodovat?");
			this.AddButton(490, 360, 0x988, 0x988, 0, GumpButtonType.Page, 7);
			this.AddLabel(540, 361, 142, "Prohlednout");

			this.AddPage(1);
			this.Html(met ? " Radost te tu znovu videt! Nejake prani?" : " Vitam te! Mohu ti neco nabidnout?");
			this.AddPage(2);
			this.Html(met ? " Aah dekuji ti za optani, jde to skvele ! Poptavka je stale vysoka, to nemohu rict." :
				" Obchoduji s nejruznejsimi alchymistickymi vyrobky, ktere nikde jinde ve Stredozemi nesezenes. Prodavam Krystaly videni a Elixiry poznani.");
			AlchemistShop.Meet(player, alchemist);
			this.AddPage(3);
			this.Html(" Hmm... Nevim.. Opravdu nevim o nicem, s cim by jsi mi ted " + (player.IsFemale ? "mohla" : "mohl") + " pomoci, je mi lito, vypadas schopne...");

			this.AddPage(4);
			this.Shop();
			this.Html(" Tak se na to podivame... Co bys " + (player.IsFemale ? "rada" : "rad") + "?<br> Zbozi, ktere se ti libi si nasbirej a ja ti to napocitam.");
			this.Category(395, 8, "Elixir poznani", null);
			this.Category(415, 9, "Krystal videni", null);
			this.Category(435, 10, "Lektvary vnimani", "NOVINKA");
			this.Category(455, 11, "Artefakty zivota", "Svice zivota a Schranka na duse");

			this.AddPage(7);
			this.Html("Moudry starik s dlouhym plnovousem.<br> Od prvniho pohledu je na tomto muzi neco tajemneho.");

			this.AddPage(8);
			this.Shop();
			this.Html("Po vypiti tohoto Elixiru, zjistis nasledujicim uderem odolnosti prisery, proti ktere stojis.");
			this.Wares(0, 1);
			this.AddPage(9);
			this.Shop();
			this.Html("Aktivace tohoto krystalu ti umozni po nasledujicim uderu odhalit zranitelnost prisery, proti ktere stojis.");
			this.Wares(1, 1);
			this.AddPage(10);
			this.Shop();
			this.Html("Toto je uplna novinka. Vypitim tohoto odvaru se ti na urcitou dobu rozsiri tve vnimani okoli.");
			this.Wares(2, 4);
			this.AddPage(11);
			this.Shop();
			this.Html("Schopny knez s pouzitim Svice zivota se dokaze navratit k zivotu.<br>A Shaman umi polapit dusi bojovnika do Schranky na duse a tim ho dokaze privolat zpet.");
			this.Wares(6, 2);
		}

		private void Html(string text) {
			this.AddHtml(170, 230, 440, 100, text, false, true);
		}

		private void Shop() {
			this.AddButton(160, 360, 0x988, 0x988, 0, GumpButtonType.Page, 4);
			this.AddLabel(202, 361, 777, "Dalsi zbozi");
			this.AddButton(325, 360, 0x988, 0x988, BuyButton, GumpButtonType.Reply, 0);
			this.AddLabel(385, 361, 777, "Koupit");
		}

		private void Category(int y, int page, string name, string note) {
			this.AddButton(25, y, 0xfa5, 0xfa7, 0, GumpButtonType.Page, page);
			this.AddLabel(60, y, ListHue, name);
			if (note != null) {
				this.AddLabel(210, y, 142, note);
			}
		}

		private void Wares(int first, int count) {
			for (int line = 0; line < count; line++) {
				AlchemistWare ware = AlchemistShop.Wares[first + line];
				int y = 395 + (line * Row);
				this.AddImage(202, y, 0x98d);
				this.AddLabel(60, y, ListHue, ware.Name + ":");
				this.AddTextEntry(210, y + 1, 90, 20, 52, FirstCount + first + line, "");
				this.AddLabel(308, y, ListHue, "ks");
				this.AddLabel(380, y, ListHue, "cena/ks :");
				this.AddLabel(480, y, ListHue, ware.Price.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".") + " gp");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC alchemist = (NPC) args[0];
			Player player = (Player) gi.Cont;
			if (gr.PressedButton != BuyButton) {
				alchemist.Say("Bylo mi potesenim");
				return;
			}
			if (player.HasTimer(AlchemistShop.OrderTK)) {
				player.RedMessage("Nejdrive si vyrid prvni objednavku");
				return;
			}
			int[] counts = new int[AlchemistShop.Wares.Length];
			for (int i = 0; i < counts.Length; i++) {
				counts[i] = AlchemistShop.ParseCount(gr.GetTextResponse(FirstCount + i));
			}
			AlchemistOrderTimer order = new AlchemistOrderTimer();
			order.alchemist = alchemist;
			order.counts = counts;
			player.AddTimer(AlchemistShop.OrderTK, order);
			order.DueInSeconds = 600;
			D_AlchemistOrder.Show(player);
		}
	}

	/// <summary>d_npc_alchymista_souhrn: the shopping list, with a button to strike a line, and the money.</summary>
	public class D_AlchemistOrder : CompiledGumpDef {
		internal const int CancelButton = 0, PayButton = 13;

		internal static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_AlchemistOrder>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			AlchemistOrderTimer order = player.GetTimer(AlchemistShop.OrderTK) as AlchemistOrderTimer;
			int[] counts = (order == null) ? new int[AlchemistShop.Wares.Length] : order.counts;
			this.SetLocation(205, 150);
			this.AddPage(0);
			this.AddBackground(0, 0, 396, 380, 2620);
			this.AddLabel(16, 12, 42, "Nakupni Listek");
			this.AddButton(356, 9, 0xfb1, 0xfb3, CancelButton, GumpButtonType.Reply, 0);
			this.AddLabel(16, 36, 2301, "Pocet ks:");
			this.AddLabel(92, 36, 2301, "Jmeno Polozky:");
			this.AddLabel(272, 36, 2301, "Cena:");
			int line = 0;
			for (int k = 0; k < counts.Length; k++) {
				if (counts[k] > 0) {
					AlchemistWare ware = AlchemistShop.Wares[k];
					int y = 62 + (line * 20);
					this.AddLabel(16, y, 2301, counts[k].ToString(CultureInfo.InvariantCulture));
					this.AddLabel(85, y, 2301, ware.Name);
					this.AddLabel(272, y, 2301, (counts[k] * ware.Price).ToString(CultureInfo.InvariantCulture));
					this.AddButton(355, y, 4005, 4007, k + 1, GumpButtonType.Reply, 0);
					line++;
				}
			}
			long account = player.Gold;
			int total = (int) Reputation.PriceFor(player, AlchemistShop.Total(counts)); //#1243 - what Deliver will charge
			long left = account - total;
			this.AddLabel(210, 300, 2301, "Soucet:");
			this.AddLabel(272, 300, 52, total.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(214, 320, 2301, "Konto:");
			this.AddLabel(272, 320, 52, account.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(192, 345, 2301, "Zustatek:");
			this.AddLabel(272, 345, (left > 0) ? 2301 : 42, left.ToString(CultureInfo.InvariantCulture));
			this.AddButton(356, 342, 0xfb7, 0xfb9, PayButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			AlchemistOrderTimer order = player.GetTimer(AlchemistShop.OrderTK) as AlchemistOrderTimer;
			int button = gr.PressedButton;
			if (order == null) {
				player.SysMessage("Alchymista zapomnel tvou objednavku, vyber si znovu");
				return;
			}
			if (button == CancelButton) {
				player.SysMessage("Zbozi odmitnuto.");
				player.DeleteTimer(AlchemistShop.OrderTK);
				if (order.alchemist != null) {
					order.alchemist.Say("Bylo mi potesenim, priste se ti snad neco bude hodit.");
				}
			} else if (button == PayButton) {
				player.DeleteTimer(AlchemistShop.OrderTK);
				if ((order.alchemist != null) && !order.alchemist.IsDeleted) {
					AlchemistShop.Deliver(order.alchemist, player, order.counts);
				}
			} else if ((button >= 1) && (button <= order.counts.Length)) {
				order.counts[button - 1] = 0;
				Show(player);
			}
		}
	}
}
