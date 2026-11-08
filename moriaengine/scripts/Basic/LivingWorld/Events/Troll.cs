using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1286 - the troll at the ford (design section 6): at night a troll stands on a road out of a Gondor pilot city. Those
	/// still fighting it at sunrise see it turn to stone and are paid, and the stone troll stays a while as a landmark. Killed before
	/// dawn counts too. If nobody fought it, it walks off at dawn.</summary>
	public sealed class TrollKind : WorldEventKind {
		public const string TrollDef = "c_event_troll", StoneDef = "c_event_stone_troll";

		public override string Key { get { return "trol"; } }
		public override string Name { get { return "Trol u brodu"; } }

		public override bool IsFor(string city) {
			return Reputation.RealmOfCity(city) == Realm.Gondor;
		}

		public override bool IsTime() {
			WorldEventSettings s = WorldEventSettings.instance;
			return VenueClock.IsOpen(s.trollNightFrom, s.trollNightTo, VenueClock.Hour);
		}

		public override WorldEvent Make(string city) {
			return new Troll(this, city);
		}
	}

	public sealed class Troll : WorldEvent {
		internal const int Leash = 3, MinDistance = 10;
		public NPC Beast;
		private int[] ford;

		/// <summary>The stone troll it left, if it turned.</summary>
		public NPC Stone;

		internal Troll(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		internal override bool Begin(int playersNear) {
			List<int[]> route = WorldEvents.RouteIn(this.Centre, Settings.trollSteps);
			if ((route == null) || (WorldSpots.Distance(route[0], this.Centre.X, this.Centre.Y) < MinDistance)) {
				return false;
			}
			this.ford = route[0];
			this.Beast = this.Spawn(TrollKind.TrollDef, this.ford, this.Centre.M, null);
			if (this.Beast == null) {
				return false;
			}
			Send(this.Beast, this.ford, this.Centre.M, Leash);
			this.EndsAt = this.StartedAt + (Settings.trollMinutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage("Na ceste za " + this.CityName + " se objevil trol! Zdrz ho do usvitu.");
			}
			CityNews.Add(this.City, "U brodu za " + this.CityName + " stoji trol. Kdo ho zdrzi do usvitu, uvidi, jak zkamenel.");
			return true;
		}

		/// <summary>Sunrise: the first hours of the morning on the game clock.</summary>
		public static bool IsDawn() {
			return VenueClock.Shift == VenueShift.Morning;
		}

		internal override void Tick() {
			if (this.Beast.IsDeleted || this.Beast.Flag_Dead) {
				this.End(true);
				foreach (Player hero in this.Heroes.Keys) {
					WorldEvents.Reward(hero, this.City, Settings.trollGold, Settings.trollExp, Settings.trollFavour);
				}
				WorldEvents.SetOutcome(this.City, 0, 0, "Trol u brodu za " + this.CityName + " padl. Cesta je volna.");
			} else if (IsDawn() || (Globals.TimeInSeconds >= this.EndsAt)) {
				this.Dawn();
			} else if ((Point2D.GetSimpleDistance(this.Beast, new Point2D(this.ford[0], this.ford[1])) > 2 * Leash)
					&& (NPCAiTimer.GetFor(this.Beast)?.Target == null)) {
				Send(this.Beast, this.ford, this.Centre.M, Leash); //back to its ford when nobody keeps it busy
			}
		}

		/// <summary>Those fighting it lately (as experience counts them), of the city's side.</summary>
		private List<Player> Fighters() {
			List<Player> fighters = new List<Player>();
			foreach (Character ch in LevelingSystem.FindFighters(this.Beast, null, false)) {
				if ((ch is Player player) && WorldEvents.IsOnCitysSide(player, this.City)) {
					fighters.Add(player);
				}
			}
			return fighters;
		}

		private void Dawn() {
			List<Player> fighters = this.Fighters();
			if (fighters.Count == 0) {
				this.End(false);
				WorldEvents.SetOutcome(this.City, 0, 0, "Trol u brodu za " + this.CityName + " s usvitem odesel do hor.");
				return;
			}
			NPCDef def = ThingDef.GetByDefname(TrollKind.StoneDef) as NPCDef;
			if (def != null) {
				this.Stone = (NPC) def.Create(this.Beast.X, this.Beast.Y, this.Beast.Z, this.Beast.M);
				this.Stone.Flag_Invulnerable = true;
				this.Stone.Direction = this.Beast.Direction;
				NPCAiTimer.Freeze(this.Stone, (Settings.trollStoneMinutes * 60) + 60);
				WorldEvents.AddLandmark(this.Stone, Settings.trollStoneMinutes);
			}
			this.End(true);
			foreach (Player fighter in fighters) {
				WorldEvents.Reward(fighter, this.City, Settings.trollGold, Settings.trollExp, Settings.trollFavour);
				fighter.SysMessage("Prvni paprsky slunce promenily trola v kamen.");
			}
			WorldEvents.SetOutcome(this.City, 0, 0, "U brodu za " + this.CityName + " stoji kamenny trol. Zdrzeli ho az do usvitu.");
		}

		protected override bool IsFoe(NPC npc) {
			return true;
		}

		public override string Describe() {
			return "Trol u brodu za " + this.CityName + " ceka na usvit.";
		}
	}
}
