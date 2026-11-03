using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>Erebor's doorstep tile, i_duch_dlazba ("nepusti ducha", systems/dungeon_bonusy.scp): an invisible tile at a
	/// house's way in that a ghost, or anyone not of the house's realm, can't step on; pets and GMs can. Erebor read the realm
	/// from the tile's region and pushed the walker back after @step; here the tile keeps its house's realm itself, so an
	/// overlapping zone (Barad-dur's) doesn't decide, and the step is refused before it's taken. .HouseDoor puts one under
	/// every door, .HousePad anywhere. The user's call: these, not the house's whole region, keep people out.</summary>
	public static class HouseDoorstep {
		internal const string Defname = "i_duch_dlazba", OtherDefname = "i_duch_dlazba2";
		internal const int ZReach = 10;
		internal const string Refused = "Do tohoto domu nemuzes.";
		private static readonly TagKey realmTK = TagKey.Acquire("_doorstep_realm_");

		internal static bool IsDoorstep(Item item) {
			return (item != null) && ((item.Def == ThingDef.GetByDefname(Defname)) || (item.Def == ThingDef.GetByDefname(OtherDefname)));
		}

		/// <summary>A tile at a spot, of a realm (None stops only ghosts): attr=090 of its @Create, move-never and invisible.</summary>
		internal static Item Place(IPoint4D at, Realm realm) {
			Item pad = (Item) ThingDef.GetByDefname(Defname).Create(at.X, at.Y, at.Z, at.M);
			pad.SetAttribute(ItemAttributes.attr_move_never, true);
			pad.SetAttribute(ItemAttributes.attr_invis, true);
			pad.SetTag(realmTK, (int) realm);
			return pad;
		}

		/// <summary>The realm the tile lets in: its own, or for a tile with none (a GM's .add) Erebor's region.tag(houserealm).</summary>
		internal static Realm RealmOf(Item pad) {
			object realm = pad.GetTag(realmTK);
			if (realm != null) {
				return (Realm) Convert.ToInt32(realm, System.Globalization.CultureInfo.InvariantCulture);
			}
			FlaggedRegion area = pad.GetMap().GetRegionFor(pad) as FlaggedRegion;
			return (area == null) ? Realm.None : area.HouseRealm;
		}

		/// <summary>Whether a tile at (x, y) about z refuses this character's step onto it.</summary>
		internal static bool Refuses(Character ch, Map map, int x, int y, int z) {
			if (ch.IsGM) {
				return false;
			}
			foreach (AbstractItem found in map.GetItemsInRange(x, y, 0)) {
				Item pad = found as Item;
				if ((pad == null) || (pad.X != x) || (pad.Y != y) || (Math.Abs(pad.Z - z) > ZReach) || !IsDoorstep(pad)) {
					continue;
				}
				if (ch.Flag_Dead) {
					return true;
				}
				Realm realm = RealmOf(pad);
				if ((realm != Realm.None) && (ch.Realm != realm) && (ch.ControlMaster == null)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>The rest of the tile's @step, for whoever it lets on (not a pet): casting and any skill stop, and i_house_safe
		/// makes them invulnerable for 1.1 s (timerd=11; 99z8 deletes a t_eq_script whose @Timer doesn't return 1), renewed by
		/// each step onto a tile. e_house_safe ends it at a step out of the house. Not for GMs, who aren't stopped either.</summary>
		internal static void AfterStep(Character ch) {
			Map map = ch.GetMap();
			bool onPad = false;
			foreach (AbstractItem found in map.GetItemsInRange(ch.X, ch.Y, 0)) {
				onPad |= (found is Item pad) && (pad.X == ch.X) && (pad.Y == ch.Y) && (Math.Abs(pad.Z - ch.Z) <= ZReach) && IsDoorstep(pad);
			}
			HouseSafeTimer safe = ch.GetTimer(HouseSafeTimer.Key) as HouseSafeTimer;
			if (!onPad) {
				FlaggedRegion area = map.GetRegionFor(ch) as FlaggedRegion;
				if ((safe != null) && ((area == null) || (area.HouseNumber == 0))) {
					safe.End(ch);
				}
				return;
			}
			if (ch.ControlMaster != null) {
				return;
			}
			ch.Trigger_Disrupt();
			ch.AbortSkill();
			if (safe == null) {
				safe = new HouseSafeTimer();
				safe.madeInvulnerable = !ch.Flag_Invulnerable;
				ch.Flag_Invulnerable = true;
				ch.AddTimer(HouseSafeTimer.Key, safe);
			}
			safe.DueInSeconds = HouseSafeTimer.Seconds;
		}

		internal static bool IsSafe(Character ch) {
			return ch.GetTimer(HouseSafeTimer.Key) != null;
		}
	}

	/// <summary>i_house_safe: the doorstep's moment of invulnerability. Ends the invulnerability only if it gave it.</summary>
	[SaveableClass, DeepCopyableClass]
	public class HouseSafeTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_house_safe_");
		internal const double Seconds = 1.1;

		[SaveableData, CopyableData]
		public bool madeInvulnerable;

		[LoadingInitializer, DeepCopyImplementation]
		public HouseSafeTimer() {
		}

		internal void End(Character ch) {
			if (this.madeInvulnerable) {
				ch.Flag_Invulnerable = false;
			}
			ch.DeleteTimer(Key);
		}

		protected override void OnTimeout(TagHolder cont) {
			if (this.madeInvulnerable && (cont is Character ch)) {
				ch.Flag_Invulnerable = false;
			}
		}
	}

	/// <summary>.HousePad: a doorstep tile where the GM targets. Its realm is the house's there, or the one named
	/// (.HousePad "mordor"), for a spot whose zones overlap.</summary>
	public class Targ_HousePad : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Kam dat dlazdici, ktera nepusti cizi realm ani duchy?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			Point4D at = new Point4D(targetted.X, targetted.Y, targetted.Z, self.M);
			Realm realm;
			if (parameter is Realm named) {
				realm = named;
			} else {
				House house = House.FindHouseAt(at);
				if (house == null) {
					self.SysMessage("Tady neni zadny dum, napis realm: .HousePad \"gondor\" nebo \"mordor\".");
					return TargetResult.Done;
				}
				realm = house.Area.HouseRealm;
			}
			HouseDoorstep.Place(at, realm);
			self.SysMessage("Dlazdice polozena, pusti jen " + realm + ".");
			return TargetResult.Done;
		}
	}
}
