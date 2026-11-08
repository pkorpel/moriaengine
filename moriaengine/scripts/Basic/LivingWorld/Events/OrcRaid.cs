using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1284 - an orc raid on a Gondor pilot city at dusk (design section 6): a warband sized by the players there
	/// comes in from outside and makes for the centre. Killed in time: those who fought it are paid and the city feasts (cheaper
	/// for a while). Not: the raiders leave with the harvest, and bread is dear for a day.</summary>
	public sealed class OrcRaidKind : WorldEventKind {
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

		public override WorldEvent Make(string city) {
			return new OrcRaid(this, city);
		}
	}

	public sealed class OrcRaid : WorldEvent {
		internal const int Leash = 6;
		private Point4D centre;

		internal OrcRaid(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		/// <summary>How many raiders come for so many players near the centre.</summary>
		public static int SizeFor(int players) {
			WorldEventSettings s = WorldEventSettings.instance;
			return Math.Max(1, Math.Min(s.raidMax, s.raidBase + (s.raidPerPlayer * players)));
		}

		internal override bool Begin(int playersNear) {
			this.centre = WorldEvents.CentreOf(this.City);
			if (this.centre == null) {
				return false;
			}
			List<int[]> route = WorldEvents.RouteIn(this.centre, Settings.raidSteps);
			if ((route == null) || (route.Count < Settings.raidSteps / 2)) {
				return false; //nowhere far enough to come from
			}
			Map map = Map.GetMap(this.centre.M);
			int[] from = route[0];
			int[] to = { this.centre.X, this.centre.Y, this.centre.Z };
			int size = SizeFor(playersNear);
			for (int i = 0; i < size; i++) {
				int[] at = WorldSpots.NearestStandable(map, from[0] + (i % 3) - 1, from[1] + (i / 3) - 1, 3);
				NPC raider = this.Spawn((i == 0) ? OrcRaidKind.ChiefDef : OrcRaidKind.RaiderDef, at, this.centre.M, null);
				Send(raider, to, this.centre.M, Leash);
			}
			if (this.Npcs.Count == 0) {
				return false;
			}
			this.EndsAt = this.StartedAt + (Settings.raidMinutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City)) {
				player.RedMessage("Skreti utoci na " + this.CityName + "! Bran mesto!");
			}
			CityNews.Add(this.City, "Skreti tahnou na " + this.CityName + "! Kdo unese zbran, at jde branit mesto.");
			return true;
		}

		internal override void Tick() {
			if (this.Alive() == 0) {
				this.Win();
			} else if (Globals.TimeInSeconds >= this.EndsAt) {
				this.Lose();
			} else {
				int[] to = { this.centre.X, this.centre.Y, this.centre.Z };
				foreach (NPC raider in this.Npcs) {
					if (!raider.IsDeleted && !raider.Flag_Dead && (Point2D.GetSimpleDistance(raider, this.centre) > Leash)) {
						Send(raider, to, this.centre.M, Leash); //a go order given up on a blocked street is given again
					}
				}
			}
		}

		private void Win() {
			this.End(true);
			foreach (KeyValuePair<Player, int> hero in this.Heroes) {
				WorldEvents.Reward(hero.Key, this.City, Settings.raidGold + (Settings.raidKillGold * hero.Value), Settings.raidExp, Settings.raidFavour);
				hero.Key.SysMessage("Skreti najezd na " + this.CityName + " byl odrazen. Mesto ti dekuje.");
			}
			WorldEvents.SetOutcome(this.City, Settings.raidWonPercent, Settings.raidWonMinutes,
				"Obranci " + this.CityName + " odrazili skreti najezd. Mesto slavi hostinou.");
		}

		private void Lose() {
			this.End(false);
			foreach (Player player in WorldEvents.PlayersNear(this.City)) {
				player.RedMessage("Skreti odtahli i s urodou. Najezd na " + this.CityName + " se nepodarilo odrazit.");
			}
			WorldEvents.SetOutcome(this.City, Settings.raidLostPercent, Settings.raidLostMinutes,
				"Skreti vyplenili pole u " + this.CityName + ". Chleba je malo a ceny stoupaji.");
		}

		public override string Describe() {
			return "Skreti najezd na " + this.CityName + ": zbyva skretu " + this.Alive().ToString(CultureInfo.InvariantCulture) + ".";
		}
	}
}
