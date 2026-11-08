/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1280 - a rank in Barad-dur's war camp: the city's reputation (ReputationLevel) read the camp's way, worst first.</summary>
	public enum CampRank {
		Zradce, Cerv, Rekrut, Vojak, Desatnik, Setnik, Nacelnik
	}

	/// <summary>#1280 - .settings "Tabor": the war camp's orders, promotions and rival captains.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class WarCampSettings : SettingsMetaCategory {
		[SavedMember]
		public static WarCampSettings instance = new WarCampSettings();

		[LoadingInitializer]
		public WarCampSettings() {
		}

		/// <summary>Standing lost in Barad-dur for refusing an order outright, and for giving one up or letting it run out.</summary>
		[SaveableData]
		public int orderRefuseLoss = 25, orderFailLoss = 40;

		/// <summary>An order's deadline, in percent of the errand's usual time.</summary>
		[SaveableData]
		public int orderMinutesPercent = 50;

		/// <summary>Gold for a promotion, times the new rank's number (Vojak 3 ... Nacelnik 6).</summary>
		[SaveableData]
		public int promotionGold = 300;

		/// <summary>In percent: a better share of an order's pay for each rank above Vojak.</summary>
		[SaveableData]
		public int rankPayBonus = 10;

		/// <summary>A captain's order: how long it runs (real minutes), the standing it earns with the captain's band and the camp,
		/// and its pay times the captain's power (1-5).</summary>
		[SaveableData]
		public int captainOrderMinutes = 60, captainOrderFavour = 30, captainOrderGold = 200, captainOrderExp = 150;

		/// <summary>Real minutes before a captain gives the same hero another order, after one is done or failed.</summary>
		[SaveableData]
		public int captainOrderCooldownMinutes = 20;

		/// <summary>A captain's order's size: the tribute in gold and the iron ingots, times his power; heads of Gondor's people.</summary>
		[SaveableData]
		public int tributePerPower = 1000, ironBase = 20, ironPerPower = 10;

		/// <summary>Serving a captain: the standing it gives with his band (the rival loses half, as with any faction).</summary>
		[SaveableData]
		public int serveFavour = 40;

		/// <summary>Days between the captains' settlements.</summary>
		[SaveableData]
		public int challengeDays = 7;

		/// <summary>In percent: the chance that the rival sends cutthroats when a captain's man takes an order; how many, how many
		/// seconds later, and how long they hunt before they leave.</summary>
		[SaveableData]
		public int cutthroatChance = 30, cutthroatCount = 2, cutthroatDelaySeconds = 60, cutthroatStaySeconds = 300;
	}

	/// <summary>
	/// #1280 - Barad-dur runs as a war camp (design section 17, option B: orders, not favours). Its reputation is a rank; its
	/// residents' errands are orders: refusing one outright, giving it up or letting it run out costs standing, the deadlines are
	/// shorter, the rank decides how big an order can be and raises its pay. A resident's friendship is the superior's favour.
	/// The rival captains are RivalCaptains.
	/// </summary>
	public static class WarCamp {
		public const string City = "Barad-dur";

		internal static readonly TagKey TopRankTK = TagKey.Acquire("_campRankTop_");

		internal static WarCampSettings Settings {
			get { return WarCampSettings.instance; }
		}

		public static bool IsCamp(string city) {
			return city == City;
		}

		public static CampRank RankOf(Character player) {
			return (CampRank) (int) Reputation.Level(player, City);
		}

		public static string RankName(CampRank rank) {
			switch (rank) {
				case CampRank.Zradce: return "Zradce";
				case CampRank.Cerv: return "Cerv";
				case CampRank.Rekrut: return "Rekrut";
				case CampRank.Vojak: return "Vojak";
				case CampRank.Desatnik: return "Desatnik";
				case CampRank.Setnik: return "Setnik";
				default: return "Nacelnik";
			}
		}

		/// <summary>The biggest order the rank is given: small up to Rekrut, medium for Vojak and Desatnik, large from Setnik.</summary>
		public static ErrandTier MaxTier(CampRank rank) {
			if (rank >= CampRank.Setnik) {
				return ErrandTier.Large;
			}
			return (rank >= CampRank.Vojak) ? ErrandTier.Medium : ErrandTier.Small;
		}

		/// <summary>Whether this giver's errand can't be offered to the player: in the camp, one above the rank.</summary>
		public static bool AboveRank(Player player, string city, ErrandTier tier) {
			return IsCamp(city) && (tier > MaxTier(RankOf(player)));
		}

		/// <summary>An order's deadline, shorter than an errand's.</summary>
		public static int Minutes(string city, int minutes) {
			return IsCamp(city) ? Math.Max(5, (minutes * Settings.orderMinutesPercent) / 100) : minutes;
		}

		/// <summary>An order's pay share: the rank's bonus above Vojak.</summary>
		public static int PayShare(Player player, string city, int share) {
			if (!IsCamp(city)) {
				return share;
			}
			int above = Math.Max(0, (int) RankOf(player) - (int) CampRank.Vojak);
			return (share * (100 + (above * Settings.rankPayBonus))) / 100;
		}

		/// <summary>An order refused outright: standing lost.</summary>
		public static void OnRefused(Player player, string city) {
			if (IsCamp(city)) {
				Reputation.Change(player, City, -Settings.orderRefuseLoss);
				player.RedMessage("Odmitl jsi rozkaz. V tabore si to zapamatuji.");
			}
		}

		/// <summary>An order given up or run out: standing lost.</summary>
		public static void OnFailed(Player player, Errand errand) {
			if (IsCamp(errand.city)) {
				Reputation.Change(player, City, -Settings.orderFailLoss);
				player.RedMessage("Rozkaz \"" + errand.name + "\" nesplnen. To se ti nevyplati.");
			}
		}

		/// <summary>After Barad-dur's standing changed, for Mordor's own: a rise to a rank never reached before pays once for each
		/// rank crossed (a loss never pays, whatever drift or a GM did before it), a fall is told.</summary>
		internal static void AfterChange(Player player, ReputationLevel before) {
			if (player.Realm != Realm.Mordor) {
				return;
			}
			CampRank was = (CampRank) (int) before, now = RankOf(player);
			object top = player.GetTag(TopRankTK);
			int best = (top == null) ? (int) was : Convert.ToInt32(top, CultureInfo.InvariantCulture);
			if ((now > was) && ((int) now > best)) {
				long gold = 0;
				for (int rank = best + 1; rank <= (int) now; rank++) {
					gold += (long) rank * Settings.promotionGold;
				}
				player.SetTag(TopRankTK, (int) now);
				GoldUtils.Grant(player, (uint) gold);
				player.SysMessage("Byl jsi povysen: " + RankName(now) + "! Odmena " + gold.ToString(CultureInfo.InvariantCulture) + " zlatych.", ClassAbilities.ClassMessageHue);
			} else if (top == null) {
				player.SetTag(TopRankTK, Math.Max((int) now, best));
			}
			if (now < was) {
				player.RedMessage("Byl jsi degradovan: " + RankName(now) + ".");
			}
		}

		/// <summary>A superior's greeting in the camp: by favour (friendship) and the player's rank.</summary>
		internal static string Greeting(Player player, int favour) {
			string rank = RankName(RankOf(player)).ToLowerInvariant();
			if (favour >= 7) {
				return "Ty jsi muj nejlepsi skret, " + rank + ". Mam pro tebe praci.";
			}
			if (favour >= 3) {
				return "Zase ty, " + rank + "? Dobre, aspon nekdo tu neni k nicemu.";
			}
			return ErrandContext.Pick("Co tu stojis, " + rank + "? Hni sebou!", "Mluv, " + rank + ". Nemam cely den.", "Na co cumis, " + rank + "?");
		}
	}
}
