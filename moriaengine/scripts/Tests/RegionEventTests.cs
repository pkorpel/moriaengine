/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#813 - server-side helpers for RegionEventTests: real archive areas carrying a
	/// region event, entered by a real teleport (Go) or step (WalkRunOrFly), so the engine's own
	/// region transition runs.</summary>
	public static class RegionEventTests {

		//the raw text: a plain account's Argv is the whole line as one literal string
		private static string[] Args(ScriptArgs sa) {
			string[] args = sa.Args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < args.Length; i++) {
				args[i] = args[i].Trim('"');
			}
			return args;
		}

		/// <summary>Points whose area (innermost) is an area with this event, a few per area,
		/// areas in defname order so a run is repeatable.</summary>
		private static IEnumerable<Point4D> PointsInside(string eventName) {
			List<FlaggedRegion> areas = new List<FlaggedRegion>();
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && !area.IsWorldRegion && RegionEventSystem.HasEvent(area, eventName)) {
					areas.Add(area);
				}
			}
			areas.Sort((a, b) => string.CompareOrdinal(a.Defname, b.Defname));
			foreach (FlaggedRegion area in areas) {
				Map map = Map.GetMap(area.Mapplane);
				int found = 0;
				foreach (ImmutableRectangle rect in area.Rectangles) {
					int stepX = Math.Max(1, (rect.MaxX - rect.MinX) / 8);
					int stepY = Math.Max(1, (rect.MaxY - rect.MinY) / 8);
					for (int x = rect.MinX + (stepX / 2); x <= rect.MaxX; x += stepX) {
						for (int y = rect.MinY + (stepY / 2); y <= rect.MaxY; y += stepY) {
							if (map.IsValidPos(x, y) && (RegionEventSystem.AreaOf(map.GetRegionFor(x, y)) == area)) {
								yield return new Point4D(x, y, map.GetTileZ(x, y), area.Mapplane);
								if (++found >= 20) {
									goto nextArea;
								}
							}
						}
					}
				}
			nextArea:
				;
			}
		}

		/// <summary>The nearest point (spiralling out from `inside`) whose area doesn't have the event.</summary>
		private static Point4D PointOutside(Point4D inside, string eventName) {
			Map map = Map.GetMap(inside.M);
			for (int r = 1; r <= 1000; r++) {
				for (int dx = -r; dx <= r; dx += r) {
					for (int dy = -r; dy <= r; dy++) {
						int x = inside.X + dx, y = inside.Y + dy;
						if (map.IsValidPos(x, y) && !RegionEventSystem.HasEvent(RegionEventSystem.AreaOf(map.GetRegionFor(x, y)), eventName)) {
							return new Point4D(x, y, map.GetTileZ(x, y), inside.M);
						}
					}
				}
			}
			throw new SEException("No point outside " + eventName + " near " + inside);
		}

		private static Point4D FirstInside(string eventName) {
			foreach (Point4D p in PointsInside(eventName)) {
				return p;
			}
			throw new SEException("No area with " + eventName);
		}

		private static string AreaName(Character ch) {
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return (area == null) ? "none" : area.Defname;
		}

		/// <summary>".TestRegionEventGo r_safezone inside|outside" - teleports self to a point
		/// whose area has (or, near the first such area, doesn't have) the event.</summary>
		[SteamFunction]
		public static void TestRegionEventGo(Character self, ScriptArgs sa) {
			string[] args = Args(sa);
			string eventName = args[0];
			Point4D inside = FirstInside(eventName);
			Point4D target = (args[1] == "inside") ? inside : PointOutside(inside, eventName);
			self.Go(target.X, target.Y, target.Z, target.M);
			Console.WriteLine("REGION_GO: " + eventName + " " + args[1] + " area=" + AreaName(self) +
				" invul=" + self.Flag_Invulnerable + " riding=" + self.Flag_Riding);
		}

		/// <summary>".TestRegionSummonsEnter r_nosumon" - a summoned creature, a summoned mount
		/// (mage_horse_brown) and an ordinary NPC, created just outside an area with the event,
		/// then each teleported into it. Reports their uids.</summary>
		[SteamFunction]
		public static void TestRegionSummonsEnter(Character self, ScriptArgs sa) {
			string eventName = Args(sa)[0];
			Point4D inside = FirstInside(eventName);
			Point4D outside = PointOutside(inside, eventName);
			NPC summon = CreateAt("c_wolf", outside);
			SummonSpellDef.MarkAsSummonedCreature(summon);
			NPC mount = CreateAt("mage_horse_brown", outside);
			SummonSpellDef.MarkAsSummonedCreature(mount);
			NPC plain = CreateAt("c_wolf", outside);
			foreach (NPC npc in new[] { summon, mount, plain }) {
				npc.Go(inside.X, inside.Y, inside.Z, inside.M);
			}
			Console.WriteLine("REGION_SUMMONS: " + eventName + " area=" + AreaName(plain) + " summon=" + summon.Uid +
				" mount=" + mount.Uid + " plain=" + plain.Uid);
		}

		private static NPC CreateAt(string defname, Point4D p) {
			return (NPC) ((NPCDef) ThingDef.GetByDefname(defname)).Create(p.X, p.Y, p.Z, p.M);
		}

		/// <summary>".TestRegionThingsAlive uid uid ..." - which of these still exist.</summary>
		[SteamFunction]
		public static void TestRegionThingsAlive(Character self, ScriptArgs sa) {
			string report = "";
			foreach (string arg in Args(sa)) {
				Thing thing = Thing.UidGetThing(Convert.ToInt32(arg, CultureInfo.InvariantCulture));
				report += " " + arg + "=" + ((thing != null) && !thing.IsDeleted);
			}
			Console.WriteLine("REGION_ALIVE:" + report);
		}

		/// <summary>".TestRegionSpellsAllowed r_nofield" - teleports self into an area with the
		/// event and reports whether Fire Field (28), Teleport (22) and Clumsy (1) pass the cast's
		/// region check there.</summary>
		[SteamFunction]
		public static void TestRegionSpellsAllowed(Character self, ScriptArgs sa) {
			string eventName = Args(sa)[0];
			Point4D inside = FirstInside(eventName);
			self.Go(inside.X, inside.Y, inside.Z, inside.M);
			//the region gate every cast goes through when it starts and when it lands
			Console.WriteLine("REGION_SPELLS: " + eventName + " area=" + AreaName(self) + " gm=" + self.IsGM +
				" field=" + SpellDef.GetById(28).CheckPermissionOutgoing(self) +
				" teleport=" + SpellDef.GetById(22).CheckPermissionOutgoing(self) +
				" other=" + SpellDef.GetById(1).CheckPermissionOutgoing(self));
		}

		/// <summary>".TestRegionMountedStep r_doly_nohorse c_horse_tan" - mounts self on a new
		/// `mount` and takes a real step inside an area with the event (trying points and
		/// directions until one step succeeds). Reports riding before and after the step.</summary>
		[SteamFunction]
		public static void TestRegionMountedStep(Character self, ScriptArgs sa) {
			string[] args = Args(sa);
			string eventName = args[0];
			NPC mount = CreateAt(args[1], new Point4D(self.X, self.Y, self.Z, self.M));
			self.Mount = mount;
			foreach (Point4D p in PointsInside(eventName)) {
				self.Go(p.X, p.Y, p.Z, p.M);
				bool before = self.Flag_Riding;
				for (int dir = 0; dir < 8; dir++) {
					self.Direction = (Direction) dir;
					if (self.WalkRunOrFly((Direction) dir, false, true)) {
						Console.WriteLine("REGION_STEP: " + eventName + " area=" + AreaName(self) + " z=" + self.Z +
							" ridingBefore=" + before + " ridingAfter=" + self.Flag_Riding);
						return;
					}
				}
			}
			Console.WriteLine("REGION_STEP: " + eventName + " none");
		}
	}
}
