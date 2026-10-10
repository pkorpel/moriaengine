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
		private const int WanderLeash = 6; //max tiles from home while wandering

		private static readonly TimerKey aiTimerTK = TimerKey.Acquire("_npcAiTimer_");

		[SaveableData] public int homeX;
		[SaveableData] public int homeY;
		[SaveableData] public sbyte homeZ;
		[SaveableData] public byte homeM;
		[SaveableData] public bool hasHome;

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

		public AIState LastState { get; private set; }

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

		private void Think(NPC self) {
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
					this.StepTowardsSmart(self, goal);
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
			if (Point2D.GetSimpleDistance(self, master) > 1) {
				this.StepTowardsSmart(self, master);
			}
		}

		private bool IsValidTarget(NPC self, Character candidate) {
			if (!candidate.IsAliveAndValid || (candidate.M != self.M)) {
				return false;
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

			//WeaponStrikeStartRange (default 5) is only about when WeaponSkillDef transitions its
			//own Drawing->Striking phase - it's NOT the range a stroke can actually land at. That
			//real range is WeaponRangeVsM/VsP (default 1 for melee - i.e. adjacent), checked by
			//WeaponSkillDef.On_Stroke's own "distance <= range" gate. Using StrikeStartRange here
			//(as earlier versions of this method did) meant any melee NPC relying on the engine's
			//defaults - anything that, unlike the #56 archer content, never got an explicit
			//WeaponRange/StrikeStartRange pair in its .def - stopped approaching 4 tiles short of
			//actual hit range and swung forever without ever connecting. Mirror On_Stroke's own
			//formula instead, so "close enough to stop walking" and "close enough to hit" always
			//agree.
			int effectiveRange = Math.Min(target.IsPlayerForCombat ? self.WeaponRangeVsP : self.WeaponRangeVsM, self.VisionRange);
			int distance = Point2D.GetSimpleDistance(self, target);
			if (distance > effectiveRange) {
				Logger.WriteDebug("NPCAiTimer.Fight: " + self.Name + "#" + self.Uid + " approaching " + target.Name + " (distance=" + distance + ", effectiveRange=" + effectiveRange + ")");
				this.StepTowardsSmart(self, target);
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

		/// <summary>
		/// Casts NPCDef.CombatSpell directly at `target`, bypassing SpellDef's targeting flow
		/// entirely (On_TargonChar etc are all hardcoded to Player - see #57) since an NPC picks
		/// its own target programmatically and has no client-side targeting cursor to drive that
		/// flow anyway. Returns false (does nothing) if this NPC has no combat spell configured,
		/// doesn't have the mana, is still on cooldown, or the target is out of spell range -
		/// callers should fall back to normal weapon combat in that case.
		/// </summary>
		private bool TryCastSpell(NPC self, Character target) {
			SpellDef spell = ((NPCDef) self.Def).CombatSpell;
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

			Direction towardsTarget = Point2D.GetDirFromTo(self, target);
			Direction away = (Direction) (((int) towardsTarget + 4) % 8);
			this.TryStep(self, away);
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
				return; //don't wander every single tick - keep it leisurely
			}

			if (Point2D.GetSimpleDistance(self.X, self.Y, this.homeX, this.homeY) >= WanderLeash) {
				//too far from home - head back instead of wandering further away
				Direction homeDir = Point2D.GetDirFromTo(self, new Point2D(this.homeX, this.homeY));
				this.TryStep(self, homeDir);
				return;
			}

			Direction dir = (Direction) Globals.dice.Next(8);
			this.TryStep(self, dir);
		}

		private bool TryStep(NPC self, Direction dir) {
			int x, y, z;
			if (!self.GetMap().CheckMovement(self, self.MovementSettings, dir, false, out x, out y, out z)) {
				Logger.WriteDebug("NPCAiTimer.TryStep: " + self.Name + "#" + self.Uid + " CheckMovement DENIED dir=" + dir + " from (" + self.X + "," + self.Y + "," + self.Z + ") settings=" + self.MovementSettings);
				return false;
			}
			if (self.Direction != dir) {
				self.Direction = dir;
			}
			return self.WalkRunOrFly(dir, false, false);
		}
	}
}
