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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>The basic idle/wander/combat/flee state an NPC's AI tick last decided on - informational only, not persisted (recomputed fresh every tick from current conditions).</summary>
	public enum AIState {
		Idle,
		Wandering,
		Fighting,
		Fleeing,
		Following,
		GoingToPoint,
		GuardingItem
	}

	/// <summary>
	/// The NPC AI "brain" tick (enhancements Phase 16, #18). Attached once per NPC (see
	/// NPC.On_Create -> EnsureInstalled) as a plain recurring BoundTimer, the same proven
	/// mechanism used elsewhere in this codebase (AStarWalkTimer, CriminalExpiryTimer) - this
	/// deliberately avoids the separate Plugin/TimerPluginDef machinery (RegenerationPlugin,
	/// WeaponSkillTargetQueuePlugin), which relies on a hand-paired .Generated.cs file whose
	/// generation pipeline isn't exercised by this engine's current dynamic-boot-compile step
	/// for scripts/Basic; a plain [SaveableClass] BoundTimer has no such dependency.
	///
	/// Each tick: if the NPC has (or finds) a living target, either fight it (walk into weapon
	/// range, then let the existing WeaponSkillTargetQueuePlugin/WeaponSkillDef machinery handle
	/// the actual swings) or flee it (if low on health); otherwise wander loosely around its
	/// home point. Target selection is delegated to NPC.ShouldAggroOn so it stays overridable
	/// per NPC/NPCDef without touching this tick loop.
	/// </summary>
	[SaveableClass]
	public class NPCAiTimer : BoundTimer {
		private const double ThinkIntervalSeconds = 1.5;
		//GM-tunable via NpcAggroSettings (.settings -> "NPC Aggro") - was a hardcoded const
		//int (AggroRange=8/GiveUpRange=12) before, defaults bumped to 14/18 per explicit
		//user request.
		private static int AggroRange { get { return NpcAggroSettings.instance.aggroRange; } }
		private static int GiveUpRange { get { return NpcAggroSettings.instance.giveUpRange; } } //target got too far away (e.g. it fled or teleported) - drop it and go back to wandering
		private const double FleeHealthFraction = 0.2;
		private const int DefaultWanderLeash = 6; //max tiles from home while wandering, for any NPC not spawned via a configured Homedist (#206)

		//#590/#603 - NPCDef.MoveRate's own baseline ("moverate=3" in the real archive,
		//confirmed by the user as the default). At this value, RescheduleForMoveRate below
		//computes an interval of exactly ThinkIntervalSeconds - i.e. identical to this
		//engine's pre-existing fixed "one tile per 1.5s tick" behavior for any NPCDef that
		//never configures MoveRate at all. Zero regression for existing content.
		private const double BaselineMoveRate = 3.0;

		//#603 - safety floor on the think-tick interval, regardless of how high MoveRate is
		//configured - without this, an extreme value would make the tick loop fire
		//pathologically often (StepTowardsSmart recomputes AStar every step, so this is also
		//a crude CPU-cost bound, not just a gameplay one).
		private const double MinThinkIntervalSeconds = 0.3;

		//#768 - a following pet (or one under a "go" order) that's behind thinks, and so steps, faster
		//than its normal MoveRate cadence until it has caught up - Sphere's follow walks every ~0.5s and
		//runs every ~0.25s once more than 3 tiles behind; the 1.5s baseline left pets far behind a walking player.
		public const int RunWhenBehindBy = 3;
		public const double CatchUpWalkSeconds = 0.45;
		public const double CatchUpRunSeconds = 0.25;
		private const int GoGiveUpTicks = 5; //think-ticks without any progress before a "go" order is given up

		private static readonly TimerKey aiTimerTK = TimerKey.Acquire("_npcAiTimer_");

		/// <summary>#892 - stop_guard: told to hold still, until a hit or an order says otherwise.</summary>
		[SaveableData] public bool isHeld;

		[SaveableData] public int homeX;
		[SaveableData] public int homeY;
		[SaveableData] public sbyte homeZ;
		[SaveableData] public byte homeM;
		[SaveableData] public bool hasHome;

		/// <summary>Max tiles from home while wandering (#206) - was a hardcoded constant for
		/// every NPC, completely ignoring SpawnInfo.Homedist (stored on the spawn gem but never
		/// read anywhere). spawn.scp's @spawnNow now sets this on each newly-spawned character's
		/// AI timer right after creation.</summary>
		[SaveableData] public int WanderLeash = DefaultWanderLeash;

		//transient - not saved. Re-acquired from scratch on the next think tick after a reload, which is fine for a live combat/aggro target.
		private Character target;

		//Tracks whether `target` has already been handed to WeaponSkillTargetQueuePlugin, so AddTarget
		//is called exactly once per engagement rather than every single think-tick. AddTarget ->
		//FightCurrentTarget always starts a BRAND NEW SkillSequenceArgs and calls PhaseSelect() on it,
		//and On_Start aborts whatever skill sequence was already running - so calling it every 1.5s tick
		//was aborting the in-progress Drawing/Striking phase before it could ever land a hit (the
		//skill's own internal retry loop runs on a 1s cadence, so a 1.5s external restart interrupted it
		//almost every time). This was the real cause of "NPC swings but never actually hits."
		private Character lastEngagedTarget;

		//#769 - transient, not saved: the path being walked by StepTowardsSmart. Re-planned from scratch after a reload.
		private NpcPathCache pathCache;

		//#768 - transient: consecutive think-ticks a "go" order made no progress
		private int goStuckTicks;

		public AIState LastState { get; private set; }

		/// <summary>Read-only introspection for tests - this NPC's current combat target, if any (null otherwise). See FollowTarget/ControlMaster on Character for the analogous already-public state.</summary>
		public Character Target {
			get {
				return this.target;
			}
		}

		[LoadingInitializer]
		public NPCAiTimer() {
		}

		/// <summary>Attaches the AI think-timer to this NPC if it doesn't have one yet. Safe to
		/// call repeatedly. Must run AFTER NPC.MoveRate has already been seeded from
		/// NPCDef.MoveRate (see NPC.On_Create's own ordering) so the very first schedule is
		/// correct from tick one, not just corrected on the next MoveRate change.</summary>
		public static void EnsureInstalled(NPC self) {
			if (!self.HasTimer(aiTimerTK)) {
				NPCAiTimer timer = new NPCAiTimer();
				self.AddTimer(aiTimerTK, timer);
				timer.RescheduleForMoveRate(self.MoveRate);
			}
		}

		public static NPCAiTimer GetFor(NPC self) {
			return self.GetTimer(aiTimerTK) as NPCAiTimer;
		}

		/// <summary>#603 - recomputes this NPC's own AI think-tick cadence from `moveRate`: a
		/// faster NPC thinks (and so takes its one tile-step - see TryStep) more OFTEN in real
		/// time, rather than taking several tile-steps within a single tick (the #590 "step
		/// budget" design this replaces, which looked like skipping/teleporting on the client
		/// since several movement packets landed with no real-world delay between them).
		/// Call whenever MoveRate might have changed - see EnsureInstalled (seeded from the
		/// NPCDef default) and NPC.MoveRate's setter (a live .info edit).</summary>
		public void RescheduleForMoveRate(double moveRate) {
			if (moveRate <= 0) {
				moveRate = BaselineMoveRate; //defend against a bad/zero .def value permanently freezing this NPC
			}
			double interval = ThinkIntervalSeconds * BaselineMoveRate / moveRate;
			if (interval < MinThinkIntervalSeconds) {
				interval = MinThinkIntervalSeconds;
			}
			this.PeriodInSeconds = interval;
			this.DueInSeconds = interval;
		}

		/// <summary>Directly sets this NPC's current combat target, bypassing ShouldAggroOn - used by the "kill" pet command and by guard duty (AlertGuardiansOf), both of which are explicit orders rather than autonomous aggro decisions.</summary>
		public void EngageTarget(Character enemy) {
			NPC self = this.Cont as NPC;
			Logger.WriteDebug("NPCAiTimer.EngageTarget: " + (self != null ? self.Name + "#" + self.Uid : "?") + " explicitly engaging " + enemy.Name);
			this.target = enemy;
			this.lastEngagedTarget = null; //force the next Fight() tick to (re-)hand this target to WeaponSkillTargetQueuePlugin
		}

		/// <summary>Fully breaks off combat - used by the "stop" and "release" pet commands, which previously only touched Follow/wander state and left an active fight running untouched.</summary>
		public void Disengage() {
			NPC self = this.Cont as NPC;
			if ((self != null) && (this.target != null)) {
				Logger.WriteDebug("NPCAiTimer.Disengage: " + self.Name + "#" + self.Uid + " breaking off from " + this.target.Name);
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, this.target);
				self.AbortSkill();
				self.Flag_WarMode = false;
			}
			this.target = null;
			this.lastEngagedTarget = null;
		}

		/// <summary>Called from Character.Trigger_HostileAction - any NPC guarding `victim` (Character.IsGuarding, protecting its FollowTarget/ControlMaster) joins the fight against `attacker`.</summary>
		public static void AlertGuardiansOf(Character victim, Character attacker) {
			if ((victim == null) || (attacker == null) || (victim == attacker)) {
				return;
			}
			//Trigger_HostileAction can fire on a victim that has already been deleted (e.g.
			//WeaponSkillDef.On_Fail calling target.Trigger_HostileAction(self) when the target died
			//from an earlier hit in the same exchange and CauseDeath already deleted it) - accessing
			//.X/.GetMap() on a deleted Thing throws, so bail out before touching either.
			if (victim.IsDeleted || attacker.IsDeleted) {
				return;
			}
			foreach (AbstractCharacter ch in victim.GetMap().GetCharsInRange(victim.X, victim.Y, AggroRange)) {
				NPC guardian = ch as NPC;
				if ((guardian == null) || !guardian.IsGuarding) {
					continue;
				}
				Character ward = guardian.FollowTarget ?? guardian.ControlMaster;
				if (ward != victim) {
					continue;
				}
				NPCAiTimer timer = GetFor(guardian);
				if (timer != null) {
					timer.EngageTarget(attacker);
				}
			}
		}

		/// <summary>True if `a` and `b` are on opposing sides of the Gondor/Mordor conflict (either realm's outcasts count as being on that realm's side for this purpose). Neutral/unset realms are never opposite to anything.</summary>
		public static bool IsOppositeRealm(Realm a, Realm b) {
			bool aGondor = (a == Realm.Gondor) || (a == Realm.GondorOutcast);
			bool aMordor = (a == Realm.Mordor) || (a == Realm.MordorOutcast);
			bool bGondor = (b == Realm.Gondor) || (b == Realm.GondorOutcast);
			bool bMordor = (b == Realm.Mordor) || (b == Realm.MordorOutcast);
			return (aGondor && bMordor) || (aMordor && bGondor);
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC self = cont as NPC;
			if ((self == null) || self.IsDeleted || self.Flag_Dead) {
				return;
			}

			if (!this.hasHome) {
				this.CaptureHome(self);
			}

			if (EliteMonster.IsLeaving(self)) {
				return; //#794 - casting its gate / leaving: frozen (Erebor's flag_freeze)
			}
			if (PeacemakingSkillDef.IsAsleep(self)) {
				return; //#962 - e_peacemade
			}

			//#797/#799 - abilities on their own clock, in any AI state (regeneration, backstab
			//hiding, mass leap hops, dispel bomb fuses)
			MonsterSpecialAttacks.OnAiTick(self);

			this.Think(self);
		}

		private void CaptureHome(NPC self) {
			this.homeX = self.X;
			this.homeY = self.Y;
			this.homeZ = (sbyte) self.Z;
			this.homeM = self.M;
			this.hasHome = true;
		}

		/// <summary>#962 - Sphere's NPCACT_FLEE: away from them for so many steps, whatever else is going on.</summary>
		public void FleeFrom(Character from, int steps) {
			this.fleeingFrom = from;
			this.fleeSteps = steps;
		}

		private void Think(NPC self) {
			if (this.isHeld) {
				return;
			}
			if ((this.fleeSteps > 0) && (this.fleeingFrom != null) && !this.fleeingFrom.IsDeleted) {
				this.fleeSteps--;
				this.LastState = AIState.Fleeing;
				this.Flee(self, this.fleeingFrom);
				return;
			}
			if ((this.target != null) && !this.IsValidTarget(self, this.target)) {
				Logger.WriteDebug("NPCAiTimer.Think: " + self.Name + "#" + self.Uid + " dropped target " + this.target.Name + " (no longer valid - dead, map mismatch, or out of GiveUpRange)");
				this.target = null;
			}

			if (this.target == null) {
				this.target = this.FindTarget(self);
				if (this.target != null) {
					GuardSpeech.OnSight(self, this.target);
					Logger.WriteDebug("NPCAiTimer.Think: " + self.Name + "#" + self.Uid + " acquired target " + this.target.Name + " via autonomous aggro");
				}
			}

			if (this.target != null) {
				if (this.IsLowOnHealth(self)) {
					this.LastState = AIState.Fleeing;
					this.Flee(self, this.target);
				} else {
					this.LastState = AIState.Fighting;
					EliteMonster.OnFightingPlayer(self, this.target); //#794 - starts its escape countdown
					this.Fight(self, this.target);
				}
				return;
			}

			IPoint3D goal = self.PetGoalPoint;
			if (goal != null) {
				int goalDistance = Point2D.GetSimpleDistance(self, goal);
				if (goalDistance <= 1) {
					self.PetGoalPoint = null; //arrived
					this.goStuckTicks = 0;
				} else {
					this.LastState = AIState.GoingToPoint;
					int beforeX = self.X, beforeY = self.Y;
					this.StepTowardsSmart(self, goal, goalDistance > RunWhenBehindBy);
					if ((self.X == beforeX) && (self.Y == beforeY)) {
						//#768 - truly stuck (AStar already hands out partial paths, so no progress at all
						//means nothing reachable gets closer): give up and say so, instead of trying forever
						if (++this.goStuckTicks >= GoGiveUpTicks) {
							Logger.WriteDebug("NPCAiTimer.Think: " + self.Name + "#" + self.Uid + " gives up going to " + goal);
							self.PetGoalPoint = null;
							this.goStuckTicks = 0;
							Character.PetResponse(self, self.FollowTarget ?? self.ControlMaster, false);
						}
					} else {
						this.goStuckTicks = 0;
						this.ScheduleCatchUpStep(Point2D.GetSimpleDistance(self, goal));
					}
					return;
				}
			}

			//#774 - "guard" + an item: stay by it until it's picked up (only its owner/friends can -
			//Item.On_DenyPickup stops anyone else), destroyed or carried off the map
			AbstractItem guarded = self.GuardedItem;
			if (guarded != null) {
				if (guarded.IsDeleted || !guarded.IsOnGround || (guarded.M != self.M)) {
					self.GuardedItem = null;
				} else {
					this.LastState = AIState.GuardingItem;
					int guardDistance = Point2D.GetSimpleDistance(self, guarded);
					if (guardDistance > 1) {
						this.StepTowardsSmart(self, guarded, guardDistance > RunWhenBehindBy);
						this.ScheduleCatchUpStep(Point2D.GetSimpleDistance(self, guarded));
					}
					return;
				}
			}

			//FollowTarget lets a GM temporarily direct ANY NPC (not just a real owned pet) via the
			//same come/go/stop/transfer commands - it defaults to ControlMaster so a real pet's
			//owner doesn't need to do anything special, but a GM-issued "come" on an unowned
			//monster sets it explicitly without making that monster anyone's permanent pet.
			Character followTarget = self.FollowTarget ?? self.ControlMaster;
			if (self.ShouldFollowOwner && (followTarget != null) && followTarget.IsAliveAndValid && (followTarget.M == self.M)) {
				this.LastState = AIState.Following;
				this.Follow(self, followTarget);
				return;
			}

			if ((self.ControlMaster != null) || (self.FollowTarget != null)) {
				//a real pet, or a monster a GM has directed at least once - stands put instead of
				//wandering off when it's not actively following or under a "go" order
				this.LastState = AIState.Idle;
				return;
			}

			this.LastState = AIState.Wandering;
			this.Wander(self);
		}

		private void Follow(NPC self, Character master) {
			int distance = Point2D.GetSimpleDistance(self, master);
			if (distance > 1) {
				this.StepTowardsSmart(self, master, distance > RunWhenBehindBy);
				this.ScheduleCatchUpStep(Point2D.GetSimpleDistance(self, master));
			}
		}

		/// <summary>#768 - still behind after this step: think again sooner than the normal cadence.</summary>
		private void ScheduleCatchUpStep(int remainingDistance) {
			double interval = CatchUpInterval(remainingDistance, this.PeriodInSeconds);
			if (interval < this.PeriodInSeconds) {
				this.DueInSeconds = interval;
			}
		}

		/// <summary>#768 - seconds until the next think-tick for a mover `distance` tiles from where it's
		/// headed: the normal interval once it's caught up (within 1 tile), otherwise a walking or (more
		/// than RunWhenBehindBy behind) running cadence - never slower than normal.</summary>
		public static double CatchUpInterval(int distance, double normalInterval) {
			if (distance <= 1) {
				return normalInterval;
			}
			double wanted = (distance > RunWhenBehindBy) ? CatchUpRunSeconds : CatchUpWalkSeconds;
			return Math.Min(normalInterval, wanted);
		}

		/// <summary>#768 - Sphere's OnHarmedBy: an NPC that gets attacked fights back, unless it's already
		/// fighting someone. Never its own master - an owner attacking its pet first makes it rebel
		/// (NPC.On_HostileAction drops the ownership), after which this does pick the ex-owner.</summary>
		public static void DefendAgainst(NPC self, Character attacker) {
			NPCAiTimer held = GetFor(self);
			if (held != null) {
				held.isHeld = false;
			}
			if (!NpcAggroSettings.instance.npcsDefendThemselves) {
				return;
			}
			if ((attacker == null) || (attacker == self) || attacker.IsDeleted || self.IsDeleted || !attacker.IsAliveAndValid) {
				return;
			}
			if (attacker == (self.FollowTarget ?? self.ControlMaster)) {
				return;
			}
			NPCAiTimer timer = GetFor(self);
			if ((timer != null) && (timer.target == null)) {
				Logger.WriteDebug("NPCAiTimer.DefendAgainst: " + self.Name + "#" + self.Uid + " fights back against " + attacker.Name);
				timer.EngageTarget(attacker);
			}
		}

		/// <summary>#962 - Sphere's ATTACK: whoever it was fighting, now it's this one.</summary>
		public static void Attack(NPC self, Character victim) {
			NPCAiTimer timer = GetFor(self);
			if ((timer != null) && (victim != null) && !victim.IsDeleted && victim.IsAliveAndValid) {
				timer.isHeld = false;
				timer.Disengage();
				timer.EngageTarget(victim);
			}
		}

		/// <summary>#1022 - e_monster's @aftergetswing: now and then a monster turns on whoever just swung at it.</summary>
		internal static void MaybeTurnOn(NPC self, Character attacker) {
			int oneIn = CombatSettings.instance.monsterTurnsOnAttackerOneIn;
			if ((self != null) && self.IsMonster && (self.ControlMaster == null) && (oneIn > 0) && (Globals.dice.Next(oneIn) == 0)) {
				Attack(self, attacker);
			}
		}

		/// <summary>#774 - a pet guarding an item goes for whoever just tried to take it (an explicit
		/// order, so it doesn't depend on npcsDefendThemselves), unless it's already fighting.</summary>
		public static void DefendItemAgainst(NPC guard, Character thief) {
			if ((thief == null) || thief.IsDeleted || !thief.IsAliveAndValid) {
				return;
			}
			NPCAiTimer timer = GetFor(guard);
			if ((timer != null) && (timer.target == null)) {
				Logger.WriteDebug("NPCAiTimer.DefendItemAgainst: " + guard.Name + "#" + guard.Uid + " goes for " + thief.Name);
				timer.EngageTarget(thief);
			}
		}

		/// <summary>#768 - Sphere's following pets attack whatever their master is fighting: called when
		/// `attacker` does something hostile to `victim`, it sends every idle pet following the attacker
		/// after the victim. Attacking one of your own pets isn't a fight the others join (the hit one
		/// rebels instead - see NPC.On_HostileAction).</summary>
		public static void AlertPetsOf(Character attacker, Character victim) {
			if (!NpcAggroSettings.instance.petsAssistOwner) {
				return;
			}
			if ((attacker == null) || (victim == null) || (attacker == victim) || attacker.IsDeleted || victim.IsDeleted) {
				return;
			}
			NPC victimNpc = victim as NPC;
			if ((victimNpc != null) && ((victimNpc.ControlMaster == attacker) || (victimNpc.FollowTarget == attacker))) {
				return;
			}
			foreach (AbstractCharacter ch in attacker.GetMap().GetCharsInRange(attacker.X, attacker.Y, AggroRange)) {
				NPC pet = ch as NPC;
				if ((pet == null) || (pet == victim) || !pet.ShouldFollowOwner || ((pet.FollowTarget ?? pet.ControlMaster) != attacker)) {
					continue;
				}
				NPCAiTimer timer = GetFor(pet);
				if ((timer != null) && (timer.target == null)) {
					timer.EngageTarget(victim);
				}
			}
		}

		private bool IsValidTarget(NPC self, Character candidate) {
			if (!candidate.IsAliveAndValid || (candidate.M != self.M)) {
				return false;
			}
			if (candidate == (self.FollowTarget ?? self.ControlMaster)) {
				return false; //never keep fighting our own current owner/commander, however the target was originally set - catches the explicit "kill"/guard-duty paths (NPCAiTimer.EngageTarget/AlertGuardiansOf), which bypass NPC.ShouldAggroOn's own exclusion entirely since they're explicit orders, not autonomous aggro. A legitimate "rebel" (owner attacks pet) clears ControlMaster/FollowTarget first (see NPC.On_HostileAction), so this doesn't block genuine self-defense after that.
			}
			return Point2D.GetSimpleDistance(self, candidate) <= GiveUpRange;
		}

		private bool IsLowOnHealth(NPC self) {
			return self.MaxHits > 0 && (self.Hits <= (self.MaxHits * FleeHealthFraction));
		}

		private Character FindTarget(NPC self) {
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, AggroRange)) {
				Character candidate = ch as Character;
				if ((candidate != null) && self.ShouldAggroOn(candidate) && self.CanInteractWith(candidate).Allow) {
					return candidate;
				}
			}
			return null;
		}

		/// <summary>Internal (not private) so a test can call this directly and deterministically,
		/// same precedent as PickSpell above - real-time AI-tick timing in this sandbox is
		/// already documented elsewhere as flaky (see LineOfSightBlockingTests.cs's own doc
		/// comment), so a real-user-reported LOS/approach bug is best proven with one direct,
		/// synchronous call rather than waiting out live combat ticks.</summary>
		internal void Fight(NPC self, Character target) {
			self.Flag_WarMode = true;

			//real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_roundattack
			//(_pullall) is a periodic AoE pulse independent of this tick's normal attack, not an
			//alternative to it - see MonsterSpecialAttacks.TryRoundattack's own doc comment for
			//why checking it here (once per think-tick while actively fighting) is a faithful
			//equivalent of the real archive's own "recharge-gated, checked on nearly every
			//combat trigger" behavior.
			MonsterSpecialAttacks.TryRoundattack(self);

			//real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_detect
			//periodically force-reveals nearby hidden/invisible characters while actively
			//fighting - same "once per think-tick while fighting" treatment as roundattack.
			MonsterSpecialAttacks.TryApplyDetect(self);

			//real-user-reported follow-up - a_tevent_e_parakrik/e_npc_curse/e_npc_summoning are
			//all periodic, recharge-gated AoE/summon procs, same "once per think-tick while
			//fighting" treatment as roundattack/detect above.
			MonsterSpecialAttacks.TryApplyParaKrik(self);
			MonsterSpecialAttacks.TryApplyNpcCurse(self);
			MonsterSpecialAttacks.TryApplyNpcCurseMana(self); //#797
			MonsterSpecialAttacks.TryApplyNpcCurseStam(self); //#797
			MonsterSpecialAttacks.TryApplyWarcry(self); //#799
			MonsterSpecialAttacks.TryApplySummoning(self);

			//real-user-reported follow-up - a_tevent_e_npc_infiniteMana just keeps this NPC's
			//mana topped up every think-tick while fighting, so a caster granted it never runs
			//dry between casts.
			MonsterSpecialAttacks.TryApplyInfiniteMana(self);

			if (this.TryCastSpell(self, target)) {
				return; //cast instead of swinging this tick - don't also touch the weapon-skill queue below
			}

			int effectiveRange;
			bool hasLineOfSight;
			NPCDef def = (NPCDef) self.Def;
			//#734 - only an NPC that actually knows an OFFENSIVE spell holds spell range between
			//casts (see HasOffensiveSpell) - the old isCaster flag went true for ANY NPC with ANY
			//spell at all, so a kit that's 100% beneficial/self-targeted (heal, magic reflection -
			//ChooseSpellToCast below always redirects those to self, never the enemy) held at spell
			//range forever with no way to ever actually engage: it never had an offensive reason to
			//close in, and never got close enough to melee either. Such an NPC now falls into the
			//melee branch below and weaves its support spells in opportunistically instead (see
			//ChooseSpellToCast's per-spell scoring).
			bool isCaster = HasOffensiveSpell(def);
			if (isCaster) {
				//A spellcaster holds its distance rather than closing to melee range between
				//casts. Without this branch, effectiveRange fell through to WeaponRangeVsM/VsP
				//below, which defaults to 1 (melee) for a caster that never actually swings a
				//weapon - so on every tick the spell was on cooldown (SpellCastCooldownSeconds=4
				//vs. a 1.5s think-tick, i.e. most ticks), this method walked the caster right up
				//next to a target it had just hit from range a moment earlier. Only close in if
				//genuinely out of spell range or LOS-blocked, not after every single cast.
				effectiveRange = GetSpellCastRange(def);
				hasLineOfSight = self.CanInteractWith(target).Allow;
			} else {
				//WeaponStrikeStartRange (default 5) is only about when WeaponSkillDef transitions
				//its own Drawing->Striking phase - it's NOT the range a stroke can actually land
				//at. That real range is WeaponRangeVsM/VsP (default 1 for melee - i.e. adjacent),
				//checked by WeaponSkillDef.On_Stroke's own "distance <= range" gate. Using
				//StrikeStartRange here (as earlier versions of this method did) meant any melee
				//NPC relying on the engine's defaults - anything that, unlike the #56 archer
				//content, never got an explicit WeaponRange/StrikeStartRange pair in its .def -
				//stopped approaching 4 tiles short of actual hit range and swung forever without
				//ever connecting. Mirror On_Stroke's own formula instead, so "close enough to stop
				//walking" and "close enough to hit" always agree. (Ranged weapon users - e.g.
				//archers - already hold their own configured range the same way, via a larger
				//WeaponRangeVsM/VsP from their .def; no separate branch needed for them.)
				effectiveRange = Math.Min(target.IsPlayerForCombat ? self.WeaponRangeVsP : self.WeaponRangeVsM, self.VisionRange);
				//real-user-reported: a non-caster (melee or ranged) NPC never actually checked
				//real line-of-sight here - unlike the caster branch above, this was hardcoded
				//true unconditionally. Point2D.GetSimpleDistance below is a flat X/Y check with
				//no concept of Z, so a ranged attacker (e.g. an archer NPC) standing at a
				//different height/floor than its target - nominally "in range" by X/Y alone, but
				//with no real line of sight to actually land a shot - never re-triggered
				//approach/repositioning: WeaponSkillDef.On_Stroke's own real LOS check (which DOES
				//account for height) would just keep silently denying the shot forever, while
				//this method kept reporting "already close enough, nothing to do" every tick.
				hasLineOfSight = self.CanInteractWith(target).Allow;
			}

			int distance = Point2D.GetSimpleDistance(self, target);
			if ((distance > effectiveRange) || !hasLineOfSight) {
				Logger.WriteDebug("NPCAiTimer.Fight: " + self.Name + "#" + self.Uid + " approaching " + target.Name + " (distance=" + distance + ", effectiveRange=" + effectiveRange + ", hasLineOfSight=" + hasLineOfSight + ")");
				this.StepTowardsSmart(self, target);

				//real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_leaper
				//(user-approved simplification: a real speed boost, not teleport-assisted
				//pursuit - see LeaperAbilityDef's own doc comment) takes extra honest
				//StepTowardsSmart steps the same tick while still closing distance.
				int extraSteps = MonsterSpecialAttacks.GetLeaperExtraSteps(self);
				for (int i = 0; i < extraSteps; i++) {
					if (Point2D.GetSimpleDistance(self, target) <= effectiveRange) {
						break; //close enough now, stop taking leaper's extra steps
					}
					this.StepTowardsSmart(self, target);
				}
			}

			//AddTarget must only be called ONCE per engagement - see lastEngagedTarget's comment. The
			//skill sequence itself (WeaponSkillDef.On_Stroke) already retries on its own every second
			//until it's close enough and lands a hit; re-adding every AI tick was aborting that
			//in-progress sequence before it ever got the chance.
			//
			//real-user-reported: a ranged NPC fired a few shots then stopped attacking entirely
			//(a caster NPC in the same fight kept going fine). lastEngagedTarget alone isn't
			//enough - a transient LOS loss during On_Stroke silently deletes the whole
			//WeaponSkillTargetQueuePlugin (see IsActivelyEngaging's own doc comment), and
			//lastEngagedTarget has no way to notice that happened. Re-add whenever the plugin's
			//own state disagrees with ours, not just on a target change.
			if ((this.lastEngagedTarget != target) || !WeaponSkillTargetQueuePlugin.IsActivelyEngaging(self, target)) {
				Logger.WriteDebug("NPCAiTimer.Fight: " + self.Name + "#" + self.Uid + " handing " + target.Name + " to WeaponSkillTargetQueuePlugin (new engagement)");
				WeaponSkillTargetQueuePlugin.AddTarget(self, target);
				this.lastEngagedTarget = target;
			}
		}

		//transient, not saved - a missed cooldown window just means one skipped cast attempt after a reload, harmless.
		//#734 real-user-reported follow-up: this NPC can only ever have ONE spell "in flight"
		//at a time - like a real caster mid-channel, it cannot start casting something else
		//while still resolving/recovering from its last cast, regardless of which spell that
		//was. A per-spell cooldown dictionary (this field's own first #734 version) let a
		//self-heal interrupt an unrelated offensive spell's cooldown window, which is wrong -
		//this stays ONE shared timestamp across every spell this NPC knows, same as the
		//original pre-#734 design, so ChooseSpellToCast's utility scoring only ever decides
		//WHICH spell to use once casting is actually available again, never WHETHER two
		//different spells can overlap.
		private double nextCastTime;
		private const double SpellCastCooldownSeconds = 4;
		private const int DefaultSpellCastRange = 10;

		//#734 - ChooseSpellToCast's utility-score weights. An offensive spell's baseline sits
		//below a self-heal's max (so a caster drops everything to save itself once truly
		//hurt) but above a self-buff's flat score (so, once buffed, a caster/hybrid spends its
		//casts offensively rather than endlessly re-rolling the same buff) and above the melee
		//branch's own implicit "nothing else to do" baseline (so a melee+support NPC still
		//prefers an available heal/buff over just swinging blindly through them).
		private const double OffensiveSpellBaseScore = 0.6;
		private const double BuffSpellScore = 0.5;
		private const double OtherBeneficialSpellScore = 0.3;
		//a self-heal scores 0 (not worth the mana) once already this close to full - avoids
		//topping off a 1-point scratch every time it's off cooldown.
		private const double HealConsiderThreshold = 0.9;

		/// <summary>#716 - NPCDef.SpellCastRange's own per-monster override, falling back to
		/// DefaultSpellCastRange when unconfigured (the negative sentinel).</summary>
		internal static int GetSpellCastRange(NPCDef def) {
			return (def.SpellCastRange > 0) ? (int) def.SpellCastRange : DefaultSpellCastRange;
		}

		/// <summary>Read-only introspection for tests - how many times this NPC has committed to
		/// a spell cast (mana spent, cooldown started) via TryCastSpell. Decoupled from whether
		/// the resulting SpellDef.Trigger_EffectChar actually lands damage (which can depend on
		/// unrelated factors like a FlaggedRegion's NoHarmfulMagicIn, not a concern of this AI
		/// timer), so this is the right signal for "did the caster try to use its spell" alone.</summary>
		public int CastAttempts { get; private set; }

		/// <summary>#734 - def.Spells (addspell=, #564) and def.CombatSpell are no longer
		/// mutually exclusive pools the way PickSpell used to treat them (Spells OR CombatSpell,
		/// whichever was configured) - every spell from either one is now a real candidate for
		/// HasOffensiveSpell/ChooseSpellToCast, so a NPCDef that happens to configure both (e.g.
		/// legacy CombatSpell content plus newer addspell= entries) gets the benefit of both
		/// instead of one silently shadowing the other.</summary>
		private static IEnumerable<SpellDef> CandidateSpells(NPCDef def) {
			List<SpellDef> spells = def.Spells;
			if (spells != null) {
				for (int i = 0; i < spells.Count; i++) {
					yield return spells[i];
				}
			}
			if (def.CombatSpell != null) {
				yield return def.CombatSpell;
			}
		}

		/// <summary>#734 - true only when this NPC knows at least one genuinely offensive
		/// (SpellFlag.IsHarmful) spell. Used by Fight() above to decide whether to hold spell
		/// range at all - a kit that's 100% beneficial/self-targeted has no such spell and so no
		/// reason to ever hold range (see Fight's own comment on isCaster for the full story).</summary>
		internal static bool HasOffensiveSpell(NPCDef def) {
			foreach (SpellDef spell in CandidateSpells(def)) {
				if ((spell.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// #734 - the utility-scored heart of this NPC's spellcasting, replacing the old
		/// PickSpell's blind uniform-random choice among EVERY configured spell regardless of
		/// need. Considers every spell this NPC knows (CandidateSpells) that's actually castable
		/// right now - cooldown clear, mana available, and for an offensive spell, target in
		/// range and LOS'd (a beneficial one needs neither, same precedent as before) - and
		/// returns whichever one is most WORTH casting (see ScoreSpell), or null if nothing is
		/// both castable and worth it, same "fall back to normal combat" contract TryCastSpell's
		/// caller already expects. Internal (not private) so a test can call this directly and
		/// deterministically, same precedent as Fight/CastAttempts above - real cooldown timing
		/// makes a live-combat sampling test impractically slow for exercising more than one
		/// scoring path per test.
		/// </summary>
		internal SpellDef ChooseSpellToCast(NPC self, Character target) {
			NPCDef def = (NPCDef) self.Def;
			SpellDef best = null;
			double bestScore = 0; //0 is "not worth it" - a real candidate must score strictly above this
			foreach (SpellDef spell in CandidateSpells(def)) {
				if (!this.IsSpellReady(self, spell)) {
					continue;
				}
				bool isBeneficial = (spell.Flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial;
				if (!isBeneficial) {
					//a self-targeted buff needs neither the enemy's distance nor a clear shot at
					//it - only an offensive spell actually needs to reach the enemy.
					if (Point2D.GetSimpleDistance(self, target) > GetSpellCastRange(def)) {
						continue;
					}
					if (!self.CanInteractWith(target).Allow) {
						continue;
					}
				}
				double score = this.ScoreSpell(self, spell, isBeneficial);
				if (score > bestScore) {
					bestScore = score;
					best = spell;
				}
			}
			return best;
		}

		/// <summary>#734 - how worth casting `spell` is right now, on a roughly-comparable
		/// scale across every spell shape this engine actually has. A self-heal (HealingSpellDef)
		/// scores by how hurt `self` genuinely is - 0 (skip) above HealConsiderThreshold, ramping
		/// up to 1.0 near death, so it outweighs even an offensive cast once things get dire. A
		/// self-buff (DurableCharEffectSpellDef, e.g. Magic Reflection) scores a flat
		/// BuffSpellScore, but only while it ISN'T already active - checked via its own real
		/// EffectPluginKey_Spell (the exact plugin key this AI path always installs under, see
		/// CastSpell below), not a guess, so a caster buffs once near the start of a fight and
		/// then stops bothering, instead of wasting a cast re-rolling the same buff every time
		/// it's off cooldown. An offensive spell scores a flat OffensiveSpellBaseScore plus a
		/// small random tie-break, so a multi-offensive-spell caster varies which one it reaches
		/// for when more than one is ready at once, without ignoring readiness the way the old
		/// uniform-random PickSpell did (that picked among ALL configured spells regardless of
		/// cooldown, silently no-op'ing whenever the roll landed on one still on cooldown).</summary>
		private double ScoreSpell(NPC self, SpellDef spell, bool isBeneficial) {
			if (!isBeneficial) {
				return OffensiveSpellBaseScore + (Globals.dice.Next(100) / 1000.0);
			}

			HealingSpellDef heal = spell as HealingSpellDef;
			if (heal != null) {
				if (self.MaxHits <= 0) {
					return 0;
				}
				double hpFraction = self.Hits / (double) self.MaxHits;
				if (hpFraction >= HealConsiderThreshold) {
					return 0;
				}
				return 1.0 - hpFraction;
			}

			DurableCharEffectSpellDef buff = spell as DurableCharEffectSpellDef;
			if (buff != null) {
				if (self.GetPlugin(buff.EffectPluginKey_Spell) != null) {
					return 0; //already active - recasting now would just waste mana re-rolling it
				}
				return BuffSpellScore;
			}

			//some other beneficial spell shape with no stronger signal available - still worth
			//considering, just with no real basis to prioritize it over anything else.
			return OtherBeneficialSpellScore;
		}

		private bool IsSpellReady(NPC self, SpellDef spell) {
			if (Globals.TimeInSeconds < this.nextCastTime) {
				return false; //still "in flight"/recovering from a previous cast - ANY spell, not just this one
			}
			return self.Mana >= spell.ManaUse;
		}

		/// <summary>
		/// Casts whichever spell ChooseSpellToCast picked, bypassing SpellDef's targeting flow
		/// entirely (On_TargonChar etc are all hardcoded to Player - see #57) since an NPC picks
		/// its own target programmatically and has no client-side targeting cursor to drive that
		/// flow anyway. Returns false (does nothing) if nothing was worth/able to be cast this
		/// tick - callers should fall back to normal weapon combat in that case.
		/// </summary>
		private bool TryCastSpell(NPC self, Character target) {
			SpellDef spell = this.ChooseSpellToCast(self, target);
			if (spell == null) {
				return false;
			}

			//real-user-reported: a beneficial spell (a self/protective buff, e.g. Magic
			//Reflection - SpellFlag.IsBeneficial) was being aimed straight at the enemy target,
			//same as an offensive one. Only a harmful spell should ever be aimed at the enemy; a
			//beneficial one targets the caster itself - mirrors exactly how the real player-cast
			//flow (SpellDef.cs's own On_Start) derives this from the same flags.
			bool isBeneficial = (spell.Flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial;
			Character effectTarget = isBeneficial ? self : target;

			this.nextCastTime = Globals.TimeInSeconds + Math.Max(SpellCastCooldownSeconds, spell.CastTime);
			self.Mana -= (short) spell.ManaUse;
			this.CastAttempts++;

			//Same visible cue the normal player-cast flow plays (Magery.cs's On_Start calls this
			//right after deducting mana, before the cast-time delay) - without it, an NPC casting
			//a spell looks like it's just standing there doing nothing, since TryCastSpell bypasses
			//that whole On_Start/SkillSequenceArgs flow entirely (see this method's own doc comment).
			AnimCalculator.PerformAnim(self, GenericAnim.Cast);

			//GM-level power (700, the classic 0-1000 permille max) rather than deriving from a Magery
			//skill value - NPCs don't have meaningful skill points set up for this, and "this monster
			//is an expert at its own signature spell" is a reasonable assumption for a first pass.
			const int spellPower = 700;
			Logger.WriteDebug("NPCAiTimer.TryCastSpell: " + self.Name + "#" + self.Uid + " casts " + spell.Name + " at " + effectTarget.Name);
			//real-user-reported, a second bug found testing the fix above: EffectFlag.FromAbility
			//(the original flag here) isn't one of the two "source" flags DurableCharEffectSpellDef.
			//On_EffectChar actually recognizes (FromPotion/FromSpellBook, needed to pick which
			//EffectPluginKey to install under) - any buff/debuff-shaped spell (anything deriving
			//from DurableCharEffectSpellDef, e.g. Magic Reflection) cast via this AI path always
			//threw "Undefined source type" and silently never installed its effect plugin at all,
			//even once correctly targeted. FromSpellBook matches the same "no other real source"
			//assumption the normal player-cast flow already makes (SpellDef.cs's own On_Start).
			EffectFlag effectFlag = EffectFlag.FromSpellBook | (isBeneficial ? EffectFlag.BeneficialEffect : EffectFlag.HarmfulEffect);
			SpellEffectArgs args = SpellEffectArgs.Acquire(self, effectTarget, effectTarget, spell, spellPower, effectFlag);
			spell.Trigger_EffectChar(effectTarget, args);
			return true;
		}

		private Character fleeingFrom;
		private int fleeSteps;

		private void Flee(NPC self, Character target) {
			Logger.WriteDebug("NPCAiTimer.Flee: " + self.Name + "#" + self.Uid + " fleeing from " + target.Name +
				" (hits=" + self.Hits + "/" + self.MaxHits + ")");
			self.Flag_WarMode = false;
			WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);

			Direction towardsTarget = Point2D.GetDirFromTo(self, target);
			Direction away = (Direction) (((int) towardsTarget + 4) % 8);
			if (!this.TryStep(self, away)) {
				this.TrySidestepCharacter(self, away);
			}
		}

		private void StepTowards(NPC self, IPoint2D target, bool running = false) {
			Direction dir = Point2D.GetDirFromTo(self, target);
			if (!this.TryStep(self, dir, running)) {
				this.TrySidestepCharacter(self, dir, running);
			}
		}

		/// <summary>#767 - NPCs can't shove past characters (CharacterCollision). When the step in
		/// dir was refused only because someone stands there, try the two neighbouring directions
		/// (random order first), like Sphere's NPC_WalkToPoint sidestep. Terrain-blocked steps are
		/// left alone so wall-pacing behavior doesn't change.</summary>
		private bool TrySidestepCharacter(NPC self, Direction dir, bool running = false) {
			Map map = self.GetMap();
			int x, y, z;
			if (!map.CheckMovement(self, self.MovementSettings, dir, false, out x, out y, out z) ||
					!CharacterCollision.IsTileBlockedFor(self, map, x, y, z)) {
				return false;
			}
			int turn = (Globals.dice.Next(2) == 0) ? 1 : -1;
			return this.TryStep(self, (Direction) (((int) dir + turn + 8) % 8), running) ||
				this.TryStep(self, (Direction) (((int) dir - turn + 8) % 8), running);
		}

		/// <summary>Like StepTowards, but routes around obstacles via the real AStar pathfinder (scripts/Basic/AI/PathFinding/AStar.cs) instead of only ever trying the single direct-line direction. Used by Follow, combat approach, the "go" goal point and home-return.
		/// #769 - the path is cached (NpcPathCache) and walked step by step; it's only re-planned when a step fails, the target moved away from where it was planned to, or every few steps. A character target counts as reached when adjacent (its own tile is occupied), and a target that can't be reached still yields a partial path toward it.</summary>
		private void StepTowardsSmart(NPC self, IPoint3D target, bool running = false) {
			if (this.pathCache == null) {
				this.pathCache = new NpcPathCache();
			}
			Direction dir;
			if (this.pathCache.TryGetNextStep(self.X, self.Y, target.X, target.Y, out dir)) {
				if (this.TryStep(self, dir, running)) {
					this.pathCache.Advance(self.X, self.Y);
					return;
				}
				this.pathCache.Invalidate(); //something moved into the way since the path was planned
			}

			Map map = self.GetMap();
			AStar.Options options = new AStar.Options();
			options.StopWithin = (target is Character) ? 1 : 0;
			options.FirstStepBlocked = (x, y, z) => CharacterCollision.IsTileBlockedFor(self, map, x, y, z);
			AStar.Result result = AStar.FindPathOnMap(self, target, map, self.MovementSettings, options);
			Direction[] path = result.Path;
			if (path == null) {
				Logger.WriteDebug("NPCAiTimer.StepTowardsSmart: " + self.Name + "#" + self.Uid + " found NO path to " + target + " (" + result.Iterations + " iterations) - falling back to direct step");
				this.pathCache.Invalidate();
				this.StepTowards(self, target, running);
			} else if (path.Length == 0) {
				this.pathCache.Invalidate(); //already there
			} else {
				this.pathCache.SetPath(path, self.X, self.Y, target.X, target.Y);
				if (this.TryStep(self, path[0], running)) {
					this.pathCache.Advance(self.X, self.Y);
				} else {
					Logger.WriteDebug("NPCAiTimer.StepTowardsSmart: " + self.Name + "#" + self.Uid + " path's first step (" + path[0] + ") failed - falling back to direct step");
					this.pathCache.Invalidate();
					this.StepTowards(self, target, running);
				}
			}
		}

		private void Wander(NPC self) {
			if (!this.hasHome || (Globals.dice.Next(3) != 0)) {
				return; //don't wander every single tick - keep it leisurely
			}

			if (Point2D.GetSimpleDistance(self.X, self.Y, this.homeX, this.homeY) >= WanderLeash) {
				//too far from home - head back instead of wandering further away. #211 - route
				//around obstacles via the same AStar-backed helper Follow/Fight already use,
				//instead of a bare direct step that could get an NPC stuck pacing against a wall
				//it could easily have walked around.
				this.StepTowardsSmart(self, new Point3D(this.homeX, this.homeY, this.homeZ));
				return;
			}

			Direction dir = (Direction) Globals.dice.Next(8);
			this.TryStep(self, dir);
		}

		private bool TryStep(NPC self, Direction dir, bool running = false) {
			int x, y, z;
			if (!self.GetMap().CheckMovement(self, self.MovementSettings, dir, false, out x, out y, out z)) {
				Logger.WriteDebug("NPCAiTimer.TryStep: " + self.Name + "#" + self.Uid + " CheckMovement DENIED dir=" + dir + " from (" + self.X + "," + self.Y + "," + self.Z + ") settings=" + self.MovementSettings);
				return false;
			}
			if (self.Direction != dir) {
				self.Direction = dir;
			}
			return self.WalkRunOrFly(dir, running, false);
		}
	}
}
