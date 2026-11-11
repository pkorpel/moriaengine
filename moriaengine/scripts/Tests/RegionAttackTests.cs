using System;
using System.Reflection;
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

		/// <summary>#1347 - damage where we stand (it lands everywhere, as on Erebor): 20 from the rabbit, 20 from the other player (uid), 5 of god
		/// damage; whether the rabbit's AI keeps us as a target; whether a lich beside us would cast at us.</summary>
		[SteamFunction]
		public static void TestRegionDamage(Character self, ScriptArgs sa) {
			Character other = (Character) Thing.UidGetCharacter(uint.Parse(sa.Args.Trim().Trim('"'), System.Globalization.CultureInfo.InvariantCulture));
			NPC rabbit = (NPC) self.GetTag(monsterTK);
			int monster = Lost(self, rabbit, DamageType.Physical, 20);
			int player = Lost(self, other, DamageType.Physical, 20);
			int god = Lost(self, rabbit, DamageType.Irresistable, 5);
			BindingFlags any = BindingFlags.Instance | BindingFlags.NonPublic;
			NPCAiTimer rabbitAi = NPCAiTimer.GetFor(rabbit);
			bool targets = (bool) typeof(NPCAiTimer).GetMethod("IsValidTarget", any).Invoke(rabbitAi, new object[] { rabbit, self });
			NPC lich = (NPC) ThingDef.GetByDefname("c_liche_nb").Create(self.X - 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(lich, 300);
			lich.Mana = lich.MaxMana;
			bool casts = (bool) typeof(NPCAiTimer).GetMethod("TryCastSpell", any).Invoke(NPCAiTimer.GetFor(lich), new object[] { lich, self });
			lich.Delete();
			self.Hits = self.MaxHits;
			Console.WriteLine("REGION_DAMAGE: monsterHurts=" + (monster > 0) + " playerHurts=" + (player > 0) + " god=" + god + " targets=" + targets + " casts=" + casts + " end");
		}

		private static int Lost(Character self, Character from, DamageType flags, double damage) {
			self.Hits = self.MaxHits;
			int before = self.Hits;
			DamageManager.CauseDamage(from, self, flags, damage);
			return before - self.Hits;
		}

		[SteamFunction]
		public static void TestRegionAttackRestore(Character self) {
			if (self.GetTag(monsterTK) is NPC rabbit) {
				rabbit.Delete();
				self.RemoveTag(monsterTK);
			}
			if (self.GetTag(flagsTK) is FlaggedRegion region) {
				region.Flags &= ~RegionFlags.Guarded;
				self.RemoveTag(flagsTK);
			}
			Console.WriteLine("REGION_RESTORED: done");
		}
	}
}
