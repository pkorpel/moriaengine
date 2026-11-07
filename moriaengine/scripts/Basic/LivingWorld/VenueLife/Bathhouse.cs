using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - the bath house: a bath makes the player fresh (cheaper shops, warmer residents), the steam has its
	/// gossip, and the stoker pays for logs. Offered through VenueActions.</summary>
	public static class Bathhouse {
		internal static readonly TagKey FreshUntilTK = TagKey.Acquire("_bath_fresh_until_");

		private sealed class FreshSource : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				double left = FreshSecondsLeft(ch);
				if (left > 0) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Svezest") {
						SecondsLeft = left, Detail = "Obchody ve mestech -" + Settings.freshDiscount + "%, lide jsou k tobe vlidnejsi", Source = "Lazne"
					});
				}
			}
		}

		private static readonly FreshSource source = new FreshSource();

		public static void Bootstrap() {
			ActiveEffects.Register(source);
			VenueActions.Register(new VenueAction("bath_bathe", "Vykoupat se", new[] { "bathhouse" }, new[] { "attendant" }, null, Bathe));
			VenueActions.Register(new VenueAction("bath_rumour", "Co se povida v pare?", new[] { "bathhouse" }, new[] { "attendant", "bather" }, null, Tavern.TellRumour));
			VenueActions.Register(new VenueAction("bath_wood", "Prinest drevo", new[] { "bathhouse" }, new[] { "stoker" }, null, BringWood));
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		public static double FreshSecondsLeft(Character ch) {
			return (ch.GetTag(FreshUntilTK) is double until) ? until - Globals.TimeInSeconds : 0;
		}

		public static bool IsFresh(Character ch) {
			return (ch != null) && (FreshSecondsLeft(ch) > 0);
		}

		/// <summary>Reputation.PriceFor: a fresh player pays freshDiscount percent less, never nothing.</summary>
		public static long Freshen(Character player, long price) {
			if ((price <= 0) || !IsFresh(player)) {
				return price;
			}
			return Math.Max(1, (price * (100 - Math.Max(0, Math.Min(100, Settings.freshDiscount))) + 50) / 100);
		}

		/// <summary>Errands.Finish: a fresh player's done errand warms its giver one point more.</summary>
		public static int FriendshipBonus(Character player) {
			return IsFresh(player) ? 1 : 0;
		}

		internal static void Bathe(Player player, NPC npc, Item gem) {
			if (IsFresh(player)) {
				npc.Say("Svezest z posledni koupele ti jeste vydrzi. Prijd pozdeji.");
				return;
			}
			int price = Settings.bathPrice;
			if (!GoldUtils.Pay(player, price, PaySource.Usual, "")) {
				npc.Say("Koupel stoji " + price + " zlatych. Tolik u sebe nemas.");
				return;
			}
			player.SetTag(FreshUntilTK, Globals.TimeInSeconds + (Settings.freshMinutes * 60.0));
			npc.Say(ErrandContext.Pick("Voda je horka, mydlo voni. Uzij si to.", "Tady mas rucnik. Pozor, kamen klouze.", "Pare je dost, jen se do ni ponor."));
			player.SysMessage("Citis se svezi: obchody jsou k tobe vlidnejsi a lide take.");
		}

		private static readonly VenueGoods.Lines woodLines = new VenueGoods.Lines {
			Ask = wage => "Prines polena, at mam cim topit. Zaplatim " + wage + " zlatych za kus.",
			Full = "Dnes uz mam dreva dost. Prijd zitra.",
			Thanks = (pieces, pay) => "Diky, " + pieces + " dreva se hodi. Tady mas " + pay + " zlatych."
		};

		internal static void BringWood(Player player, NPC npc, Item gem) {
			VenueGoods.Buy(player, npc, gem, "i_log", Settings.woodWage, Settings.woodPerDay, "bathwood", woodLines);
		}
	}
}
