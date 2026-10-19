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

		// #454 point 3 - run/stand archery as default weapon-type behavior (RangerSettings.
		// bowMustStandStill/crossbowMustStandStill), confirmed real Erebor mechanic
		// (combat_storepos/restorepos): a stand-still weapon locks the attacker to the spot
		// they were standing in when the engagement began, snapping them back if they try to
		// walk away. Bow is free to move by default (false).
		//
		// Real finding while building this: WeaponSkillTargetQueuePlugin.AddTarget's "re-add
		// the target" restart (every successful swing, via FightCurrentTarget -> PhaseSelect
		// -> On_Start -> self.AbortSkill()) re-enters On_Start/On_Stroke's Drawing phase on
		// EVERY single swing, not just once at the start of combat - confirmed directly via a
		// temporary diagnostic, see PR history. A naive "set once when entering Striking"
		// anchor gets silently re-captured at wherever the attacker currently stands on every
		// one of those restarts, making it useless beyond the current swing. Fixed by making
		// the set idempotent (only set if not already present) and clearing it ONLY at the
		// genuine "stop attacking" call sites below (target gone/LOS lost/out of range/no
		// ammo) - NOT in the generic On_Abort, which fires on both a real stop and this
		// per-swing restart artifact and can't tell them apart.
		static TagKey standStillAnchorTK = TagKey.Acquire("_weaponStandStillAnchor_");

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
				self.RemoveTag(standStillAnchorTK);
				self.AbortSkill();
				return TriggerResult.Cancel;
			}
			if (!self.CanInteractWith(target).Allow) {
				WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
				CombatMessages.Send(self, "[Combat] You lose sight of " + target.Name + ". You stop attacking.");
				self.RemoveTag(standStillAnchorTK);
				self.AbortSkill();
				return TriggerResult.Cancel;
			}

			// Snap back BEFORE computing distance below, so every range check this tick uses
			// the corrected (anchored) position, not wherever the attacker just walked to.
			Point4D standStillAnchor = (Point4D) self.GetTag(standStillAnchorTK);
			if ((standStillAnchor != null) && !standStillAnchor.Equals(self)) {
				self.P(standStillAnchor);
				CombatMessages.Send(self, "[Combat] You must stand still to use this weapon.");
			}

			int distance = Point2D.GetSimpleDistance(self, target);
			WeaponSkillPhase phase = (WeaponSkillPhase) Convert.ToInt64(skillSeqArgs.Param1);
			if (phase == WeaponSkillPhase.Drawing) {
				if (distance <= self.WeaponStrikeStartRange) {
					skillSeqArgs.Param1 = WeaponSkillPhase.Striking;
					// Idempotent - a restarted swing re-enters Drawing->Striking every single
					// time (see this class's own doc comment above), so only capture a fresh
					// anchor when one isn't already tracking this ongoing engagement.
					if (RequiresStandingStill(self) && (self.GetTag(standStillAnchorTK) == null)) {
						self.SetTag(standStillAnchorTK, self.P());
					}
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
					self.RemoveTag(standStillAnchorTK);
					self.AbortSkill();
				} else if (((TimeSpan) skillSeqArgs.Param2) <= Globals.TimeAsSpan) {
					int range = Math.Min(target.IsPlayerForCombat ? self.WeaponRangeVsP : self.WeaponRangeVsM, self.VisionRange);
					if (distance <= range) {
						Projectile projectile = self.WeaponProjectile;
						if (projectile != null) {
							switch (Globals.dice.Next(3)) {
								case 0://arrow appears on ground
									Projectile onGround = (Projectile) projectile.Dupe();
									onGround.P(target);
									onGround.Amount = 1;
									break;
								case 1://arrow appears in targets backpack
									Projectile inPack = (Projectile) projectile.Dupe();
									inPack.Cont = target.Backpack;
									inPack.Amount = 1;
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
							self.RemoveTag(standStillAnchorTK);
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

						skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

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
				// Stand-still anchor (RangerSettings) is deliberately NOT cleared here - On_Abort
				// fires on every per-swing restart too (see this class's own top-of-file comment),
				// not just a genuine stop. It's cleared directly at the real stop call sites in
				// On_Stroke instead (target gone/LOS lost/out of range/no ammo).
			}
		}
	}

	public class WeaponSkillDefLoc : CompiledLocStringCollection<WeaponSkillDefLoc> {
		internal string YouHaveNoAmmo = "Nem� st�elivo.";
	}
}