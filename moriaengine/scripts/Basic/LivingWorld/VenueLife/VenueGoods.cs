using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1258 - a resident buying goods from the player's pack for wages: the bath house's logs, the smith's ore. A cap
	/// of pieces a real day per player and resident, and friendship +1 for the first load of the day. Only goods nobody sells
	/// belong here, or the wages could be bought.</summary>
	public static class VenueGoods {
		/// <summary>The words a buyer says: when there's nothing to buy (with the wage), when the day's cap is met, and the thanks
		/// (with the pieces in words and the pay).</summary>
		public sealed class Lines {
			public Func<int, string> Ask;
			public string Full;
			public Func<string, int, string> Thanks;
		}

		/// <summary>Pieces taken now, 0 when none.</summary>
		public static int Buy(Player player, NPC npc, Item gem, string defname, int wage, int perDay, string key, Lines lines) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			string id = Residents.IdOf(gem);
			if ((def == null) || (id == null)) {
				return 0;
			}
			TagKey dayTK = TagKey.Acquire("_goods_" + key + "_day_"), countTK = TagKey.Acquire("_goods_" + key + "_count_");
			long now = Errands.Now.Ticks;
			int today = 0;
			if ((player.GetTag(dayTK) is long since) && ((now - since) < TimeSpan.TicksPerDay)) {
				today = (player.GetTag(countTK) is int c) ? c : 0;
			}
			int room = perDay - today;
			if (room <= 0) {
				npc.Say(lines.Full);
				return 0;
			}
			int count = Math.Min(room, Errands.CountIn(player.Backpack, def));
			if (count <= 0) {
				npc.Say(lines.Ask(wage));
				return 0;
			}
			count -= Errands.Consume(player.Backpack, def, count); //what was really taken
			if (count <= 0) {
				return 0;
			}
			if (today == 0) {
				player.SetTag(dayTK, now);
				Residents.AddFriendship(player, id, 1);
			}
			player.SetTag(countTK, today + count);
			GoldUtils.Grant(player, (uint) (count * wage));
			npc.Say(lines.Thanks(Pieces(count), count * wage));
			return count;
		}

		/// <summary>Czech counted forms: 1 kus, 2-4 kusy, 5+ kusu.</summary>
		public static string Pieces(int count) {
			return (count == 1) ? "jeden kus" : ((count < 5) ? count + " kusy" : count + " kusu");
		}

		/// <summary>Tests: the day's count for a buyer's key.</summary>
		public static void ForgetDay(Player player, string key) {
			player.RemoveTag(TagKey.Acquire("_goods_" + key + "_day_"));
			player.RemoveTag(TagKey.Acquire("_goods_" + key + "_count_"));
		}
	}
}
