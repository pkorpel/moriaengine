using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1307 - a running world event as saved: the base fields, and a kind's own state in named bags (ints, doubles,
	/// strings, things, numbers), so the event classes keep their own shapes and say what they save in SaveTo/LoadFrom.</summary>
	[SaveableClass]
	public sealed class WorldEventRecord {
		[SaveableData]
		public string kind = "", city = "", id = "";

		[SaveableData]
		public int centreX, centreY, centreZ, centreM;

		[SaveableData]
		public double startedAt, endsAt;

		[SaveableData]
		public bool ended, won;

		[SaveableData]
		public List<Thing> npcs = new List<Thing>(), items = new List<Thing>(), heroes = new List<Thing>();

		[SaveableData]
		public List<int> heroKills = new List<int>();

		[SaveableData]
		public List<string> intKeys = new List<string>(), doubleKeys = new List<string>(), stringKeys = new List<string>(), stringValues = new List<string>();

		[SaveableData]
		public List<int> intValues = new List<int>();

		[SaveableData]
		public List<double> doubleValues = new List<double>();

		/// <summary>Thing lists, flat: thingKeys[i] names the list thingValues[i] belongs to.</summary>
		[SaveableData]
		public List<string> thingKeys = new List<string>(), numberKeys = new List<string>();

		[SaveableData]
		public List<Thing> thingValues = new List<Thing>();

		[SaveableData]
		public List<int> numberValues = new List<int>();

		[LoadingInitializer]
		public WorldEventRecord() {
		}

		public void SetInt(string key, int value) {
			this.intKeys.Add(key);
			this.intValues.Add(value);
		}

		public int GetInt(string key, int otherwise = 0) {
			int at = this.intKeys.IndexOf(key);
			return (at < 0) ? otherwise : this.intValues[at];
		}

		public void SetBool(string key, bool value) {
			this.SetInt(key, value ? 1 : 0);
		}

		public bool GetBool(string key) {
			return this.GetInt(key) != 0;
		}

		public void SetDouble(string key, double value) {
			this.doubleKeys.Add(key);
			this.doubleValues.Add(value);
		}

		public double GetDouble(string key, double otherwise = 0) {
			int at = this.doubleKeys.IndexOf(key);
			return (at < 0) ? otherwise : this.doubleValues[at];
		}

		public void SetString(string key, string value) {
			this.stringKeys.Add(key);
			this.stringValues.Add(value ?? "");
		}

		public string GetString(string key) {
			int at = this.stringKeys.IndexOf(key);
			return (at < 0) ? null : this.stringValues[at];
		}

		public void SetThings<T>(string key, IEnumerable<T> things) where T : Thing {
			foreach (T thing in things) {
				if ((thing != null) && !thing.IsDeleted) {
					this.thingKeys.Add(key);
					this.thingValues.Add(thing);
				}
			}
		}

		public void SetThing(string key, Thing thing) {
			if ((thing != null) && !thing.IsDeleted) {
				this.SetThings(key, new[] { thing });
			}
		}

		/// <summary>The list saved under `key` as it was (deleted ones too), for pairs kept side by side.</summary>
		public List<Thing> GetThingsRaw(string key) {
			List<Thing> list = new List<Thing>();
			for (int i = 0; i < this.thingKeys.Count; i++) {
				if (this.thingKeys[i] == key) {
					list.Add(this.thingValues[i]);
				}
			}
			return list;
		}

		/// <summary>The list saved under `key`, those still in the world and of the type.</summary>
		public List<T> GetThings<T>(string key) where T : Thing {
			List<T> list = new List<T>();
			for (int i = 0; i < this.thingKeys.Count; i++) {
				if ((this.thingKeys[i] == key) && (this.thingValues[i] is T thing) && !thing.IsDeleted) {
					list.Add(thing);
				}
			}
			return list;
		}

		public T GetThing<T>(string key) where T : Thing {
			List<T> list = this.GetThings<T>(key);
			return (list.Count == 0) ? null : list[0];
		}

		public void SetNumbers(string key, IEnumerable<int> numbers) {
			foreach (int number in numbers) {
				this.numberKeys.Add(key);
				this.numberValues.Add(number);
			}
		}

		public List<int> GetNumbers(string key) {
			List<int> list = new List<int>();
			for (int i = 0; i < this.numberKeys.Count; i++) {
				if (this.numberKeys[i] == key) {
					list.Add(this.numberValues[i]);
				}
			}
			return list;
		}

		/// <summary>A route of tiles, each three numbers.</summary>
		public void SetTiles(string key, IEnumerable<int[]> tiles) {
			if (tiles == null) {
				return;
			}
			foreach (int[] tile in tiles) {
				this.SetNumbers(key, tile);
			}
		}

		public List<int[]> GetTiles(string key) {
			List<int> numbers = this.GetNumbers(key);
			List<int[]> tiles = new List<int[]>();
			for (int i = 0; i + 2 < numbers.Count; i += 3) {
				tiles.Add(new[] { numbers[i], numbers[i + 1], numbers[i + 2] });
			}
			return tiles;
		}

		/// <summary>A player set or player-to-count map: the players under `key`, their counts under `key` + "#".</summary>
		public void SetPlayers(string key, IEnumerable<KeyValuePair<Player, int>> players) {
			foreach (KeyValuePair<Player, int> pair in players) {
				if ((pair.Key != null) && !pair.Key.IsDeleted) {
					this.thingKeys.Add(key);
					this.thingValues.Add(pair.Key);
					this.SetNumbers(key + "#", new[] { pair.Value });
				}
			}
		}

		public List<KeyValuePair<Player, int>> GetPlayers(string key) {
			List<KeyValuePair<Player, int>> list = new List<KeyValuePair<Player, int>>();
			List<int> counts = this.GetNumbers(key + "#");
			int n = 0;
			for (int i = 0; i < this.thingKeys.Count; i++) {
				if (this.thingKeys[i] == key) {
					int count = (n < counts.Count) ? counts[n] : 0;
					n++;
					if ((this.thingValues[i] is Player player) && !player.IsDeleted) {
						list.Add(new KeyValuePair<Player, int>(player, count));
					}
				}
			}
			return list;
		}

		/// <summary>A running event's base fields and its kind's own state.</summary>
		internal static WorldEventRecord Of(WorldEvent e) {
			WorldEventRecord r = new WorldEventRecord {
				kind = e.Kind.Key, city = e.City, id = e.Id, startedAt = e.StartedAt, endsAt = e.EndsAt, ended = e.Ended, won = e.Won
			};
			if (e.Centre != null) {
				r.centreX = e.Centre.X;
				r.centreY = e.Centre.Y;
				r.centreZ = e.Centre.Z;
				r.centreM = e.Centre.M;
			}
			r.npcs.AddRange(e.Npcs.FindAll(n => !n.IsDeleted));
			r.items.AddRange(e.Items.FindAll(i => !i.IsDeleted));
			foreach (KeyValuePair<Player, int> hero in e.Heroes) {
				if (!hero.Key.IsDeleted) {
					r.heroes.Add(hero.Key);
					r.heroKills.Add(hero.Value);
				}
			}
			e.SaveTo(r);
			return r;
		}

		/// <summary>The event again, from its record: its kind makes it, the saved fields go back, its kind's state reads them.
		/// Null when the kind no longer exists.</summary>
		internal WorldEvent Restore() {
			WorldEventKind k = WorldEvents.KindOf(this.kind);
			if (k == null) {
				return null;
			}
			WorldEvent e = k.Make(this.city);
			e.Id = this.id;
			e.Centre = new Point4D((ushort) this.centreX, (ushort) this.centreY, (sbyte) this.centreZ, (byte) this.centreM);
			e.StartedAt = this.startedAt;
			e.EndsAt = this.endsAt;
			e.Ended = this.ended;
			e.Won = this.won;
			foreach (Thing npc in this.npcs) {
				if ((npc is NPC n) && !n.IsDeleted) {
					e.Npcs.Add(n);
				}
			}
			foreach (Thing item in this.items) {
				if ((item is Item i) && !i.IsDeleted) {
					e.Items.Add(i);
				}
			}
			for (int i = 0; i < this.heroes.Count; i++) {
				if ((this.heroes[i] is Player hero) && !hero.IsDeleted) {
					e.Heroes[hero] = (i < this.heroKills.Count) ? this.heroKills[i] : 0;
				}
			}
			e.LoadFrom(this);
			return e;
		}
	}

	/// <summary>#1307 - the world events' state as saved, the user's answer "save everything": the running events, the cities'
	/// outcomes and their history, the realms' experience bonus, the landmarks, the scheduler's clock and the criers' news.</summary>
	[SaveableClass]
	public sealed class WorldEventsState {
		[SaveableData]
		public List<WorldEventRecord> events = new List<WorldEventRecord>();

		[SaveableData]
		public List<string> outcomeCities = new List<string>(), outcomeTexts = new List<string>(), outcomeIds = new List<string>();

		[SaveableData]
		public List<int> outcomePercents = new List<int>();

		[SaveableData]
		public List<double> outcomeUntils = new List<double>();

		/// <summary>The history after the running outcomes in the same lists: the first outcomeCount are running.</summary>
		[SaveableData]
		public int outcomeCount;

		[SaveableData]
		public List<int> bonusRealms = new List<int>(), bonusPercents = new List<int>();

		[SaveableData]
		public List<double> bonusUntils = new List<double>();

		[SaveableData]
		public List<Thing> landmarks = new List<Thing>();

		[SaveableData]
		public double nextStartAt, nextChanceAt, nextCalendarAt;

		[SaveableData]
		public int serial;

		[SaveableData]
		public List<string> newsCities = new List<string>(), newsTexts = new List<string>();

		[SaveableData]
		public List<long> newsUntils = new List<long>();

		[LoadingInitializer]
		public WorldEventsState() {
		}
	}

	/// <summary>#1307 - saves and restores the world events with the world (StaticMemberSaver: a snapshot at each save, the restore
	/// once everything is loaded).</summary>
	[HasSavedMembers]
	public static class WorldEventsStore {
		[SavedMember]
		private static WorldEventsState State {
			get { return Snapshot(); }
			set { Restore(value); }
		}

		internal static WorldEventsState Snapshot() {
			WorldEventsState s = new WorldEventsState();
			foreach (WorldEvent e in WorldEvents.running) {
				if (!e.Ended) {
					s.events.Add(WorldEventRecord.Of(e));
				}
			}
			foreach (WorldEvents.CityOutcome o in WorldEvents.outcomes) {
				AddOutcome(s, o);
			}
			s.outcomeCount = s.outcomeCities.Count;
			foreach (WorldEvents.CityOutcome o in WorldEvents.history) {
				AddOutcome(s, o);
			}
			foreach (KeyValuePair<Realm, KeyValuePair<int, double>> bonus in WorldEvents.experienceBonus) {
				s.bonusRealms.Add((int) bonus.Key);
				s.bonusPercents.Add(bonus.Value.Key);
				s.bonusUntils.Add(bonus.Value.Value);
			}
			s.landmarks.AddRange(WorldEvents.landmarks.FindAll(t => !t.IsDeleted));
			s.nextStartAt = WorldEvents.nextStartAt;
			s.nextChanceAt = WorldEvents.nextChanceAt;
			s.nextCalendarAt = WorldEvents.nextCalendarAt;
			s.serial = WorldEvents.serial;
			CityNews.Snapshot(s.newsCities, s.newsTexts, s.newsUntils);
			return s;
		}

		private static void AddOutcome(WorldEventsState s, WorldEvents.CityOutcome o) {
			s.outcomeCities.Add(o.city ?? "");
			s.outcomeTexts.Add(o.text ?? "");
			s.outcomeIds.Add(o.eventId ?? "");
			s.outcomePercents.Add(o.percent);
			s.outcomeUntils.Add(o.until);
		}

		internal static void Restore(WorldEventsState s) {
			if (s == null) {
				return;
			}
			WorldEvents.running.Clear();
			WorldEvents.byId.Clear();
			foreach (WorldEventRecord record in s.events) {
				try {
					WorldEvent e = record.Restore();
					if (e != null) {
						WorldEvents.running.Add(e);
						WorldEvents.byId[e.Id] = e;
					}
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteError("A world event (" + record.id + ") couldn't be restored", ex);
				}
			}
			WorldEvents.outcomes.Clear();
			WorldEvents.history.Clear();
			for (int i = 0; i < s.outcomeCities.Count; i++) {
				WorldEvents.CityOutcome o = new WorldEvents.CityOutcome {
					city = s.outcomeCities[i], text = At(s.outcomeTexts, i), eventId = At(s.outcomeIds, i),
					percent = (i < s.outcomePercents.Count) ? s.outcomePercents[i] : 0, until = (i < s.outcomeUntils.Count) ? s.outcomeUntils[i] : 0
				};
				if (o.eventId.Length == 0) {
					o.eventId = null;
				}
				(i < s.outcomeCount ? WorldEvents.outcomes : WorldEvents.history).Add(o);
			}
			WorldEvents.experienceBonus.Clear();
			for (int i = 0; (i < s.bonusRealms.Count) && (i < s.bonusPercents.Count) && (i < s.bonusUntils.Count); i++) {
				WorldEvents.experienceBonus[(Realm) s.bonusRealms[i]] = new KeyValuePair<int, double>(s.bonusPercents[i], s.bonusUntils[i]);
			}
			WorldEvents.landmarks.Clear();
			WorldEvents.landmarks.AddRange(s.landmarks.FindAll(t => (t != null) && !t.IsDeleted));
			WorldEvents.nextStartAt = s.nextStartAt;
			WorldEvents.nextChanceAt = s.nextChanceAt;
			WorldEvents.nextCalendarAt = s.nextCalendarAt;
			WorldEvents.serial = Math.Max(WorldEvents.serial, s.serial); //new ids never meet a restored event's
			CityNews.Restore(s.newsCities, s.newsTexts, s.newsUntils);
			if (WorldEvents.running.Count > 0) {
				WorldEvents.EnsureRunning(); //the restored events tick on without waiting for a login
			}
			Logger.WriteInfo(true, "World events restored: " + WorldEvents.running.Count + " of " + s.events.Count + " running, clock="
				+ Globals.Instance.HasTimer(WorldEvents.TimerKeyForTests)); //once a boot
		}

		private static string At(List<string> list, int i) {
			return (i < list.Count) ? list[i] : "";
		}
	}
}
