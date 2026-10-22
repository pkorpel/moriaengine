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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>The basic idle/wander/combat/flee state an NPC's AI tick last decided on - informational only, not persisted (recomputed fresh every tick from current conditions).</summary>
	public enum AIState {
		Idle,
		Wandering,
		Fighting,
		Fleeing,
		Following,
		GoingToPoint
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
		private const int AggroRange = 8;
		private const int GiveUpRange = 12; //target got too far away (e.g. it fled or teleported) - drop it and go back to wandering
		private const double FleeHealthFraction = 0.2;
		private const int DefaultWanderLeash = 6; //max tiles from home while wandering, for any NPC not spawned via a configured Homedist (#206)

		//#590 - NPCDef.MoveRate's own baseline ("moverate=3" in the real archive, confirmed by
		//the user as the default). At this value, RefillMoveBudget below always grants exactly
		//1 move per think-tick with zero fractional carry - i.e. identical to this engine's
		//pre-existing fixed "one tile per 1.5s tick" behavior for any NPCDef that never
		//configures MoveRate at all. Zero regression for existing content.
		private const double BaselineMoveRate = 3.0;

		//Safety cap on how many tiles a single think-tick can cover, regardless of how high
		//MoveRate is configured - without this, an extreme value would let one 1.5s tick cover
		//an unbounded distance in a tight loop (StepTowardsSmart recomputes AStar every step,
		//so this is also a crude CPU-cost bound, not just a gameplay one).
		private const int MaxStepsPerTick = 4;

		private static readonly TimerKey aiTimerTK = TimerKey.Acquire("_npcAiTimer_");

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

		//#590 - transient, not saved (same precedent as nextSpellCastTime below) - a missed
		//fractional carry after a reload just means one tick's worth of sub-tile "debt" is
		//lost, harmless. moveAccumulator carries fractional step budget across ticks (so e.g.
		//MoveRate=1.5, half of baseline, nets out to one step every other tick on average);
		//movesRemainingThisTick is how many more tile-steps THIS Think() call is still allowed
		//to spend, decremented by TryStep only on an actual successful move.
		private double moveAccumulator;
		private int movesRemainingThisTick;

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

		/// <summary>Attaches the AI think-timer to this NPC if it doesn't have one yet. Safe to call repeatedly.</summary>
		public static void EnsureInstalled(NPC self) {
			if (!self.HasTimer(aiTimerTK)) {
				NPCAiTimer timer = new NPCAiTimer();
				self.AddTimer(aiTimerTK, timer);
				timer.DueInSeconds = ThinkIntervalSeconds;
				timer.PeriodInSeconds = ThinkIntervalSeconds;
			}
		}

		public static NPCAiTimer GetFor(NPC self) {
			return self.GetTimer(aiTimerTK) as NPCAiTimer;
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

			this.Think(self);
		}

		private void CaptureHome(NPC self) {
			this.homeX = self.X;
			this.homeY = self.Y;
			this.homeZ = (sbyte) self.Z;
			this.homeM = self.M;
			this.hasHome = true;
		}

		/// <summary>#590 - refills this tick's step budget from NPCDef.MoveRate. Called once per
		/// Think() call, before any state branching, so it applies uniformly whether this tick
		/// ends up fighting/fleeing/following/wandering. See BaselineMoveRate's own comment for
		/// why MoveRate=3 (or unset) always yields exactly 1, matching old behavior exactly.</summary>
		private void RefillMoveBudget(NPC self) {
			double rate = ((NPCDef) self.Def).MoveRate;
			if (rate <= 0) {
				rate = BaselineMoveRate; //defend against a bad/zero .def value permanently freezing this NPC
			}
			this.moveAccumulator += rate / BaselineMoveRate;
			if (this.moveAccumulator > MaxStepsPerTick) {
				this.moveAccumulator = MaxStepsPerTick; //cap backlog - a long-idle fast NPC shouldn't "catch up" in one burst
			}
			this.movesRemainingThisTick = (int) this.moveAccumulator;
			this.moveAccumulator -= this.movesRemainingThisTick;
		}

		private void Think(NPC self) {
			this.RefillMoveBudget(self);

			if ((this.target != null) && !this.IsValidTarget(self, this.target)) {
				Logger.WriteDebug("NPCAiTimer.Think: " + self.Name + "#" + self.Uid + " dropped target " + this.target.Name + " (no longer valid - dead, map mismatch, or out of GiveUpRange)");
				this.target = null;
			}

			if (this.target == null) {
				this.target = this.FindTarget(self);
				if (this.target != null) {
					Logger.WriteDebug("NPCAiTimer.Think: " + self.Name + "#" + self.Uid + " acquired target " + this.target.Name + " via autonomous aggro");
				}
			}

			if (this.target != null) {
				if (this.IsLowOnHealth(self)) {
					this.LastState = AIState.Fleeing;
					this.Flee(self, this.target);
				} else {
					this.LastState = AIState.Fighting;
					this.Fight(self, this.target);
				}
				return;
			}

			IPoint3D goal = self.PetGoalPoint;
			if (goal != null) {
				if (Point2D.GetSimpleDistance(self, goal) <= 1) {
					self.PetGoalPoint = null; //arrived
				} else {
					this.LastState = AIState.GoingToPoint;
					//#590 - loop while this tick's MoveRate-driven budget allows another step,
					//same shape as Follow/Fight/Wander/Flee below.
					while ((this.movesRemainingThisTick > 0) && (Point2D.GetSimpleDistance(self, goal) > 1)) {
						int beforeX = self.X, beforeY = self.Y;
						this.StepTowardsSmart(self, goal);
						if ((self.X == beforeX) && (self.Y == beforeY)) {
							break; //blocked - don't keep retrying the same tick
						}
					}
					if (Point2D.GetSimpleDistance(self, goal) <= 1) {
						self.PetGoalPoint = null; //arrived mid-tick after a multi-step burst
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
			//#590 - loop while this tick's MoveRate-driven budget allows another step AND we
			//haven't caught up yet; StepTowardsSmart recomputes a fresh path each iteration
			//(matches its own existing doc comment), and TryStep's own budget gate (not
			//re-checked here) is what actually stops a baseline (MoveRate=3) NPC after one step,
			//exactly as before.
			while ((this.movesRemainingThisTick > 0) && (Point2D.GetSimpleDistance(self, master) > 1)) {
				int beforeX = self.X, beforeY = self.Y;
				this.StepTowardsSmart(self, master);
				if ((self.X == beforeX) && (self.Y == beforeY)) {
					break; //blocked - don't keep retrying the same tick
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

		private void Fight(NPC self, Character target) {
			self.Flag_WarMode = true;

			if (this.TryCastSpell(self, target)) {
				return; //cast instead of swinging this tick - don't also touch the weapon-skill queue below
			}

			int effectiveRange;
			bool hasLineOfSight;
			NPCDef def = (NPCDef) self.Def;
			//#564 - a caster with ONLY an addspell-granted Spells list (no CombatSpell) must still
			//hold spell range rather than closing to melee, same as a CombatSpell-only caster
			bool isCaster = (def.CombatSpell != null) || ((def.Spells != null) && (def.Spells.Count > 0));
			if (isCaster) {
				//A spellcaster holds its distance rather than closing to melee range between
				//casts. Without this branch, effectiveRange fell through to WeaponRangeVsM/VsP
				//below, which defaults to 1 (melee) for a caster that never actually swings a
				//weapon - so on every tick the spell was on cooldown (SpellCastCooldownSeconds=4
				//vs. a 1.5s think-tick, i.e. most ticks), this method walked the caster right up
				//next to a target it had just hit from range a moment earlier. Only close in if
				//genuinely out of spell range or LOS-blocked, not after every single cast.
				effectiveRange = SpellCastRange;
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
				hasLineOfSight = true;
			}

			int distance = Point2D.GetSimpleDistance(self, target);
			if ((distance > effectiveRange) || !hasLineOfSight) {
				Logger.WriteDebug("NPCAiTimer.Fight: " + self.Name + "#" + self.Uid + " approaching " + target.Name + " (distance=" + distance + ", effectiveRange=" + effectiveRange + ", hasLineOfSight=" + hasLineOfSight + ")");
				//#590 - loop while budget remains and we're still out of range/LOS - lets a
				//faster-than-baseline (MoveRate > 3) NPC close distance by more than one tile in
				//a single think-tick; TryStep's own gate (not re-checked here) stops a baseline
				//NPC after exactly one step, same as before this change.
				while (this.movesRemainingThisTick > 0) {
					int beforeX = self.X, beforeY = self.Y;
					this.StepTowardsSmart(self, target);
					if ((self.X == beforeX) && (self.Y == beforeY)) {
						break; //blocked - don't keep retrying the same tick
					}
					distance = Point2D.GetSimpleDistance(self, target);
					hasLineOfSight = isCaster ? self.CanInteractWith(target).Allow : true;
					if ((distance <= effectiveRange) && hasLineOfSight) {
						break; //close enough now
					}
				}
			}

			//AddTarget must only be called ONCE per engagement - see lastEngagedTarget's comment. The
			//skill sequence itself (WeaponSkillDef.On_Stroke) already retries on its own every second
			//until it's close enough and lands a hit; re-adding every AI tick was aborting that
			//in-progress sequence before it ever got the chance.
			if (this.lastEngagedTarget != target) {
				Logger.WriteDebug("NPCAiTimer.Fight: " + self.Name + "#" + self.Uid + " handing " + target.Name + " to WeaponSkillTargetQueuePlugin (new engagement)");
				WeaponSkillTargetQueuePlugin.AddTarget(self, target);
				this.lastEngagedTarget = target;
			}
		}

		//transient, not saved - a missed cooldown window just means one skipped cast attempt after a reload, harmless
		private double nextSpellCastTime;
		private const double SpellCastCooldownSeconds = 4;
		private const int SpellCastRange = 10;

		/// <summary>Read-only introspection for tests - how many times this NPC has committed to
		/// a spell cast (mana spent, cooldown started) via TryCastSpell. Decoupled from whether
		/// the resulting SpellDef.Trigger_EffectChar actually lands damage (which can depend on
		/// unrelated factors like a FlaggedRegion's NoHarmfulMagicIn, not a concern of this AI
		/// timer), so this is the right signal for "did the caster try to use its spell" alone.</summary>
		public int CastAttempts { get; private set; }

		/// <summary>
		/// Casts NPCDef.CombatSpell directly at `target`, bypassing SpellDef's targeting flow
		/// entirely (On_TargonChar etc are all hardcoded to Player - see #57) since an NPC picks
		/// its own target programmatically and has no client-side targeting cursor to drive that
		/// flow anyway. Returns false (does nothing) if this NPC has no combat spell configured,
		/// doesn't have the mana, is still on cooldown, or the target is out of spell range -
		/// callers should fall back to normal weapon combat in that case.
		/// </summary>
		/// <summary>#564 - picks uniformly at random among def.Spells (addspell=) when that list
		/// is configured, falling back to the single def.CombatSpell otherwise. Returns null if
		/// neither is configured (this NPC has no spell at all). Internal (not private) so a
		/// test can sample the distribution directly without needing real combat timing/cooldowns
		/// - TryCastSpell's 4-second cooldown makes a 200+-sample live-combat test impractically
		/// slow, and this method is pure/deterministic-enough logic that's fine to test directly,
		/// same precedent as other pure static helpers elsewhere in this codebase
		/// (ProfessionDef.LevelToTitleTier, etc).</summary>
		internal static SpellDef PickSpell(NPCDef def) {
			List<SpellDef> spells = def.Spells;
			if ((spells != null) && (spells.Count > 0)) {
				return spells[Globals.dice.Next(spells.Count)];
			}
			return def.CombatSpell;
		}

		private bool TryCastSpell(NPC self, Character target) {
			SpellDef spell = PickSpell((NPCDef) self.Def);
			if (spell == null) {
				return false;
			}
			if (Globals.TimeInSeconds < this.nextSpellCastTime) {
				return false;
			}
			if (self.Mana < spell.ManaUse) {
				return false;
			}
			if (Point2D.GetSimpleDistance(self, target) > SpellCastRange) {
				return false;
			}
			if (!self.CanInteractWith(target).Allow) {
				return false;
			}

			this.nextSpellCastTime = Globals.TimeInSeconds + Math.Max(SpellCastCooldownSeconds, spell.CastTime);
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
			Logger.WriteDebug("NPCAiTimer.TryCastSpell: " + self.Name + "#" + self.Uid + " casts " + spell.Name + " at " + target.Name);
			SpellEffectArgs args = SpellEffectArgs.Acquire(self, target, target, spell, spellPower, EffectFlag.FromAbility | EffectFlag.HarmfulEffect);
			spell.Trigger_EffectChar(target, args);
			return true;
		}

		private void Flee(NPC self, Character target) {
			Logger.WriteDebug("NPCAiTimer.Flee: " + self.Name + "#" + self.Uid + " fleeing from " + target.Name +
				" (hits=" + self.Hits + "/" + self.MaxHits + ")");
			self.Flag_WarMode = false;
			WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);

			//#590 - loop while budget remains, recomputing "away" each step since the target
			//(and thus the opposite direction) can shift as both sides move.
			while (this.movesRemainingThisTick > 0) {
				Direction towardsTarget = Point2D.GetDirFromTo(self, target);
				Direction away = (Direction) (((int) towardsTarget + 4) % 8);
				if (!this.TryStep(self, away)) {
					break; //blocked that direction - don't keep retrying the same tick
				}
			}
		}

		private void StepTowards(NPC self, IPoint2D target) {
			Direction dir = Point2D.GetDirFromTo(self, target);
			this.TryStep(self, dir);
		}

		/// <summary>Like StepTowards, but routes around obstacles via the real AStar pathfinder (scripts/Basic/AI/PathFinding/AStar.cs) instead of only ever trying the single direct-line direction. Used by Follow ("come") and the "go" goal-point order, where the user specifically wants NPCs to navigate around basic obstacles rather than getting stuck walking into a wall. Recomputes the path fresh every tick rather than caching it - correct for a moving follow-target, and simple; worth revisiting if profiling ever shows this is too hot with many pets following at once.</summary>
		private void StepTowardsSmart(NPC self, IPoint3D target) {
			Direction[] path = AStar.GetPathFromTo(self, target, self.GetMap(), self.MovementSettings);
			if (path == null) {
				Logger.WriteDebug("NPCAiTimer.StepTowardsSmart: " + self.Name + "#" + self.Uid + " found NO path to " + target + " - falling back to direct step");
				this.StepTowards(self, target);
			} else if (path.Length == 0) {
				//already adjacent/at the target per AStar's own exact-tile check - nothing to step
			} else if (!this.TryStep(self, path[0])) {
				Logger.WriteDebug("NPCAiTimer.StepTowardsSmart: " + self.Name + "#" + self.Uid + " path's first step (" + path[0] + ") failed CheckMovement - falling back to direct step");
				//the first step didn't actually work (e.g. something else moved into the way since the path
				//was computed) - fall back to a direct attempt rather than freezing in place
				this.StepTowards(self, target);
			}
		}

		private void Wander(NPC self) {
			if (!this.hasHome || (Globals.dice.Next(3) != 0)) {
				return; //don't wander every single tick - keep it leisurely (unrelated to MoveRate - this 1-in-3 gate decides WHETHER to wander at all this tick, MoveRate only affects how far once it does)
			}

			//#590 - loop while budget remains, same shape as Follow/Fight/Flee above.
			while (this.movesRemainingThisTick > 0) {
				int beforeX = self.X, beforeY = self.Y;
				if (Point2D.GetSimpleDistance(self.X, self.Y, this.homeX, this.homeY) >= WanderLeash) {
					//too far from home - head back instead of wandering further away. #211 - route
					//around obstacles via the same AStar-backed helper Follow/Fight already use,
					//instead of a bare direct step that could get an NPC stuck pacing against a wall
					//it could easily have walked around.
					this.StepTowardsSmart(self, new Point3D(this.homeX, this.homeY, this.homeZ));
				} else {
					Direction dir = (Direction) Globals.dice.Next(8);
					this.TryStep(self, dir);
				}
				if ((self.X == beforeX) && (self.Y == beforeY)) {
					break; //blocked - don't keep retrying the same tick
				}
			}
		}

		private bool TryStep(NPC self, Direction dir) {
			//#590 - out of step budget for this tick (either MoveRate < baseline hasn't accrued
			//a full step yet, or a faster NPC already spent its whole burst earlier this same
			//tick). Doesn't consume anything - there's nothing left to consume.
			if (this.movesRemainingThisTick <= 0) {
				return false;
			}
			int x, y, z;
			if (!self.GetMap().CheckMovement(self, self.MovementSettings, dir, false, out x, out y, out z)) {
				Logger.WriteDebug("NPCAiTimer.TryStep: " + self.Name + "#" + self.Uid + " CheckMovement DENIED dir=" + dir + " from (" + self.X + "," + self.Y + "," + self.Z + ") settings=" + self.MovementSettings);
				return false;
			}
			if (self.Direction != dir) {
				self.Direction = dir;
			}
			bool moved = self.WalkRunOrFly(dir, false, false);
			if (moved) {
				this.movesRemainingThisTick--; //only a REAL move spends budget - a blocked/failed attempt above returned before this point
			}
			return moved;
		}
	}
}
