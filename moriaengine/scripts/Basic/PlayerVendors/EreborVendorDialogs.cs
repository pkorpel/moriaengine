using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_vendor: fifteen rows a page, a row's button opens the thing or the section; the owner moves rows up or to
	/// the top, adds goods to this list, reverses it, gives a section its icon or takes an empty one back.</summary>
	public class D_EreborVendor : CompiledGumpDef {
		private const int Width = 640, RowHeight = 30, Hue = 955, Previous = 1, Next = 2, AddMore = 3, RemoveSection = 4, Reverse = 5, Icon = 6;

		public static void Show(Player player, EreborVendor vendor, int page, Item section) {
			player.Dialog(player, SingletonScript<D_EreborVendor>.Instance, new DialogArgs(vendor, page, section));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			int page = (int) args[1];
			Item section = (Item) args[2];
			Player viewer = (Player) sendTo;
			bool owner = vendor.IsOwner(viewer);
			List<Item> entries = vendor.EntriesOf(section);
			int first = page * EreborVendor.RowsAPage;
			int rows = Math.Min(EreborVendor.RowsAPage, Math.Max(entries.Count - first, 0));
			string path = (section == null) ? vendor.Name : vendor.Name + " > " + EreborVendor.Description(section);
			this.SetLocation(10, 10);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 130 + (rows * RowHeight), 2620);
			this.AddLabel(20, 15, Hue, path);
			this.AddButton(Width - 40, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(20, 45, Hue, "kup");
			this.AddLabel(150, 45, Hue, "Nazev zbozi");
			this.AddLabel(Width - 140, 45, Hue, "Cena/gp");
			for (int i = 0; i < rows; i++) {
				Item entry = entries[first + i];
				int y = 70 + (i * RowHeight);
				this.AddButton(20, y, 9905, 9904, 103 + (i * 3), GumpButtonType.Reply, 0);
				if (owner) {
					this.AddButton(45, y, 0x984, 0x983, 101 + (i * 3), GumpButtonType.Reply, 0);
					this.AddButton(65, y, 0x984, 0x983, 102 + (i * 3), GumpButtonType.Reply, 0);
				}
				bool isSection = EreborVendor.IsSection(entry);
				Item content = isSection ? entry : (EreborVendor.Content(entry) ?? entry);
				this.AddItem(90, y, content.Model, content.Color);
				string units = EreborVendor.IsUnit(entry) ? " (" + EreborVendor.Units(entry) + " ks)" : "";
				this.AddLabel(150, y, Hue, EreborVendor.Description(entry) + units);
				if (!isSection) {
					this.AddLabel(Width - 140, y, Hue, EreborVendor.Price(entry).ToString(CultureInfo.InvariantCulture));
				}
			}
			int bottom = 80 + (rows * RowHeight);
			if (first > 0) {
				this.AddButton(Width - 60, bottom, 0xfa, 0xfb, Previous, GumpButtonType.Reply, 0);
			}
			if (first + rows < entries.Count) {
				this.AddButton(Width - 35, bottom, 0xfc, 0xfd, Next, GumpButtonType.Reply, 0);
			}
			if (owner) {
				this.AddButton(20, bottom, 4005, 4007, AddMore, GumpButtonType.Reply, 0);
				this.AddLabel(55, bottom, Hue, "Pridat dalsi zbozi");
				this.AddButton(200, bottom, 4005, 4007, Reverse, GumpButtonType.Reply, 0);
				this.AddLabel(235, bottom, Hue, "Prevratit nabidku");
				if (section != null) {
					this.AddButton(380, bottom, 4005, 4007, Icon, GumpButtonType.Reply, 0);
					this.AddLabel(415, bottom, Hue, "Ikona");
					if (entries.Count == 0) {
						this.AddButton(470, bottom, 4005, 4007, RemoveSection, GumpButtonType.Reply, 0);
						this.AddLabel(505, bottom, Hue, "Odebrat sekci");
					}
				}
			} else {
				this.AddLabel(20, bottom, Hue, "Vase konto: " + viewer.Gold + " gp");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			int page = (int) args[1];
			Item section = (Item) args[2];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (!EreborVendors.IsActive(vendor, player)) {
				return;
			}
			if (button == 0) {
				if (section != null) { //closing a sub-section goes back up
					Show(player, vendor, 0, EreborVendor.ParentOf(section));
				}
				return;
			}
			bool owner = vendor.IsOwner(player);
			List<Item> entries = vendor.EntriesOf(section);
			switch (button) {
				case Previous:
					Show(player, vendor, Math.Max(page - 1, 0), section);
					return;
				case Next:
					Show(player, vendor, page + 1, section);
					return;
				case AddMore:
					if (owner) {
						player.SysMessage("Target an item in your pack to stock.");
						player.Target(SingletonScript<Targ_EreborVendorStock>.Instance, new object[] { vendor, section });
					}
					return;
				case Reverse:
					if (owner) {
						entries.Reverse();
						Show(player, vendor, 0, section);
					}
					return;
				case Icon:
					if (owner && (section != null)) {
						player.SysMessage("target something to copy icon from.");
						player.Target(SingletonScript<Targ_EreborVendorIcon>.Instance, new object[] { vendor, section });
					}
					return;
				case RemoveSection:
					if (owner && (section != null) && EreborVendors.RemoveSection(vendor, player, section)) {
						Show(player, vendor, 0, EreborVendor.ParentOf(section));
					}
					return;
			}
			int index = (page * EreborVendor.RowsAPage) + ((button - 100) / 3);
			if ((button < 100) || (index >= entries.Count)) {
				return;
			}
			Item entry = entries[index];
			switch ((button - 100) % 3) {
				case 0:
					if (EreborVendor.IsSection(entry)) {
						Show(player, vendor, 0, entry);
					} else {
						D_EreborVendorItem.Show(player, vendor, entry, section);
					}
					break;
				case 1:
					if (owner && (index > 0)) {
						entries.RemoveAt(index);
						entries.Insert(index - 1, entry);
					}
					Show(player, vendor, page, section);
					break;
				case 2:
					if (owner) {
						entries.RemoveAt(index);
						entries.Insert(0, entry);
					}
					Show(player, vendor, page, section);
					break;
			}
		}
	}

	/// <summary>d_vendor_item: the description and price; units for a unit entry; Koupit, or the owner's Stahnout and Zmenit.</summary>
	public class D_EreborVendorItem : CompiledGumpDef {
		private const int Width = 300, Hue = 955, Take = 1, Browse = 2, ChangePrice = 4, Look = 5, PriceEntry = 1, UnitsEntry = 2;

		public static void Show(Player player, EreborVendor vendor, Item entry, Item section) {
			player.Dialog(player, SingletonScript<D_EreborVendorItem>.Instance, new DialogArgs(vendor, entry, section));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			Item entry = (Item) args[1];
			bool owner = vendor.IsOwner((Character) sendTo);
			bool unit = EreborVendor.IsUnit(entry);
			Item content = EreborVendor.Content(entry) ?? entry;
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, unit ? 260 : 230, 2620);
			this.AddLabel(20, 15, Hue, ((content.Amount > 1) ? content.Amount + " " : "") + content.Name);
			this.AddHtml(20, 40, Width - 40, 80, EreborVendor.Description(entry), false, true);
			this.AddLabel(20, 130, Hue, "Barva");
			this.AddLabel(80, 130, (content.Color > 0) ? content.Color : Hue, "||||||||||");
			this.AddLabel(20, 155, Hue, "Cena");
			if (owner) {
				this.AddTextEntry(80, 155, 100, 20, Hue, PriceEntry, EreborVendor.Price(entry).ToString(CultureInfo.InvariantCulture));
				this.AddButton(190, 155, 4005, 4007, ChangePrice, GumpButtonType.Reply, 0);
				this.AddLabel(225, 155, Hue, "Zmenit");
			} else {
				this.AddLabel(80, 155, Hue, EreborVendor.Price(entry).ToString(CultureInfo.InvariantCulture));
			}
			int y = 180;
			if (unit) {
				this.AddLabel(20, y, Hue, "Jednotek?");
				this.AddTextEntry(100, y, 60, 20, Hue, UnitsEntry, owner ? "" : "1");
				this.AddLabel(170, y, Hue, "/ " + EreborVendor.Units(entry));
				y += 25;
			}
			this.AddButton(20, y + 10, 4005, 4007, Take, GumpButtonType.Reply, 0);
			this.AddLabel(55, y + 10, Hue, owner ? "Stahnout" : "Koupit");
			if (unit) {
				this.AddButton(150, y + 10, 4005, 4007, Browse, GumpButtonType.Reply, 0);
				this.AddLabel(185, y + 10, Hue, "Projit");
			} else if (!(content is PetFigurine)) {
				this.AddButton(150, y + 10, 4005, 4007, Look, GumpButtonType.Reply, 0);
				this.AddLabel(185, y + 10, Hue, "Prohledni");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			Item entry = (Item) args[1];
			Player player = (Player) gi.Cont;
			if ((gr.PressedButton == 0) || !EreborVendors.IsActive(vendor, player) || entry.IsDeleted || (entry.TopObj() != vendor)) {
				return;
			}
			bool owner = vendor.IsOwner(player);
			int.TryParse(gr.GetTextResponse(UnitsEntry) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int units);
			if (gr.PressedButton == ChangePrice) {
				if (owner && int.TryParse(gr.GetTextResponse(PriceEntry) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int price)) {
					entry.SetTag(EreborVendor.PriceTK, Math.Max(price, 0));
				}
				D_EreborVendor.Show(player, vendor, 0, (Item) args[2]);
			} else if ((gr.PressedButton == Browse) || (gr.PressedButton == Look)) {
				EreborVendors.Peek(player, entry);
			} else if (gr.PressedButton == Take) {
				if (owner) {
					EreborVendors.Withdraw(vendor, player, entry, units);
				} else {
					EreborVendors.Buy(vendor, player, entry, Math.Max(units, 1));
				}
			}
		}
	}

	/// <summary>d_vendor_stock: description and price; Prodat, or for a container Jednotkovy prodej.</summary>
	public class D_EreborVendorStock : CompiledGumpDef {
		private const int Width = 400, Hue = 955, SellIt = 3, Units = 1, NewSection = 2, DescriptionEntry = 0, PriceEntry = 1;

		public static void Show(Player player, EreborVendor vendor, Thing what, Item section) {
			player.Dialog(player, SingletonScript<D_EreborVendorStock>.Instance, new DialogArgs(vendor, what, section));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Thing what = (Thing) args[1];
			Item item = what as Item;
			string name = (item != null) ? (((item.Amount > 1) ? item.Amount + " " : "") + item.Name) : what.Name;
			this.SetLocation(100, 175);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 170, 2620);
			this.AddLabel(20, 15, Hue, "Na prodej: " + name);
			this.AddButton(Width - 40, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(20, 45, Hue, "Popis");
			this.AddTextEntry(80, 45, Width - 100, 20, Hue, DescriptionEntry, name);
			this.AddLabel(20, 75, Hue, "Cena");
			this.AddTextEntry(80, 75, 120, 20, Hue, PriceEntry, "");
			this.AddButton(20, 110, 9905, 9904, SellIt, GumpButtonType.Reply, 0);
			this.AddLabel(45, 110, Hue, "Prodat");
			if ((item is Container) && (item.Def != ThingDef.GetByDefname("i_truhla_heslo"))) {
				this.AddButton(130, 110, 9905, 9904, Units, GumpButtonType.Reply, 0);
				this.AddLabel(155, 110, Hue, "Jednotkovy prodej");
				this.AddButton(290, 110, 9905, 9904, NewSection, GumpButtonType.Reply, 0);
				this.AddLabel(315, 110, Hue, "Nova sekce");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			Thing what = (Thing) args[1];
			Player player = (Player) gi.Cont;
			if ((gr.PressedButton == 0) || !EreborVendors.IsActive(vendor, player) || !vendor.IsOwner(player) || what.IsDeleted) {
				return;
			}
			string description = gr.GetTextResponse(DescriptionEntry);
			int.TryParse(gr.GetTextResponse(PriceEntry) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int price);
			Item section = (Item) args[2];
			if (what is NPC pet) {
				EreborVendors.StockPet(vendor, player, pet, description, price, section);
			} else if ((gr.PressedButton == Units) && (what is Container box)) {
				EreborVendors.StockUnits(vendor, player, box, description, price, section);
			} else if ((gr.PressedButton == NewSection) && (what is Container group)) {
				EreborVendors.MakeSection(vendor, player, group, description, section);
			} else if (what is Item item) {
				EreborVendors.StockItem(vendor, player, item, description, price, section);
			}
		}
	}

	/// <summary>d_vendor_desire: what the vendor buys, at what price and how many more.</summary>
	public class D_EreborVendorDesire : CompiledGumpDef {
		private const int Width = 640, RowHeight = 25, Hue = 955, AddLine = 3;

		public static void Show(Player player, EreborVendor vendor) {
			player.Dialog(player, SingletonScript<D_EreborVendorDesire>.Instance, new DialogArgs(vendor));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			Player viewer = (Player) sendTo;
			bool owner = vendor.IsOwner(viewer);
			List<string> lines = vendor.BuyBack;
			int rows = Math.Min(lines.Count, 30);
			this.SetLocation(10, 10);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 120 + (rows * RowHeight), 2620);
			this.AddLabel(20, 15, Hue, vendor.Name + " vykupuje");
			this.AddButton(Width - 40, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(20, 45, Hue, "kup");
			this.AddLabel(80, 45, Hue, "Nazev zbozi");
			this.AddLabel(Width - 220, 45, Hue, "Cena/gp");
			this.AddLabel(Width - 100, 45, Hue, "Max ks");
			for (int i = 0; i < rows; i++) {
				string[] parts = lines[i].Split('|');
				ThingDef def = ThingDef.GetByDefname(parts[0]);
				int y = 70 + (i * RowHeight);
				if (owner) {
					this.AddButton(20, y, 9905, 9904, 100 + i, GumpButtonType.Reply, 0);
				}
				this.AddLabel(80, y, Hue, (def == null) ? parts[0] : def.Name);
				this.AddLabel(Width - 220, y, Hue, parts[1]);
				this.AddLabel(Width - 100, y, Hue, parts[2]);
			}
			int bottom = 80 + (rows * RowHeight);
			if (owner) {
				this.AddButton(20, bottom, 4005, 4007, AddLine, GumpButtonType.Reply, 0);
				this.AddLabel(55, bottom, Hue, "Pridat vec k vykupu");
			} else {
				this.AddLabel(20, bottom, Hue, "Vase konto: " + viewer.Gold + " gp");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			Player player = (Player) gi.Cont;
			if ((gr.PressedButton == 0) || !EreborVendors.IsActive(vendor, player) || !vendor.IsOwner(player)) {
				return;
			}
			if (gr.PressedButton == AddLine) {
				player.SysMessage("Target an item to set its price.");
				player.Target(SingletonScript<Targ_EreborVendorPrice>.Instance, vendor);
				return;
			}
			int index = gr.PressedButton - 100;
			if ((index >= 0) && (index < vendor.BuyBack.Count)) {
				ThingDef def = ThingDef.GetByDefname(vendor.BuyBack[index].Split('|')[0]);
				if (def != null) {
					D_EreborVendorPrice.Show(player, vendor, def);
				}
			}
		}
	}

	/// <summary>d_vendor_price and d_vendor_price_2: the price and the limit of one thing, or Odebrat.</summary>
	public class D_EreborVendorPrice : CompiledGumpDef {
		private const int Width = 400, Hue = 955, Set = 1, Remove = 2, PriceEntry = 1, LimitEntry = 2;

		public static void Show(Player player, EreborVendor vendor, ThingDef def) {
			player.Dialog(player, SingletonScript<D_EreborVendorPrice>.Instance, new DialogArgs(vendor, def));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			ThingDef def = (ThingDef) args[1];
			bool listed = EreborVendors.BuyBackOf(vendor, def, out int price, out int limit);
			this.SetLocation(100, 200);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 150, 2620);
			this.AddLabel(20, 15, Hue, "Nastavit vykup: " + def.Name + " (" + def.PrettyDefname + ")");
			this.AddLabel(20, 45, Hue, "Cena");
			this.AddTextEntry(110, 45, 120, 20, Hue, PriceEntry, listed ? price.ToString(CultureInfo.InvariantCulture) : "");
			this.AddLabel(20, 75, Hue, "Limit (ks)");
			this.AddTextEntry(110, 75, 120, 20, Hue, LimitEntry, listed ? limit.ToString(CultureInfo.InvariantCulture) : "");
			this.AddButton(20, 110, 0xfb7, 0xfb8, Set, GumpButtonType.Reply, 0);
			this.AddLabel(55, 110, Hue, "Nastavit");
			if (listed) {
				this.AddButton(150, 110, 0xfb1, 0xfb3, Remove, GumpButtonType.Reply, 0);
				this.AddLabel(185, 110, Hue, "Odebrat");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			ThingDef def = (ThingDef) args[1];
			Player player = (Player) gi.Cont;
			if (!EreborVendors.IsActive(vendor, player) || !vendor.IsOwner(player)) {
				return;
			}
			if (gr.PressedButton == Set) {
				int.TryParse(gr.GetTextResponse(PriceEntry) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int price);
				int.TryParse(gr.GetTextResponse(LimitEntry) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit);
				EreborVendors.SetBuyBack(vendor, def, price, limit);
			} else if (gr.PressedButton == Remove) {
				EreborVendors.RemoveBuyBack(vendor, def);
			}
			if (gr.PressedButton != 0) {
				D_EreborVendorDesire.Show(player, vendor);
			}
		}
	}

	/// <summary>d_vendor_sell: what the vendor would buy from the pack; a row asks d_vendor_sell_item's question.</summary>
	public class D_EreborVendorSell : CompiledGumpDef {
		private const int Width = 640, RowHeight = 25, Hue = 955;

		public static void Show(Player player, EreborVendor vendor) {
			player.Dialog(player, SingletonScript<D_EreborVendorSell>.Instance, new DialogArgs(vendor, EreborVendors.Offers(vendor, player)));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			List<KeyValuePair<Item, int>> offers = (List<KeyValuePair<Item, int>>) args[1];
			int rows = Math.Min(offers.Count, 30);
			this.SetLocation(10, 10);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 110 + (rows * RowHeight), 2620);
			this.AddLabel(20, 15, Hue, "Co chces prodat? (muzu zaplatit:" + ((vendor.Gold * 100) / (100 + vendor.Commission)) + ")");
			this.AddButton(Width - 40, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(20, 45, Hue, "kup");
			this.AddLabel(80, 45, Hue, "Nazev zbozi");
			this.AddLabel(Width - 140, 45, Hue, "Cena/gp");
			for (int i = 0; i < rows; i++) {
				int y = 70 + (i * RowHeight);
				Item item = offers[i].Key;
				this.AddButton(20, y, 9905, 9904, 100 + i, GumpButtonType.Reply, 0);
				this.AddLabel(80, y, Hue, ((item.Amount > 1) ? item.Amount + " " : "") + item.Name);
				this.AddLabel(Width - 140, y, Hue, offers[i].Value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			EreborVendor vendor = (EreborVendor) args[0];
			List<KeyValuePair<Item, int>> offers = (List<KeyValuePair<Item, int>>) args[1];
			Player player = (Player) gi.Cont;
			int index = gr.PressedButton - 100;
			if ((gr.PressedButton == 0) || !EreborVendors.IsActive(vendor, player) || (index < 0) || (index >= offers.Count)) {
				return;
			}
			KeyValuePair<Item, int> offer = offers[index];
			D_TownConfirm.Ask(player, "Opravdu to chces prodat? " + offer.Key.Amount + " " + offer.Key.Name + ", cena " + offer.Value, () => {
				if (EreborVendors.IsActive(vendor, player)) {
					EreborVendors.Sell(vendor, player, offer.Key, offer.Value);
					Show(player, vendor);
				}
			});
		}
	}

	/// <summary>d_vendor_help, the owner's; others get the customer's lines.</summary>
	public class D_EreborVendorHelp : CompiledGumpDef {
		private const int Width = 420, Hue = 955;
		private static readonly string[] ownerLines = {
			"'Sklad', 'Stock': zamer vec ve svem batohu nebo svoje zvire na prodej, nastav popis a cenu.",
			"'Nakup', 'Buy', 'Supplies': nabidka prodavanych veci.",
			"'Cena', 'Price', 'Nastavit': zamer vec, kterou ma vendor vykupovat, a nastav cenu a limit.",
			"'Poptavka', 'Desire': seznam vykupovanych veci.",
			"'Penize', 'Penez', 'Gold': kolik ma vendor zlata.",
			"'Pridat', 'Add': zamer zlato nebo sek, ktery vendorovi das.",
			"'Zbozi', 'Collect': vykoupene zbozi do tve banky.",
			"'Vydelek': penize vendora do tve banky.",
			"'Oblect', 'Obleceni', 'Clothing': vymena obleceni s vendorem.",
			"'Vlastni' vs 'Zradce': obchod jen se svou stranou, nebo s kymkoliv.",
			".vendoradd(5000): penize z banky vendorovi.",
		};
		private static readonly string[] customerLines = {
			"Oslov vendora jeho jmenem a slovem:",
			"'Nakup', 'Buy': co vendor prodava.",
			"'Prodat', 'Sell': co od tebe vendor koupi.",
			"'Poptavka', 'Desire': co vendor vykupuje.",
			"'Kdo': komu vendor patri.",
		};

		public static void Show(Player player, bool owner) {
			player.Dialog(player, SingletonScript<D_EreborVendorHelp>.Instance, new DialogArgs(owner));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string[] lines = ((bool) args[0]) ? ownerLines : customerLines;
			this.SetLocation(100, 20);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 70 + (lines.Length * 22), 2620);
			this.AddLabel(20, 15, Hue, "Help - Ovladani vendora");
			this.AddButton(Width - 40, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			for (int i = 0; i < lines.Length; i++) {
				this.AddLabelCropped(20, 45 + (i * 22), Width - 40, 20, Hue, lines[i]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
