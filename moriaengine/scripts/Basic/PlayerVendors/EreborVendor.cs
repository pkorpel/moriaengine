using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class EreborVendorDef {
	}

	/// <summary>#1017 - Erebor's player vendor, customvendor.scp: what it sells waits in its bank box, each in a pouch carrying a
	/// description and price; it buys what its owner lists, with its own purse, and keeps 10% of every sale. Spoken to by
	/// its words; one vendor at a time, and a step ends the dealing.</summary>
	[ViewableClass]
	public partial class EreborVendor {
		private static readonly TagKey ownerTK = TagKey.Acquire("_ev_owner_");
		private static readonly TagKey goldTK = TagKey.Acquire("_ev_gold_");
		private static readonly TagKey commissionTK = TagKey.Acquire("_ev_provize_");
		private static readonly TagKey pureTK = TagKey.Acquire("_ev_pure_");
		private static readonly TagKey entriesTK = TagKey.Acquire("_ev_entries_");
		private static readonly TagKey buyBackTK = TagKey.Acquire("_ev_buyback_");
		private static readonly TagKey goodsTK = TagKey.Acquire("_ev_goods_");
		internal static readonly TagKey DescriptionTK = TagKey.Acquire("_ev_desc_");
		internal static readonly TagKey PriceTK = TagKey.Acquire("_ev_price_");
		internal static readonly TagKey UnitTK = TagKey.Acquire("_ev_unit_");
		internal static readonly TagKey SessionTK = TagKey.Acquire("_ev_session_");
		internal const int MaxItemsInEntry = 500, RowsAPage = 15;

		public override void On_Create() {
			base.On_Create();
			this.Flag_Invulnerable = true; //e_vendor_custom's first hit makes it invulnerable and frozen
			this.SetTag(MysticMind.ImmobileTK, 1);
		}

		public Character Owner {
			get {
				Character owner = this.GetTag(ownerTK) as Character;
				return ((owner == null) || owner.IsDeleted) ? null : owner;
			}
			set {
				this.SetTag(ownerTK, value);
			}
		}

		public bool IsOwner(Character ch) {
			return (ch != null) && (ch == this.Owner);
		}

		/// <summary>tag.gold: a number, not coins.</summary>
		public long Gold {
			get { return (this.GetTag(goldTK) is long gold) ? gold : 0; }
			set { this.SetTag(goldTK, value); }
		}

		/// <summary>tag.provize, 10 at first use.</summary>
		public int Commission {
			get { return (this.GetTag(commissionTK) is int commission) ? commission : EreborVendors.Settings.vendorCommissionPercent; }
			set { this.SetTag(commissionTK, value); }
		}

		/// <summary>tag.pure: no dealing with the other realm.</summary>
		public bool Pure {
			get { return this.GetTag(pureTK) != null; }
			set {
				if (value) {
					this.SetTag(pureTK, 1);
				} else {
					this.RemoveTag(pureTK);
				}
			}
		}

		/// <summary>What's on sale, in order: pouches (or a unit container, or a figurine) in the bank box.</summary>
		public List<Item> Entries {
			get {
				List<Item> list = this.GetTag(entriesTK) as List<Item>;
				if (list == null) {
					list = new List<Item>();
					this.SetTag(entriesTK, list);
				}
				list.RemoveAll(item => item.IsDeleted || (item.TopObj() != this));
				return list;
			}
		}

		/// <summary>i_buyer_box: "defname|price|limit".</summary>
		public List<string> BuyBack {
			get {
				List<string> list = this.GetTag(buyBackTK) as List<string>;
				if (list == null) {
					list = new List<string>();
					this.SetTag(buyBackTK, list);
				}
				return list;
			}
		}

		/// <summary>i_buyer_goods_box, in the bank box.</summary>
		public Container Goods {
			get {
				Container goods = this.GetTag(goodsTK) as Container;
				if ((goods == null) || goods.IsDeleted) {
					goods = (Container) ThingDef.GetByDefname("i_backpack").Create(this.Bank);
					goods.Name = "Buyer goods Box";
					this.SetTag(goodsTK, goods);
				}
				return goods;
			}
		}

		/// <summary>e_vendor_custom's @userdclick.</summary>
		public override void On_DClick(AbstractCharacter from) {
			this.Title = "Zamestnavatel prodejce: " + ((this.Owner == null) ? "nikdo" : this.Owner.Name);
			base.On_DClick(from);
		}

		internal static string Description(Item entry) {
			return (entry.GetTag(DescriptionTK) as string) ?? entry.Name;
		}

		internal static int Price(Item entry) {
			return (entry.GetTag(PriceTK) is int price) ? price : 0;
		}

		internal static bool IsUnit(Item entry) {
			return entry.GetTag(UnitTK) != null;
		}

		/// <summary>The thing an entry sells: the pouch's one item, or the unit container, or the figurine.</summary>
		internal static Item Content(Item entry) {
			if (IsUnit(entry) || !(entry is Container pouch)) {
				return entry;
			}
			foreach (AbstractItem inside in pouch) {
				return inside as Item;
			}
			return null;
		}

		/// <summary>countamount: what a unit container holds, stacks by their amount.</summary>
		internal static int Units(Item entry) {
			int count = 0;
			if (entry is Container box) {
				foreach (AbstractItem inside in box) {
					count += Math.Max((int) inside.Amount, 1);
				}
			}
			return count;
		}
	}

	/// <summary>t_deed_vendor: Erebor's deed works anywhere.</summary>
	public class t_deed_erebor_vendor : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			if ((clicker is Player player) && (self.TopObj() == player)) {
				bool female = self.Def == ThingDef.GetByDefname("i_deed_erebor_vendor_f");
				EreborVendors.Place(player, female, player.P());
				self.Delete();
			}
		}
	}

	public static class EreborVendors {
		internal static HouseSettings Settings {
			get { return HouseSettings.instance; }
		}

		internal static EreborVendor Place(Character owner, bool female, IPoint4D where) {
			EreborVendor vendor = (EreborVendor) ((NPCDef) ThingDef.GetByDefname(female ? "c_erebor_vendor_f" : "c_erebor_vendor")).Create(where.X, where.Y, where.Z, where.M);
			vendor.Owner = owner;
			return vendor;
		}

		/// <summary>restrictside: an "ortodox" vendor won't deal with the other realm.</summary>
		internal static bool Refuses(EreborVendor vendor, Character speaker) {
			if (vendor.Pure && !speaker.IsGM && (vendor.Owner != null) && (vendor.Owner.Realm != speaker.Realm)) {
				vendor.Say("S takovymi jako jsi ty nejednam!");
				return true;
			}
			return false;
		}

		/// <summary>checkVendorDialog: one vendor at a time; a step or a spell ends it.</summary>
		internal static bool StartSession(EreborVendor vendor, Character speaker) {
			if ((speaker.GetTag(EreborVendor.SessionTK) is EreborVendor other) && (other != vendor) && !other.IsDeleted) {
				speaker.SysMessage("Ne, jednani se dvema obchodniky zaroven je neslusne.");
				return false;
			}
			speaker.SetTag(EreborVendor.SessionTK, vendor);
			return true;
		}

		/// <summary>isVendorInactive: every button checks it and does nothing when it's true.</summary>
		internal static bool IsActive(EreborVendor vendor, Character ch) {
			return (vendor != null) && !vendor.IsDeleted && (ch.GetTag(EreborVendor.SessionTK) == vendor);
		}

		internal static void EndSession(Character ch) {
			ch.RemoveTag(EreborVendor.SessionTK);
		}

		private static bool Ghost(Character ch) {
			if (ch.Flag_Dead) {
				ch.RedMessage("Jako duch tohle nemuzes!");
				return true;
			}
			return false;
		}

		/// <summary>spk_vendor_custom, the owner's words and everyone's.</summary>
		internal static bool Hear(EreborVendor vendor, Player speaker, string speech) {
			bool owner = vendor.IsOwner(speaker);
			bool has(params string[] words) {
				foreach (string word in words) {
					if (speech.Contains(word)) {
						return true;
					}
				}
				return false;
			}
			if (has("forget", "zapomen")) {
				vendor.Say("Provedu");
				if (owner || speaker.IsGM) {
					vendor.Owner = vendor.Owner ?? speaker;
				}
			} else if (has("kdo", "komu")) {
				Character master = vendor.Owner;
				vendor.Say("Mym zamestavatelem je " + ((master == null) ? "nikdo" : ((master.IsFemale ? "pani " : "pan ") + master.Name)) + ".");
			} else if (has("oblect", "obleceni", "clothing")) {
				if (!Ghost(speaker)) {
					vendor.Say("Hned se prevleknu");
					if (!Refuses(vendor, speaker) && owner) {
						SwapClothes(vendor, speaker);
					}
				}
			} else if (has("nakup", "kup", "buy", "supplies", "wares")) {
				if (!Ghost(speaker)) {
					vendor.Say("Maly moment");
					if (!Refuses(vendor, speaker) && StartSession(vendor, speaker)) {
						if (vendor.Entries.Count > 0) {
							D_EreborVendor.Show(speaker, vendor, 0);
						} else {
							vendor.Say("Nemam nic na prodej.");
						}
					}
				}
			} else if (has("stock", "sklad")) {
				vendor.Say("Okamzicek");
				if (!Refuses(vendor, speaker) && StartSession(vendor, speaker) && owner) {
					speaker.SysMessage("Target an item in your pack to stock.");
					speaker.Target(SingletonScript<Targ_EreborVendorStock>.Instance, vendor);
				}
			} else if (has("cena", "price", "nastavit")) {
				if (!Refuses(vendor, speaker) && StartSession(vendor, speaker) && owner) {
					speaker.SysMessage("Target an item to set its price.");
					speaker.Target(SingletonScript<Targ_EreborVendorPrice>.Instance, vendor);
				}
			} else if (has("gold", "penez", "penize")) {
				vendor.Say("Pockej, spocitam to");
				if (!Refuses(vendor, speaker) && owner) {
					vendor.Say("Mam k dispozici " + vendor.Gold + " zlata.");
				}
			} else if (has("vydelek")) {
				if (!Ghost(speaker) && !Refuses(vendor, speaker)) {
					vendor.Say("Hned to bude...");
					if (owner) {
						PayOut(vendor, speaker);
					}
				}
			} else if (has("zbozi", "collect")) {
				if (!Ghost(speaker) && !Refuses(vendor, speaker)) {
					vendor.Say("Hned to bude...");
					if (owner) {
						Collect(vendor, speaker);
					}
				}
			} else if (has("redeed")) {
				if (speaker.IsGM) {
					Redeed(vendor, speaker);
				}
			} else if (has("pridat", "add")) {
				if (!Ghost(speaker) && !Refuses(vendor, speaker) && owner) {
					speaker.SysMessage("Target gold to add to vendors account.");
					speaker.Target(SingletonScript<Targ_EreborVendorGold>.Instance, vendor);
				}
			} else if (has("sell", "prod")) {
				if (!Ghost(speaker)) {
					vendor.Say("Ukaz co mas");
					if (!Refuses(vendor, speaker) && StartSession(vendor, speaker)) {
						D_EreborVendorSell.Show(speaker, vendor);
					}
				}
			} else if (has("poptav", "desire")) {
				if (!Ghost(speaker)) {
					vendor.Say("Shanim toto zbozi");
					if (!Refuses(vendor, speaker) && StartSession(vendor, speaker)) {
						D_EreborVendorDesire.Show(speaker, vendor);
					}
				}
			} else if (has("info", "napove", "help")) {
				if (!Refuses(vendor, speaker)) {
					D_EreborVendorHelp.Show(speaker, owner);
				}
			} else if (has("ortodox", "vlast")) {
				if (owner) {
					vendor.Pure = true;
					vendor.Say("S neprateli meho pana nejednam !");
				}
			} else if (has("zrad", "duffer")) {
				if (owner) {
					vendor.Pure = false;
					vendor.Say("Kdo ma penize, je nas pritel.");
				}
			} else {
				return false;
			}
			return true;
		}

		/// <summary>oblect: the owner's clothes onto the vendor and the vendor's to the owner. Erebor said hair too, but never did.</summary>
		internal static void SwapClothes(EreborVendor vendor, Character owner) {
			List<Item> vendors = Worn(vendor), owners = Worn(owner);
			foreach (Item item in vendors) {
				item.Cont = vendor.Bank;
			}
			foreach (Item item in owners) {
				item.Cont = vendor;
			}
			foreach (Item item in vendors) {
				item.Cont = owner.Backpack;
			}
		}

		private static List<Item> Worn(Character ch) {
			List<Item> worn = new List<Item>();
			foreach (AbstractItem item in ch) {
				int layer = item.Z;
				if ((item is Item real) && (layer >= (int) LayerNames.Hand1) && (layer <= (int) LayerNames.Leggins) && (layer != (int) LayerNames.Hair) &&
						(layer != (int) LayerNames.Pack) && (layer != (int) LayerNames.Light) && (layer != (int) LayerNames.Hidden)) {
					worn.Add(real);
				}
			}
			return worn;
		}

		/// <summary>vydelek: the purse to the owner's bank as purses and strongboxes.</summary>
		internal static void PayOut(EreborVendor vendor, Character owner) {
			long gold = vendor.Gold;
			if (gold <= 0) {
				vendor.Say("Dnes pro tebe nemam zadne penize...");
				return;
			}
			CashiersItem.CreatePurses(owner, (int) Math.Min(gold, int.MaxValue), owner.Bank);
			Logger.WriteInfo(true, owner.Name + " vybral " + gold + " zlata z vendora " + vendor.Name + "(" + vendor.Uid + ")");
			vendor.Gold = 0;
			vendor.Say("Dostal jsi " + gold + " zlata.");
		}

		/// <summary>zbozi: what the vendor bought goes into a backpack in the owner's bank.</summary>
		internal static void Collect(EreborVendor vendor, Character owner) {
			Container goods = vendor.Goods;
			if (goods.Count == 0) {
				vendor.Say("Dnes pro tebe nemam zadne zbozi.");
				return;
			}
			Container bag = (Container) ThingDef.GetByDefname("i_backpack").Create(owner.Bank);
			bag.Name = "Collected from " + vendor.Name;
			foreach (AbstractItem item in new List<AbstractItem>(goods)) {
				item.Cont = bag;
			}
			vendor.Say("Nakoupene zbozi najdes v bance.");
		}

		/// <summary>A GM's redeed: gold, stock, goods and clothes in a backpack, and a deed. Erebor's was always the male deed.</summary>
		internal static void Redeed(EreborVendor vendor, Character gm) {
			Container bag = (Container) ThingDef.GetByDefname("i_backpack").Create(gm.Backpack);
			bag.Name = "From redeeded vendor " + vendor.Name;
			if (vendor.Gold > 0) {
				GoldUtils.GrantInto(bag, (uint) Math.Min(vendor.Gold, uint.MaxValue));
			}
			foreach (Item entry in vendor.Entries) {
				Item content = EreborVendor.Content(entry);
				if ((content != null) && (content != entry)) {
					content.Cont = bag;
					entry.Delete();
				} else {
					entry.Cont = bag;
				}
			}
			foreach (AbstractItem item in new List<AbstractItem>(vendor.Goods)) {
				item.Cont = bag;
			}
			foreach (Item item in Worn(vendor)) {
				item.Cont = bag;
			}
			((ItemDef) ThingDef.GetByDefname(vendor.Model == 0x191 ? "i_deed_erebor_vendor_f" : "i_deed_erebor_vendor")).Create(bag);
			vendor.Delete();
		}

		#region stocking
		/// <summary>i_vendor_stock's @TargOn_Item.</summary>
		internal static string StockRefusal(EreborVendor vendor, Player owner, Item item) {
			if ((item.TopObj() != owner) || (item.Cont == owner)) {
				return "Zamer vec ve svem batohu nebo svoje zvire na prodej.";
			}
			if (item.HasAttribute(ItemAttributes.attr_invis) || item.HasAttribute(ItemAttributes.attr_move_never) || item.HasAttribute(ItemAttributes.attr_newbie)) {
				return "Tohle nemuzes prodat.";
			}
			if ((item.Type != null) && (item.Type.Defname == "t_gold")) {
				return "Nemuzes prodavat hotovost.";
			}
			if ((item is Container box) && (box.Count > MaxItems)) {
				return "V containeru nesmis mit vice jak 500ks zbozi !";
			}
			return null;
		}

		private const int MaxItems = EreborVendor.MaxItemsInEntry;

		/// <summary>Button 3: one thing for one price, in a pouch in the bank box.</summary>
		internal static Item StockItem(EreborVendor vendor, Player owner, Item item, string description, int price) {
			if (item.TopObj() != owner) {
				owner.SysMessage("Predmet musis mit u sebe !");
				return null;
			}
			Container pouch = (Container) ThingDef.GetByDefname("i_pouch").Create(vendor.Bank);
			pouch.Name = ((item.Amount > 1) ? item.Amount + " " : "") + item.Name;
			pouch.Color = item.Color;
			item.Cont = pouch;
			return AddEntry(vendor, pouch, description, price, false);
		}

		/// <summary>Button 1: a container's things, each at the price.</summary>
		internal static Item StockUnits(EreborVendor vendor, Player owner, Container box, string description, int price) {
			if (box.TopObj() != owner) {
				owner.SysMessage("Predmet musis mit u sebe !");
				return null;
			}
			box.Cont = vendor.Bank;
			Item entry = AddEntry(vendor, box, description, price, true);
			return entry;
		}

		/// <summary>A pet goes on sale shrunk to its figurine; the buyer's double click brings it back as theirs.</summary>
		internal static Item StockPet(EreborVendor vendor, Player owner, NPC pet, string description, int price) {
			if (pet.ControlMaster != owner) {
				owner.SysMessage("Toto neni tvoje zvire");
				return null;
			}
			Container pouch = (Container) ThingDef.GetByDefname("i_pouch").Create(vendor.Bank);
			PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(pouch);
			figurine.Keep(pet);
			pouch.Name = pet.Name;
			pet.Delete();
			return AddEntry(vendor, pouch, description, price, false);
		}

		private static Item AddEntry(EreborVendor vendor, Item entry, string description, int price, bool unit) {
			entry.SetTag(EreborVendor.DescriptionTK, string.IsNullOrEmpty(description) ? entry.Name : description);
			entry.SetTag(EreborVendor.PriceTK, Math.Max(price, 0));
			if (unit) {
				entry.SetTag(EreborVendor.UnitTK, 1);
			}
			vendor.Entries.Add(entry);
			return entry;
		}
		#endregion

		#region buying and withdrawing
		/// <summary>Button 1 for a customer. Erebor took the price from the buyer and gave the vendor 90%.</summary>
		internal static bool Buy(EreborVendor vendor, Player buyer, Item entry, int units) {
			int price = EreborVendor.Price(entry);
			if (EreborVendor.IsUnit(entry)) {
				int have = EreborVendor.Units(entry);
				units = Math.Max(1, Math.Min(units, have));
				if (!GoldUtils.Pay(buyer, (long) price * units)) {
					return false;
				}
				vendor.Gold += ((long) price * units * (100 - vendor.Commission)) / 100;
				GiveUnits((Container) entry, buyer, units);
				if (EreborVendor.Units(entry) == 0) {
					vendor.Entries.Remove(entry);
					entry.Delete();
				}
				return true;
			}
			if (!GoldUtils.Pay(buyer, price)) {
				return false;
			}
			vendor.Gold += ((long) price * (100 - vendor.Commission)) / 100;
			vendor.Say("Dekuji za obchod.");
			Hand(vendor, entry, buyer);
			return true;
		}

		/// <summary>Button 1 for the owner: back to the pack.</summary>
		internal static void Withdraw(EreborVendor vendor, Player owner, Item entry, int units) {
			if (EreborVendor.IsUnit(entry) && (units > 0)) {
				GiveUnits((Container) entry, owner, Math.Min(units, EreborVendor.Units(entry)));
				if (EreborVendor.Units(entry) > 0) {
					return;
				}
			}
			vendor.Say((EreborVendor.Content(entry) is PetFigurine) ? "Zvire vraceno." : "Vec vracena do tveho batohu.");
			Hand(vendor, entry, owner);
		}

		private static void Hand(EreborVendor vendor, Item entry, Character to) {
			vendor.Entries.Remove(entry);
			if (EreborVendor.IsUnit(entry)) {
				entry.RemoveTag(EreborVendor.UnitTK);
				entry.RemoveTag(EreborVendor.PriceTK);
				entry.RemoveTag(EreborVendor.DescriptionTK);
				entry.Cont = to.Backpack;
				return;
			}
			Item content = EreborVendor.Content(entry);
			if (content != null) {
				content.Cont = to.Backpack;
			}
			entry.Delete();
		}

		/// <summary>pv_giveitemunit: whole things first, a stack split for the rest.</summary>
		private static void GiveUnits(Container box, Character to, int units) {
			foreach (AbstractItem inside in new List<AbstractItem>(box)) {
				if (units <= 0) {
					return;
				}
				Item item = (Item) inside;
				if (item.Amount <= units) {
					units -= item.Amount;
					item.Cont = to.Backpack;
				} else {
					Item part = (Item) ((ItemDef) item.Def).Create(to.Backpack);
					part.Amount = units;
					part.Color = item.Color;
					item.Amount -= units;
					units = 0;
				}
			}
		}
		#endregion

		#region buy-back
		internal static void SetBuyBack(EreborVendor vendor, ThingDef def, int price, int limit) {
			RemoveBuyBack(vendor, def);
			vendor.BuyBack.Add(def.PrettyDefname + "|" + Math.Max(price, 0) + "|" + Math.Max(limit, 0));
		}

		internal static void RemoveBuyBack(EreborVendor vendor, ThingDef def) {
			vendor.BuyBack.RemoveAll(line => line.Split('|')[0] == def.PrettyDefname);
		}

		/// <summary>The vendor's price and limit for a def, or false.</summary>
		internal static bool BuyBackOf(EreborVendor vendor, ThingDef def, out int price, out int limit) {
			foreach (string line in vendor.BuyBack) {
				string[] parts = line.Split('|');
				if ((parts.Length == 3) && (ThingDef.GetByDefname(parts[0]) == def)) {
					price = int.Parse(parts[1], CultureInfo.InvariantCulture);
					limit = int.Parse(parts[2], CultureInfo.InvariantCulture);
					return true;
				}
			}
			price = 0;
			limit = 0;
			return false;
		}

		/// <summary>trysellit: the top of the pack, what the vendor buys, priced price x amount.</summary>
		internal static List<KeyValuePair<Item, int>> Offers(EreborVendor vendor, Character seller) {
			List<KeyValuePair<Item, int>> offers = new List<KeyValuePair<Item, int>>();
			foreach (AbstractItem found in seller.Backpack) {
				if (!(found is Item item) || item.HasAttribute(ItemAttributes.attr_invis) || item.HasAttribute(ItemAttributes.attr_move_never) ||
						item.HasAttribute(ItemAttributes.attr_newbie)) {
					continue;
				}
				if (BuyBackOf(vendor, item.Def, out int price, out int limit) && (price > 0)) {
					offers.Add(new KeyValuePair<Item, int>(item, price * Math.Max((int) item.Amount, 1)));
				}
			}
			return offers;
		}

		/// <summary>vendorsell: the vendor pays price x 1.1 from its purse, the seller gets the price, the limit drops.</summary>
		internal static bool Sell(EreborVendor vendor, Player seller, Item item, int price) {
			if (item.IsDeleted || (item.TopObj() != seller)) {
				seller.SysMessage("bugere bugerska, tohle neni tvoje");
				return false;
			}
			long cost = ((long) price * (100 + vendor.Commission)) / 100;
			if (vendor.Gold < cost) {
				seller.SysMessage("Nedostatek penez na " + item.Name + ".");
				return false;
			}
			BuyBackOf(vendor, item.Def, out int unitPrice, out int limit);
			int nextLimit = limit - Math.Max((int) item.Amount, 1);
			if (nextLimit < 0) {
				seller.SysMessage("Vic kusu " + item.Name + " uz nevykupuji.");
				return false;
			}
			SetBuyBack(vendor, item.Def, unitPrice, nextLimit);
			Logger.WriteInfo(true, seller.Name + " prodal item vendorovi " + vendor.Name + ": " + item.Amount + " " + item.Name + " za " + price);
			item.Cont = vendor.Goods;
			vendor.Gold -= cost;
			GoldUtils.Grant(seller, (uint) price);
			return true;
		}
		#endregion

		/// <summary>i_vendor_gold: gold or a cheque from the owner's things into the purse.</summary>
		internal static void AddGold(EreborVendor vendor, Player owner, Item item) {
			if (item.TopObj() != owner) {
				owner.SysMessage("Target gold to add to vendors account.");
				return;
			}
			if ((item.Type != null) && (item.Type.Defname == "t_gold")) {
				vendor.Gold += item.Amount;
				item.Delete();
			} else if (item is CashiersItem check) {
				vendor.Gold += check.Value;
				item.Delete();
			} else {
				owner.SysMessage("Target gold to add to vendors account.");
				return;
			}
			vendor.Say("Mam k dispozici " + vendor.Gold + " zlata.");
		}

		/// <summary>.vendoradd(N): from the owner's bank into a vendor's purse.</summary>
		[SteamFunction]
		public static void VendorAdd(Character self, ScriptArgs sa) {
			string arg = (sa == null) ? "" : sa.Args.Trim().Trim('"');
			if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || (amount <= 0)) {
				self.RedMessage("Musis zadat hodnotu v gp, kterou chces vendorovi dat. ");
				self.SysMessage("Priklad spravneho pouziti: .vendoradd(5000)");
				return;
			}
			self.SysMessage("Kteremu vendorovi chces pridat penize?");
			((Player) self).Target(SingletonScript<Targ_EreborVendorBankGold>.Instance, amount);
		}

		internal static void AddFromBank(Player owner, Character target, int amount) {
			if (!(target is EreborVendor vendor)) {
				owner.SysMessage("Toto neni vendor !");
				return;
			}
			if (Refuses(vendor, owner)) {
				return;
			}
			if (!vendor.IsOwner(owner)) {
				owner.RedMessage("Toto neni Tvuj vendor !");
				return;
			}
			if (GoldUtils.Pay(owner, amount, PaySource.Bank, "V bance nemas dostatek penez. Potrebujes " + amount + " gp.")) {
				vendor.Gold += amount;
				vendor.Say("Dekuji. Penize jsem si ulozil.");
			}
		}
	}

	/// <summary>spk_vendor_custom.</summary>
	public sealed class EreborVendorSpeech : AbstractSpeech {
		public new static void Bootstrap() {
			new EreborVendorSpeech("spk_erebor_vendor").Register();
		}

		private EreborVendorSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is EreborVendor vendor) && (speechArgs.Speaker is Player speaker) &&
					EreborVendors.Hear(vendor, speaker, (speechArgs.Speech ?? "").ToLowerInvariant())) {
				return SpeechResult.ActedUponExclusively;
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	public class Targ_EreborVendorStock : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			EreborVendor vendor = (EreborVendor) parameter;
			if (!EreborVendors.IsActive(vendor, self)) {
				return TargetResult.Done;
			}
			string refusal = EreborVendors.StockRefusal(vendor, self, targetted);
			if (refusal != null) {
				self.SysMessage(refusal);
				return TargetResult.RestartTargetting;
			}
			D_EreborVendorStock.Show(self, vendor, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			EreborVendor vendor = (EreborVendor) parameter;
			if (!EreborVendors.IsActive(vendor, self)) {
				return TargetResult.Done;
			}
			if ((targetted is NPC pet) && (pet.ControlMaster == self) && !(pet is EreborVendor) && !SummonSpellDef.IsSummonedCreature(pet)) {
				D_EreborVendorStock.Show(self, vendor, pet);
				return TargetResult.Done;
			}
			self.SysMessage("Zamer vec ve svem batohu nebo svoje zvire na prodej.");
			return TargetResult.RestartTargetting;
		}
	}

	public class Targ_EreborVendorPrice : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if ((targetted.Type != null) && (targetted.Type.Defname == "t_rune")) {
				self.SysMessage("Nelze vykupovat runy.");
				return TargetResult.RestartTargetting;
			}
			D_EreborVendorPrice.Show(self, (EreborVendor) parameter, targetted.Def);
			return TargetResult.Done;
		}
	}

	public class Targ_EreborVendorGold : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			EreborVendors.AddGold((EreborVendor) parameter, self, targetted);
			return TargetResult.Done;
		}
	}

	public class Targ_EreborVendorBankGold : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			EreborVendors.AddFromBank(self, targetted, (int) parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Tady nic nevidis.");
			return TargetResult.Done;
		}
	}
}
