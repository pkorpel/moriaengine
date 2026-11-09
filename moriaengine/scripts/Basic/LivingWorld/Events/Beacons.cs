using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1286 - the beacons of Gondor (design section 6): a chain of beacons out from Minas Tirith towards Edoras, to be lit
	/// in order before time runs out. All lit: the lighters are paid and every Gondor player gets an experience bonus for a while.</summary>
	public sealed class BeaconsKind : WorldEventKind {
		public const string BeaconDef = "i_event_beacon", City = "Minas_Tirith";
		private static readonly string[] places = { City };

		public override string Key { get { return "majaky"; } }
		public override string Name { get { return "Majaky Gondoru"; } }

		public override IList<string> Places {
			get { return places; }
		}

		public override bool IsFor(string city) {
			return string.Equals(city, City, StringComparison.OrdinalIgnoreCase);
		}

		public override string Description {
			get {
				WorldEventSettings s = WorldEventSettings.instance;
				return "Kdy: kdykoli.<br>"
					+ "Co se deje: " + s.beaconCount + " majaku stoji v rade od Minas Tirith smerem k Edorasu, asi kazdych " + s.beaconSpacing
					+ " poli. Gondorsky chat vola po jejich zapaleni. Zapaluji se dvojklikem do 2 poli, ale jen po poradi. Duch ani nekdo z nepratelske rise "
					+ "je nezapali, GM ano (bez odmeny).<br>"
					+ "Vyhra: vsechny hori do " + EventText.Time(s.beaconMinutes) + ". Kazdy zapalovac na strane mesta dostane " + s.beaconGold + " zl, "
					+ s.beaconExp + " zkusenosti a " + s.beaconFavour + " povesti. Vsichni hraci Gondoru maji " + EventText.Pct(s.beaconBonusPercent)
					+ " zkusenosti navic na " + EventText.Time(s.beaconBonusMinutes) + " (ne kdyz zapalovali jen GM).<br>"
					+ "Prohra: cas vyprsi, majaky zmizi.";
			}
		}

		public override WorldEvent Make(string city) {
			return new Beacons(this, city);
		}
	}

	public sealed class Beacons : WorldEvent {
		public const int UnlitModel = 0x0DE1, LitModel = 0x0DE3; //kindling, a campfire
		internal static readonly TagKey IndexTK = TagKey.Acquire("_wev_beacon_");
		public int Lit;
		public readonly HashSet<Player> Lighters = new HashSet<Player>();

		internal Beacons(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		/// <summary>#1307 - a beacon gone between the last Tick and a save: the chain can't be lit in full any more.</summary>
		private bool missingBeacon;

		internal override void SaveTo(WorldEventRecord r) {
			r.SetInt("beacons", this.Items.Count);
			r.SetInt("lit", this.Lit);
			r.SetThings("lighters", this.Lighters);
		}

		internal override void LoadFrom(WorldEventRecord r) {
			this.Lit = r.GetInt("lit");
			this.missingBeacon = this.Items.Count < r.GetInt("beacons", this.Items.Count);
			this.Lighters.UnionWith(r.GetThings<Player>("lighters"));
		}

		/// <summary>The way the chain runs: towards Edoras where both are on one map, else west.</summary>
		private void Direction(out double dx, out double dy) {
			dx = -1;
			dy = 0;
			Point4D edoras = WorldEvents.CentreOf("Edoras");
			if ((edoras != null) && (edoras.M == this.Centre.M)) {
				double ex = edoras.X - this.Centre.X, ey = edoras.Y - this.Centre.Y;
				double length = Math.Sqrt((ex * ex) + (ey * ey));
				if (length > 0) {
					dx = ex / length;
					dy = ey / length;
				}
			}
		}

		internal override bool Begin(int playersNear) {
			Map map = Map.GetMap(this.Centre.M);
			this.Direction(out double dx, out double dy);
			int[] previous = { this.Centre.X, this.Centre.Y, this.Centre.Z };
			for (int i = 0; i < Settings.beaconCount; i++) {
				int distance = Settings.beaconSpacing * (i + 1);
				int x = this.Centre.X + (int) Math.Round(dx * distance), y = this.Centre.Y + (int) Math.Round(dy * distance);
				//the tile nearest its point on the line that a walker gets to from the beacon before (no ledge, island or closed
				//house), at least half a spacing on: the chain bends round walls and hills but goes on towards Edoras
				int[] at = null;
				foreach (int[] tile in WorldEvents.Reachable(map, previous, 2 * Settings.beaconSpacing)) {
					if ((WorldSpots.Distance(tile, previous[0], previous[1]) >= Settings.beaconSpacing / 2)
							&& ((at == null) || (WorldSpots.Distance(tile, x, y) < WorldSpots.Distance(at, x, y)))) {
						at = tile;
					}
				}
				Item beacon = this.Place(BeaconsKind.BeaconDef, at, this.Centre.M);
				if (beacon == null) {
					return false; //a beacon with nowhere to stand: no chain
				}
				beacon.Model = UnlitModel;
				beacon.Name = "Majak " + (i + 1).ToString(CultureInfo.InvariantCulture);
				beacon.SetTag(IndexTK, i);
				previous = at;
			}
			this.EndsAt = this.StartedAt + (Settings.beaconMinutes * 60.0);
			ChatSystem.Announce(ChatChannel.Gondor, "Minas Tirith", "Zapalte majaky! Vede od Minas Tirith k Edorasu, " + this.Items.Count
				+ " majaku musi horet po sobe do " + Settings.beaconMinutes + " minut.");
			CityNews.Add(this.City, "Majaky Gondoru je treba zapalit. Prvni stoji za mestem smerem k Edorasu.");
			return true;
		}

		/// <summary>A player lights a beacon: only the next in the chain catches.</summary>
		internal bool Light(Item beacon, Player player) {
			int index = Convert.ToInt32(beacon.GetTag(IndexTK), CultureInfo.InvariantCulture);
			if (index < this.Lit) {
				player.SysMessage("Tento majak uz hori.");
				return false;
			}
			if (index > this.Lit) {
				player.SysMessage("Nejdriv musi horet majak cislo " + (this.Lit + 1) + ".");
				return false;
			}
			beacon.Model = LitModel;
			this.Lit++;
			if (WorldEvents.IsOnCitysSide(player, this.City)) {
				this.Lighters.Add(player);
			}
			ChatSystem.Announce(ChatChannel.Gondor, "Minas Tirith", "Hori majak " + this.Lit + " z " + this.Items.Count + ".");
			return true;
		}

		internal override void Tick() {
			if (this.missingBeacon) {
				this.End(false); //not a win one beacon short
			} else if (this.Lit >= this.Items.Count) {
				this.Win();
			} else if (Globals.TimeInSeconds >= this.EndsAt) {
				this.End(false);
				WorldEvents.SetOutcome(this.City, 0, 0, "Majaky Gondoru nezaplaly vcas. Rohan se o nasi nouzi nedozvedel.");
			}
		}

		private void Win() {
			this.End(true);
			foreach (Player lighter in this.Lighters) {
				WorldEvents.Reward(lighter, this.City, Settings.beaconGold, Settings.beaconExp, Settings.beaconFavour);
			}
			if (this.Lighters.Count == 0) {
				WorldEvents.SetOutcome(this.City, 0, 0, "Majaky Gondoru zaplaly, ale nikdo z Gondoru u nich nebyl.");
				return; //a chain lit by staff alone rouses nobody
			}
			WorldEvents.SetExperienceBonus(Realm.Gondor, Settings.beaconBonusPercent, Settings.beaconBonusMinutes);
			ChatSystem.Announce(ChatChannel.Gondor, "Minas Tirith", "Vsechny majaky hori! Gondor povstava: " + Settings.beaconBonusMinutes
				+ " minut o " + Settings.beaconBonusPercent + "% vic zkusenosti.");
			WorldEvents.SetOutcome(this.City, 0, 0, "Majaky Gondoru zaplaly az k Edorasu. Gondor je pripraven.");
		}

		protected override bool IsFoe(NPC npc) {
			return false;
		}

		public override string Describe() {
			return "Majaky Gondoru: hori " + this.Lit + " z " + this.Items.Count + ".";
		}
	}

	/// <summary>#1286 - a beacon's double click: lights it if it's the next.</summary>
	public class E_EventBeacon : CompiledTriggerGroup {
		internal const int Reach = 2;

		public TriggerResult On_DClick(Item self, Character clicker) {
			if ((clicker is Player player) && (WorldEvents.EventOf(self) is Beacons beacons) && !beacons.Ended) {
				if (player.Flag_Dead) {
					player.SysMessage("Duch majak nezapali.");
				} else if (!player.IsGM && !WorldEvents.IsOnCitysSide(player, beacons.City)) {
					player.SysMessage("Majaky Gondoru zapaluje jen Gondor.");
				} else if (Point2D.GetSimpleDistance(self, player) > Reach) {
					player.SysMessage("Jsi od majaku moc daleko.");
				} else {
					beacons.Light(self, player);
				}
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1286 - attaches the beacons' double click to their def.</summary>
	public static class WorldEventsInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(BeaconsKind.BeaconDef, SingletonScript<E_EventBeacon>.Instance);
			QuestNpc.Attach(FireKind.FireDef, SingletonScript<E_EventFire>.Instance);
			QuestNpc.Attach(FestivalKind.HostDef, SingletonScript<E_FestivalHost>.Instance);
			QuestNpc.Attach(FestivalKind.OrcHostDef, SingletonScript<E_FestivalHost>.Instance);
			QuestNpc.Attach(FestivalKind.PigDef, SingletonScript<E_FestivalAnimal>.Instance);
			QuestNpc.Attach(FestivalKind.RatDef, SingletonScript<E_FestivalAnimal>.Instance);
		}
	}
}
