using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1286 - fever in the city (design section 6): a few residents fall ill. A double click with a clean bandage and a
	/// ginseng root (kingsfoil's stand-in) cures one. All cured in time: the curers are paid, the cured remember them, and in
	/// Minas Tirith the Houses of Healing do too. Not: the sick recover slowly, and the criers tell it.</summary>
	public sealed class FeverKind : WorldEventKind {
		public const string BandageDef = "i_bandage", HerbDef = "i_reag_ginseng", HealersFaction = "mt_healers";

		public override string Key { get { return "horecka"; } }
		public override string Name { get { return "Horecka ve meste"; } }

		public override bool IsFor(string city) {
			return Array.Exists(WorldEvents.Cities, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
		}

		public override WorldEvent Make(string city) {
			return new Fever(this, city);
		}
	}

	public sealed class Fever : WorldEvent {
		internal const int CoughEveryTicks = 6;

		/// <summary>The sick: the city's own residents, not the event's NPCs (nothing of theirs goes when it ends).</summary>
		public readonly List<NPC> Sick = new List<NPC>();
		public readonly Dictionary<Player, int> Curers = new Dictionary<Player, int>();
		private readonly List<Item> cured = new List<Item>();
		private int ticks;

		internal Fever(WorldEventKind kind, string city)
			: base(kind, city) {
		}

		internal override bool Begin(int playersNear) {
			List<NPC> residents = new List<NPC>();
			foreach (Item gem in Residents.AllGems) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((work != null) && string.Equals(work.City, this.City, StringComparison.OrdinalIgnoreCase)
						&& (Residents.BodyOf(gem) is NPC body) && !body.IsDeleted && !body.Flag_Dead && !IsSickAnywhere(body)) {
					residents.Add(body);
				}
			}
			for (int i = 0; (i < Settings.feverCount) && (residents.Count > 0); i++) {
				int pick = Globals.dice.Next(residents.Count);
				this.Sick.Add(residents[pick]);
				residents.RemoveAt(pick);
			}
			if (this.Sick.Count == 0) {
				return false; //nobody lives here yet (no venues marked)
			}
			this.EndsAt = this.StartedAt + (Settings.feverMinutes * 60.0);
			foreach (Player player in WorldEvents.PlayersNear(this.City, this.Kind)) {
				player.RedMessage("V " + this.CityName + " se siri horecka! Nemocni potrebuji obvaz a lecive koreni.");
			}
			CityNews.Add(this.City, "V " + this.CityName + " se siri horecka. Kdo umi obvazovat, at prinese obvazy a zensen.");
			return true;
		}

		private static bool IsSickAnywhere(NPC npc) {
			foreach (WorldEvent e in WorldEvents.Running) {
				if ((e is Fever fever) && fever.Sick.Contains(npc)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>From ResidentTalk: a double click on a sick resident tries to cure them, instead of the usual talk. True when
		/// the resident is sick in a running fever (whether or not the cure took).</summary>
		public static bool TryCure(NPC npc, Player player) {
			foreach (WorldEvent e in WorldEvents.Running) {
				if ((e is Fever fever) && !fever.Ended && fever.Sick.Contains(npc)) {
					if (Errands.CanTalk(player, npc)) { //near enough, in sight
						fever.Cure(npc, player);
					}
					return true;
				}
			}
			return false;
		}

		internal bool Cure(NPC npc, Player player) {
			ItemDef bandage = ThingDef.GetByDefname(FeverKind.BandageDef) as ItemDef;
			ItemDef herb = ThingDef.GetByDefname(FeverKind.HerbDef) as ItemDef;
			if ((bandage == null) || (herb == null) || (Errands.CountIn(player.Backpack, bandage) < 1) || (Errands.CountIn(player.Backpack, herb) < 1)) {
				npc.Say("*kasle* Potrebuju obvaz... a lecive koreni...");
				player.SysMessage("K vyleceni potrebujes cisty obvaz a koren zensenu.");
				return false;
			}
			Errands.Consume(player.Backpack, bandage, 1);
			Errands.Consume(player.Backpack, herb, 1);
			this.Sick.Remove(npc);
			Item gem = Residents.GemOf(npc);
			if (gem != null) {
				this.cured.Add(gem);
				if (WorldEvents.IsOnCitysSide(player, this.City)) {
					Residents.AddFriendship(player, Residents.IdOf(gem), Settings.feverFriendship);
				}
			}
			if (WorldEvents.IsOnCitysSide(player, this.City)) {
				this.Curers.TryGetValue(player, out int done);
				this.Curers[player] = done + 1;
			}
			npc.Say("Uz je mi lip. Dekuji ti.");
			return true;
		}

		internal override void Tick() {
			this.Sick.RemoveAll(n => n.IsDeleted || n.Flag_Dead); //a resident gone for good isn't sick any more
			if (this.Sick.Count == 0) {
				this.Win();
			} else if (Globals.TimeInSeconds >= this.EndsAt) {
				this.End(false);
				WorldEvents.SetOutcome(this.City, 0, 0, "Horecka v " + this.CityName + " si vyzadala svou dan. Nemocni se lizou z nemoci jen pomalu.");
			} else if ((++this.ticks % CoughEveryTicks) == 0) {
				this.Sick[Globals.dice.Next(this.Sick.Count)].Say("*kasle*");
			}
		}

		private void Win() {
			this.End(true);
			foreach (KeyValuePair<Player, int> curer in this.Curers) {
				WorldEvents.Reward(curer.Key, this.City, Settings.feverGold * curer.Value, Settings.feverExp * curer.Value, Settings.feverFavour);
				if (string.Equals(this.City, BeaconsKind.City, StringComparison.OrdinalIgnoreCase) && !curer.Key.IsDeleted) {
					Reputation.ChangeFaction(curer.Key, FeverKind.HealersFaction, Settings.feverFavour); //the Houses of Healing remember
				}
			}
			WorldEvents.SetOutcome(this.City, 0, 0, "Horecka v " + this.CityName + " ustoupila. Nemocni jsou na nohou.");
		}

		protected override bool IsFoe(NPC npc) {
			return false;
		}

		public override string Describe() {
			return "Horecka v " + this.CityName + ": nemocnych " + this.Sick.Count + ".";
		}
	}
}
