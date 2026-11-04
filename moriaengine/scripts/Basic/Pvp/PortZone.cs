using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's PvP PortZone (questy/pvp_events.scp by GM Yavanna, used through the r_PvP_PortZone region
	/// type): a player who dies in one of the four PortZone arenas (Arena Gondor, Arena Mordor and their Podzemi) comes back
	/// 3 seconds later. i_PvP_PortZonePorter's @Timer: back to the spot of death, so the body is there, resurrected with
	/// s_resurrection at 1000 (the arenas resurrect at once, region_cf_instantRes, so the 5 s rule doesn't apply), then to
	/// the area's PortMeTo point if a GM set one (.PortZonePoint), else to 5280,3472,35 on plane 0 for a Gondor player and
	/// on plane 55 for anyone else - both inside the arena itself. The last ridden mount comes along if it's the player's
	/// pet within 20 tiles and is ridden again; one further away gets "Tve zvire je prilis daleko.". Paralysis goes.
	/// Erebor's stone=0 isn't ported: nothing here sets Sphere's STONE (Erebor set it only during a name change).
	/// Erebor attached e_PvP_PortZone on entering and took it off on leaving; here the area of death decides, the same.</summary>
	public static class PortZone {
		public const string RegionType = "r_PvP_PortZone";

		internal const int DelaySeconds = 3, MountReach = 20;

		internal static readonly Point4D GondorPoint = new Point4D(5280, 3472, 35, 0);
		internal static readonly Point4D OtherPoint = new Point4D(5280, 3472, 35, 55);

		public static bool IsPortZone(FlaggedRegion area) {
			if (area != null) {
				foreach (string name in area.RegionResources) {
					if (StringComparer.OrdinalIgnoreCase.Equals(name, RegionType)) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>e_PvP_PortZone's @Death, from Player.On_Death: the porter starts.</summary>
		public static void OnDeath(Player player) {
			if (!IsPortZone(RegionEventSystem.AreaOf(player.Region))) {
				return;
			}
			PortZoneTimer timer = new PortZoneTimer();
			timer.deathPoint = new Point4D(player.X, player.Y, player.Z, player.M);
			timer.DueInSeconds = DelaySeconds;
			player.AddTimer(PortZoneTimer.Key, timer);
		}

		/// <summary>The GM-set PortMeTo of the area, else the realm's default.</summary>
		internal static Point4D Destination(Player player, FlaggedRegion area) {
			if ((area != null) && (area.PortMeTo != null)) {
				return area.PortMeTo;
			}
			return (player.Realm == Realm.Gondor) ? GondorPoint : OtherPoint;
		}

		/// <summary>i_PvP_PortZonePorter's @Timer.</summary>
		internal static void Port(Player player, Point4D deathPoint) {
			player.Go(deathPoint);
			if (player.Flag_Dead) {
				SpellDef resurrection = SpellDef.GetByDefname("s_resurrection");
				if (resurrection != null) {
					resurrection.ApplyEffectChar(player, player, 1000, EffectFlag.FromAbility);
				} else {
					player.Resurrect();
				}
			}
			Character mount = player.LastMount;
			bool takeMount = false;
			if ((mount != null) && !mount.IsDeleted) {
				if ((mount.M != player.M) || (Point2D.GetSimpleDistance(mount, player) > MountReach)) {
					player.SysMessage(Loc<PortZoneLoc>.Get(player.Language).MountTooFar);
				} else if (!mount.Flag_Dead && (mount.Mount == null) && mount.IsPetOf(player)) {
					takeMount = true;
				}
			}
			Point4D destination = Destination(player, RegionEventSystem.AreaOf(player.Region));
			player.Go(destination);
			if (takeMount) {
				mount.Go(destination);
			}
			ParalyzeEffectPluginDef.Unparalyze(player);
			if (takeMount) {
				mount.Trigger_DClick(player);
			}
		}

		/// <summary>Erebor's region tag PortMeTo, set by hand: where the area's dead go. The GM's spot, or "clear".</summary>
		[SteamFunction]
		public static void PortZonePoint(Character self, ScriptArgs sa) {
			FlaggedRegion area = RegionEventSystem.AreaOf(self.Region);
			PortZoneLoc texts = Loc<PortZoneLoc>.Get(self.Language);
			if (!IsPortZone(area)) {
				self.SysMessage(texts.NotPortZone);
				return;
			}
			string word = (sa == null) ? "" : (sa.Args ?? "").Trim('"', ' ').ToLowerInvariant();
			if ((word == "clear") || (word == "zrusit")) {
				area.PortMeTo = null;
				self.SysMessage(string.Format(texts.PointCleared, area.Name));
			} else {
				area.PortMeTo = new Point4D(self.X, self.Y, self.Z, self.M);
				self.SysMessage(string.Format(texts.PointSet, area.Name, area.PortMeTo.ToNormalString()));
			}
			Logger.WriteInfo(true, "PORTZONE_POINT: " + self.Name + " area=" + area.Defname + " set=" + ((area.PortMeTo == null) ? "" : area.PortMeTo.ToNormalString()) + " |");
		}
	}

	/// <summary>i_PvP_PortZonePorter: 3 s after a death in a PortZone arena.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PortZoneTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_pvpPortZone_");

		[SaveableData, CopyableData]
		public Point4D deathPoint;

		[LoadingInitializer, DeepCopyImplementation]
		public PortZoneTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Player player) && !player.IsDeleted) {
				PortZone.Port(player, this.deathPoint);
			}
		}
	}

	public class PortZoneLoc : CompiledLocStringCollection<PortZoneLoc> {
		public string MountTooFar = "Tve zvire je prilis daleko.";
		public string NotPortZone = "Tady neni PvP PortZone arena.";
		public string PointSet = "{0}: mrtvi pujdou na {1}.";
		public string PointCleared = "{0}: mrtvi pujdou na vychozi misto.";
	}
}
