using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1070 - one line of goods in an offer, Erebor's "name,baseid,price,current,max,respawn,stackable".</summary>
	internal sealed class DiyGoods {
		internal string Name, Defname;
		internal int Price, Current, Max, Respawn;
		internal bool Stacks;
	}

	/// <summary>#1070 - Erebor's systems/npc_diy_vendor.scp: c_diy_vendor and c_diy_vendor_w, an NPC shop a GM builds in a dialog.
	/// Buttons lead to pages of text, a page holds up to ten offers, an offer buys from players (VYKUP) or sells to them (PRODEJ).
	/// Everything lives in tags on the NPC's spawn gem, so one spawn is one shop; an NPC with no gem keeps them itself. Erebor kept
	/// each piece as one comma list and warned GMs off commas; here every field is a tag of its own. Money comes from nowhere and
	/// goes nowhere, and the counts never change after a deal: a count is only the most one deal may take, as on Erebor.</summary>
	public static class DiyVendor {
		internal const int Buttons = 6, Offers = 10, MaxGoods = 26, FirstPortrait = 3000, LastPortrait = 3164;
		internal const int Vykup = 0, Prodej = 1;
		internal const int DclickReach = 3, SpeechReach = 8, DialogReach = 11;
		internal const string FarMessage = "Na takovou vzdalenost s tebou jednat nebude";
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");

		internal static bool IsDiyVendor(Character ch) {
			return (ch is NPC) && ((ch.Def == ThingDef.GetByDefname("c_diy_vendor")) || (ch.Def == ThingDef.GetByDefname("c_diy_vendor_w")));
		}

		/// <summary>memoryfindtype(Memory_ISpawned).link, or the NPC when nothing spawned it.</summary>
		internal static Thing Store(NPC vendor) {
			Thing gem = vendor.GetTag(spawnGemTK) as Thing;
			return ((gem != null) && !gem.IsDeleted) ? gem : vendor;
		}

		private static TagKey Key(string name) {
			return TagKey.Acquire("_diy_" + name);
		}

		private static string Text(Thing store, string name) {
			return store.GetTag(Key(name)) as string ?? "";
		}

		private static int Number(Thing store, string name) {
			return ToNumber(store.GetTag(Key(name)));
		}

		internal static int ToNumber(object value) {
			if (value is int i) {
				return i;
			}
			int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n);
			return n;
		}

		private static void Set(Thing store, string name, object value) {
			store.SetTag(Key(name), value);
		}

		private static void Remove(Thing store, string name) {
			store.RemoveTag(Key(name));
		}

		internal static int Portrait(Thing store) {
			int portrait = Number(store, "portrait");
			return (portrait == 0) ? FirstPortrait : portrait;
		}

		internal static void SetPortrait(Thing store, int portrait) {
			Set(store, "portrait", portrait);
		}

		internal static string Intro(Thing store) {
			return Text(store, "intro");
		}

		internal static void SetIntro(Thing store, string text) {
			Set(store, "intro", text ?? "");
		}

		private static string ButtonKey(int address, int slot) {
			return "b_" + address + "_" + slot;
		}

		internal static bool HasButton(Thing store, int address, int slot) {
			return store.GetTag(Key(ButtonKey(address, slot) + "_label")) != null;
		}

		internal static string ButtonLabel(Thing store, int address, int slot) {
			return Text(store, ButtonKey(address, slot) + "_label");
		}

		internal static string ButtonText(Thing store, int address, int slot) {
			return Text(store, ButtonKey(address, slot) + "_text");
		}

		/// <summary>Where a button leads: its address times ten plus its slot, as the GM editor set it.</summary>
		internal static int ButtonChild(Thing store, int address, int slot) {
			return Number(store, ButtonKey(address, slot) + "_child");
		}

		internal static void SetButton(Thing store, int address, int slot, string label, string text) {
			string key = ButtonKey(address, slot);
			Set(store, key + "_label", label ?? "");
			Set(store, key + "_text", text ?? "");
			Set(store, key + "_child", (address * 10) + slot);
		}

		/// <summary>Smaz button: only the button; what lies under it stays, as on Erebor.</summary>
		internal static void RemoveButton(Thing store, int address, int slot) {
			string key = ButtonKey(address, slot);
			Remove(store, key + "_label");
			Remove(store, key + "_text");
			Remove(store, key + "_child");
		}

		/// <summary>A top-level button shows on every page, at the same spot as a page's own button of that slot, so
		/// Erebor's editor meant to refuse that slot below the top; its check read the wrong tag and never did.</summary>
		internal static bool SlotTakenAtTop(Thing store, int address, int slot) {
			return (address > 0) && HasButton(store, 0, slot);
		}

		private static string OfferKey(int address, int slot) {
			return "o_" + address + "_" + slot;
		}

		internal static bool HasOffer(Thing store, int address, int slot) {
			return store.GetTag(Key(OfferKey(address, slot) + "_name")) != null;
		}

		internal static string OfferName(Thing store, int address, int slot) {
			return Text(store, OfferKey(address, slot) + "_name");
		}

		internal static int OfferKind(Thing store, int address, int slot) {
			return Number(store, OfferKey(address, slot) + "_kind");
		}

		internal static void SetOffer(Thing store, int address, int slot, string name, int kind) {
			Set(store, OfferKey(address, slot) + "_name", (name ?? "").Trim());
			Set(store, OfferKey(address, slot) + "_kind", kind);
		}

		internal static void RemoveOffer(Thing store, int address, int slot) {
			int count = GoodsCount(store, address, slot);
			for (int k = 1; k <= count; k++) {
				RemoveGoodsTags(store, address, slot, k);
			}
			string key = OfferKey(address, slot);
			Remove(store, key + "_count");
			Remove(store, key + "_name");
			Remove(store, key + "_kind");
		}

		internal static int GoodsCount(Thing store, int address, int slot) {
			return Number(store, OfferKey(address, slot) + "_count");
		}

		private static string GoodsKey(int address, int slot, int k) {
			return OfferKey(address, slot) + "_" + k;
		}

		internal static DiyGoods Goods(Thing store, int address, int slot, int k) {
			string key = GoodsKey(address, slot, k);
			return new DiyGoods {
				Name = Text(store, key + "_name"),
				Defname = Text(store, key + "_def"),
				Price = Number(store, key + "_price"),
				Current = Number(store, key + "_current"),
				Max = Number(store, key + "_max"),
				Respawn = Number(store, key + "_respawn"),
				Stacks = Number(store, key + "_stack") != 0
			};
		}

		internal static void SetGoods(Thing store, int address, int slot, int k, DiyGoods goods) {
			string key = GoodsKey(address, slot, k);
			Set(store, key + "_name", goods.Name ?? "");
			Set(store, key + "_def", goods.Defname ?? "");
			Set(store, key + "_price", goods.Price);
			Set(store, key + "_current", goods.Current);
			Set(store, key + "_max", goods.Max);
			Set(store, key + "_respawn", goods.Respawn);
			Set(store, key + "_stack", goods.Stacks ? 1 : 0);
		}

		private static void RemoveGoodsTags(Thing store, int address, int slot, int k) {
			string key = GoodsKey(address, slot, k);
			foreach (string field in new[] { "_name", "_def", "_price", "_current", "_max", "_respawn", "_stack" }) {
				Remove(store, key + field);
			}
		}

		/// <summary>Pridej zbozi: i_diy_vendor_pridavac's line, the item's name and def and zeros. False when the offer is full.</summary>
		internal static bool AddGoods(Thing store, int address, int slot, Item item) {
			int count = GoodsCount(store, address, slot);
			if (count >= MaxGoods) {
				return false;
			}
			count++;
			Set(store, OfferKey(address, slot) + "_count", count);
			SetGoods(store, address, slot, count, new DiyGoods { Name = item.Name, Defname = item.Def.PrettyDefname });
			return true;
		}

		/// <summary>Smazat: the rest move up a line. Erebor lowered the count through the player's tag, so its last line doubled.</summary>
		internal static void RemoveGoods(Thing store, int address, int slot, int k) {
			int count = GoodsCount(store, address, slot);
			if ((k < 1) || (k > count)) {
				return;
			}
			for (int i = k; i < count; i++) {
				SetGoods(store, address, slot, i, Goods(store, address, slot, i + 1));
			}
			RemoveGoodsTags(store, address, slot, count);
			Set(store, OfferKey(address, slot) + "_count", count - 1);
		}

		/// <summary>Drawing an offer: a line with no max and no respawn gets 100 as its count, Erebor's first IF; its second
		/// wrote to a tag of a broken name and did nothing.</summary>
		internal static DiyGoods GoodsForShow(Thing store, int address, int slot, int k) {
			DiyGoods goods = Goods(store, address, slot, k);
			if ((goods.Respawn == 0) && (goods.Max == 0)) {
				goods.Current = 100;
				Set(store, GoodsKey(address, slot, k) + "_current", 100);
			}
			return goods;
		}

		/// <summary>@UserDclick: the dialog within 3 tiles, a ghost is told off; a GM also gets the editor, from anywhere.
		/// False leaves a player further away to the paperdoll, as Erebor's trigger didn't return 1.</summary>
		internal static bool TryDClick(NPC vendor, Character from) {
			Player player = from as Player;
			if ((player == null) || !IsDiyVendor(vendor)) {
				return false;
			}
			bool handled = false;
			if (Point2D.GetSimpleDistance(vendor, player) < DclickReach) {
				if (player.Flag_Dead) {
					player.RedMessage("Jako duch tohle nemuzes.");
					return true;
				}
				D_DiyVendor.Show(player, vendor, 0, null);
				handled = true;
			}
			if (player.IsGM) {
				D_DiyVendorGm.Show(player, vendor, 0);
				handled = true;
			}
			return handled;
		}

		/// <summary>spk_diy_vendor: within 8 tiles the dialog, further the far message; a GM also gets the editor.</summary>
		internal static void Hear(NPC vendor, Player player) {
			if (Point2D.GetSimpleDistance(vendor, player) > SpeechReach) {
				if (player.Flag_Dead) {
					player.RedMessage("Jako duch tohle nemuzes!");
					return;
				}
				player.SysMessage(FarMessage);
			} else {
				D_DiyVendor.Show(player, vendor, 0, null);
			}
			if (player.IsGM) {
				D_DiyVendorGm.Show(player, vendor, 0);
			}
		}

		/// <summary>Sphere's rescount of a def: the pack and the bags in it.</summary>
		internal static int CountIn(Container container, ThingDef def) {
			int total = 0;
			foreach (AbstractItem item in container) {
				if (item.Def == def) {
					total += item.Amount;
				} else if (item is Container inner) {
					total += CountIn(inner, def);
				}
			}
			return total;
		}

		private static int ConsumeFrom(Container container, ThingDef def, int amount) {
			foreach (AbstractItem item in new List<AbstractItem>(container)) {
				if (amount <= 0) {
					break;
				}
				if (item.Def == def) {
					amount -= (int) ((Item) item).Consume(amount);
				} else if (item is Container inner) {
					amount = ConsumeFrom(inner, def, amount);
				}
			}
			return amount;
		}

		/// <summary>d_dyi_vendor_smlouva's OK. VYKUP takes the player's goods for gold (all they have when it's fewer),
		/// PRODEJ takes gold through GoldUtils.Pay for goods: one by one, or one stack of stackable ones.</summary>
		internal static void Deal(NPC vendor, Player player, int kind, DiyGoods goods, string countText) {
			if (Point2D.GetSimpleDistance(vendor, player) > DialogReach) {
				player.SysMessage(FarMessage);
				return;
			}
			int count = ToNumber(countText);
			if ((count <= 0) || (count > goods.Current)) {
				player.SysMessage("Nezkousej me obalamutit...");
				return;
			}
			ThingDef def = ThingDef.GetByDefname(goods.Defname);
			if (kind == Vykup) {
				int have = (def == null) ? 0 : CountIn(player.Backpack, def);
				if (have >= count) {
					ConsumeFrom(player.Backpack, def, count);
					GoldUtils.Grant(player, (uint) Math.Min(uint.MaxValue, (long) goods.Price * count));
				} else if (have > 0) {
					player.SysMessage("Mas u sebe jen " + have + " kusu,ale ja je vemu...");
					GoldUtils.Grant(player, (uint) Math.Min(uint.MaxValue, (long) goods.Price * have));
					ConsumeFrom(player.Backpack, def, have);
				} else {
					player.SysMessage("Nesplnujes obchodni podminky");
				}
				return;
			}
			ItemDef itemDef = def as ItemDef;
			if ((itemDef == null) || !GoldUtils.Pay(player, (long) goods.Price * count)) {
				player.SysMessage("Nesplnujes obchodni podminky");
				return;
			}
			if (goods.Stacks) {
				((Item) itemDef.Create(player.Backpack)).Amount = count;
			} else {
				for (int i = 0; i < count; i++) {
					itemDef.Create(player.Backpack);
				}
			}
		}
	}

	/// <summary>[speech spk_diy_vendor]: a greeting or a word of buying.</summary>
	public sealed class DiyVendorSpeech : AbstractSpeech {
		private static readonly string[] words = { "hi", "dobry", "ahoj", "zdravi", "buy", "koupit", "nakup" };

		public new static void Bootstrap() {
			new DiyVendorSpeech("spk_diy_vendor").Register();
		}

		private DiyVendorSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC vendor = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech ?? "";
			if ((vendor == null) || (player == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			foreach (string word in words) {
				if (speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
					DiyVendor.Hear(vendor, player);
					return SpeechResult.ActedUponExclusively;
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>i_diy_vendor_pridavac: "Zamer item,ktery chces pridat", the item goes into the offer.</summary>
	public class Targ_DiyVendorGoods : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamer item,ktery chces pridat");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			object[] where = (object[]) parameter;
			NPC vendor = (NPC) where[0];
			int address = (int) where[1], slot = (int) where[2];
			if (!DiyVendor.AddGoods(DiyVendor.Store(vendor), address, slot, targetted)) {
				self.SysMessage("Nabidka je jiz plna");
			}
			D_DiyOfferGm.Show(self, vendor, address, slot);
			return TargetResult.Done;
		}
	}
}
