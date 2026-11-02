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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class WeaponSkillDef : SkillDef {
		//param1 = WeaponSkillPhase
		//param2 = duein TimeSpan

		public enum WeaponSkillPhase {
			Drawing,//first phase, the target must get in under startsrike range
			Striking//second phase, can strike after delay if target close <= range
		}

		public class WeaponSkillParam {
			public WeaponSkillPhase phase;
			public double dueAt;

			public WeaponSkillParam(WeaponSkillPhase phase, double dueAt) {
				this.phase = phase;
				this.dueAt = dueAt;
			}
		}

		public WeaponSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.Target1 == null || skillSeqArgs.Target1 == skillSeqArgs.Self) {
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		// Testing combat messages (CombatSettings.showCombatMessages): FightCurrentTarget
		// re-runs PhaseSelect -> On_Start on literally every successful swing (that's how the
		// attack "keeps going" - see WeaponSkillTargetQueuePlugin.FightCurrentTarget), not just
		// on a genuinely new engagement. Without this dedup tag, "You start attacking X" would
		// print on every single hit instead of once per engagement.
		static TagKey lastAttackedTargetTK = TagKey.Acquire("_combatMessagesLastTarget_");

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;
			self.AbortSkill(); //abort previous skill

			WeaponSkillTargetTrackerPlugin.InstallTargetTracker(target, self);

			if (!ReferenceEquals(self.GetTag(lastAttackedTargetTK), target)) {
				self.SetTag(lastAttackedTargetTK, target);
				CombatMessages.Send(self, "[Combat] You start attacking " + target.Name + ".");
			}

			skillSeqArgs.Param1 = WeaponSkillPhase.Drawing;
			skillSeqArgs.DelayInSeconds = 0;
			skillSeqArgs.DelayStroke();

			return TriggerResult.Cancel; //cancel because we're not using the skill's delay just yet
		}

		static TimerKey animTk = TimerKey.Acquire("_weaponAnimDelay_");

		// Throttles the Drawing/Striking "still too far" retry messages below (the loop that
		// emits them can tick every second while the attacker is walking into range) to avoid
		// spamming the client once per second.
		static TagKey tooFarMessageTimeTK = TagKey.Acquire("_combatMessagesTooFarTime_");
		const double tooFarMessageThrottleSeconds = 3;

		static void SendThrottledTooFarMessage(Character self, string message) {
			if (!CombatSettings.instance.showCombatMessages) {
				return;
			}
			object lastShown = self.GetTag(tooFarMessageTimeTK);
			TimeSpan now = Globals.TimeAsSpan;
			if ((lastShown == null) || ((now - (TimeSpan) lastShown).TotalSeconds >= tooFarMessageThrottleSeconds)) {
				self.SetTag(tooFarMessageTimeTK, now);
				self.SysMessage(message);
			}
		}

		// #512 - separate tag/throttle from the "too far" one above so the two messages (which
		// can both legitimately apply close together - e.g. moving away while already out of
		// strike-stop range) don't suppress each other.
		static TagKey standStillMessageTimeTK = TagKey.Acquire("_combatMessagesStandStillTime_");
		const double standStillMessageThrottleSeconds = 3;

		static void SendThrottledStandStillMessage(Character self, string message) {
			if (!CombatSettings.instance.showCombatMessages) {
				return;
			}
			object lastShown = self.GetTag(standStillMessageTimeTK);
			TimeSpan now = Globals.TimeAsSpan;
			if ((lastShown == null) || ((now - (TimeSpan) lastShown).TotalSeconds >= standStillMessageThrottleSeconds)) {
				self.SetTag(standStillMessageTimeTK, now);
				self.SysMessage(message);
			}
		}

		// #454 point 3 / reworked per #512 - run/stand archery as default weapon-type behavior
		// (RangerSettings.bowMustStandStill/crossbowMustStandStill), confirmed real Erebor
		// mechanic. Originally ported as a hard position-snap-back ("combat_storepos/
		// restorepos"), which the user asked to rework after testing (#512/#513) - moving while
		// using a stand-still weapon used to teleport the attacker back to a fixed point
		// instead of just failing the attack. Reworked to check Character.Flag_Moving fresh on
		// every stroke instead of tracking a fixed anchor position at all: moving simply skips
		// this stroke (no hit, no teleport, same "retry in 1s" path already used for the
		// Drawing-phase "too far" case below); standing still on a later stroke lets it proceed
		// normally. No per-engagement state to set/clear/leak across restarts anymore - simpler
		// than the anchor approach and symmetric in both directions by construction.
		static bool RequiresStandingStill(Character self) {
			switch (self.WeaponType) {
				case WeaponType.Bow:
					return RangerSettings.instance.bowMustStandStill;
				case WeaponType.XBow:
					return RangerSettings.instance.crossbowMustStandStill;
				default:
					return false;
			}
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;
			//A killing blow's own On_Stroke (further below, success branch) re-adds this target
			//via AddTarget before its damage is actually applied (On_Success runs only after
			//this method returns) - when that blow is lethal, AddTarget's restart (On_Start ->
			//DelayStroke with a 0s delay) can re-enter this exact method again in the same tick,
			//by which point the target is already deleted (non-player CauseDeath deletes
			//immediately). CanInteractWith/CanSeeLOS can't be called on a deleted Thing, so
			//check first rather than let that throw.
			if (target.IsDeleted) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				CombatMessages.Send(self, "[Combat] Your target is gone. You stop attacking.");
				self.AbortSkill();
				return TriggerResult.Cancel;
			}
			if (!self.CanInteractWith(target).Allow) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				CombatMessages.Send(self, "[Combat] You lose sight of " + target.Name + ". You stop attacking.");
				self.AbortSkill();
				return TriggerResult.Cancel;
			}

			// #512 - a stand-still weapon just fails this stroke while the attacker is moving
			// (no teleport) - falls through to the same "retry in 1s" tail this method already
			// uses for the Drawing-phase "too far" case, so the next stroke re-checks fresh.
			if (RequiresStandingStill(self) && self.Flag_Moving) {
				SendThrottledStandStillMessage(self, "[Combat] You must stand still to use this weapon.");
				skillSeqArgs.DelaySpan = TimeSpan.FromSeconds(1);
				skillSeqArgs.DelayStroke();
				return TriggerResult.Cancel;
			}

			int distance = Point2D.GetSimpleDistance(self, target);
			WeaponSkillPhase phase = (WeaponSkillPhase) Convert.ToInt64(skillSeqArgs.Param1);
			if (phase == WeaponSkillPhase.Drawing) {
				if (distance <= self.WeaponStrikeStartRange) {
					if ((self is Player) && !self.IsGM) { //#976 - Erebor's @hittry
						PlayerHunger.HurtIfStarving(self);
						if (!WorkLight.CheckWithMessage(self)) {
							skillSeqArgs.DelaySpan = TimeSpan.FromSeconds(1);
							skillSeqArgs.DelayStroke();
							return TriggerResult.Cancel;
						}
					}
					skillSeqArgs.Param1 = WeaponSkillPhase.Striking;
					TimeSpan delay = self.WeaponDelay;
					skillSeqArgs.Param2 = Globals.TimeAsSpan + delay;
					skillSeqArgs.DelaySpan = delay;
					skillSeqArgs.DelayStroke();
					self.AddTimer(animTk, new WeaponAnimTimer()).DueInSeconds = delay.TotalSeconds / 2;
					return TriggerResult.Cancel;
				}
				SendThrottledTooFarMessage(self, "[Combat] You need to get closer to " + target.Name + " to attack.");
			} else {
				if (distance > self.WeaponStrikeStopRange) {
					CombatMessages.Send(self, "[Combat] " + target.Name + " is too far away. You stop attacking.");
					self.AbortSkill();
				} else if (((TimeSpan) skillSeqArgs.Param2) <= Globals.TimeAsSpan) {
					int range = Math.Min(target.IsPlayerForCombat ? self.WeaponRangeVsP : self.WeaponRangeVsM, self.VisionRange);
					if (distance <= range) {
						Projectile projectile = self.WeaponProjectile;
						if (projectile != null) {
							switch (Globals.dice.Next(3)) {
								case 0://arrow appears on ground
									Projectile onGround = (Projectile) projectile.Dupe();
									onGround.Amount = 1;
									onGround.P(target);
									break;
								case 1://arrow appears in targets backpack
									//#709 - real-user-reported: a ranged NPC fired a few shots then
									//stopped attacking entirely. Root cause: Amount was set AFTER
									//Cont, but the Cont setter auto-merges a stackable item into an
									//already-matching stack in the destination container
									//(AbstractItem.TryStackToAnyInside/Trigger_StackInCont) and
									//DELETES the incoming item as part of that merge
									//(toStack.InternalDelete()) - #667 made arrows/bolts/scrolls
									//stackable, so once the target's backpack already held one
									//dropped arrow from an earlier shot, every subsequent shot's
									//dupe got silently deleted mid-merge, then crashed on the very
									//next line trying to set Amount on the now-deleted object -
									//aborting the whole stroke uncaught and leaving the NPC's combat
									//sequence stuck. Setting Amount first means a merge (if any)
									//operates on the already-correct value, and nothing after this
									//line touches the (possibly now-deleted) reference.
									Projectile inPack = (Projectile) projectile.Dupe();
									inPack.Amount = 1;
									inPack.Cont = target.Backpack;
									break;
								//else arrow disappears
							}
							int amount = projectile.Amount;
							if (amount < 2) {
								projectile.Delete();
							} else {
								projectile.Amount = amount - 1;
							}
						} else if (self.WeaponProjectileType != ProjectileType.None) {
							self.SysMessage(Loc<WeaponSkillDefLoc>.Get(self.Language).YouHaveNoAmmo);
							self.AbortSkill();
							return TriggerResult.Cancel;
						}

						if (!self.Flag_Moving) {
							self.Direction = Point2D.GetDirFromTo(self, target);
						}

						int projectileAnim = self.WeaponProjectileAnim;
						if (projectileAnim >= 0) {
							EffectFactory.EffectFromTo(self, target, projectileAnim, 10, 1, false, false, 0, 0);
						}

						skillSeqArgs.Success = this.RollHit(self, target);

						if (self.CanInteractWith(target).Allow) {
							WeaponSkillTargetQueuePlugin.AddTarget(self, target);//we're not really adding the target, just restarting the attack, most probably
						} else {
							WeaponSkillTargetQueuePlugin.FightCurrentTarget(self);
						}

						return TriggerResult.Continue;
					}
					SendThrottledTooFarMessage(self, "[Combat] " + target.Name + " is still too far to hit.");
				}
			}

			skillSeqArgs.DelaySpan = TimeSpan.FromSeconds(1);
			skillSeqArgs.DelayStroke();//we keep stroking, this needs to be the current skill...
			return TriggerResult.Cancel;
		}

		[ViewableClass]
		[SaveableClass]
		[DeepCopyableClass]
		public class WeaponAnimTimer : BoundTimer {
			[LoadingInitializer]
			[DeepCopyImplementation]
			public WeaponAnimTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				AnimCalculator.PerformAttackAnim((Character) cont);
			}
		}

		/// <summary>#860 - Sphere's Calc_CombatChanceToHit plus its skill check. A difficulty of 0
		/// skips the check, the swing just hits.</summary>
		private bool RollHit(Character self, Character target) {
			NPC npc = self as NPC;
			if ((npc != null) && (npc.AggroMode == AggroMode.GuardsRealm)) {
				return true; //GUARDSINSTANTKILL=1 in Erebor's sphere.ini
			}
			int difficulty;
			if (ParalyzeEffectPluginDef.IsParalyzed(target)) {
				difficulty = Globals.dice.Next(10);
			} else {
				bool targetShootsBack = (target.CurrentSkillName == SkillName.Archery) && (this.Id != (int) SkillName.Archery);
				int limit = CombatCalculator.CalculateHitDifficultyLimit(this.SkillValueOfChar(self), SkillDef.SkillValueOfChar(self, SkillName.Tactics),
					SkillDef.SkillValueOfChar(target, SkillName.Tactics), target.Stam, targetShootsBack);
				difficulty = (limit > 0) ? Globals.dice.Next(limit) : 0;
			}
			return (difficulty == 0) || this.CheckSuccess(self, difficulty * 10);
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;

			DamageManager.ProcessSwing(self, target);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;

			//Same re-entrancy case as On_Stroke's IsDeleted guard above - a restarted sequence
			//for a target that died from the stroke which triggered the restart.
			if (target.IsDeleted) {
				return;
			}

			target.Trigger_HostileAction(self);
			SoundCalculator.PlayMissSound(self);
			CombatMessages.Send(self, "[Combat] You miss " + target.Name + ".");
			CombatMessages.Send(target, "[Combat] " + self.Name + " misses you.");
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			//skillSeqArgs.Self.SysMessage("Aborting skill:" + skillSeqArgs.SkillDef.Key);
			Character target = (Character) skillSeqArgs.Target1;
			if (target != null) {
				Character self = skillSeqArgs.Self;
				//WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				WeaponSkillTargetTrackerPlugin.UnInstallTargetTracker(target, self);
				// Testing combat messages: let "You start attacking X" fire again next time,
				// now that this engagement has genuinely ended (see On_Start's own comment).
				self.RemoveTag(lastAttackedTargetTK);
			}
		}
	}

	public class WeaponSkillDefLoc : CompiledLocStringCollection<WeaponSkillDefLoc> {
		internal string YouHaveNoAmmo = "Nema strelivo.";
	}
}