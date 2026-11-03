using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1070 - Erebor's systems/npc_drazitel.scp: c_drazitel, the Prodejce, sells coal, magic thread, staff
	/// crystals and bowstrings at fixed prices to his own realm, as the alchemist does. An order is held 10 minutes,
	/// i_drazitel_memory. A GM also sees his sales.</summary>
	public static class ProdejceShop {
		internal const string NpcDef = "c_drazitel";
		internal const int ReachDistance = 3;
		internal static readonly TimerKey OrderTK = TimerKey.Acquire("_drazitelOrder_");
		private static readonly TagKey metTK = TagKey.Acquire("_drazitelMet_");

		internal static readonly AlchemistWare[] Wares = {
			new AlchemistWare("Uhli", "i_coal", 250, true),
			new AlchemistWare("Magicka Nit", "i_magicka_nit", 10000, true),
			new AlchemistWare("Krystal Mahagon", "i_krystal_mahagon", 12000, false),
			new AlchemistWare("Krystal Teak", "i_krystal_teak", 1200, false),
			new AlchemistWare("Krystal Oak", "i_krystal_oak", 400, false),
			new AlchemistWare("Krystal Chestnut", "i_krystal_chestnut", 60, false),
			new AlchemistWare("Krystal Spruce", "i_krystal_spruce", 30, false),
			new AlchemistWare("Tetiva Mahagon", "i_tetiva_mahagon", 9000, false),
			new AlchemistWare("Tetiva Teak", "i_tetiva_teak", 900, false),
			new AlchemistWare("Tetiva Oak", "i_tetiva_oak", 300, false),
			new AlchemistWare("Tetiva Chestnut", "i_tetiva_chestnut", 45, false),
			new AlchemistWare("Tetiva Spruce", "i_tetiva_spruce", 25, false),
		};

		internal static string[] WareNames {
			get { return Array.ConvertAll(Wares, ware => ware.Name); }
		}

		/// <summary>f_drazitel_check_value: coal and thread up to 1000 at once, the rest up to 100.</summary>
		internal static int MaxOf(int ware) {
			return (ware < 2) ? 1000 : 100;
		}

		internal static bool IsProdejce(Character ch) {
			return (ch is NPC) && (ch.Def == ThingDef.GetByDefname(NpcDef));
		}

		internal static bool HaveMet(Player player, NPC prodejce) {
			return Equals(player.GetTag(metTK), prodejce.Uid);
		}

		internal static void Meet(Player player, NPC prodejce) {
			player.SetTag(metTK, prodejce.Uid);
		}

		/// <summary>f_npc_drazitel: his own realm gets the shop, everyone else is sent away; a GM sees the sales too.</summary>
		internal static bool TryOpen(NPC prodejce, Character from) {
			Player player = from as Player;
			if ((player == null) || !IsProdejce(prodejce)) {
				return false;
			}
			if (!player.IsGM && (Point2D.GetSimpleDistance(prodejce, player) > ReachDistance)) {
				player.SysMessage("Na takovou vzdalenost s tebou jednat nebude");
				return true;
			}
			if (prodejce.Realm == player.Realm) {
				player.Dialog(player, SingletonScript<D_Prodejce>.Instance, new DialogArgs(prodejce));
			} else if (!player.IsGM) {
				prodejce.Say("Nech me byt !");
			}
			if (player.IsGM) {
				ShopStatistics.Show(player, prodejce, WareNames);
			}
			return true;
		}

		/// <summary>Button 13: pays, counts the sale, hands out what was ordered.</summary>
		internal static void Deliver(NPC prodejce, Player player, int[] counts) {
			int total = Total(counts);
			if (!GoldUtils.Pay(player, total)) {
				return;
			}
			ShopStatistics.Record(prodejce, player, counts, total);
			for (int i = 0; i < Wares.Length; i++) {
				if (counts[i] > 0) {
					Hand(player, Wares[i], counts[i]);
				}
			}
		}

		internal static int Total(int[] counts) {
			int total = 0;
			for (int i = 0; i < Wares.Length; i++) {
				total += counts[i] * Wares[i].Price;
			}
			return total;
		}

		/// <summary>Coal and thread as one stack; crystals and bowstrings one by one, more than five in a bag, Zbozi.</summary>
		private static void Hand(Player player, AlchemistWare ware, int count) {
			ItemDef def = ThingDef.GetByDefname(ware.Defname) as ItemDef;
			if (def == null) {
				return;
			}
			Container into = player.Backpack;
			if (ware.Stacks) {
				((Item) def.Create(into)).Amount = count;
				return;
			}
			if (count > 5) {
				Container bag = (Container) ((ItemDef) ThingDef.GetByDefname("i_bag")).Create(into);
				bag.Name = "Zbozi";
				into = bag;
			}
			for (int i = 0; i < count; i++) {
				def.Create(into);
			}
		}

		internal static int ParseCount(string text, int ware) {
			if (!int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)) {
				return 0;
			}
			return Math.Max(0, Math.Min(MaxOf(ware), count));
		}
	}

	/// <summary>[speech spk_drazitel]: a greeting or a word of buying, within 3 tiles.</summary>
	public sealed class ProdejceSpeech : AbstractSpeech {
		private static readonly string[] words = { "hi", "dobry", "ahoj", "zdravi", "buy", "koupit", "nakup" };

		public new static void Bootstrap() {
			new ProdejceSpeech("spk_drazitel").Register();
		}

		private ProdejceSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC prodejce = listener as NPC;
			Character speaker = speechArgs.Speaker as Character;
			string speech = speechArgs.Speech ?? "";
			if ((prodejce == null) || (speaker == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			foreach (string word in words) {
				if (speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
					return ProdejceShop.TryOpen(prodejce, speaker) ? SpeechResult.ActedUponExclusively : SpeechResult.IgnoredOrActedUpon;
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>i_drazitel_memory: an order that's waiting.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ProdejceOrderTimer : BoundTimer {
		[SaveableData, CopyableData]
		public NPC prodejce;

		[SaveableData, CopyableData]
		public int[] counts;

		[DeepCopyImplementation, LoadingInitializer]
		public ProdejceOrderTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}
}
