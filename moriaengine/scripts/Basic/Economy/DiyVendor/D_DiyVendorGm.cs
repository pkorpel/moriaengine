using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1070 - d_dyi_vendor_gmmenu: portrait, name, the intro, then the six buttons and ten offers of the page the GM
	/// has clicked to. Renaming renames the vendor; Erebor's renamed src.targ, whatever the GM last targeted.</summary>
	public class D_DiyVendorGm : CompiledGumpDef {
		internal const int HomeButton = 10, IntroButton = 11, PortraitButton = 70, NameButton = 71;
		internal const int EditButton = 71, RemoveButton = 77, OpenOffer = 83, EditOffer = 93, RemoveOffer = 103, SubButton = 113;
		private static readonly int[,] slotSpots = { { 85, 330 }, { 485, 330 }, { 885, 330 }, { 85, 430 }, { 485, 430 }, { 885, 430 } };

		internal static void Show(Player gm, NPC vendor, int address) {
			vendor.Dialog(gm, SingletonScript<D_DiyVendorGm>.Instance, new DialogArgs(vendor, address));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1];
			Thing store = DiyVendor.Store(vendor);
			this.SetLocation(0, 0);
			this.AddPage(0);
			this.AddBackground(20, 200, 1200, 800, 3000);
			this.AddButton(200, 230, 0xfa5, 0xfa7, PortraitButton, GumpButtonType.Reply, 0);
			this.AddLabel(255, 230, 20, "Nastavit obrazek(cislo 3000-3164)");
			this.AddImage(70, 230, 0x98d);
			this.AddTextEntry(75, 231, 100, 50, 90, PortraitButton, DiyVendor.Portrait(store).ToString(CultureInfo.InvariantCulture));
			this.AddButton(200, 260, 0xfa5, 0xfa7, IntroButton, GumpButtonType.Reply, 0);
			this.AddLabel(255, 260, 20, "text k nultemu gumpu");
			this.AddButton(200, 290, 0xfa5, 0xfa7, NameButton, GumpButtonType.Reply, 0);
			this.AddLabel(255, 290, 20, "Zmenit jmeno");
			this.AddImage(70, 290, 0x98d);
			this.AddTextEntry(75, 291, 100, 50, 90, NameButton, vendor.Name);
			this.AddHtml(500, 220, 700, 100, "Uvedomte si, ze pridavate button a nabidku v te casti gumpu, kam jste se proklikali. Pokud zvolite button " +
				"v gumpu 0, bude viditelny ze vsech ostatnich gumpu (adresa != 0) a jinde uz jej nepujde pridat. Nastavujte spravne, zda je item " +
				"stackovatelny nebo ne.", false, true);
			this.AddLabel(28, 305, 350, "--------------------------------------------Edit buttonu,aktualni adresa:  " + address + "  ----------------------------------");
			for (int slot = 1; slot <= DiyVendor.Buttons; slot++) {
				int x = slotSpots[slot - 1, 0], y = slotSpots[slot - 1, 1];
				if (DiyVendor.HasButton(store, 0, slot)) {
					this.AddButton(x, y, 0x988, 0x988, slot, GumpButtonType.Reply, 0);
					this.AddLabel(x + 8, y + 2, 50, DiyVendor.ButtonLabel(store, 0, slot));
				} else if (DiyVendor.HasButton(store, address, slot)) {
					this.AddButton(x, y, 0x988, 0x988, SubButton + slot, GumpButtonType.Reply, 0);
					this.AddLabel(x + 8, y + 2, 50, DiyVendor.ButtonLabel(store, address, slot));
				}
				this.AddButton(x - 30, y + 30, 0xfa5, 0xfa7, EditButton + slot, GumpButtonType.Reply, 0);
				this.AddLabel(x, y + 30, 800, "Editovat button");
				this.AddButton(x - 30, y + 60, 0xfa5, 0xfa7, RemoveButton + slot, GumpButtonType.Reply, 0);
				this.AddLabel(x, y + 60, 800, "Smaz button a vse co je v podmenu");
			}
			this.AddLabel(28, 530, 350, "--------------------------------------------Nabidky v buttonu adresa:  " + address + "  ----------------------------------");
			for (int slot = 1; slot <= DiyVendor.Offers; slot++) {
				int x = (slot <= 5) ? 55 : 655, y = 555 + (((slot - 1) % 5) * 80);
				if (DiyVendor.HasOffer(store, address, slot)) {
					this.AddButton(x, y, 0xfa5, 0xfa7, OpenOffer + slot, GumpButtonType.Reply, 0);
					this.AddLabel(x + 30, y, 50, DiyVendor.OfferName(store, address, slot));
					this.AddButton(x, y + 40, 0xfa5, 0xfa7, RemoveOffer + slot, GumpButtonType.Reply, 0);
					this.AddLabel(x + 30, y + 40, 800, "Smaz nabidku a jeji obsah");
				}
				this.AddButton(x, y + 20, 0xfa5, 0xfa7, EditOffer + slot, GumpButtonType.Reply, 0);
				this.AddLabel(x + 30, y + 20, 800, "Editovat nabidku");
			}
			this.AddButton(1190, 970, 0x5780, 0x5781, HomeButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1];
			Player gm = (Player) gi.Cont;
			int button = gr.PressedButton;
			if ((button == 0) || vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			Thing store = DiyVendor.Store(vendor);
			if (button == HomeButton) {
				Show(gm, vendor, 0);
			} else if (button == IntroButton) {
				D_DiyIntroEdit.Show(gm, vendor, address);
			} else if (button == PortraitButton) {
				DiyVendor.SetPortrait(store, DiyVendor.ToNumber(gr.GetTextResponse(PortraitButton)));
			} else if (button == NameButton) {
				vendor.Name = gr.GetTextResponse(NameButton);
			} else if ((button >= 1) && (button <= DiyVendor.Buttons)) {
				Show(gm, vendor, DiyVendor.ButtonChild(store, 0, button));
			} else if ((button > SubButton) && (button <= SubButton + DiyVendor.Buttons)) {
				Show(gm, vendor, DiyVendor.ButtonChild(store, address, button - SubButton));
			} else if ((button > EditButton) && (button <= EditButton + DiyVendor.Buttons)) {
				int slot = button - EditButton;
				if (DiyVendor.SlotTakenAtTop(store, address, slot)) {
					gm.SysMessage("pozice buttonu je jiz v zacatecnim(gump_0)obsazena.Nelze pridat ");
				} else {
					D_DiyButtonEdit.Show(gm, vendor, address, slot);
				}
			} else if ((button > RemoveButton) && (button <= RemoveButton + DiyVendor.Buttons)) {
				DiyVendor.RemoveButton(store, address, button - RemoveButton);
			} else if ((button > OpenOffer) && (button <= OpenOffer + DiyVendor.Offers)) {
				D_DiyOfferGm.Show(gm, vendor, address, button - OpenOffer);
				Show(gm, vendor, address);
			} else if ((button > EditOffer) && (button <= EditOffer + DiyVendor.Offers)) {
				D_DiyOfferEdit.Show(gm, vendor, address, button - EditOffer);
			} else if ((button > RemoveOffer) && (button <= RemoveOffer + DiyVendor.Offers)) {
				DiyVendor.RemoveOffer(store, address, button - RemoveOffer);
			}
		}
	}

	/// <summary>d_diy_hlavni_buttony_edit: a button's label and its page's text.</summary>
	public class D_DiyButtonEdit : CompiledGumpDef {
		internal const int SetButton = 1, LabelEntry = 10, TextEntryId = 11;

		internal static void Show(Player gm, NPC vendor, int address, int slot) {
			vendor.Dialog(gm, SingletonScript<D_DiyButtonEdit>.Instance, new DialogArgs(vendor, address, slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Thing store = DiyVendor.Store(vendor);
			this.SetLocation(285, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 1200, 110, 2620);
			this.AddLabel(150, 12, 30, "Nastaveni buttonu");
			this.AddLabel(13, 34, 42, "Nazev buttonu:");
			this.AddTextEntry(120, 34, 100, 19, 100, LabelEntry, DiyVendor.ButtonLabel(store, address, slot));
			this.AddLabel(13, 56, 42, "Text:");
			this.AddTextEntry(120, 56, 1000, 19, 100, TextEntryId, DiyVendor.ButtonText(store, address, slot));
			this.AddButton(13, 80, 0xfa5, 0xfa7, SetButton, GumpButtonType.Reply, 0);
			this.AddLabel(63, 80, 42, "Nastavit");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Player gm = (Player) gi.Cont;
			if (vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			if (gr.PressedButton == SetButton) {
				DiyVendor.SetButton(DiyVendor.Store(vendor), address, slot, gr.GetTextResponse(LabelEntry), gr.GetTextResponse(TextEntryId));
			}
			D_DiyVendorGm.Show(gm, vendor, address);
		}
	}

	/// <summary>d_diy_uvodnitxt_edit: the text of the top page.</summary>
	public class D_DiyIntroEdit : CompiledGumpDef {
		internal const int SetButton = 1, TextEntryId = 1;

		internal static void Show(Player gm, NPC vendor, int address) {
			vendor.Dialog(gm, SingletonScript<D_DiyIntroEdit>.Instance, new DialogArgs(vendor, address));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			this.SetLocation(285, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 600, 90, 2620);
			this.AddLabel(200, 12, 2, "Zadani uvodniho textu");
			this.AddLabel(13, 34, 3, "text");
			this.AddTextEntry(60, 34, 420, 19, 100, TextEntryId, DiyVendor.Intro(DiyVendor.Store(vendor)));
			this.AddButton(13, 57, 0xfa5, 0xfa7, SetButton, GumpButtonType.Reply, 0);
			this.AddLabel(48, 57, 4, "Nastav");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			Player gm = (Player) gi.Cont;
			if ((gr.PressedButton != SetButton) || vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			DiyVendor.SetIntro(DiyVendor.Store(vendor), gr.GetTextResponse(TextEntryId));
			D_DiyVendorGm.Show(gm, vendor, (int) args[1]);
		}
	}

	/// <summary>d_diy_hlavni_nabidka_edit: an offer's name, and whether the vendor buys (VYKUP) or sells (PRODEJ).</summary>
	public class D_DiyOfferEdit : CompiledGumpDef {
		internal const int VykupButton = 1, ProdejButton = 2, NameEntry = 1;

		internal static void Show(Player gm, NPC vendor, int address, int slot) {
			vendor.Dialog(gm, SingletonScript<D_DiyOfferEdit>.Instance, new DialogArgs(vendor, address, slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			this.SetLocation(285, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 600, 110, 2620);
			this.AddLabel(200, 12, 30, "Nastaveni nabidky");
			this.AddLabel(13, 34, 42, "nazev nabidky:");
			this.AddTextEntry(120, 34, 420, 19, 100, NameEntry, DiyVendor.OfferName(DiyVendor.Store(vendor), address, slot));
			this.AddButton(13, 58, 0xfa5, 0xfa7, VykupButton, GumpButtonType.Reply, 0);
			this.AddLabel(63, 58, 42, "VYKUP   (Nastavi nabidku jako vykup.Veci v nabidce bude vendor vykupovat.)");
			this.AddButton(13, 80, 0xfa5, 0xfa7, ProdejButton, GumpButtonType.Reply, 0);
			this.AddLabel(63, 80, 42, "PRODEJ  (Nastavi nabidku jako prodej.Veci v nabidce bude vendor prodavat.)");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Player gm = (Player) gi.Cont;
			if (vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			if ((gr.PressedButton == VykupButton) || (gr.PressedButton == ProdejButton)) {
				DiyVendor.SetOffer(DiyVendor.Store(vendor), address, slot, gr.GetTextResponse(NameEntry),
					(gr.PressedButton == VykupButton) ? DiyVendor.Vykup : DiyVendor.Prodej);
			}
			D_DiyVendorGm.Show(gm, vendor, address);
		}
	}

	/// <summary>d_diy_nabidka_gm: an offer's lines, to delete, edit or add by targeting an item.</summary>
	public class D_DiyOfferGm : CompiledGumpDef {
		internal const int AddGoodsButton = 1, FirstRemove = 10, FirstEdit = 50;
		private const int Row = 19, Edge = 13;

		internal static void Show(Player gm, NPC vendor, int address, int slot) {
			vendor.Dialog(gm, SingletonScript<D_DiyOfferGm>.Instance, new DialogArgs(vendor, address, slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Thing store = DiyVendor.Store(vendor);
			int count = DiyVendor.GoodsCount(store, address, slot);
			int height = (count + 4) * 21;
			this.SetLocation(285, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 800, height + 30, 2620);
			this.AddLabel(300, 12, 400, DiyVendor.OfferName(store, address, slot));
			this.AddLabel(13, 34, 20, "Smazat");
			this.AddLabel(348, 34, 20, "edit");
			this.AddLabel(385, 34, 20, "cena");
			this.AddLabel(488, 34, 20, "max.ks.");
			this.AddLabel(591, 34, 20, "respawn");
			this.AddLabel(690, 34, 20, "ks/respawn");
			for (int k = 1; k <= count; k++) {
				DiyGoods goods = DiyVendor.Goods(store, address, slot, k);
				int y = (k + 2) * Row;
				this.AddButton(Edge, y, 0xfa5, 0xfa7, FirstRemove + k, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, 100, goods.Name);
				this.AddButton(348, y, 0xfa5, 0xfa7, FirstEdit + k, GumpButtonType.Reply, 0);
				this.AddLabel(385, y, 100, goods.Price.ToString(CultureInfo.InvariantCulture));
				this.AddLabel(488, y, 100, goods.Max.ToString(CultureInfo.InvariantCulture));
				this.AddLabel(591, y, 100, goods.Respawn.ToString(CultureInfo.InvariantCulture));
			}
			this.AddButton(Edge, height + 5, 0xfa5, 0xfa7, AddGoodsButton, GumpButtonType.Reply, 0);
			this.AddLabel(Edge + 32, height + 5, 42, "Pridej zbozi");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2];
			Player gm = (Player) gi.Cont;
			int button = gr.PressedButton;
			if ((button == 0) || vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			Thing store = DiyVendor.Store(vendor);
			int count = DiyVendor.GoodsCount(store, address, slot);
			if (button == AddGoodsButton) {
				if (count < DiyVendor.MaxGoods) {
					gm.Target(SingletonScript<Targ_DiyVendorGoods>.Instance, new object[] { vendor, address, slot });
					return;
				}
				gm.SysMessage("Nabidka je jiz plna");
			} else if ((button > FirstEdit) && (button <= FirstEdit + count)) {
				D_DiyGoodsEdit.Show(gm, vendor, address, slot, button - FirstEdit);
			} else if ((button > FirstRemove) && (button <= FirstRemove + count)) {
				DiyVendor.RemoveGoods(store, address, slot, button - FirstRemove);
			}
			Show(gm, vendor, address, slot);
		}
	}

	/// <summary>d_diy_vendor_uprava: one line's fields, and whether it's handed out as one stack.</summary>
	public class D_DiyGoodsEdit : CompiledGumpDef {
		internal const int SetButton = 1, StackButton = 2, NoStackButton = 3, FirstEntry = 10;
		private static readonly string[] labels = { "Jmeno", "'ID' predmetu", "Cena jednoho kusu", "Aktualni pocet kusu", "Maximalni pocet kusu", "Doba spawnu(desitky minut)" };

		internal static void Show(Player gm, NPC vendor, int address, int slot, int k) {
			vendor.Dialog(gm, SingletonScript<D_DiyGoodsEdit>.Instance, new DialogArgs(vendor, address, slot, k));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			DiyGoods goods = DiyVendor.Goods(DiyVendor.Store(vendor), (int) args[1], (int) args[2], (int) args[3]);
			string[] values = { goods.Name, goods.Defname, Number(goods.Price), Number(goods.Current), Number(goods.Max), Number(goods.Respawn) };
			this.SetLocation(300, 300);
			this.AddPage(0);
			this.AddBackground(0, 0, 350, 250, 2620);
			this.AddLabel(120, 12, 400, "UPRAVA ZBOZI");
			for (int i = 0; i < labels.Length; i++) {
				int y = 34 + (i * 22);
				this.AddLabel(13, y, 60, labels[i]);
				this.AddTextEntry((i < 2) ? 140 : ((i < 5) ? 190 : 240), y, 220, 19, 100, FirstEntry + i, values[i]);
			}
			this.AddLabel(53, 167, 60, "NEstuckovatelny item");
			this.AddLabel(53, 187, 60, "Stuckovatelny item");
			if (goods.Stacks) {
				this.AddButton(13, 167, 0xfa5, 0xfa7, NoStackButton, GumpButtonType.Reply, 0);
				this.AddImage(13, 187, 9904);
			} else {
				this.AddImage(13, 167, 9904);
				this.AddButton(13, 187, 0xfa5, 0xfa7, StackButton, GumpButtonType.Reply, 0);
			}
			this.AddButton(13, 215, 0xfa5, 0xfa7, SetButton, GumpButtonType.Reply, 0);
			this.AddLabel(45, 215, 42, "NASTAVIT");
		}

		private static string Number(int n) {
			return n.ToString(CultureInfo.InvariantCulture);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			NPC vendor = (NPC) args[0];
			int address = (int) args[1], slot = (int) args[2], k = (int) args[3];
			Player gm = (Player) gi.Cont;
			int button = gr.PressedButton;
			if ((button == 0) || vendor.IsDeleted || !gm.IsGM) {
				return;
			}
			Thing store = DiyVendor.Store(vendor);
			if (k > DiyVendor.GoodsCount(store, address, slot)) {
				return;
			}
			DiyGoods goods = DiyVendor.Goods(store, address, slot, k);
			goods.Name = gr.GetTextResponse(FirstEntry);
			goods.Defname = gr.GetTextResponse(FirstEntry + 1);
			goods.Price = DiyVendor.ToNumber(gr.GetTextResponse(FirstEntry + 2));
			goods.Current = DiyVendor.ToNumber(gr.GetTextResponse(FirstEntry + 3));
			goods.Max = DiyVendor.ToNumber(gr.GetTextResponse(FirstEntry + 4));
			goods.Respawn = DiyVendor.ToNumber(gr.GetTextResponse(FirstEntry + 5));
			if (button == StackButton) {
				goods.Stacks = true;
			} else if (button == NoStackButton) {
				goods.Stacks = false;
			}
			DiyVendor.SetGoods(store, address, slot, k, goods);
			if (button != SetButton) {
				Show(gm, vendor, address, slot, k);
			}
		}
	}
}
