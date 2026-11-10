using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1019 - region rules for starting an attack, driven by Tests/Protocol/RegionAttackTests.cs with two real players.</summary>
	public static class RegionAttackTests {
		private static readonly TagKey monsterTK = TagKey.Acquire("_regionAttackTest_monster_");
		private static readonly TagKey flagsTK = TagKey.Acquire("_regionAttackTest_flags_");

		/// <summary>Into a region (by defname; "guarded" = Edoras, flagged guarded for the test only), standing on its spawn
		/// point, a held rabbit beside: the region's name, whether we're in it, the rabbit.</summary>
		[SteamFunction]
		public static void TestRegionAttackGo(Character self, ScriptArgs sa) {
			string name = sa.Args.Trim().Trim('"');
			bool guarded = name == "guarded";
			FlaggedRegion region = (FlaggedRegion) StaticRegion.GetByDefname(guarded ? "a_Edoras" : name);
			if (guarded) {
				self.SetTag(flagsTK, region);
				region.Flags |= RegionFlags.Guarded;
			}
			Point4D p = region.P;
			self.Go(p.X, p.Y, p.Z, p.M);
			if (!IsIn(self, region)) { //a spawn point can lie outside: the middle of its first rectangle
				ImmutableRectangle rect = region.Rectangles[0];
				self.Go((rect.MinX + rect.MaxX) / 2, (rect.MinY + rect.MaxY) / 2, p.Z, p.M);
			}
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(rabbit, 300);
			if (self.GetTag(monsterTK) is NPC old) {
				old.Delete();
			}
			self.SetTag(monsterTK, rabbit);
			Console.WriteLine("REGION_GO: region=" + region.Defname + " inside=" + IsIn(self, region) +
				" guarded=" + region.Flag_Guarded + " safe=" + region.Flag_Safe + " nopvp=" + region.Flag_NoPvp + " arena=" + region.Flag_Arena + " rabbit=" + (uint) rabbit.FlaggedUid + " me=" + (uint) self.FlaggedUid + " end");
		}

		private static bool IsIn(Character self, Region region) {
			for (Region r = self.Region; r != null; r = r.Parent) {
				if (r == region) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Beside the character with this uid.</summary>
		[SteamFunction]
		public static void TestRegionAttackJoin(Character self, ScriptArgs sa) {
			Character other = (Character) Thing.UidGetCharacter(uint.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture));
			self.Go(other.X, other.Y + 1, other.Z, other.M);
			Console.WriteLine("REGION_JOIN: uid=" + (uint) self.FlaggedUid + " end");
		}

		/// <summary>Whom we're fighting now (the target queue's current), then forget it and leave war; the guarded flag goes.</summary>
		[SteamFunction]
		public static void TestRegionAttackReport(Character self) {
			Character target = WeaponSkillTargetQueuePlugin.CurrentTarget(self);
			Console.WriteLine("REGION_ATTACK: target=" + ((target == null) ? "none" : ((uint) target.FlaggedUid).ToString(System.Globalization.CultureInfo.InvariantCulture)) + " end");
			if (target != null) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
			}
			self.Flag_WarMode = false;
		}

		[SteamFunction]
		public static void TestRegionAttackRestore(Character self) {
			if (self.GetTag(flagsTK) is FlaggedRegion region) {
				region.Flags &= ~RegionFlags.Guarded;
				self.RemoveTag(flagsTK);
			}
			Console.WriteLine("REGION_RESTORED: done");
		}
	}
}
