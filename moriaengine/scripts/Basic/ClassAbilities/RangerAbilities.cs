using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the ranger's abilities: specabilities.scp and Effects/splitshot.scp. A critical shot,
	/// from .sniper's counter, is what sets off a readied knockback or split shot.</summary>
	public static class RangerAbilities {
		private const string Frozen = "You are frozen and cannot move.";
		private static readonly TagKey nextKnockbackTK = TagKey.Acquire("_nextRangeArrow_");
		private static readonly TagKey nextSplitShotTK = TagKey.Acquire("_nextSplitShot_");
		private static readonly TimerKey knockbackReadyTK = TimerKey.Acquire("_knockbackReady_");
		private static readonly TimerKey splitShotReadyTK = TimerKey.Acquire("_splitShotReady_");

		internal static bool IsRanger(Character ch) {
			return Professions.HasProfession(ch, Professions.Ranger);
		}

		internal static bool IsShooting(Character ch) {
			return (ch.WeaponType == WeaponType.Bow) || (ch.WeaponType == WeaponType.XBow);
		}

		[SteamFunction]
		public static void Sniper(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Ranger)) {
				self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " zasadit kriticky uder.", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>[Function knockback]: the next critical shots within 10 s throw the target back. It
		/// shares its cooldown tag with nothing else here, Erebor's is tag.nextrangearrow.</summary>
		[SteamFunction("Knockback")]
		public static void KnockbackAbility(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!IsRanger(self)) {
				self.SysMessage("Toto muze pouzit jen ranger.");
			} else if (WarriorAbilities.IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (!self.IsGM && (Globals.dice.Next(10001) > (ClassAbilities.Points(self, "knockback") * settings.knockbackChance))) {
				self.RedMessage("Nepovedlo se.");
				self.SetTag(nextKnockbackTK, Globals.TimeInSeconds + settings.knockbackDelaySeconds);
			} else if (ClassAbilities.SecondsTag(self, nextKnockbackTK) > Globals.TimeInSeconds) {
				self.SysMessage("Jeste nelze pouzit.");
				self.SetTag(nextKnockbackTK, Globals.TimeInSeconds + settings.knockbackDelaySeconds);
			} else if (ClassAbilities.ReduceStamina(self, 25)) {
				self.SysMessage("Ucinek tve strelby je drtivy.");
				LongRangePlugin.LongRangeDef.Deactivate(self); //src.tag(rangearrowdst,0)
				self.DeleteTimer(knockbackReadyTK);
				EffectEndTimer timer = new EffectEndTimer();
				self.AddTimer(knockbackReadyTK, timer);
				timer.DueInSeconds = settings.knockbackEffectSeconds;
				self.SetTag(nextKnockbackTK, Globals.TimeInSeconds + settings.knockbackDelaySeconds);
			}
		}

		/// <summary>[Function longrange], for everyone. Sets the weapon's range through the ability
		/// a_longrange, which also makes each blow disrupt the target's casting.</summary>
		[SteamFunction("LongRange")]
		public static void LongRangeAbility(Player self) {
			ActivableAbilityDef def = LongRangePlugin.LongRangeDef;
			if (WarriorAbilities.IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (self.Stam < ClassAbilitySettings.instance.longRangeStamina) {
				self.RedMessage("Nemas dost staminy.");
			} else {
				def.Activate(self);
				if (def.IsActive(self)) {
					self.Stam = (short) (self.Stam - ClassAbilitySettings.instance.longRangeStamina);
					self.SysMessage("Tvuj dosah je nyni vetsi.");
				}
			}
		}

		[SteamFunction]
		public static void SplitShot(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!IsRanger(self)) {
				self.RedMessage("Toto muze pouzit jen hranicar.");
			} else if (self.Flag_Dead) {
				self.RedMessage("Jako duch nemuzes pouzivat tuto schopnost.");
			} else if (WarriorAbilities.IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (ClassAbilities.Points(self, "splitshot") <= 0) {
				self.RedMessage("Neumis pouzivat tuto schopnost.");
			} else if (ClassAbilities.SecondsTag(self, nextSplitShotTK) > Globals.TimeInSeconds) {
				self.RedMessage("Jeste nelze pouzit.");
			} else if (ClassAbilities.ReduceStamina(self, settings.splitShotStamina)) {
				self.DeleteTimer(splitShotReadyTK);
				EffectEndTimer timer = new EffectEndTimer();
				self.AddTimer(splitShotReadyTK, timer);
				timer.DueInSeconds = settings.splitShotEffectSeconds;
				self.SysMessage("Chystas se vystrelit rozdelenou strelu!");
				self.SetTag(nextSplitShotTK, Globals.TimeInSeconds + settings.splitShotDelaySeconds);
			}
		}

		/// <summary>The critical shot of @AfterSwing: x1.8, and what was readied goes off.</summary>
		internal static void CriticalShot(Player self, Character target, WeaponSwingArgs swing) {
			self.SysMessage("Skvely zasah!", ClassAbilities.ClassMessageHue);
			swing.DamageAfterAC = (swing.DamageAfterAC * ClassAbilitySettings.instance.criticalPermille) / 1000;
			if (self.HasTimer(splitShotReadyTK)) {
				self.DeleteTimer(splitShotReadyTK);
				SplitTo(self, target);
			}
			if (self.HasTimer(knockbackReadyTK)) {
				Direction away = Point2D.GetDirFromTo(self, target);
				MonsterSpecialAttacks.ForceMove(target, ClassAbilitySettings.instance.knockbackRange, (x, y) => away, (x, y) => false);
			}
		}

		/// <summary>How far from the target the shot spreads: more with points and distance.</summary>
		internal static int SplitRange(int points, int distance) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int range = ((points * 100 * (((distance * 100) / settings.splitShotAoeDivisor) - 1)) / 2000) + 1;
			return (range >= settings.splitShotMaxRange) ? settings.splitShotMaxRange - 1 : range;
		}

		/// <summary>Permille of a blow at dist from the target, the second number by PvM or PvP.</summary>
		internal static int SplitPermille(int points, int damageSetting, int distFromTarget) {
			int koef = ((points * 100 * (damageSetting - 50)) / 2000) + 50;
			return (ClassAbilitySettings.instance.splitShotMaxRange - distFromTarget) * koef;
		}

		/// <summary>splitShotThem: who stands near the one that was hit takes a part of the shot.</summary>
		private static void SplitTo(Player self, Character target) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int points = ClassAbilities.Points(self, "splitshot");
			int range = SplitRange(points, Point2D.GetSimpleDistance(self, target));
			if (range <= 0) {
				self.RedMessage("Cil je prilis blizko pro utok rozdelenou strelou!");
				return;
			}
			List<Character> hit = new List<Character>();
			foreach (AbstractCharacter near in target.GetMap().GetCharsInRange(target.X, target.Y, range)) {
				if ((near is Character ch) && (ch != self) && (ch != target) && !ch.IsGM && !ch.Flag_Dead &&
						self.CanSeeLOS(ch).Allow && !IsFriendOf(self, ch)) {
					hit.Add(ch);
				}
			}
			foreach (Character victim in hit) {
				int permille = SplitPermille(points, victim.IsPlayer ? settings.splitShotDamageVsP : settings.splitShotDamageVsM, Point2D.GetSimpleDistance(victim, target));
				double damage = (DamageManager.GetWeaponSwingArgs(self, victim).DamageAfterAC * permille) / 1000;
				DamageManager.CauseDamage(self, victim, self.WeaponDamageType, damage);
			}
		}

		/// <summary>ismyfriend: the same realm, or one's own.</summary>
		private static bool IsFriendOf(Character self, Character other) {
			if ((other is NPC npc) && ((npc.ControlMaster == self) || npc.IsPetOf(self))) {
				return true;
			}
			return other.IsPlayer && (other.Realm == self.Realm);
		}
	}

	/// <summary>Holds a readied effect for its time, as Erebor's memory items do.</summary>
	[SaveableClass, DeepCopyableClass]
	public class EffectEndTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public EffectEndTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}
}
