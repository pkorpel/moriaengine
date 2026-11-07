using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1242 - residents of the living cities. A resident is a world piece: a spawn gem of one that carries who the resident
	/// is (name, sex, role, workplace and home venue, traits), so a killed resident comes back as the same person and a rebuilt
	/// world gets them back from the file. The NPC is only the body: SpawnedPieces.Prepare calls Prepare for each one spawned.
	/// What a resident thinks of a player is kept on the player (Friendship), so it outlives the body too.</summary>
	public static class Residents {
		private static readonly TagKey nameTK = TagKey.Acquire("_res_name_");
		private static readonly TagKey femaleTK = TagKey.Acquire("_res_female_");
		private static readonly TagKey roleTK = TagKey.Acquire("_res_role_");
		private static readonly TagKey venueTK = TagKey.Acquire("_res_venue_");
		private static readonly TagKey homeTK = TagKey.Acquire("_res_home_");
		private static readonly TagKey traitsTK = TagKey.Acquire("_res_traits_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey pieceIdTK = TagKey.Acquire("_worldPieceId_");
		private static readonly TagKey bodyTK = TagKey.Acquire("_res_body_");

		/// <summary>A resident's traits: what a schedule and later errands ask of them.</summary>
		public static readonly string[] AllTraits = { "spolecensky", "zbozny", "morous", "uzvaneny", "lakomy", "romanticky", "zvedavy", "pracovity" };

		public const int MaxFriendship = 10;
		private const int LeashInside = 2;

		private static List<Item> all;

		/// <summary>Every resident's gem, from the world on first use.</summary>
		public static IList<Item> AllGems {
			get {
				if (all == null) {
					all = new List<Item>();
					foreach (Thing thing in Thing.AllThings) {
						Item gem = thing as Item;
						if ((gem != null) && !gem.IsDeleted && IsResidentGem(gem)) {
							all.Add(gem);
						}
					}
				}
				all.RemoveAll(g => g.IsDeleted);
				return all;
			}
		}

		public static void Reset() {
			all = null;
		}

		public static bool IsResidentGem(Thing gem) {
			return (gem != null) && (gem.GetTag(roleTK) != null);
		}

		public static string IdOf(Thing gem) {
			return gem.GetTag(pieceIdTK) as string;
		}

		public static string NameOf(Thing gem) {
			return (gem.GetTag(nameTK) as string) ?? "";
		}

		public static bool IsFemale(Thing gem) {
			return gem.GetTag(femaleTK) != null;
		}

		public static string RoleKeyOf(Thing gem) {
			return gem.GetTag(roleTK) as string;
		}

		public static string VenueIdOf(Thing gem) {
			return gem.GetTag(venueTK) as string;
		}

		public static string HomeIdOf(Thing gem) {
			return gem.GetTag(homeTK) as string;
		}

		public static void SetHome(Thing gem, string homeVenueId) {
			if (string.IsNullOrEmpty(homeVenueId)) {
				gem.RemoveTag(homeTK);
			} else {
				gem.SetTag(homeTK, homeVenueId);
			}
		}

		public static string[] TraitsOf(Thing gem) {
			string traits = gem.GetTag(traitsTK) as string;
			return string.IsNullOrEmpty(traits) ? new string[0] : traits.Split(',');
		}

		public static bool HasTrait(Thing gem, string trait) {
			return Array.IndexOf(TraitsOf(gem), trait) >= 0;
		}

		public static CityVenue WorkplaceOf(Thing gem) {
			return VenueRegistry.ById(VenueIdOf(gem));
		}

		public static VenueRole RoleOf(Thing gem) {
			CityVenue venue = WorkplaceOf(gem);
			VenueType type = (venue == null) ? null : venue.VenueType;
			if (type != null) {
				foreach (VenueRole role in type.Roles) {
					if (role.Key == RoleKeyOf(gem)) {
						return role;
					}
				}
			}
			return null;
		}

		/// <summary>The resident's gem behind an NPC, or null when the NPC isn't a resident.</summary>
		public static Item GemOf(Character npc) {
			Item gem = (npc == null) ? null : npc.GetTag(spawnGemTK) as Item;
			return IsResidentGem(gem) ? gem : null;
		}

		/// <summary>The resident's living body, or null while dead (the gem spawns another). Prepare keeps it on the gem, so the
		/// scheduler doesn't search the map for a thousand residents every round.</summary>
		public static NPC BodyOf(Item gem) {
			NPC npc = gem.GetTag(bodyTK) as NPC;
			if ((npc != null) && !npc.IsDeleted && !npc.Flag_Dead && (npc.GetTag(spawnGemTK) == gem)) {
				return npc;
			}
			foreach (AbstractCharacter ch in gem.GetMap().GetCharsInRange(gem.X, gem.Y, 24)) { //a body made before the gem knew it
				npc = ch as NPC;
				if ((npc != null) && !npc.IsDeleted && (npc.GetTag(spawnGemTK) == gem)) {
					gem.SetTag(bodyTK, npc);
					return npc;
				}
			}
			return null;
		}

		public static List<Item> OfVenue(string venueId) {
			List<Item> list = new List<Item>();
			foreach (Item gem in AllGems) {
				if (string.Equals(VenueIdOf(gem), venueId, StringComparison.OrdinalIgnoreCase)) {
					list.Add(gem);
				}
			}
			return list;
		}

		/// <summary>A venue's residents gone, gems and bodies. Returns how many.</summary>
		public static int RemoveOfVenue(string venueId) {
			List<Item> gems = OfVenue(venueId);
			foreach (Item gem in gems) {
				NPC body = BodyOf(gem);
				if (body != null) {
					body.Delete();
				}
				gem.Delete();
			}
			return gems.Count;
		}

		public static void Rename(Item gem, string name) {
			gem.SetTag(nameTK, name);
			NPC body = BodyOf(gem);
			if (body != null) {
				body.Name = name;
			}
		}

		/// <summary>From SpawnedPieces.Prepare, for every body a resident's gem spawns: who it is, and where it should be now.</summary>
		public static void Prepare(Thing gem, Character body) {
			if (!IsResidentGem(gem)) {
				return;
			}
			body.Name = NameOf(gem);
			gem.SetTag(bodyTK, body);
			VenueRole role = RoleOf((Item) gem);
			if (role != null) {
				body.Title = role.Name.ToLowerInvariant();
			}
			NPC npc = body as NPC;
			if (npc != null) {
				ResidentSchedule.Apply((Item) gem, npc, true);
			}
			ResidentSchedule.EnsureRunning();
		}

		/// <summary>The roles of a venue that have nobody yet, filled: the count each role wants, by ids that stay
		/// (venue id/role/n), so filling again adds only what's missing. Returns how many were added.</summary>
		public static int FillRoles(CityVenue venue) {
			VenueType type = venue.VenueType;
			if ((type == null) || (venue.VenueId == null)) {
				return 0;
			}
			WorldPieces pieces = new WorldPieces();
			int added = 0;
			List<string> namesInCity = NamesInCity(venue.City);
			foreach (VenueRole role in type.Roles) {
				for (int n = 1; n <= role.Count; n++) {
					string id = venue.VenueId + "/" + role.Key + "/" + n.ToString(CultureInfo.InvariantCulture);
					if (ById(id) != null) {
						continue;
					}
					bool female = !IsOrcCity(venue.City) && (Globals.dice.Next(2) == 0);
					string name = PickName(venue.City, female, namesInCity);
					namesInCity.Add(name);
					Create(pieces, id, venue, role.Key, female, name, PickHome(venue), PickTraits());
					added++;
				}
			}
			return added;
		}

		/// <summary>One resident's gem with its identity, and its first body named at once.</summary>
		public static Item Create(WorldPieces pieces, string id, CityVenue venue, string roleKey, bool female, string name, string homeId, string traits) {
			int[] spot = ResidentSchedule.SpotIn(venue, id);
			if (spot == null) {
				spot = new[] { venue.X, venue.Y, venue.Z };
			}
			string defname = IsOrcCity(venue.City) ? "c_resident_orc" : (female ? "c_resident_woman" : "c_resident_man");
			pieces.Npc(id, defname, spot, venue.M, RealmOf(venue), false, LeashInside);
			Item gem = ById(id, true);
			gem.SetTag(nameTK, name);
			if (female) {
				gem.SetTag(femaleTK, 1);
			} else {
				gem.RemoveTag(femaleTK);
			}
			gem.SetTag(roleTK, roleKey);
			gem.SetTag(venueTK, venue.VenueId);
			SetHome(gem, homeId);
			gem.SetTag(traitsTK, traits ?? "");
			if (all != null && !all.Contains(gem)) {
				all.Add(gem);
			}
			NPC body = BodyOf(gem);
			if (body != null) { //spawned before the gem knew who it is
				Prepare(gem, body);
			}
			return gem;
		}

		public static Item ById(string id) {
			return ById(id, false);
		}

		private static Item ById(string id, bool rescan) {
			if (rescan) {
				foreach (Thing thing in Thing.AllThings) {
					if (string.Equals(thing.GetTag(pieceIdTK) as string, id, StringComparison.OrdinalIgnoreCase) && !thing.IsDeleted) {
						return thing as Item;
					}
				}
				return null;
			}
			foreach (Item gem in AllGems) {
				if (string.Equals(IdOf(gem), id, StringComparison.OrdinalIgnoreCase)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>Barad-dur's folk are orcs; Harad and Umbar are cities of Men.</summary>
		public static bool IsOrcCity(string city) {
			return string.Equals(city, "Barad-dur", StringComparison.OrdinalIgnoreCase);
		}

		private static Realm RealmOf(CityVenue venue) {
			FlaggedRegion area = venue.GetMap().GetRegionFor(venue) as FlaggedRegion;
			while (area != null) {
				if (area.Realm != Realm.None) {
					return area.Realm;
				}
				area = area.Parent as FlaggedRegion;
			}
			return Realm.None;
		}

		private static string NameList(string city, bool female) {
			if (IsOrcCity(city)) {
				return "NAMES_ORC";
			}
			if (string.Equals(city, "Hobitin", StringComparison.OrdinalIgnoreCase)) {
				return female ? "NAMES_HOBBIT_FEMALE" : "NAMES_HOBBIT_MALE";
			}
			return female ? "NAMES_HUMANFEMALE" : "NAMES_HUMANMALE";
		}

		/// <summary>A name of the city's list not yet used in that city, if one comes up in a few tries.</summary>
		private static string PickName(string city, bool female, List<string> taken) {
			string list = NameList(city, female);
			string name = null;
			for (int i = 0; i < 12; i++) {
				name = SteamEngine.CompiledScripts.NameList.Random(list) ?? (female ? "Obyvatelka" : "Obyvatel");
				if (!taken.Contains(name)) {
					break;
				}
			}
			return name;
		}

		private static List<string> NamesInCity(string city) {
			List<string> names = new List<string>();
			foreach (Item gem in AllGems) {
				CityVenue venue = WorkplaceOf(gem);
				if ((venue != null) && string.Equals(venue.City, city, StringComparison.OrdinalIgnoreCase)) {
					names.Add(NameOf(gem));
				}
			}
			return names;
		}

		/// <summary>The city's home with the fewest residents, or none: then the workplace is home too.</summary>
		private static string PickHome(CityVenue workplace) {
			CityVenue best = null;
			int fewest = int.MaxValue;
			foreach (CityVenue venue in VenueRegistry.InCity(workplace.City)) {
				if ((venue.VenueType != null) && (venue.VenueType.Key == "house")) {
					int count = 0;
					foreach (Item gem in AllGems) {
						count += string.Equals(HomeIdOf(gem), venue.VenueId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
					}
					if (count < fewest) {
						fewest = count;
						best = venue;
					}
				}
			}
			return (best == null) ? null : best.VenueId;
		}

		private static string PickTraits() {
			int first = Globals.dice.Next(AllTraits.Length), second = Globals.dice.Next(AllTraits.Length - 1);
			if (second >= first) {
				second++;
			}
			return AllTraits[first] + "," + AllTraits[second];
		}

		/// <summary>What resident `residentId` thinks of the player, 0 to 10. Kept on the player, so it survives the resident's death.</summary>
		public static int Friendship(Character player, string residentId) {
			object value = player.GetTag(FriendshipKey(residentId));
			return (value == null) ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		public static int AddFriendship(Character player, string residentId, int delta) {
			int now = Math.Max(0, Math.Min(MaxFriendship, Friendship(player, residentId) + delta));
			if (now == 0) {
				player.RemoveTag(FriendshipKey(residentId));
			} else {
				player.SetTag(FriendshipKey(residentId), now);
			}
			return now;
		}

		private static TagKey FriendshipKey(string residentId) {
			return TagKey.Acquire("_rf_" + residentId.ToLowerInvariant());
		}
	}

	/// <summary>#1242 - where a resident is at each hour: at work in the role's shifts, some evenings at the tavern (a sociable one)
	/// or the temple (a pious one), otherwise at home, which is the workplace when the city has no home venue. Moving the NPC's
	/// home point is all it takes: the AI walks home within its small leash. Where no player is near (NPC sleep) the body is put
	/// there at once, so an area wakes with everyone in place.</summary>
	public static class ResidentSchedule {
		private static readonly TimerKey schedulerTK = TimerKey.Acquire("_resident_scheduler_");
		public const double TickSeconds = 20;
		private const int LeashInside = 2, LeashOpenAir = 4;

		private static readonly Dictionary<string, List<int[]>> spotsByVenue = new Dictionary<string, List<int[]>>();

		public static void EnsureRunning() {
			if (!Globals.Instance.HasTimer(schedulerTK)) {
				ResidentSchedulerTimer timer = new ResidentSchedulerTimer();
				Globals.Instance.AddTimer(schedulerTK, timer);
				timer.DueInSeconds = TickSeconds;
				timer.PeriodInSeconds = TickSeconds;
			}
		}

		/// <summary>The venue a resident belongs in during the given shift.</summary>
		public static CityVenue PlaceFor(Item gem, VenueShift shift) {
			CityVenue work = Residents.WorkplaceOf(gem);
			VenueRole role = Residents.RoleOf(gem);
			if ((work != null) && (role != null) && ((role.Shift & shift) != 0)) {
				return work;
			}
			string city = (work == null) ? null : work.City;
			if ((shift == VenueShift.Evening) && (city != null)) {
				if (Residents.HasTrait(gem, "spolecensky")) {
					CityVenue tavern = FirstOfType(city, "tavern", "messhall");
					if (tavern != null) {
						return tavern;
					}
				}
				if (Residents.HasTrait(gem, "zbozny")) {
					CityVenue temple = FirstOfType(city, "temple", null);
					if (temple != null) {
						return temple;
					}
				}
			}
			CityVenue home = VenueRegistry.ById(Residents.HomeIdOf(gem));
			return home ?? work;
		}

		private static CityVenue FirstOfType(string city, string key, string orKey) {
			foreach (CityVenue venue in VenueRegistry.InCity(city)) {
				VenueType type = venue.VenueType;
				if ((type != null) && ((type.Key == key) || (type.Key == orKey))) {
					return venue;
				}
			}
			return null;
		}

		/// <summary>Moves the resident's home point to its place for now; puts the body there at once where no player is near,
		/// on another plane, or when `place` is set (a fresh body).</summary>
		public static void Apply(Item gem, NPC npc, bool place) {
			CityVenue venue = PlaceFor(gem, VenueClock.Shift);
			if (venue == null) {
				return;
			}
			int[] spot = SpotIn(venue, Residents.IdOf(gem));
			if (spot == null) {
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai != null) {
				ai.homeX = spot[0];
				ai.homeY = spot[1];
				ai.homeZ = (sbyte) spot[2];
				ai.homeM = venue.M;
				ai.hasHome = true;
				ai.WanderLeash = venue.IsOpenAir ? LeashOpenAir : LeashInside;
			}
			bool asleep = !npc.GetMap().Activity.IsAwake(npc.X, npc.Y, Globals.TimeInSeconds);
			if ((place || asleep || (npc.M != venue.M)) && !venue.Contains(npc)) {
				npc.Go(new Point4D(spot[0], spot[1], spot[2], venue.M));
			}
		}

		/// <summary>A tile of the venue a character can stand on, the same one for the same resident: residents of one venue spread
		/// over it rather than piling on its middle.</summary>
		public static int[] SpotIn(CityVenue venue, string residentId) {
			List<int[]> spots = SpotsOf(venue);
			if (spots.Count == 0) {
				return null;
			}
			int hash = 0;
			foreach (char c in residentId ?? "") {
				hash = unchecked((hash * 31) + c);
			}
			return spots[(hash & 0x7FFFFFFF) % spots.Count];
		}

		private static List<int[]> SpotsOf(CityVenue venue) {
			string key = venue.Uid + "|" + CityVenue.FormatRects(venue.Rectangles) + "|" + venue.ZMin;
			List<int[]> spots;
			if (spotsByVenue.TryGetValue(key, out spots)) {
				return spots;
			}
			spots = new List<int[]>();
			Map map = venue.GetMap();
			foreach (ImmutableRectangle rect in venue.Rectangles) {
				for (int x = rect.MinX; x <= rect.MaxX; x++) {
					for (int y = rect.MinY; y <= rect.MaxY; y++) {
						int z = venue.IsOpenAir ? WorldSpots.StandZ(map, x, y) : WorldSpots.LowestStandZ(map, x, y);
						if (map.IsValidPos(x, y) && venue.Contains(x, y, z) && map.CanFit(x, y, z, 16, true, false, true)) {
							spots.Add(new[] { x, y, z });
						}
					}
				}
			}
			spotsByVenue[key] = spots;
			return spots;
		}

		/// <summary>One round for every resident with a body: the scheduler's tick, and tests'.</summary>
		public static int ApplyAll() {
			int moved = 0;
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC npc = Residents.BodyOf(gem);
				if (npc != null) {
					try {
						Apply(gem, npc, false);
						moved++;
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e); //one broken resident mustn't stop the city
					}
				}
			}
			return moved;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class ResidentSchedulerTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public ResidentSchedulerTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			ResidentSchedule.ApplyAll();
		}
	}
}
