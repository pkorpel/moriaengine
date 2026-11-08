using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1286 - fire in the city (design section 6): fires start at a house (a marked venue, or a spot near the centre) and
	/// spread. Players beat them out by double click, at a cost in stamina. All out in time: the helpers are paid, and the people of
	/// that house remember them. Too many fires, or time out: the house burns down.</summary>
	public sealed class FireKind : WorldEventKind {
		public const string FireDef = "i_event_fire";

		public override string Key { get { return "pozar"; } }
		public override string Name { get { return "Pozar ve meste"; } }

		public override bool IsFor(string city) {
			return Array.Exists(WorldEvents.Cities, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
		}

		public override WorldEvent Make(string city) {
			return new Fire(this, city);
		}
	}

	public sealed class Fire : WorldEvent {
		internal static readonly TagKey BeatsTK = TagKey.Acquire("_wev_fire_beats_");
		internal const int SpotSteps = 12, MinFromCentre = 4;

		/// <summary>The house on fire, when the city has marked venues; its people remember who saved it.</summary>
		public CityVenue House;
		private int[] spot;
		private byte plane;
		internal double nextSpread;
		public readonly Dictionary<Player, int> Helpers = new Dictionary<Player, int>();

		internal Fire(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		public string Where {
			get { return (this.House == null) ? this.CityName : (this.House.Title + " v " + this.CityName); }
		}

		internal override bool Begin(int playersNear) {
			this.plane = this.Centre.M;
			List<CityVenue> venues = VenueRegistry.InCity(this.City);
			if (venues.Count > 0) {
				this.House = venues[Globals.dice.Next(venues.Count)];
				this.spot = ResidentSchedule.SpotIn(this.House, this.Id); //a standable spot inside the house
				this.plane = this.House.M;
			}
			Map map = Map.GetMap(this.plane);
			if (this.spot == null) {
				this.House = null;
				this.plane = this.Centre.M;
				map = Map.GetMap(this.plane);
				List<int[]> near = WorldEvents.Reachable(map, new[] { this.Centre.X, this.Centre.Y, this.Centre.Z }, SpotSteps);
				near.RemoveAll(t => WorldSpots.Distance(t, this.Centre.X, this.Centre.Y) < MinFromCentre);
				if (near.Count == 0) {
					return false;
				}
				this.spot = near[Globals.dice.Next(near.Count)];
			}
			for (int i = 0; i < Settings.fireStart; i++) {
				this.Spread();
			}
			if (this.Items.Count == 0) {
				return false;
			}
			this.EndsAt = this.StartedAt + (Settings.fireMinutes * 60.0);
			this.nextSpread = this.StartedAt + Settings.fireSpreadSeconds;
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage("Hori " + this.Where + "! Pomozte hasit!");
			}
			CityNews.Add(this.City, "Hori " + this.Where + ". Kazda ruka u ohne je dobra.");
			return true;
		}

		/// <summary>The fires still burning.</summary>
		public List<Item> Burning {
			get { return this.Items.FindAll(f => !f.IsDeleted); }
		}

		/// <summary>A new fire beside one that burns (the first by the house), where none burns yet.</summary>
		internal bool Spread() {
			List<Item> burning = this.Burning;
			Map map = Map.GetMap(this.plane);
			int[] from = this.spot;
			if (burning.Count > 0) {
				Item some = burning[Globals.dice.Next(burning.Count)];
				from = WorldSpots.NearestStandable(map, some.X, some.Y, 1) ?? this.spot;
			}
			List<int[]> around = WorldEvents.Reachable(map, from, 2);
			around.RemoveAll(t => burning.Exists(f => (f.X == t[0]) && (f.Y == t[1])));
			if (around.Count == 0) {
				return false;
			}
			int[] at = around[Globals.dice.Next(around.Count)];
			Item fire = this.Place(FireKind.FireDef, at, this.plane);
			if (fire != null) {
				fire.SetTag(BeatsTK, 0);
			}
			return fire != null;
		}

		/// <summary>A player beats at a fire: stamina for a blow, and enough blows put it out.</summary>
		internal bool Beat(Item fire, Player player) {
			if (player.Stam < Settings.fireBeatStamina) {
				player.SysMessage("Jsi prilis unaveny, abys hasil.");
				return false;
			}
			player.Stam = (short) (player.Stam - Settings.fireBeatStamina);
			int beats = Convert.ToInt32(fire.GetTag(BeatsTK), CultureInfo.InvariantCulture) + 1;
			if (WorldEvents.IsOnCitysSide(player, this.City)) {
				this.Helpers.TryGetValue(player, out int done);
				this.Helpers[player] = done + 1;
			}
			if (beats >= Settings.fireBeats) {
				fire.Delete();
				player.SysMessage("Uhasil jsi ohen.");
			} else {
				fire.SetTag(BeatsTK, beats);
				player.SysMessage("Mlatis do plamenu...");
			}
			return true;
		}

		internal override void Tick() {
			int burning = this.Burning.Count;
			if (burning == 0) {
				this.Win();
			} else if ((burning >= Settings.fireMax) || (Globals.TimeInSeconds >= this.EndsAt)) {
				this.Lose();
			} else if (Globals.TimeInSeconds >= this.nextSpread) {
				this.nextSpread = Globals.TimeInSeconds + Settings.fireSpreadSeconds;
				this.Spread();
			}
		}

		private void Win() {
			this.End(true);
			foreach (Player helper in this.Helpers.Keys) {
				WorldEvents.Reward(helper, this.City, Settings.fireGold, Settings.fireExp, Settings.fireFavour);
				if ((this.House != null) && !helper.IsDeleted) {
					foreach (Item gem in Residents.OfVenue(this.House.VenueId)) {
						Residents.AddFriendship(helper, Residents.IdOf(gem), Settings.fireFriendship);
					}
				}
			}
			WorldEvents.SetOutcome(this.City, 0, 0, "Pozar " + ((this.House == null) ? "v " + this.CityName : this.Where) + " se podarilo uhasit.");
		}

		private void Lose() {
			this.End(false);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage("Ohen nikdo nezastavil. " + this.Where + " lehl popelem.");
			}
			WorldEvents.SetOutcome(this.City, 0, 0, "Pozar " + ((this.House == null) ? "v " + this.CityName : this.Where) + " nikdo neuhasil. Zbyla jen spaleniste.");
		}

		protected override bool IsFoe(NPC npc) {
			return false;
		}

		public override string Describe() {
			return "Hori " + this.Where + "! Ohnu: " + this.Burning.Count + ".";
		}
	}

	/// <summary>#1286 - a fire's double click: a blow at it.</summary>
	public class E_EventFire : CompiledTriggerGroup {
		internal const int Reach = 2;

		public TriggerResult On_DClick(Item self, Character clicker) {
			if ((clicker is Player player) && (WorldEvents.EventOf(self) is Fire fire) && !fire.Ended) {
				if (player.Flag_Dead) {
					player.SysMessage("Duch ohen neuhasi.");
				} else if (Point2D.GetSimpleDistance(self, player) > Reach) {
					player.SysMessage("Jsi od ohne moc daleko.");
				} else {
					fire.Beat(self, player);
				}
			}
			return TriggerResult.Cancel;
		}
	}
}
