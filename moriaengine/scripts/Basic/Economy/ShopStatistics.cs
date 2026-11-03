using System;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1070 - what an NPC shop (the alchemist, the Prodejce) has sold, for GMs: Erebor's tag(pocet_N) and
	/// f_npc_drazitel_vybrano on the spawn, kept here on the NPC. Erebor's sum went wrong from a million on
	/// (it took milions*100000 off); this one counts.</summary>
	public static class ShopStatistics {
		private static readonly TagKey totalTK = TagKey.Acquire("_shopSoldFor_");

		private static TagKey CountTK(int ware) {
			return TagKey.Acquire("_shopSold_" + ware.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>A non-GM's purchase: each ware's count and the sum.</summary>
		internal static void Record(NPC shop, Player buyer, int[] counts, int total) {
			if (buyer.IsGM) {
				return;
			}
			for (int i = 0; i < counts.Length; i++) {
				if (counts[i] > 0) {
					shop.SetTag(CountTK(i), Sold(shop, i) + counts[i]);
				}
			}
			shop.SetTag(totalTK, SoldFor(shop) + total);
		}

		internal static long Sold(NPC shop, int ware) {
			return Convert.ToInt64(shop.GetTag(CountTK(ware)) ?? 0, CultureInfo.InvariantCulture);
		}

		internal static long SoldFor(NPC shop) {
			return Convert.ToInt64(shop.GetTag(totalTK) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>f_value_cascade: 1M 2K 3.</summary>
		internal static string Cascade(long number) {
			long millions = number / 1000000, thousands = (number % 1000000) / 1000, units = number % 1000;
			if (millions > 0) {
				return millions + "M " + thousands + "K " + units;
			}
			if (thousands > 0) {
				return thousands + "K " + units;
			}
			return units.ToString(CultureInfo.InvariantCulture);
		}

		internal static void Show(Player gm, NPC shop, string[] wareNames) {
			gm.Dialog(gm, SingletonScript<D_ShopStatistics>.Instance, new DialogArgs(shop, wareNames));
		}
	}

	/// <summary>d_npc_alchymista_statistika, d_npc_drazitel_statistika.</summary>
	public class D_ShopStatistics : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC shop = (NPC) args[0];
			string[] names = (string[]) args[1];
			this.SetLocation(205, 150);
			this.AddPage(0);
			this.AddBackground(0, 0, 370, 80 + (names.Length * 20), 2620);
			this.AddLabel(16, 12, 42, "Statistika vykoupeneho zbozi");
			this.AddButton(330, 9, 0xfb1, 0xfb3, 1, GumpButtonType.Reply, 0);
			this.AddLabel(16, 36, 52, "Jmeno Polozky:");
			this.AddLabel(199, 36, 52, "Vykoupeno Ks:");
			for (int i = 0; i < names.Length; i++) {
				this.AddLabel(16, 58 + (i * 20), 2301, names[i]);
				this.AddLabel(202, 58 + (i * 20), 2301, ShopStatistics.Cascade(ShopStatistics.Sold(shop, i)) + " ks");
			}
			int y = 66 + (names.Length * 20);
			this.AddLabel(16, y - 8, 52, "Celkem prodano za:");
			this.AddLabel(202, y - 8, 2301, ShopStatistics.Cascade(ShopStatistics.SoldFor(shop)) + " gp");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
