using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1070 - d_npc_drazitel, its pages and Erebor's texts. Koupit reads the counts on every page. Erebor's page 11
	/// showed the crystals' text; it has its own here, and page 10's he/she choice is made.</summary>
	public class D_Prodejce : CompiledGumpDef {
		internal const int BuyButton = 1, FirstCount = 1;
		private const int TextHue = 1152, ListHue = 152, Row = 25;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC prodejce = (NPC) args[0];
			Player player = (Player) sendTo;
			bool met = ProdejceShop.HaveMet(player, prodejce);
			bool she = player.IsFemale;
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, 0x3001);
			this.AddLabel(220, 210, 52, prodejce.Name + ", prodejce");
			this.AddButton(160, 330, 0x988, 0x988, 0, GumpButtonType.Page, 2);
			this.AddLabel(met ? 182 : 215, 332, TextHue, met ? "Jak jdou obchody?" : "Kdo Jsi?");
			this.AddButton(325, 330, 0x988, 0x988, 0, GumpButtonType.Page, 3);
			this.AddLabel(360, 332, TextHue, "Mohu Pomoci?");
			this.AddButton(490, 330, 0x988, 0x988, 0, GumpButtonType.Page, 4);
			this.AddLabel(538, 332, TextHue, "Obchodovat?");
			this.AddButton(490, 360, 0x988, 0x988, 0, GumpButtonType.Page, 7);
			this.AddLabel(540, 361, 142, "Prohlednout");

			this.AddPage(1);
			this.Html(met ? " Radost te tu znovu videt! Nejake prani?" : " Vitam te! Mhou ti neco nabidnout?");
			this.AddPage(2);
			this.Html(met ? " Aah dekuji ti za optani, jde to skvele ! Poptavka je stale vysoka, to nemohu rict." :
				" Jmenuji se " + prodejce.Name + ", obchoduji s nejruznejsimi surovinami. Prodavam kvalitni uhli, vzacne nerosty nebo tetivy, ze kterych si vybere i nejnarocnejsi Ranger! Jen si prohlidni mou nabidku a uvidis " + (she ? "sama" : "sam") + "...");
			ProdejceShop.Meet(player, prodejce);
			this.AddPage(3);
			this.Html(" Hmm... Nevim.. Opravdu nevim o nicem, s cim by jsi mi ted " + (she ? "mohla" : "mohl") + " pomoci, je mi lito, vypadas schopne...");

			this.AddPage(4);
			this.Shop();
			this.Html(" Tak se na to podivame... Co bys " + (she ? "rada" : "rad") + "?<br> Zbozi, ktere se ti libi si nasbirej a ja ti to napocitam.");
			this.Category(395, 8, "Kovarske potreby", null);
			this.Category(415, 9, "Krejcovske potreby", null);
			this.Category(435, 10, "Aktivacni krystaly", "Hole");
			this.Category(455, 11, "Tetivy", "Luky a Kuse");

			this.AddPage(7);
			this.Html(" Statny mladik s obchodnim duchem.<br> Po letmem rozhlednuti jsi " + (she ? "presvedcena" : "presvedcen") +
				", ze tento nenapadny hoch ti muze na miste nabidnout vice zbozi, nez by jsi kdy " + (she ? "byla schopna" : "byl schopen") +
				" ve svem zivote spotrebovat ty spolu se " + (she ? "svym muzem" : "svou zenou") + " a vasimi peti ratolestmi...");

			this.AddPage(8);
			this.Shop();
			this.Html(" Mam tu jen kvalitni a suche uhli! Bud si " + (she ? "jista" : "jist") + ", ze ti s nim vyhen nikdy nevyhasne.<br> Najednou ti muzu nalozit az 1 000 kusu.");
			this.Wares(0, 1);
			this.AddPage(9);
			this.Shop();
			this.Html(" Myslim, ze tyhle nite si zaslouzi tvou pozornost. Mas li zalibu v kvalitnich latkach, tato krejcovska lahudka te jiste neurazi.");
			this.Wares(1, 1);
			this.AddPage(10);
			this.Shop();
			this.Html(" Mam tu velke mnozstvi magickych krystalu, dobry remeslnik vi jak je upnout na hul. V rukou maga je to pak velmi mocna zbran!<br> Neber si vsak vice, jak 100 kusu naraz. Beztak by jsi to " + (she ? "nepobrala" : "nepobral") + ".");
			this.Wares(2, 5);
			this.AddPage(11);
			this.Shop();
			this.Html(" Zde je ma zasoba tetiv. Mam tu par levnych kousku pro nenarocne, ale i drahe, kvalitni a velmi pevne tetivy, ktere udrizi napnutou i tu nejtuzsi kusi!<br> Neber si vsak vice, jak 100 kusu naraz. Beztak by jsi to " + (she ? "nepobrala" : "nepobral") + ".");
			this.Wares(7, 5);
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
				AlchemistWare ware = ProdejceShop.Wares[first + line];
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
			NPC prodejce = (NPC) args[0];
			Player player = (Player) gi.Cont;
			if (gr.PressedButton != BuyButton) {
				prodejce.Say("Bylo mi potesenim");
				return;
			}
			if (player.HasTimer(ProdejceShop.OrderTK)) {
				player.RedMessage("Nejdrive si vyrid prvni objednavku");
				return;
			}
			int[] counts = new int[ProdejceShop.Wares.Length];
			for (int i = 0; i < counts.Length; i++) {
				counts[i] = ProdejceShop.ParseCount(gr.GetTextResponse(FirstCount + i), i);
			}
			ProdejceOrderTimer order = new ProdejceOrderTimer();
			order.prodejce = prodejce;
			order.counts = counts;
			player.AddTimer(ProdejceShop.OrderTK, order);
			order.DueInSeconds = 600;
			D_ProdejceOrder.Show(player);
		}
	}

	/// <summary>d_npc_drazitel_souhrn: the shopping list, a button to strike a line, and the money. Erebor printed the price
	/// column as the unevaluated "n*price"; it's the sum here.</summary>
	public class D_ProdejceOrder : CompiledGumpDef {
		internal const int CancelButton = 0, PayButton = 13;

		internal static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_ProdejceOrder>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			ProdejceOrderTimer order = player.GetTimer(ProdejceShop.OrderTK) as ProdejceOrderTimer;
			int[] counts = (order == null) ? new int[ProdejceShop.Wares.Length] : order.counts;
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
					AlchemistWare ware = ProdejceShop.Wares[k];
					int y = 62 + (line * 20);
					this.AddLabel(16, y, 2301, counts[k].ToString(CultureInfo.InvariantCulture));
					this.AddLabel(85, y, 2301, ware.Name);
					this.AddLabel(272, y, 2301, (counts[k] * ware.Price).ToString(CultureInfo.InvariantCulture));
					this.AddButton(355, y, 4005, 4007, k + 1, GumpButtonType.Reply, 0);
					line++;
				}
			}
			long account = player.Gold;
			int total = (int) Reputation.PriceFor(player, ProdejceShop.Total(counts)); //#1243 - what Deliver will charge
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
			ProdejceOrderTimer order = player.GetTimer(ProdejceShop.OrderTK) as ProdejceOrderTimer;
			int button = gr.PressedButton;
			if (order == null) {
				player.SysMessage("Prodejce zapomnel tvou objednavku, vyber si znovu");
				return;
			}
			if (button == CancelButton) {
				player.SysMessage("Zbozi odmitnuto.");
				player.DeleteTimer(ProdejceShop.OrderTK);
				if (order.prodejce != null) {
					order.prodejce.Say("Bylo mi potesenim, priste se ti snad neco bude hodit.");
				}
			} else if (button == PayButton) {
				player.DeleteTimer(ProdejceShop.OrderTK);
				if ((order.prodejce != null) && !order.prodejce.IsDeleted) {
					ProdejceShop.Deliver(order.prodejce, player, order.counts);
				}
			} else if ((button >= 1) && (button <= order.counts.Length)) {
				order.counts[button - 1] = 0;
				Show(player);
			}
		}
	}
}
