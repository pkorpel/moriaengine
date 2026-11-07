using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - Mordor's fighting pits, the theatre's counterpart: bet on one of two fighters, the fight is rolled round by
	/// round and the pitmaster tells it (design 4.1).</summary>
	public static class Pits {
		private static readonly TagKey hourTK = TagKey.Acquire("_pits_hour_");
		private static readonly TagKey countTK = TagKey.Acquire("_pits_count_");
		private static readonly string[] stockNames = { "Grishnak", "Ugluk" };

		/// <summary>Tests: d6 rolls in this order before any random one.</summary>
		public static readonly Queue<int> RollOverride = new Queue<int>();

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("pits_bet", "Vsadit na zapasnika", new[] { "pits" }, new[] { "pitmaster" }, null, OfferBet));
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		internal static int[] Bets {
			get { return new[] { Settings.pitBetSmall, Settings.pitBetMedium, Settings.pitBetLarge }; }
		}

		/// <summary>The pit's two fighters by name: its residents in the fighter role, or two old hands when it has fewer.</summary>
		public static string[] Fighters(CityVenue venue) {
			List<string> names = new List<string>();
			if (venue != null) {
				foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
					if ((Residents.RoleKeyOf(gem) == "fighter") && (names.Count < 2)) {
						names.Add(Residents.NameOf(gem));
					}
				}
			}
			for (int i = 0; names.Count < 2; i++) {
				names.Add(stockNames[i]);
			}
			return names.ToArray();
		}

		internal static void OfferBet(Player player, NPC npc, Item gem) {
			if (player.IsGM) {
				npc.Say("GM nesazi. Za sazku by neplatil.");
				return;
			}
			D_PitBet dialog = SingletonScript<D_PitBet>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc));
		}

		private static int Roll() {
			return (RollOverride.Count > 0) ? RollOverride.Dequeue() : Globals.dice.Next(1, 7);
		}

		/// <summary>A bet on fighter 0 or 1, from the pack. Each round both roll a d6, the higher hits for the difference (a tie
		/// is a miss); the first to lose 10 falls, or the one hurt more after 12 rounds. A winning bet takes double back and an even
		/// fight returns the stake, so neither fighter is the better bet. False when no bet was placed.</summary>
		public static bool Bet(Player player, NPC pitmaster, int fighter, int bet) {
			if (player.IsGM || (fighter < 0) || (fighter > 1)) {
				return false;
			}
			long now = Errands.Now.Ticks;
			int count = 0;
			if ((player.GetTag(hourTK) is long since) && ((now - since) < TimeSpan.TicksPerHour)) {
				count = (player.GetTag(countTK) is int c) ? c : 0;
			} else {
				player.SetTag(hourTK, now);
				player.SetTag(countTK, 0);
			}
			if (count >= Settings.pitBetsPerHour) {
				pitmaster.Say("Dost sazek. Prijd za hodinu, cerve.");
				return false;
			}
			if (!GoldUtils.Pay(player, bet, PaySource.Pack, "")) {
				pitmaster.Say("Nemas na sazku. Zmiz.");
				return false;
			}
			player.SetTag(countTK, count + 1);
			Item gem = Residents.GemOf(pitmaster);
			string[] names = Fighters((gem == null) ? null : Residents.WorkplaceOf(gem));
			int[] hurt = new int[2];
			int rounds = 0;
			while ((hurt[0] < 10) && (hurt[1] < 10) && (rounds < 12)) {
				rounds++;
				int a = Roll(), b = Roll();
				if (a > b) {
					hurt[1] += a - b;
				} else if (b > a) {
					hurt[0] += b - a;
				}
			}
			pitmaster.Say(names[0] + " a " + names[1] + " se do sebe pustili!");
			if (hurt[0] == hurt[1]) {
				GoldUtils.Grant(player, (uint) bet);
				pitmaster.Say("Po " + rounds + " kolech stoji oba. Nerozhodne, sazka se vraci.");
				return true;
			}
			int winner = (hurt[0] < hurt[1]) ? 0 : 1;
			string said = "Po " + rounds + " kolech pada " + names[1 - winner] + ". Vitez: " + names[winner] + "! ";
			if (winner == fighter) {
				GoldUtils.Grant(player, (uint) (bet * 2));
				said += "Tvoje vyhra, " + (bet * 2) + " zlatych.";
			} else {
				said += "Tvoje zlato je moje.";
			}
			pitmaster.Say(said);
			return true;
		}
	}

	/// <summary>#1256 - the pits' bets: a row of three bets for each fighter; it comes back after each fight. args: the pitmaster.</summary>
	public class D_PitBet : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC pitmaster = (NPC) args[0];
			Item gem = Residents.GemOf(pitmaster);
			string[] names = Pits.Fighters((gem == null) ? null : Residents.WorkplaceOf(gem));
			int[] bets = Pits.Bets;
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 400, 100 + (names.Length * bets.Length * 22), 2620);
			this.AddLabel(16, 12, 95, "Zapasiste: na koho vsadis?");
			int y = 40;
			for (int f = 0; f < names.Length; f++) {
				for (int b = 0; b < bets.Length; b++) {
					this.AddButton(16, y, 0xFA5, 0xFA7, 1 + (f * bets.Length) + b, GumpButtonType.Reply, 0);
					this.AddLabel(52, y + 2, 68, bets[b] + " zlatych na " + names[f]);
					y += 22;
				}
			}
			this.AddButton(16, y + 4, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 6, 955, "Dost.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC pitmaster = args[0] as NPC;
			int[] bets = Pits.Bets;
			int at = gr.PressedButton - 1;
			if ((player == null) || (pitmaster == null) || pitmaster.IsDeleted || (at < 0) || (at >= 2 * bets.Length) || !Errands.CanTalk(player, pitmaster)) {
				return;
			}
			VenueAction action = VenueActions.Get("pits_bet");
			if ((action == null) || !VenueActions.For(player, pitmaster, Residents.GemOf(pitmaster)).Contains(action)) {
				return;
			}
			if (Pits.Bet(player, pitmaster, at / bets.Length, bets[at % bets.Length])) {
				player.Dialog(player, this, new DialogArgs(pitmaster));
			}
		}
	}
}
