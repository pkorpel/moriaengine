using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

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

		/// <summary>This stop's ferryman, if one stands within Ferry.StopRange of its spot (a GM may have moved him), or null.</summary>
		public NPC Ferryman {
			get {
				int[] spot = this.Spot;
				if (spot == null) {
					return null;
				}
				foreach (AbstractCharacter ch in Map.GetMap(this.M).GetCharsInRange(spot[0], spot[1], Ferry.StopRange)) {
					if ((ch is NPC npc) && !npc.IsDeleted && (npc.Def == ThingDef.GetByDefname(Ferry.FerrymanDef))) {
						return npc;
					}
				}
				return null;
			}
		}

		/// <summary>Where a passenger steps ashore: beside the stop's ferryman where he stands (else beside his spot), never on
		/// his tile.</summary>
		public Point4D Landing {
			get {
				NPC ferryman = this.Ferryman;
				int[] spot = (ferryman != null) ? new[] { (int) ferryman.X, (int) ferryman.Y, (int) ferryman.Z } : this.Spot;
				if (spot == null) {
					return null;
				}
				Map map = Map.GetMap(this.M);
				foreach (int[] offset in new[] { new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 }, new[] { 2, 0 }, new[] { 0, 2 } }) {
					int[] at = WorldSpots.NearestStandable(map, spot[0] + offset[0], spot[1] + offset[1], 2);
					if ((at != null) && ((at[0] != spot[0]) || (at[1] != spot[1]))) {
						return new Point4D(at[0], at[1], at[2], this.M);
					}
				}
				return null;
			}
		}

		/// <summary>The realm tag where the stop lands (Dol Amroth 1, Umbar 2, the island 2).</summary>
		public int RealmTag {
			get {
				Point4D landing = this.Landing;
				return (landing == null) ? 0 : TravelStops.RealmTagAt(landing);
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
		public const int StopRange = 20;

		public static readonly FerryStop[] All = {
			new FerryStop(Island, "Ostrov nestalosti", "a_Ostrov_nestalosti", -5, 12),
			new FerryStop("dol_amroth", "Dol Amroth", "a_Dol_Amroth", 4, 2),
			new FerryStop("umbar", "Umbar", "a_Umbar", -4, 2)
		};

		public static FerryStop Get(string key) {
			return Array.Find(All, s => s.Key == key);
		}

		/// <summary>The stop a ferryman serves: the one whose tile is nearest him, within StopRange; else none (a stray ferryman a
		/// GM made elsewhere is no ferry).</summary>
		public static FerryStop StopOf(NPC ferryman) {
			FerryStop best = null;
			int bestDistance = StopRange + 1;
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

		/// <summary>Where a ferryman takes this passenger: from a harbour, the island (Gondor's heroes too: q041's smuggler is there);
		/// from the island, the harbours not on the enemy's land, as the travel stones' rule. An outcast belongs to the side they came
		/// from (q099's way back is on the island): a Mordor outcast to Umbar, everyone else not of Mordor to Dol Amroth.</summary>
		public static List<FerryStop> DestinationsFrom(FerryStop here, Character passenger) {
			List<FerryStop> list = new List<FerryStop>();
			foreach (FerryStop stop in All) {
				if ((here != null) && (stop != here) && (stop.IsIsland != here.IsIsland) && (stop.IsIsland || !IsEnemyHarbour(passenger, stop))) {
					list.Add(stop);
				}
			}
			return list;
		}

		private static bool IsEnemyHarbour(Character passenger, FerryStop harbour) {
			if ((passenger == null) || passenger.IsGM) {
				return false;
			}
			bool mordorSide = (passenger.Realm == Realm.Mordor) || (passenger.Realm == Realm.MordorOutcast);
			return mordorSide ? (harbour.RealmTag == 1) : (harbour.RealmTag == 2);
		}

		/// <summary>Why the passenger can't take the boat now, told to them, or null: a ghost, the jailed or a camp's convict, a
		/// possessing mystic, the voodoo's no-port mark.</summary>
		internal static string Refusal(Player player) {
			if (player.Flag_Dead) {
				return "Duchy neprevazim.";
			}
			if (Jail.IsJailed(player) || Camp.IsInCamp(player)) {
				return "Tebe nikam nevezu.";
			}
			return null;
		}

		/// <summary>The boat booked: the fare paid, then (a GM at once) a count of rideSeconds by the ferryman, as the travel stones'
		/// coach; whoever strays more than strayTiles from him by then is left behind, the fare with the ferryman. False when nothing
		/// was booked.</summary>
		public static bool Take(Player player, NPC ferryman, FerryStop to) {
			string refusal = Refusal(player);
			if (refusal != null) {
				ferryman.Say(refusal);
				return false;
			}
			if (MysticHypnosis.RefusesMenu(player) || ShamanVoodoo.RefusesRide(player)) {
				return false;
			}
			FerryStop here = StopOf(ferryman);
			if ((here == null) || !DestinationsFrom(here, player).Contains(to) || (to.Landing == null)) {
				ferryman.Say("Tam neplujeme.");
				return false;
			}
			int fare = Math.Max(0, TravelSettings.instance.ferryPrice);
			player.DeleteTimer(FerryRide.Key);
			if (!GoldUtils.Pay(player, fare, PaySource.Usual, "")) {
				ferryman.Say("Plavba stoji " + fare + " zlatych.");
				return false;
			}
			ferryman.Say("Nalodit! Plujeme do: " + to.Name + ".");
			if (player.IsGM) {
				Sail(player, to);
			} else {
				player.AddTimer(FerryRide.Key, new FerryRide(ferryman, to));
			}
			return true;
		}

		/// <summary>The crossing: ashore at `to` with what follows.</summary>
		internal static void Sail(Character passenger, FerryStop to) {
			Point4D landing = to.Landing;
			if (landing == null) {
				passenger.RedMessage("Lod dnes nepluje, kontaktuj GM.");
				return;
			}
			TravelRide.Carry(passenger, landing);
			passenger.SysMessage("Po dlouhe plavbe vystupujes na breh: " + to.Name + ".");
		}
	}

	/// <summary>#1307 - the boat's count by the ferryman, as the travel stones' TravelRide: stray too far by its end and the boat sails
	/// without you, the fare stays with the ferryman.</summary>
	[SaveableClass, DeepCopyableClass]
	public class FerryRide : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_ferryRide_");

		[SaveableData, CopyableData]
		public int fromX, fromY, countLeft;

		[SaveableData, CopyableData]
		public byte fromM;

		[SaveableData, CopyableData]
		public string toKey = "";

		[DeepCopyImplementation, LoadingInitializer]
		public FerryRide() {
		}

		internal FerryRide(NPC ferryman, FerryStop to) {
			this.fromX = ferryman.X;
			this.fromY = ferryman.Y;
			this.fromM = ferryman.M;
			this.toKey = to.Key;
			this.countLeft = TravelSettings.instance.rideSeconds;
			this.DueInSeconds = 1;
			this.PeriodInSeconds = 1;
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = cont as Character;
			if (self == null) {
				return;
			}
			if (this.countLeft > 0) {
				self.Message(TravelStops.Count(this.countLeft));
				this.countLeft--;
				return;
			}
			this.Complete(self);
		}

		/// <summary>The count's end (a test's shortcut too): aboard if near enough, alive and free, else left behind.</summary>
		internal void Complete(Character self) {
			this.Delete();
			FerryStop to = Ferry.Get(this.toKey);
			if ((to == null) || self.Flag_Dead || ((self is Player player) && (Ferry.Refusal(player) != null))) {
				return;
			}
			if ((self.M != this.fromM) || (Point2D.GetSimpleDistance(self, new Point2D(this.fromX, this.fromY)) > TravelSettings.instance.strayTiles)) {
				self.RedMessage("Lod odplula bez tebe!");
				return;
			}
			Ferry.Sail(self, to);
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
			if ((clicker is Player player) && QuestNpc.IsCloseEnough(self, player) && !MysticHypnosis.RefusesMenu(player)) {
				string refusal = Ferry.Refusal(player);
				if (refusal != null) {
					self.Say(refusal);
					return TriggerResult.Cancel;
				}
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
			List<FerryStop> to = Ferry.DestinationsFrom(Ferry.StopOf(ferryman), sendTo as Character);
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
