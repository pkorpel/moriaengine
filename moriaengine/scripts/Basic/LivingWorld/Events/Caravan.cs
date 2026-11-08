using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1284 - a caravan on the road (design section 6): a merchant with pack animals walks into a pilot city from a
	/// road outside, and bandits ambush him halfway. He arrives: his escorts are paid and the city's goods are cheaper for a
	/// while. He dies, or time runs out: the caravan is lost.</summary>
	public sealed class CaravanKind : WorldEventKind {
		public const string MerchantDef = "c_event_merchant", PackDef = "c_event_packhorse", BanditDef = "c_event_bandit";

		public override string Key { get { return "karavana"; } }
		public override string Name { get { return "Karavana"; } }

		public override bool IsFor(string city) {
			return Array.Exists(WorldEvents.Cities, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
		}

		public override WorldEvent Make(string city) {
			return new Caravan(this, city);
		}
	}

	public sealed class Caravan : WorldEvent {
		internal const int WaypointStep = 4, ArrivedWithin = 3, MinRoute = 12;
		private Point4D centre;
		private List<int[]> route;
		internal int next, ambushAt;
		public NPC Merchant;
		public bool Ambushed;

		internal Caravan(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		/// <summary>How many bandits lie in wait for so many players near the city.</summary>
		public static int BanditsFor(int players) {
			WorldEventSettings s = WorldEventSettings.instance;
			return Math.Max(1, Math.Min(s.banditMax, s.banditBase + (s.banditPerPlayer * players)));
		}

		private int playersAtStart;

		internal override bool Begin(int playersNear) {
			this.playersAtStart = playersNear;
			this.centre = WorldEvents.CentreOf(this.City);
			if (this.centre == null) {
				return false;
			}
			this.route = WorldEvents.RouteIn(this.centre, Settings.caravanSteps);
			if ((this.route == null) || (this.route.Count < MinRoute) || (WorldSpots.Distance(this.route[0], this.centre.X, this.centre.Y) <= ArrivedWithin + 6)) {
				return false; //a road that starts in the square is no road
			}
			Map map = Map.GetMap(this.centre.M);
			int[] from = this.route[0];
			this.Merchant = this.Spawn(CaravanKind.MerchantDef, from, this.centre.M, "Kupec " + (NameList.Random(Residents.NameList(this.City, false)) ?? "z jihu"));
			if (this.Merchant == null) {
				return false;
			}
			for (int i = 0; i < Settings.caravanPack; i++) {
				NPC pack = this.Spawn(CaravanKind.PackDef, WorldSpots.NearestStandable(map, from[0] + i + 1, from[1], 3), this.centre.M, null);
				if (pack != null) {
					pack.FollowTarget = this.Merchant;
					pack.ShouldFollowOwner = true;
				}
			}
			this.ambushAt = this.route.Count / 2;
			this.next = 0;
			this.Advance();
			this.EndsAt = this.StartedAt + (Settings.caravanMinutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City)) {
				player.SysMessage("Do " + this.CityName + " miri karavana. Cesta neni bezpecna, kupec by uvital doprovod.");
			}
			CityNews.Add(this.City, "Na ceste do " + this.CityName + " je karavana. Kupci hledaji doprovod.");
			return true;
		}

		/// <summary>Where the merchant is walking now.</summary>
		internal int[] Waypoint {
			get { return this.route[this.next]; }
		}

		/// <summary>The merchant's next waypoint, a few steps on along the road (the last one is the centre).</summary>
		private void Advance() {
			this.next = Math.Min(this.next + WaypointStep, this.route.Count - 1);
			Send(this.Merchant, this.route[this.next], this.centre.M, 1);
		}

		internal override void Tick() {
			if ((this.Merchant == null) || this.Merchant.IsDeleted || this.Merchant.Flag_Dead) {
				this.Lose("Kupec padl rukou lupicu.");
				return;
			}
			if (Point2D.GetSimpleDistance(this.Merchant, this.centre) <= ArrivedWithin) {
				this.Win();
				return;
			}
			if (Globals.TimeInSeconds >= this.EndsAt) {
				this.Lose("Karavana nedorazila vcas.");
				return;
			}
			if (!this.Ambushed && (this.next >= this.ambushAt)) {
				this.Ambush();
			}
			if (this.Ambushed && (this.Alive(IsBandit) > 0)) {
				this.NoteEscorts();
				return; //the merchant waits out the fight
			}
			if (Point2D.GetSimpleDistance(this.Merchant, new Point2D(this.route[this.next][0], this.route[this.next][1])) <= 1) {
				this.Advance();
			} else {
				Send(this.Merchant, this.route[this.next], this.centre.M, 1); //a go order given up is given again
			}
		}

		/// <summary>Those who stood by the merchant in the fight: they're paid if he arrives, as those who fought are.</summary>
		public readonly HashSet<Player> Escorts = new HashSet<Player>();

		private void NoteEscorts() {
			foreach (AbstractCharacter ch in this.Merchant.GetMap().GetPlayersInRange(this.Merchant.X, this.Merchant.Y, Settings.escortRange)) {
				if ((ch is Player player) && !player.Flag_Dead && WorldEvents.IsOnCitysSide(player, this.City)) {
					this.Escorts.Add(player);
				}
			}
		}

		protected override bool IsFoe(NPC npc) {
			return IsBandit(npc);
		}

		private static bool IsBandit(NPC npc) {
			return npc.Def == ThingDef.GetByDefname(CaravanKind.BanditDef);
		}

		/// <summary>The bandits come out by the road and go for the merchant.</summary>
		internal void Ambush() {
			this.Ambushed = true;
			Map map = this.Merchant.GetMap();
			int count = BanditsFor(Math.Max(this.playersAtStart, WorldEvents.PlayersNear(this.City).Count));
			for (int i = 0; i < count; i++) {
				int side = (i % 2 == 0) ? 4 : -4;
				int[] at = WorldSpots.NearestStandable(map, this.Merchant.X + side, this.Merchant.Y + (i / 2) - 1, 3);
				NPC bandit = this.Spawn(CaravanKind.BanditDef, at, this.Merchant.M, null);
				if (bandit != null) {
					Send(bandit, new[] { this.Merchant.X, this.Merchant.Y, this.Merchant.Z }, this.Merchant.M, 8);
					NPCAiTimer.GetFor(bandit)?.EngageTarget(this.Merchant);
				}
			}
			this.Merchant.PetGoalPoint = null; //he stops where he is, his home with him
			Send(this.Merchant, new[] { this.Merchant.X, this.Merchant.Y, this.Merchant.Z }, this.Merchant.M, 1);
			this.NoteEscorts();
			this.Merchant.Say("Prepadeni! Pomoc!");
		}

		private void Win() {
			HashSet<Player> escorts = new HashSet<Player>(this.Heroes.Keys); //who fought, and who stood by him in the fight: not who waited in the square
			escorts.UnionWith(this.Escorts);
			this.Merchant.Say("Jsme tu! Diky vam, pratele.");
			this.End(true);
			foreach (Player escort in escorts) {
				WorldEvents.Reward(escort, this.City, Settings.caravanGold, Settings.caravanExp, Settings.caravanFavour);
			}
			WorldEvents.SetOutcome(this.City, Settings.caravanPercent, Settings.caravanOutcomeMinutes,
				"Karavana dorazila do " + this.CityName + ". Zbozi na trhu zlevnilo.");
		}

		private void Lose(string why) {
			this.End(false);
			foreach (Player player in WorldEvents.PlayersNear(this.City)) {
				player.RedMessage(why + " Karavana do " + this.CityName + " je ztracena.");
			}
			WorldEvents.SetOutcome(this.City, 0, 0, "Karavana do " + this.CityName + " se ztratila na ceste. Zbozi nedorazilo.");
		}

		public override string Describe() {
			if (this.Ambushed && (this.Alive(IsBandit) > 0)) {
				return "Karavanu do " + this.CityName + " prepadli lupici!";
			}
			return "Karavana miri do " + this.CityName + ".";
		}
	}
}
