using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1100 - Erebor's e_tele_to_target and i_attack_guard (systems/npc_ai.scp), on guards (e_guard, e_guard_ent and
	/// those answering spk_guard). A guard fighting a player runs at them once a second, up to 8 steps at once, so it seems
	/// to teleport onto them; one more than 70 tiles from its spawn teleports home instead. With no fight left, a guard
	/// heading home from more than 7 tiles teleports there (e_guard's @skillstroke), and a guard fighting an NPC spawned
	/// in a town drops it and teleports home.
	/// i_attack_guard returns early unless the victim stands in an insta-logout area when `cont.tag.guard` is set, but
	/// that tag is on the guards' chardefs only and 0.99z8 reads `cont.tag` from the instance alone (EreborNext's M_Tag;
	/// Erebor's own scripts read `typedef.tag(guard)`), so it never held: the pursuit ran everywhere.
	/// Erebor's 5-step cap far from the spawn isn't ported: it held only where the guard went home instead. Guards with no
	/// spawn gem (hired ones) count from their home, as all of Erebor's guards had a gem.</summary>
	public static class GuardPursuit {
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");

		/// <summary>An unowned guard of Erebor's e_guard, e_guard_ent or spk_guard.</summary>
		public static bool IsGuard(NPC npc) {
			return (npc.ControlMaster == null) && (MonsterSpecialAttacks.HasGuardEvent(npc) || GuardSpeech.IsGuard(npc));
		}

		public static bool IsPursuing(NPC guard) {
			return guard.HasTimer(GuardPursuitTimer.Key);
		}

		/// <summary>From NPCAiTimer.Fight, each think-tick. True if the guard dropped the fight and went home.</summary>
		internal static bool OnFighting(NPC guard, NPCAiTimer ai, Character target) {
			if (!IsGuard(guard)) {
				return false;
			}
			if ((target is NPC other) && IsSpawnedInTown(other)) {
				//guards hitting each other, Erebor's comment says
				ai.Disengage();
				GoHome(guard, ai);
				return true;
			}
			//e_tele_to_target's @skillstroke: a player not frozen, invulnerable, invisible or hidden
			if (target.IsPlayer && !target.Flag_Invulnerable && !ParalyzeEffectPluginDef.IsParalyzed(target) &&
					(NpcAggroSettings.instance.guardPursuitSteps > 0) && !IsPursuing(guard)) {
				GuardPursuitTimer timer = new GuardPursuitTimer();
				guard.AddTimer(GuardPursuitTimer.Key, timer);
				timer.PeriodInSeconds = GuardPursuitTimer.Seconds;
				timer.DueInSeconds = GuardPursuitTimer.Seconds;
			}
			return false;
		}

		/// <summary>From NPCAiTimer.Wander when the guard heads home. True if it teleported there.</summary>
		internal static bool TeleportsHome(NPC guard, NPCAiTimer ai) {
			if (!IsGuard(guard) || IsPursuing(guard) || (DistanceFromSpawn(guard, ai) <= NpcAggroSettings.instance.guardTeleportsHomeFrom)) {
				return false;
			}
			GoHome(guard, ai);
			return true;
		}

		/// <summary>i_attack_guard's @timer.</summary>
		internal static void Pursue(NPC guard, NPCAiTimer ai, Character victim) {
			if (!victim.IsPlayer) {
				return;
			}
			NpcAggroSettings settings = NpcAggroSettings.instance;
			int distance = Point2D.GetSimpleDistance(guard, victim);
			if ((victim.M != guard.M) || (distance > settings.guardPursuitRange) || !guard.CanSeeLOS(victim).Allow) {
				return;
			}
			SkillName action = guard.CurrentSkillName;
			int keepAway = ((action == SkillName.Magery) || (action == SkillName.Archery)) ? 2 : 0; //guardranged
			int steps = distance - keepAway - 1;
			if (steps < 0) {
				return;
			}
			if (DistanceFromSpawn(guard, ai) > settings.guardLeavesForHomeAt) {
				GoHome(guard, ai);
				return;
			}
			ai.RunTowards(guard, victim, System.Math.Min(steps, settings.guardPursuitSteps));
		}

		/// <summary>Tiles from the guard's spawn gem, or from its home when it has none.</summary>
		public static int DistanceFromSpawn(NPC guard, NPCAiTimer ai) {
			if ((guard.GetTag(spawnGemTK) is Thing gem) && !gem.IsDeleted && (gem.M == guard.M)) {
				return Point2D.GetSimpleDistance(guard, gem);
			}
			if (!ai.hasHome || (ai.homeM != guard.M)) {
				return 0;
			}
			return Point2D.GetSimpleDistance(guard.X, guard.Y, ai.homeX, ai.homeY);
		}

		//go(<home>)
		private static void GoHome(NPC guard, NPCAiTimer ai) {
			if (ai.hasHome) {
				guard.Go(ai.homeX, ai.homeY, ai.homeZ, ai.homeM);
			}
		}

		//e_guard's @skillstroke: act.memoryfindtype(MEMORY_ISPAWNED).link.region.tag(jmeno_mesta)
		private static bool IsSpawnedInTown(NPC npc) {
			if (!(npc.GetTag(spawnGemTK) is Thing gem) || gem.IsDeleted) {
				return false;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(gem.Region);
			return (area != null) && !string.IsNullOrEmpty(area.City);
		}
	}

	/// <summary>#1100 - i_attack_guard: on a guard while it fights a player, gone with the fight.</summary>
	[SaveableClass, DeepCopyableClass]
	public class GuardPursuitTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_guardPursuit_");
		internal const double Seconds = 1.0;

		[LoadingInitializer, DeepCopyImplementation]
		public GuardPursuitTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC guard) || guard.IsDeleted || guard.Flag_Dead) {
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(guard);
			Character victim = (ai == null) ? null : ai.Target;
			if ((victim == null) || victim.IsDeleted) {
				guard.DeleteTimer(Key); //e_tele_to_target leaves when no fight is left
				return;
			}
			if (!ai.isHeld && victim.IsAliveAndValid) {
				GuardPursuit.Pursue(guard, ai, victim);
			}
		}
	}
}
