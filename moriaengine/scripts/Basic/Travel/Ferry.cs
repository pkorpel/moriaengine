using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1307 - one end of the ferry to Ostrov nestalosti, the island of q041's smuggler and q099's Podivin: where its ferryman
	/// stands and where a passenger steps ashore.</summary>
	public sealed class FerryStop {
		public readonly string Key, Name;
		private readonly string area;
		private readonly int dx, dy;

		internal FerryStop(string key, string name, string area, int dx, int dy) {
			this.Key = key;
			this.Name = name;
			this.area = area;
			this.dx = dx;
			this.dy = dy;
		}

		/// <summary>The ferryman's tile: beside the area's spawn point (the island's: the smuggler's shore).</summary>
		public int[] Spot {
			get {
				StaticRegion region = StaticRegion.GetByDefname(this.area);
				return (region == null) ? null : WorldSpots.NearestStandable(Map.GetMap(region.P.M), region.P.X + this.dx, region.P.Y + this.dy, 6);
			}
		}

		public byte M {
			get {
				StaticRegion region = StaticRegion.GetByDefname(this.area);
				return (region == null) ? (byte) 0 : region.P.M;
			}
		}

		/// <summary>Where a passenger steps ashore: beside the ferryman, not on his tile.</summary>
		public Point4D Landing {
			get {
				int[] spot = this.Spot;
				if (spot == null) {
					return null;
				}
				int[] at = WorldSpots.NearestStandable(Map.GetMap(this.M), spot[0] + 1, spot[1] + 1, 3);
				return (at == null) ? null : new Point4D(at[0], at[1], at[2], this.M);
			}
		}

		public bool IsIsland {
			get { return this.Key == Ferry.Island; }
		}
	}

	/// <summary>#1307 - the ferry, the user's answer to how players reach Ostrov nestalosti: a ferryman in Dol Amroth (Gondor's
	/// side) and one in Umbar (Mordor's) take a passenger to the island, the island's ferryman to either harbour; the fare
	/// (TravelSettings.ferryPrice) through GoldUtils.Pay, what follows the passenger comes along, as on the travel stones (TravelRide.Carry).</summary>
	public static class Ferry {
		public const string Island = "ostrov", FerrymanDef = "c_ferryman";

		public static readonly FerryStop[] All = {
			new FerryStop(Island, "Ostrov nestalosti", "a_Ostrov_nestalosti", -5, 12),
			new FerryStop("dol_amroth", "Dol Amroth", "a_Dol_Amroth", 4, 2),
			new FerryStop("umbar", "Umbar", "a_Umbar", -4, 2)
		};

		public static FerryStop Get(string key) {
			return Array.Find(All, s => s.Key == key);
		}

		/// <summary>The stop a ferryman serves: the one whose tile is nearest him.</summary>
		public static FerryStop StopOf(NPC ferryman) {
			FerryStop best = null;
			int bestDistance = int.MaxValue;
			foreach (FerryStop stop in All) {
				int[] spot = stop.Spot;
				if ((spot != null) && (stop.M == ferryman.M)) {
					int distance = Math.Max(Math.Abs(spot[0] - ferryman.X), Math.Abs(spot[1] - ferryman.Y));
					if (distance < bestDistance) {
						best = stop;
						bestDistance = distance;
					}
				}
			}
			return best;
		}

		/// <summary>Where a ferryman goes: from the island to either harbour, from a harbour to the island.</summary>
		public static List<FerryStop> DestinationsFrom(FerryStop here) {
			List<FerryStop> list = new List<FerryStop>();
			foreach (FerryStop stop in All) {
				if ((here != null) && (stop != here) && (stop.IsIsland != here.IsIsland)) {
					list.Add(stop);
				}
			}
			return list;
		}

		/// <summary>The ride: the fare paid, the passenger and what follows them ashore at `to`. False when they didn't go.</summary>
		public static bool Take(Player player, NPC ferryman, FerryStop to) {
			FerryStop here = StopOf(ferryman);
			Point4D landing = (to == null) ? null : to.Landing;
			if ((here == null) || !DestinationsFrom(here).Contains(to) || (landing == null)) {
				ferryman.Say("Tam neplujeme.");
				return false;
			}
			int fare = Math.Max(0, TravelSettings.instance.ferryPrice);
			if (!GoldUtils.Pay(player, fare, PaySource.Usual, "")) {
				ferryman.Say("Plavba stoji " + fare + " zlatych.");
				return false;
			}
			ferryman.Say("Nalodit! Plujeme do: " + to.Name + ".");
			TravelRide.Carry(player, landing);
			player.SysMessage("Po dlouhe plavbe vystupujes na breh: " + to.Name + ".");
			return true;
		}
	}

	/// <summary>Attaches the ferryman's double click to his def.</summary>
	public static class FerryInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(Ferry.FerrymanDef, SingletonScript<E_Ferryman>.Instance);
		}
	}

	/// <summary>#1307 - the ferryman's double click: his destinations and the fare.</summary>
	public class E_Ferryman : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if ((clicker is Player player) && QuestNpc.IsCloseEnough(self, player)) {
				D_Ferry dialog = SingletonScript<D_Ferry>.Instance;
				player.DialogClose(dialog, 0);
				player.Dialog(player, dialog, new DialogArgs(self));
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1307 - the ferryman's destinations: a button each. args: the ferryman.</summary>
	public class D_Ferry : CompiledGumpDef {
		internal const int FirstStop = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC ferryman = (NPC) args[0];
			List<FerryStop> to = Ferry.DestinationsFrom(Ferry.StopOf(ferryman));
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 320, 90 + (to.Count * 24), 2620);
			this.AddLabel(16, 12, 95, "Prevoznik: kam to bude?");
			int y = 44;
			foreach (FerryStop stop in to) {
				this.AddButton(16, y, 0xFA5, 0xFA7, FirstStop + Array.IndexOf(Ferry.All, stop), GumpButtonType.Reply, 0);
				this.AddLabel(52, y + 2, 68, stop.Name + " (" + Math.Max(0, TravelSettings.instance.ferryPrice) + " zlatych)");
				y += 24;
			}
			this.AddButton(16, y, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, 955, "Nikam.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC ferryman = args[0] as NPC;
			int at = gr.PressedButton - FirstStop;
			if ((player == null) || (ferryman == null) || ferryman.IsDeleted || (at < 0) || (at >= Ferry.All.Length) || !QuestNpc.IsCloseEnough(ferryman, player)) {
				return;
			}
			Ferry.Take(player, ferryman, Ferry.All[at]);
		}
	}
}
