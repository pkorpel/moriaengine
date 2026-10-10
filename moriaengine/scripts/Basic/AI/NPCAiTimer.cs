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
			this.target = enemy;
		}

		/// <summary>Called from Character.Trigger_HostileAction - any NPC guarding `victim` (Character.IsGuarding, protecting its FollowTarget/ControlMaster) joins the fight against `attacker`.</summary>
		public static void AlertGuardiansOf(Character victim, Character attacker) {
			if ((victim == null) || (attacker == null) || (victim == attacker)) {
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
				this.target = null;
			}

			if (this.target == null) {
				this.target = this.FindTarget(self);
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
					this.StepTowards(self, goal);
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
				Direction dir = Point2D.GetDirFromTo(self, master);
				this.TryStep(self, dir);
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

			int distance = Point2D.GetSimpleDistance(self, target);
			if (distance > self.WeaponStrikeStartRange) {
				this.StepTowards(self, target);
			}

			WeaponSkillTargetQueuePlugin.AddTarget(self, target);
		}

		private void Flee(NPC self, Character target) {
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
				return false;
			}
			if (self.Direction != dir) {
				self.Direction = dir;
			}
			return self.WalkRunOrFly(dir, false, false);
		}
	}
}
