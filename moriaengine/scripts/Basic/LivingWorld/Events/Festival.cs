using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1289 - a festival by season (design section 6): it starts by the real calendar (UTC) in its places, once a day per
	/// place. A host at the centre gives each guest one festival food; a runaway animal flees from players, and whoever catches it
	/// wins a prize, a few rounds; the city's prices are a little lower while it lasts; the guests get a little experience at the
	/// end. No cosmetics (the user's rewards rule).</summary>
	public sealed class FestivalKind : WorldEventKind {
		public const string HostDef = "c_event_festival_host", OrcHostDef = "c_event_festival_host_orc";
		public const string PigDef = "c_event_festival_pig", RatDef = "c_event_festival_rat";

		private readonly string key, name;
		private readonly string[] places;
		private readonly int from, to; //month * 100 + day
		public readonly string FoodDef, AnimalDef, AnimalName;

		public FestivalKind(string key, string name, string[] places, int fromMonth, int fromDay, int toMonth, int toDay, string foodDef, string animalDef, string animalName) {
			this.key = key;
			this.name = name;
			this.places = places;
			this.from = (fromMonth * 100) + fromDay;
			this.to = (toMonth * 100) + toDay;
			this.FoodDef = foodDef;
			this.AnimalDef = animalDef;
			this.AnimalName = animalName;
		}

		private static readonly string[] gondorPilots = { "Minas_Tirith", "Edoras", "Hobitin" };

		public static readonly FestivalKind[] All = {
			new FestivalKind("slunovrat", "Slavnost slunovratu", new[] { "Minas_Tirith", "Edoras" }, 6, 20, 6, 26, "i_cake", PigDef, "selatko"),
			new FestivalKind("dozinky", "Dozinky", new[] { "Edoras", "Hobitin" }, 9, 15, 9, 30, "i_bread_loaf", PigDef, "selatko"),
			new FestivalKind("oslava", "Hobiti oslava", new[] { "Hobitin" }, 9, 20, 9, 24, "i_cake", PigDef, "selatko"),
			new FestivalKind("yule", "Svatek Yule", gondorPilots, 12, 20, 1, 2, "i_mug_ale", PigDef, "selatko"),
			new FestivalKind("den_oka", "Den Oka", new[] { "Barad-dur", "Harad", "Umbar" }, 3, 20, 3, 26, "i_ham", RatDef, "uprchly krysak")
		};

		public override string Key { get { return this.key; } }
		public override string Name { get { return this.name; } }

		public override IList<string> Places {
			get { return this.places; }
		}

		public override bool StartsByCalendar {
			get { return true; }
		}

		public override bool CountsTowardLimit {
			get { return false; }
		}

		public override bool IsFor(string city) {
			return Array.Exists(this.places, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Its days: a window of the year, which may run over the new year (Yule).</summary>
		public bool InWindow(DateTime day) {
			int md = (day.Month * 100) + day.Day;
			return (this.from <= this.to) ? ((md >= this.from) && (md <= this.to)) : ((md >= this.from) || (md <= this.to));
		}

		public override bool IsTime() {
			return this.InWindow(Errands.Now);
		}

		public override WorldEvent Make(string city) {
			return new Festival(this, city);
		}
	}

	public sealed class Festival : WorldEvent {
		internal const int AnimalSteps = 10, AnimalMinFromCentre = 4, FleeWithin = 3, FleeSteps = 4, CatchReach = 1;
		public NPC Host, Animal;
		public int Rounds;
		public readonly HashSet<Player> Fed = new HashSet<Player>();
		public readonly HashSet<Player> Guests = new HashSet<Player>();
		public readonly Dictionary<Player, int> Catches = new Dictionary<Player, int>();

		internal Festival(FestivalKind kind, string city)
			: base(kind, city) {
		}

		internal FestivalKind Feast {
			get { return (FestivalKind) this.Kind; }
		}

		internal override bool Begin(int playersNear) {
			Map map = Map.GetMap(this.Centre.M);
			int[] at = WorldSpots.NearestStandable(map, this.Centre.X + 2, this.Centre.Y, 3);
			this.Host = this.Spawn((Reputation.RealmOfCity(this.City) == Realm.Mordor) ? FestivalKind.OrcHostDef : FestivalKind.HostDef, at, this.Centre.M, null);
			if (this.Host == null) {
				return false;
			}
			this.Host.Flag_Invulnerable = true;
			NPCAiTimer.Freeze(this.Host, (Settings.festivalMinutes * 60) + 60);
			if (!this.SpawnAnimal()) {
				return false;
			}
			this.EndsAt = this.StartedAt + (Settings.festivalMinutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.SysMessage(this.Kind.Name + " v " + this.CityName + " zacina! Poradatel na namesti rozdava jidlo a kdo chyti " + this.Feast.AnimalName + ", vyhraje.");
			}
			WorldEvents.SetOutcome(this.City, -Settings.festivalDiscount, Settings.festivalMinutes, this.Kind.Name + " v " + this.CityName + ": jidlo pro vsechny, hry na namesti a trh levnejsi.");
			return true;
		}

		/// <summary>A runaway animal somewhere a few steps from the centre.</summary>
		internal bool SpawnAnimal() {
			Map map = Map.GetMap(this.Centre.M);
			List<int[]> tiles = WorldEvents.Reachable(map, new[] { this.Centre.X, this.Centre.Y, this.Centre.Z }, AnimalSteps);
			tiles.RemoveAll(t => WorldSpots.Distance(t, this.Centre.X, this.Centre.Y) < AnimalMinFromCentre);
			if (tiles.Count == 0) {
				return false;
			}
			int[] at = tiles[Globals.dice.Next(tiles.Count)];
			this.Animal = this.Spawn(this.Feast.AnimalDef, at, this.Centre.M, this.Feast.AnimalName);
			Send(this.Animal, at, this.Centre.M, AnimalSteps);
			return this.Animal != null;
		}

		/// <summary>The host's gift: one festival food for each guest.</summary>
		internal bool Feed(Player player) {
			if (!this.Fed.Add(player)) {
				this.Host.Say("Uz jsi svuj podil dostal. Nech neco i ostatnim.");
				return false;
			}
			ThingDef food = ThingDef.GetByDefname(this.Feast.FoodDef);
			if (food != null) {
				food.Create(player.Backpack);
			}
			this.Guests.Add(player);
			this.Host.Say("Vitej na slavnosti! Tohle je pro tebe.");
			return true;
		}

		/// <summary>The animal caught: a prize for the catcher, and another runs.</summary>
		internal void Catch(Player player) {
			this.Rounds++;
			this.Guests.Add(player);
			if (WorldEvents.IsOnCitysSide(player, this.City)) {
				this.Catches.TryGetValue(player, out int caught);
				this.Catches[player] = caught + 1;
				WorldEvents.Reward(player, this.City, Settings.festivalPrizeGold, Settings.festivalPrizeExp, 0);
			}
			this.Animal.Delete();
			this.Animal = null;
			if (this.Rounds < Settings.festivalRounds) {
				this.SpawnAnimal();
			}
		}

		internal override void Tick() {
			if (Globals.TimeInSeconds >= this.EndsAt) {
				this.Finish();
				return;
			}
			if ((this.Animal != null) && (this.Animal.IsDeleted || this.Animal.Flag_Dead)) {
				this.Animal = null; //killed, not caught: no prize, the next one runs
				this.Rounds++;
				if (this.Rounds < Settings.festivalRounds) {
					this.SpawnAnimal();
				}
			}
			if (this.Animal != null) {
				Player nearest = null;
				foreach (AbstractCharacter ch in this.Animal.GetMap().GetPlayersInRange(this.Animal.X, this.Animal.Y, FleeWithin)) {
					if ((ch is Player player) && !player.Flag_Dead && ((nearest == null) || (Point2D.GetSimpleDistance(player, this.Animal) < Point2D.GetSimpleDistance(nearest, this.Animal)))) {
						nearest = player;
					}
				}
				if (nearest != null) {
					NPCAiTimer.GetFor(this.Animal)?.FleeFrom(nearest, FleeSteps);
				}
			}
		}

		private void Finish() {
			this.End(true);
			foreach (Player guest in this.Guests) {
				WorldEvents.Reward(guest, this.City, 0, Settings.festivalExp, Settings.festivalFavour);
			}
			WorldEvents.SetOutcome(this.City, 0, 0, this.Kind.Name + " v " + this.CityName + " skoncila. Hoste se rozchazeji.");
		}

		protected override bool IsFoe(NPC npc) {
			return false;
		}

		public override string Describe() {
			return this.Kind.Name + " v " + this.CityName + ": jidlo u poradatele, chyt " + this.Feast.AnimalName + "!";
		}
	}

	/// <summary>#1289 - the festival host's double click: the guest's festival food.</summary>
	public class E_FestivalHost : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if ((clicker is Player player) && !player.Flag_Dead && (WorldEvents.EventOf(self) is Festival festival) && !festival.Ended
					&& QuestNpc.IsCloseEnough(self, player)) {
				festival.Feed(player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1289 - the runaway animal's double click: caught, if the player is right beside it.</summary>
	public class E_FestivalAnimal : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if ((clicker is Player player) && !player.Flag_Dead && (WorldEvents.EventOf(self) is Festival festival) && !festival.Ended && (festival.Animal == self)) {
				if ((player.M != self.M) || (Point2D.GetSimpleDistance(self, player) > Festival.CatchReach)) {
					player.SysMessage(festival.Feast.AnimalName + " ti uteklo. Musis byt hned u nej.");
				} else {
					festival.Catch(player);
				}
			}
			return TriggerResult.Cancel;
		}
	}
}
