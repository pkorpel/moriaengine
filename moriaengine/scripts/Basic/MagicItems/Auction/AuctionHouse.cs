using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1070 - the in-game magic item auction, in place of Erebor's web auction (web/web.scp's .aukce, which only
	/// exported marked items to a website; they never left their owners). The user's calls: each realm its own auction,
	/// .aukce anywhere, no house cut, the seller's name and realm shown. Erebor's listing rules stay: identified magic items
	/// (tmi_info) of your own, never newbie or move-never, prices up to 1000M. A listed item waits in the auction house's
	/// hidden box on plane 250, its listing in tags on the item; bids are paid at once and given back when outbid; at the
	/// end the item and the gold go to the banks.</summary>
	public static class AuctionHouse {
		internal const int MaxPrice = 1000000000;
		private const string NotListable = "Tento predmet nelze pridat do aukce.";
		private static readonly TagKey boxTK = TagKey.Acquire("_auctionHouse_");
		private static readonly TagKey sellerTK = TagKey.Acquire("_auc_seller_");
		private static readonly TagKey realmTK = TagKey.Acquire("_auc_realm_");
		private static readonly TagKey startTK = TagKey.Acquire("_auc_start_");
		private static readonly TagKey buyoutTK = TagKey.Acquire("_auc_buyout_");
		private static readonly TagKey bidTK = TagKey.Acquire("_auc_bid_");
		private static readonly TagKey bidderTK = TagKey.Acquire("_auc_bidder_");
		private static readonly TagKey endsTK = TagKey.Acquire("_auc_ends_");
		private static Container box;

		/// <summary>The hidden box holding what's listed, found by its tag after a restart, made on first need.</summary>
		internal static Container Box {
			get {
				if ((box != null) && !box.IsDeleted) {
					return box;
				}
				box = FindSavedBox();
				if (box == null) {
					box = (Container) ThingDef.GetByDefname("i_backpack").Create(10, 10, 0, (byte) MysticHypnosis.ParkedPlane);
					box.Name = "Aukce";
					box.SetTag(boxTK, 1);
					box.SetAttribute(ItemAttributes.attr_move_never, true);
					box.SetAttribute(ItemAttributes.attr_invis, true);
				}
				if (box.GetTimer(AuctionSweepTimer.Key) == null) {
					box.AddTimer(AuctionSweepTimer.Key, new AuctionSweepTimer()).DueInSeconds = AuctionSweepTimer.Period;
				}
				return box;
			}
		}

		/// <summary>The box as the world holds it, by its tag; nothing is made or added.</summary>
		internal static Container FindSavedBox() {
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is Container found) && (found.GetTag(boxTK) != null) && !found.IsDeleted) {
					return found;
				}
			}
			return null;
		}

		internal static bool IsListed(Item item) {
			return (item != null) && !item.IsDeleted && (item.GetTag(sellerTK) != null) && (item.Cont == Box);
		}

		internal static Character Seller(Item item) {
			return item.GetTag(sellerTK) as Character;
		}

		internal static Realm RealmOf(Item item) {
			return (Realm) Convert.ToInt32(item.GetTag(realmTK) ?? 0, CultureInfo.InvariantCulture);
		}

		internal static int Start(Item item) {
			return Number(item, startTK);
		}

		internal static int Buyout(Item item) {
			return Number(item, buyoutTK);
		}

		internal static int Bid(Item item) {
			return Number(item, bidTK);
		}

		internal static Character Bidder(Item item) {
			return item.GetTag(bidderTK) as Character;
		}

		internal static DateTime Ends(Item item) {
			return new DateTime(Convert.ToInt64(item.GetTag(endsTK) ?? 0L, CultureInfo.InvariantCulture), DateTimeKind.Utc);
		}

		/// <summary>For tests: the listing ends now, so the next sweep settles it.</summary>
		internal static void EndNow(Item item) {
			item.SetTag(endsTK, DateTime.UtcNow.Ticks - 1);
		}

		private static int Number(Item item, TagKey key) {
			return Convert.ToInt32(item.GetTag(key) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>The least the next bid may be: the start price, or the bid now and AuctionSettings' share of it more.</summary>
		internal static int MinBid(Item item) {
			int bid = Bid(item);
			if (Bidder(item) == null) {
				return Math.Max(1, Start(item));
			}
			long raise = Math.Max(1, ((long) bid * AuctionSettings.instance.minRaisePercent) / 100);
			return (int) Math.Min(MaxPrice, bid + raise);
		}

		/// <summary>What a viewer sees: the own realm's listings, a GM all; the soonest to end first.</summary>
		internal static List<Item> Listings(Character viewer) {
			List<Item> found = new List<Item>();
			foreach (AbstractItem thing in Box) {
				if ((thing is Item item) && IsListed(item) && (viewer.IsGM || (RealmOf(item) == viewer.Realm))) {
					found.Add(item);
				}
			}
			found.Sort((a, b) => Ends(a).CompareTo(Ends(b)));
			return found;
		}

		internal static int CountOf(Character seller) {
			int count = 0;
			foreach (AbstractItem thing in Box) {
				if ((thing is Item item) && IsListed(item) && (Seller(item) == seller)) {
					count++;
				}
			}
			return count;
		}

		/// <summary>The listing rules, f_AddItemToAuction plus a realm and a count; null when it may go in.</summary>
		internal static string Refusal(Player seller, Item item) {
			if ((item == null) || (item.TopObj() != seller) || (item.Cont == seller)) {
				return "Vec musis mit u sebe.";
			}
			if (item.HasAttribute(ItemAttributes.attr_newbie) || item.HasAttribute(ItemAttributes.attr_move_never) || !MagicItems.IsMagic(item)) {
				return NotListable;
			}
			if (CountOf(seller) >= AuctionSettings.instance.maxListings) {
				return "Vic nez " + AuctionSettings.instance.maxListings + " aukci najednou mit nemuzes.";
			}
			return null;
		}

		/// <summary>Puts an item up: it leaves the seller for the box, the listing on it.</summary>
		internal static string List(Player seller, Item item, int start, int buyout) {
			string refusal = Refusal(seller, item);
			if (refusal != null) {
				return refusal;
			}
			item.Cont = Box;
			item.SetTag(sellerTK, seller);
			item.SetTag(realmTK, (int) seller.Realm);
			item.SetTag(startTK, start);
			item.SetTag(buyoutTK, buyout);
			item.RemoveTag(bidTK);
			item.RemoveTag(bidderTK);
			item.SetTag(endsTK, DateTime.UtcNow.AddHours(AuctionSettings.instance.durationHours).Ticks);
			Logger.WriteInfo(true, "Aukce: " + seller.Name + " (" + seller.Account?.Name + ") pridal " + item.Name + " (" + item.Uid + ") za " + start + ", vykup " + buyout);
			return null;
		}

		/// <summary>Prihodit: own realm, not one's own item, at least the minimum; the gold goes at once, the bid before it back.
		/// A bid reaching the buyout buys it.</summary>
		internal static void PlaceBid(Player bidder, Item item, int amount) {
			if (!CanTrade(bidder, item)) {
				return;
			}
			int buyout = Buyout(item);
			if ((buyout > 0) && (amount >= buyout)) {
				BuyOut(bidder, item);
				return;
			}
			int min = MinBid(item);
			if (amount < min) {
				bidder.SysMessage("Nejmensi mozny prihoz je " + min + " zlatych.");
				return;
			}
			if (!GoldUtils.Pay(bidder, amount)) {
				return;
			}
			ReturnBid(item, "Na " + item.Name + " v aukci nekdo prihodil vic. Tvych " + Bid(item) + " zlatych je zpet v bance.");
			item.SetTag(bidTK, amount);
			item.SetTag(bidderTK, bidder);
			bidder.SysMessage("Prihodil" + (bidder.IsFemale ? "a" : "") + " jsi " + amount + " zlatych na " + item.Name + ".");
			Notify(Seller(item), "Na tvuj predmet " + item.Name + " v aukci nekdo prihodil " + amount + " zlatych.");
		}

		/// <summary>Koupit: the buyout price, and the listing ends with this buyer.</summary>
		internal static void BuyOut(Player buyer, Item item) {
			if (!CanTrade(buyer, item)) {
				return;
			}
			int buyout = Buyout(item);
			if (buyout <= 0) {
				buyer.SysMessage("Tento predmet nema vykupni cenu.");
				return;
			}
			if (!GoldUtils.Pay(buyer, buyout)) {
				return;
			}
			ReturnBid(item, "Predmet " + item.Name + " z aukce nekdo koupil za vykupni cenu. Tvych " + Bid(item) + " zlatych je zpet v bance.");
			item.SetTag(bidTK, buyout);
			item.SetTag(bidderTK, buyer);
			Settle(item);
		}

		private static bool CanTrade(Player buyer, Item item) {
			if (!IsListed(item)) {
				buyer.SysMessage("Tato aukce uz skoncila.");
				return false;
			}
			if (buyer.IsGM) {
				buyer.SysMessage("GM v aukci neprihazuje.");
				return false;
			}
			if (Seller(item) == buyer) {
				buyer.SysMessage("Na vlastni predmet prihazovat nemuzes.");
				return false;
			}
			if (RealmOf(item) != buyer.Realm) {
				buyer.SysMessage("Tato aukce neni pro tvuj realm.");
				return false;
			}
			return true;
		}

		/// <summary>Stahnout: the seller while nobody has bid, a GM always (a bid goes back); the item to the seller's bank.</summary>
		internal static void Withdraw(Player who, Item item) {
			if (!IsListed(item)) {
				who.SysMessage("Tato aukce uz skoncila.");
				return;
			}
			if (!who.IsGM && ((Seller(item) != who) || (Bidder(item) != null))) {
				who.SysMessage((Seller(item) == who) ? "Na predmet uz nekdo prihodil, stahnout ho nemuzes." : "Tohle neni tvoje aukce.");
				return;
			}
			ReturnBid(item, "Aukce na " + item.Name + " byla zrusena. Tvych " + Bid(item) + " zlatych je zpet v bance.");
			Character seller = Seller(item);
			Clear(item);
			Deliver(item, seller, "Tvuj predmet " + item.Name + " byl stazen z aukce a je v bance.");
		}

		/// <summary>The end: the item to the winner's bank and the bid, less the house cut, to the seller's; with no bid the
		/// item goes back to the seller's bank.</summary>
		internal static void Settle(Item item) {
			Character seller = Seller(item), winner = Bidder(item);
			int bid = Bid(item);
			Clear(item);
			if ((winner == null) || winner.IsDeleted) {
				Deliver(item, seller, "Aukce na " + item.Name + " skoncila bez nabidky, predmet je zpet v tve bance.");
				return;
			}
			Deliver(item, winner, "Vyhral" + (winner.IsFemale ? "a" : "") + " jsi aukci na " + item.Name + " za " + bid + " zlatych, predmet je v tve bance.");
			int paid = bid - (int) (((long) bid * AuctionSettings.instance.houseCutPercent) / 100);
			if ((seller != null) && !seller.IsDeleted) {
				GoldUtils.GrantToBank(seller, (uint) Math.Max(0, paid));
				Notify(seller, "Tvuj predmet " + item.Name + " se v aukci prodal za " + bid + " zlatych, " + paid + " zlatych je v tve bance.");
			}
			Logger.WriteInfo(true, "Aukce: " + item.Name + " (" + item.Uid + ") prodano " + winner.Name + " za " + bid);
		}

		/// <summary>The minute sweep: every listing past its end.</summary>
		internal static void Sweep() {
			DateTime now = DateTime.UtcNow;
			foreach (AbstractItem thing in new List<AbstractItem>(Box)) {
				if ((thing is Item item) && IsListed(item) && (Ends(item) <= now)) {
					Settle(item);
				}
			}
		}

		private static void ReturnBid(Item item, string message) {
			Character bidder = Bidder(item);
			int bid = Bid(item);
			if ((bidder != null) && !bidder.IsDeleted && (bid > 0)) {
				GoldUtils.GrantToBank(bidder, (uint) bid);
				Notify(bidder, message);
			}
		}

		private static void Clear(Item item) {
			foreach (TagKey key in new[] { sellerTK, realmTK, startTK, buyoutTK, bidTK, bidderTK, endsTK }) {
				item.RemoveTag(key);
			}
		}

		private static void Deliver(Item item, Character to, string message) {
			if ((to == null) || to.IsDeleted) {
				Logger.WriteWarning("Aukce: " + item.Name + " (" + item.Uid + ") nema komu se vratit, smazano");
				item.Delete();
				return;
			}
			item.Cont = to.Bank;
			Notify(to, message);
		}

		/// <summary>At once to someone online, otherwise as a message waiting for them (.messages).</summary>
		internal static void Notify(Character to, string text) {
			if ((to == null) || to.IsDeleted) {
				return;
			}
			if (to.IsOnline) {
				to.SysMessage(text);
			} else {
				MsgsBoard.AddNewMessage(to, new DelayedMsg(text));
			}
		}

		private static readonly string[] metals = { null, "copper", "iron", "silver", "gold", "verite", "valorite", "obsidian", "adamantinum", "mithril", "sand" };
		private static readonly string[] woods = { null, "spruce", "chestnut", "silver", "gold", "oak", "teak", "mahagon", "eben", "elven", "sand" };

		/// <summary>A weapon's or armor's material, by the item names' own words: wood for staves and bows (spruce to elven),
		/// metal for the rest (copper to mithril); null for things with none. A magic item's name doesn't say it.</summary>
		internal static string MaterialOf(Item item) {
			if (!(item.Def is IObjectWithMaterial made) || (made.Material == Material.None)) {
				return null;
			}
			string[] names = (made.MaterialType == MaterialType.Wood) ? woods : metals;
			int at = (int) made.Material;
			return (at < names.Length) ? names[at] : made.Material.ToString().ToLowerInvariant();
		}

		/// <summary>The name as the auction shows it: with the material in brackets when there is one.</summary>
		internal static string ShownName(Item item) {
			string material = MaterialOf(item);
			return (material == null) ? item.Name : (item.Name + " (" + material + ")");
		}

		internal static string TimeLeft(Item item) {
			TimeSpan left = Ends(item) - DateTime.UtcNow;
			if (left <= TimeSpan.Zero) {
				return "konci";
			}
			if (left.TotalDays >= 1) {
				return (int) left.TotalDays + "d " + left.Hours + "h";
			}
			return (int) left.TotalHours + "h " + left.Minutes + "m";
		}

		/// <summary>.aukce: the auction's dialog. A new listing starts from its Pridat predmet button (D_AuctionNew); the user
		/// didn't want Erebor's prices as command parameters, so anything after the command is ignored.</summary>
		[SteamFunction]
		public static void Aukce(Character self, ScriptArgs sa) {
			if (self is Player player) {
				D_Auction.Show(player, 0);
			}
		}

		/// <summary>The prices of a new listing, Erebor's checks: a realm, up to 1000M each, a buyout of 0 is none, else no
		/// lower than the start. Null when they're fine.</summary>
		internal static string PriceRefusal(Player player, long start, long buyout) {
			if (player.Realm == Realm.None) {
				return "Bez zvoleneho Realmu nemuzes pridavat veci do aukce.";
			}
			if ((start < 0) || (buyout < 0)) {
				return "Ceny zadavej cislem, nula a vic.";
			}
			if (start > MaxPrice) {
				return "Startovni cena presahuje povolene maximum 1000M.";
			}
			if (buyout > MaxPrice) {
				return "Vykupni cena presahuje povolene maximum 1000M.";
			}
			if ((buyout > 0) && (buyout < start)) {
				return "Vykupni cena nemuze byt nizsi nez startovni.";
			}
			return null;
		}

		/// <summary>i_zamerovatko_aukce: the cursor for an item or a container of them, at these prices.</summary>
		internal static void StartTargeting(Player player, int start, int buyout) {
			player.SysMessage("Zamerujes predmety do aukce s:", ClassAbilities.ClassMessageHue);
			player.SysMessage("Startovni cenou " + start + ", Vykupni cenou " + buyout, ClassAbilities.ClassMessageHue);
			player.Target(SingletonScript<Targ_Auction>.Instance, new[] { start, buyout });
		}
	}

	/// <summary>i_zamerovatko_aukce: an item, or every item straight inside a container, of your own.</summary>
	public class Targ_Auction : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamer predmet, ktery chces pridat do aukce.");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			int[] prices = (int[]) parameter;
			if (targetted.TopObj() != self) {
				self.SysMessage("Vec musis mit u sebe.");
				D_Auction.Show(self, 0);
				return TargetResult.Done;
			}
			int added = 0;
			if (targetted is Container container) {
				foreach (AbstractItem inside in new List<AbstractItem>(container)) {
					if ((inside is Item item) && (AuctionHouse.List(self, item, prices[0], prices[1]) == null)) {
						added++;
					}
				}
			} else {
				string refusal = AuctionHouse.List(self, targetted, prices[0], prices[1]);
				if (refusal != null) {
					self.SysMessage(refusal);
				} else {
					added = 1;
				}
			}
			string did = self.IsFemale ? "Pridala" : "Pridal";
			if (added >= 5) {
				self.SysMessage(did + " jsi " + added + " predmetu do aukce.", ClassAbilities.ClassMessageHue);
			} else if (added > 1) {
				self.SysMessage(did + " jsi " + added + " predmety do aukce.", ClassAbilities.ClassMessageHue);
			} else if (added == 1) {
				self.SysMessage(did + " jsi " + ((targetted is Container) ? "1 predmet" : targetted.Name) + " do aukce.", ClassAbilities.ClassMessageHue);
			}
			D_Auction.Show(self, 0);
			return TargetResult.Done;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			D_Auction.Show(self, 0);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Zamer predmet nebo kontejner...");
			return TargetResult.RestartTargetting;
		}
	}

	/// <summary>Settles ended listings every minute.</summary>
	[SaveableClass, DeepCopyableClass]
	public class AuctionSweepTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_auction_sweep_");
		internal const int Period = 60;

		[LoadingInitializer, DeepCopyImplementation]
		public AuctionSweepTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			AuctionHouse.Sweep();
			this.DueInSeconds = Period;
			this.PeriodInSeconds = Period;
		}
	}
}
