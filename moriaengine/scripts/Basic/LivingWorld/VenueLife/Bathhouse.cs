using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - the bath house: a bath makes the player fresh (cheaper shops, warmer residents), the steam has its
	/// gossip, and the stoker pays for logs. Offered through VenueActions.</summary>
	public static class Bathhouse {
		internal static readonly TagKey FreshUntilTK = TagKey.Acquire("_bath_fresh_until_");
		internal static readonly TagKey WoodDayTK = TagKey.Acquire("_bath_wood_day_");
		private static readonly TagKey woodCountTK = TagKey.Acquire("_bath_wood_count_");

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

		internal static void BringWood(Player player, NPC npc, Item gem) {
			ItemDef log = ThingDef.GetByDefname("i_log") as ItemDef;
			string id = Residents.IdOf(gem);
			if ((log == null) || (id == null)) {
				return;
			}
			long now = Errands.Now.Ticks;
			int today = 0;
			if ((player.GetTag(WoodDayTK) is long since) && ((now - since) < TimeSpan.TicksPerDay)) {
				today = (player.GetTag(woodCountTK) is int c) ? c : 0;
			}
			int room = Settings.woodPerDay - today;
			if (room <= 0) {
				npc.Say("Dnes uz mam dreva dost. Prijd zitra.");
				return;
			}
			int count = Math.Min(room, Errands.CountIn(player.Backpack, log));
			if (count <= 0) {
				npc.Say("Prines polena, at mam cim topit. Zaplatim " + Settings.woodWage + " zlatych za kus.");
				return;
			}
			count -= Errands.Consume(player.Backpack, log, count); //what was really taken
			if (count <= 0) {
				return;
			}
			if (today == 0) {
				player.SetTag(WoodDayTK, now);
				Residents.AddFriendship(player, id, 1);
			}
			player.SetTag(woodCountTK, today + count);
			GoldUtils.Grant(player, (uint) (count * Settings.woodWage));
			string logs = (count == 1) ? "poleno se hodi" : ((count < 5) ? count + " polena se hodi" : count + " polen se hodi");
			npc.Say("Diky, " + logs + ". Tady mas " + (count * Settings.woodWage) + " zlatych.");
		}
	}
}
