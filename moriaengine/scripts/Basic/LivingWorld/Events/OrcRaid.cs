using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1286 - what a warband event is made of: its people, its size, its time, what it pays and its outcome either way,
	/// and what it says. Read from .settings each time, so a GM's change counts at the next event.</summary>
	public sealed class WarbandRules {
		public string ChiefDef, RaiderDef;
		public int Minutes, Base, PerPlayer, Max, Steps;
		public int Gold, KillGold, Exp, Favour, WonPercent, WonMinutes, LostPercent, LostMinutes;
		public string StartCall, StartNews, WonThanks, WonNews, LostCall, LostNews, Line;
	}

	/// <summary>#1286 - a kind of warband event (an orc raid, corsairs at a harbour): a band comes in from outside and makes for the
	/// centre (Warband).</summary>
	public abstract class WarbandKind : WorldEventKind {
		public abstract WarbandRules Rules { get; }

		public override WorldEvent Make(string city) {
			return new Warband(this, city);
		}

		/// <summary>How many come for so many players near the centre.</summary>
		public int SizeFor(int players) {
			WarbandRules r = this.Rules;
			return Math.Max(1, Math.Min(r.Max, r.Base + (r.PerPlayer * players)));
		}
	}

	/// <summary>#1284 - an orc raid on a Gondor pilot city at dusk (design section 6): a warband sized by the players there
	/// comes in from outside and makes for the centre. Killed in time: those who fought it are paid and the city feasts (cheaper
	/// for a while). Not: the raiders leave with the harvest, and bread is dear for a day.</summary>
	public sealed class OrcRaidKind : WarbandKind {
		public const string RaiderDef = "c_event_raider", ChiefDef = "c_event_raid_chief";

		public override string Key { get { return "najezd"; } }
		public override string Name { get { return "Skreti najezd"; } }

		public override bool IsFor(string city) {
			return Reputation.RealmOfCity(city) == Realm.Gondor;
		}

		public override bool IsTime() {
			WorldEventSettings s = WorldEventSettings.instance;
			return VenueClock.IsOpen(s.raidDuskFrom, s.raidDuskTo, VenueClock.Hour);
		}

		public override WarbandRules Rules {
			get {
				WorldEventSettings s = WorldEventSettings.instance;
				return new WarbandRules {
					ChiefDef = ChiefDef, RaiderDef = RaiderDef,
					Minutes = s.raidMinutes, Base = s.raidBase, PerPlayer = s.raidPerPlayer, Max = s.raidMax, Steps = s.raidSteps,
					Gold = s.raidGold, KillGold = s.raidKillGold, Exp = s.raidExp, Favour = s.raidFavour,
					WonPercent = s.raidWonPercent, WonMinutes = s.raidWonMinutes, LostPercent = s.raidLostPercent, LostMinutes = s.raidLostMinutes,
					StartCall = "Skreti utoci na {0}! Bran mesto!",
					StartNews = "Skreti tahnou na {0}! Kdo unese zbran, at jde branit mesto.",
					WonThanks = "Skreti najezd na {0} byl odrazen. Mesto ti dekuje.",
					WonNews = "Obranci {0} odrazili skreti najezd. Mesto slavi hostinou.",
					LostCall = "Skreti odtahli i s urodou. Najezd na {0} se nepodarilo odrazit.",
					LostNews = "Skreti vyplenili pole u {0}. Chleba je malo a ceny stoupaji.",
					Line = "Skreti najezd na {0}: zbyva skretu {1}."
				};
			}
		}
	}

	/// <summary>#1286 - corsairs at the harbour (design section 6): Umbar's ships land raiders at Pelargir, Dol Amroth's harbour.
	/// Beaten: the harbour stays open and its goods a little cheaper. Not: the harbour closes, and prices rise for hours.</summary>
	public sealed class CorsairKind : WarbandKind {
		public const string CorsairDef = "c_event_corsair", CaptainDef = "c_event_corsair_captain", City = "Dol_Amroth";
		private static readonly string[] places = { City };

		public override string Key { get { return "korzari"; } }
		public override string Name { get { return "Korzari v pristavu"; } }

		public override IList<string> Places {
			get { return places; }
		}

		public override bool IsFor(string city) {
			return string.Equals(city, City, StringComparison.OrdinalIgnoreCase);
		}

		public override string AreaFor(string city) {
			return "a_Pelargir";
		}

		public override string PlaceName(string city) {
			return "Pelargir";
		}

		public override WarbandRules Rules {
			get {
				WorldEventSettings s = WorldEventSettings.instance;
				return new WarbandRules {
					ChiefDef = CaptainDef, RaiderDef = CorsairDef,
					Minutes = s.corsairMinutes, Base = s.corsairBase, PerPlayer = s.corsairPerPlayer, Max = s.corsairMax, Steps = s.corsairSteps,
					Gold = s.corsairGold, KillGold = s.corsairKillGold, Exp = s.corsairExp, Favour = s.corsairFavour,
					WonPercent = s.corsairWonPercent, WonMinutes = s.corsairWonMinutes, LostPercent = s.corsairLostPercent, LostMinutes = s.corsairLostMinutes,
					StartCall = "Korzari z Umbaru pristali v {0}! K pristavu!",
					StartNews = "Korzarske lode pristaly v {0}. Pristav potrebuje obrance.",
					WonThanks = "Korzari v {0} byli zahnani. Pristav ti dekuje.",
					WonNews = "Korzari byli zahnani od {0}. Pristav zustava otevreny.",
					LostCall = "Korzari vyplenili pristav v {0} a odplouvaji.",
					LostNews = "Korzari vyplenili pristav v {0}. Pristav je zavreny a zbozi drahe.",
					Line = "Korzari v {0}: zbyva jich {1}."
				};
			}
		}
	}

	/// <summary>#1284/#1286 - a warband under way (Warband kinds: the orc raid, the corsairs).</summary>
	public sealed class Warband : WorldEvent {
		internal const int Leash = 6;

		internal Warband(WarbandKind kind, string city)
			: base(kind, city) {
		}

		private WarbandRules Rules {
			get { return ((WarbandKind) this.Kind).Rules; }
		}

		internal override bool Begin(int playersNear) {
			WarbandRules r = this.Rules;
			List<int[]> route = WorldEvents.RouteIn(this.Centre, r.Steps);
			if ((route == null) || (route.Count < r.Steps / 2) || (WorldSpots.Distance(route[0], this.Centre.X, this.Centre.Y) <= Leash + 4)) {
				return false; //nowhere far enough to come from
			}
			Map map = Map.GetMap(this.Centre.M);
			int[] from = route[0];
			int[] to = { this.Centre.X, this.Centre.Y, this.Centre.Z };
			int size = ((WarbandKind) this.Kind).SizeFor(playersNear);
			for (int i = 0; i < size; i++) {
				int[] at = WorldSpots.NearestStandable(map, from[0] + (i % 3) - 1, from[1] + (i / 3) - 1, 3);
				NPC raider = this.Spawn((i == 0) ? r.ChiefDef : r.RaiderDef, at, this.Centre.M, null);
				Send(raider, to, this.Centre.M, Leash);
			}
			if (this.Npcs.Count == 0) {
				return false;
			}
			this.EndsAt = this.StartedAt + (r.Minutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage(string.Format(CultureInfo.InvariantCulture, r.StartCall, this.CityName));
			}
			CityNews.Add(this.City, string.Format(CultureInfo.InvariantCulture, r.StartNews, this.CityName));
			return true;
		}

		internal override void Tick() {
			if (this.Alive() == 0) {
				this.Win();
			} else if (Globals.TimeInSeconds >= this.EndsAt) {
				this.Lose();
			} else {
				int[] to = { this.Centre.X, this.Centre.Y, this.Centre.Z };
				foreach (NPC raider in this.Npcs) {
					if (!raider.IsDeleted && !raider.Flag_Dead && (Point2D.GetSimpleDistance(raider, this.Centre) > Leash)) {
						Send(raider, to, this.Centre.M, Leash); //a go order given up on a blocked street is given again
					}
				}
			}
		}

		private void Win() {
			WarbandRules r = this.Rules;
			this.End(true);
			foreach (KeyValuePair<Player, int> hero in this.Heroes) {
				WorldEvents.Reward(hero.Key, this.City, r.Gold + (r.KillGold * hero.Value), r.Exp, r.Favour);
				hero.Key.SysMessage(string.Format(CultureInfo.InvariantCulture, r.WonThanks, this.CityName));
			}
			WorldEvents.SetOutcome(this.City, r.WonPercent, r.WonMinutes, string.Format(CultureInfo.InvariantCulture, r.WonNews, this.CityName));
		}

		private void Lose() {
			WarbandRules r = this.Rules;
			this.End(false);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage(string.Format(CultureInfo.InvariantCulture, r.LostCall, this.CityName));
			}
			WorldEvents.SetOutcome(this.City, r.LostPercent, r.LostMinutes, string.Format(CultureInfo.InvariantCulture, r.LostNews, this.CityName));
		}

		protected override bool IsFoe(NPC npc) {
			return true; //every one of them is a raider
		}

		public override string Describe() {
			return string.Format(CultureInfo.InvariantCulture, this.Rules.Line, this.CityName, this.Alive().ToString(CultureInfo.InvariantCulture));
		}
	}
}
