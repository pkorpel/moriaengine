using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>One of the alchemist's eight wares.</summary>
	internal sealed class AlchemistWare {
		internal readonly string Name, Defname;
		internal readonly int Price;
		internal readonly bool Stacks;

		internal AlchemistWare(string name, string defname, int price, bool stacks) {
			this.Name = name;
			this.Defname = defname;
			this.Price = price;
			this.Stacks = stacks;
		}
	}

	/// <summary>#1017 - Erebor's systems/npc_Alchymista.scp: c_alchymista, the Nejvyssi Akolyta, sells the
	/// life candle and other artefacts to his own realm. An order is held 10 minutes, as i_alchymista_memory.</summary>
	public static class AlchemistShop {
		internal const string NpcDef = "c_alchymista";
		internal const int MaxEach = 100, ReachDistance = 3;
		internal static readonly TimerKey OrderTK = TimerKey.Acquire("_alchymistaOrder_");
		private static readonly TagKey metTK = TagKey.Acquire("_alchymistaMet_");

		internal static readonly AlchemistWare[] Wares = {
			new AlchemistWare("Elixir poznani", "i_empathy_bottle", 5000, true),
			new AlchemistWare("Krystal videni", "i_empathy_item", 10000, false),
			new AlchemistWare("Elixir vnimani", "i_potion_HPsightElixir", 24000, true),
			new AlchemistWare("Silny odvar vnimani", "i_potion_HPsightGreat", 13500, true),
			new AlchemistWare("Lektvar vnimani", "i_potion_HPsight", 7125, true),
			new AlchemistWare("Slaby odvar vnimani", "i_potion_HPsightLess", 3750, true),
			new AlchemistWare("Svice zivota", "i_svice_zivota", 30000, false),
			new AlchemistWare("Schranka na duse", "i_soul_case", 12000, true),
		};

		internal static string[] WareNames {
			get { return Array.ConvertAll(Wares, ware => ware.Name); }
		}

		internal static bool IsAlchemist(Character ch) {
			return (ch is NPC) && (ch.Def == ThingDef.GetByDefname(NpcDef));
		}

		internal static bool HaveMet(Player player, NPC alchemist) {
			return Equals(player.GetTag(metTK), alchemist.Uid);
		}

		internal static void Meet(Player player, NPC alchemist) {
			player.SetTag(metTK, alchemist.Uid);
		}

		/// <summary>f_npc_alchymista: his own realm gets the shop, everyone else is sent away. Within reach,
		/// as canReachNPC, 3 tiles.</summary>
		internal static bool TryOpen(NPC alchemist, Character from) {
			Player player = from as Player;
			if ((player == null) || !IsAlchemist(alchemist)) {
				return false;
			}
			if (!player.IsGM) { //canReachNPC
				if (player.Flag_Dead) {
					return true;
				}
				if (Point2D.GetSimpleDistance(alchemist, player) > ReachDistance) {
					player.SysMessage(alchemist.Name + " je prilis daleko.");
					return true;
				}
			}
			if (alchemist.Realm == player.Realm) {
				player.Dialog(player, SingletonScript<D_Alchemist>.Instance, new DialogArgs(alchemist));
			} else if (!player.IsGM) {
				alchemist.Say("Nech me byt !");
			}
			if (player.IsGM) {
				ShopStatistics.Show(player, alchemist, WareNames); //#1070 - d_npc_alchymista_statistika
			}
			return true;
		}

		/// <summary>The button "ON=13": pays, then hands out what was ordered.</summary>
		internal static void Deliver(NPC alchemist, Player player, int[] counts) {
			int total = Total(counts);
			if (!GoldUtils.Pay(player, total)) {
				return;
			}
			ShopStatistics.Record(alchemist, player, counts, total); //#1070 - f_npc_drazitel_vybrano
			for (int i = 0; i < Wares.Length; i++) {
				if (counts[i] > 0) {
					Hand(player, Wares[i], counts[i]);
				}
			}
			alchemist.Say("Bylo mi potesenim");
		}

		internal static int Total(int[] counts) {
			int total = 0;
			for (int i = 0; i < Wares.Length; i++) {
				total += counts[i] * Wares[i].Price;
			}
			return total;
		}

		/// <summary>f_alchymista_vydej_bagl: a stack at once; more than five single things come in a bag of
		/// their own, Zbozi.</summary>
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

		/// <summary>f_alchymista_check_value: a number from 0 to the most of one ware.</summary>
		internal static int ParseCount(string text) {
			int count;
			if (!int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) {
				return 0;
			}
			return Math.Max(0, Math.Min(MaxEach, count));
		}
	}

	/// <summary>[speech spk_alchymista]: a greeting or a word of buying opens the shop.</summary>
	public sealed class AlchemistSpeech : AbstractSpeech {
		private static readonly string[] words = { "hi", "dobry", "ahoj", "zdravi", "buy", "koupit", "nakup" };

		public new static void Bootstrap() {
			new AlchemistSpeech("spk_alchymista").Register();
		}

		private AlchemistSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC alchemist = listener as NPC;
			Character speaker = speechArgs.Speaker as Character;
			string speech = speechArgs.Speech ?? "";
			if ((alchemist == null) || (speaker == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			foreach (string word in words) {
				if (speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
					return AlchemistShop.TryOpen(alchemist, speaker) ? SpeechResult.ActedUponExclusively : SpeechResult.IgnoredOrActedUpon;
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>i_alchymista_memory: an order that's waiting.</summary>
	[SaveableClass, DeepCopyableClass]
	public class AlchemistOrderTimer : BoundTimer {
		[SaveableData, CopyableData]
		public NPC alchemist;

		[SaveableData, CopyableData]
		public int[] counts;

		[DeepCopyImplementation, LoadingInitializer]
		public AlchemistOrderTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}
}
