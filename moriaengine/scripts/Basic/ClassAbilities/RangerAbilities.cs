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
		private static readonly TagKey nextCrushingArrowTK = TagKey.Acquire("_nextCrushingArrow_");
		private static readonly TagKey spreeTargetTK = TagKey.Acquire("_killingSpreeTarget_");
		private const int SpreeBaseBonus = 5;
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
				SplitShotReadyTimer timer = new SplitShotReadyTimer();
				self.AddTimer(splitShotReadyTK, timer);
				timer.DueInSeconds = settings.splitShotEffectSeconds;
				AbilityIcons.ShowSplitShot(self); //#1070
				self.SysMessage("Chystas se vystrelit rozdelenou strelu!");
				self.SetTag(nextSplitShotTK, Globals.TimeInSeconds + settings.splitShotDelaySeconds);
			}
		}

		/// <summary>[function crushingArrow]: a shot readied for 1.5 s that wears a monster's physical resists.</summary>
		[SteamFunction]
		public static void CrushingArrow(Player self) {
			Character target = WeaponSkillTargetQueuePlugin.CurrentTarget(self);
			if (!IsRanger(self)) {
				self.SysMessage("Toto muze pouzit jen ranger.");
			} else if (self.Flag_Dead) {
				self.SysMessage("Jako duch nemuzes tuto abilitu pouzit!");
			} else if (WarriorAbilities.IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (ClassAbilities.Points(self, "crushingArrow") <= 0) {
				self.RedMessage("Neumis pouzivat tuto schopnost.");
			} else if (ClassAbilities.SecondsTag(self, nextCrushingArrowTK) > Globals.TimeInSeconds) {
				self.SysMessage("Jeste nelze pouzit.");
			} else if (!IsShooting(self)) {
				self.SysMessage("Musis v ruce drzet strelnou zbran!");
			} else if (target == null) {
				self.SysMessage("Lze pouzit jen pri boji");
			} else if (Point2D.GetSimpleDistance(self, target) > 15) {
				self.SysMessage("Cil je prilis daleko!");
			} else if (!self.CanSeeLOS(target).Allow) {
				self.RedMessage("Nevidis na cil!");
			} else if (!HasAmmo(self)) {
				return;
			} else if (ClassAbilities.ReduceStamina(self, ClassAbilitySettings.instance.crushingArrowStamina)) {
				self.SysMessage("Chystas se vystrelit drtivou strelu!", ClassAbilities.ClassMessageHue);
				self.SetTag(nextCrushingArrowTK, Globals.TimeInSeconds + ClassAbilitySettings.instance.crushingArrowNextSeconds);
				self.DeleteTimer(CrushingArrowTimer.Key);
				CrushingArrowTimer timer = new CrushingArrowTimer();
				timer.target = target;
				timer.fromX = self.X;
				timer.fromY = self.Y;
				self.AddTimer(CrushingArrowTimer.Key, timer);
				timer.DueInSeconds = ClassAbilitySettings.instance.crushingArrowDelaySeconds;
				AbilityIcons.ShowCrushingArrow(self); //#1070
			}
		}

		/// <summary>f_crushingArrow. Against players Erebor has this commented out.</summary>
		internal static void FireCrushingArrow(Player self, Character target) {
			if (!IsShooting(self)) {
				self.SysMessage("Musis v ruce drzet strelnou zbran!");
			} else if (!self.CanSeeLOS(target).Allow) {
				self.RedMessage("Nevidis na cil!");
			} else if (!HasAmmo(self)) {
				return;
			} else if (Globals.dice.Next(1, 1001) > self.GetSkill(SkillName.Archery)) {
				self.SysMessage((self.IsFemale ? "Minula" : "Minul") + " jsi cil!");
			} else if (Point2D.GetSimpleDistance(self, target) > ClassAbilitySettings.instance.crushingArrowReach) {
				self.RedMessage("Cil je prilis daleko!");
			} else if (target is NPC npc) {
				int resist = npc.ExperienceReward / 10;
				if (resist >= 1000) {
					self.SysMessage("Cil zcela odolava tvemu drtivemu sipu!", ClassAbilities.ClassMessageHue);
					return;
				}
				self.SysMessage(CrushingMessage(resist), ClassAbilities.ClassMessageHue);
				int points = ClassAbilities.Points(self, "crushingArrow");
				int power = (points * 10) + ((int) DamageManager.GetWeaponSwingArgs(self, npc).attack * 3);
				power -= (power * resist) / 1000;
				CrushedResistsTimer.Apply(npc, power, ClassAbilitySettings.instance.crushingArrowBaseSeconds + (points * ClassAbilitySettings.instance.crushingArrowSecondsPerPoint));
				ShotTaken(self, npc);
			}
		}

		/// <summary>#1070 - f_archer_validateAmmo: the bow's own ammunition in the pack.</summary>
		private static bool HasAmmo(Player self) {
			if (self.WeaponProjectile == null) {
				self.SysMessage("Nemas zadne strelivo.");
				return false;
			}
			return true;
		}

		/// <summary>#1070 - f_crushingArrow_successEffect: the arrow flies, the bow sounds, and f_archer_consumeAmmoOnShot
		/// takes one.</summary>
		private static void ShotTaken(Player self, Character target) {
			Projectile ammo = self.WeaponProjectile;
			int anim = self.WeaponProjectileAnim;
			if (anim >= 0) {
				EffectFactory.EffectFromTo(self, target, anim, 10, 1, false, false);
			}
			Item hand = self.FindLayer(LayerNames.Hand2) as Item;
			self.Sound(CraftingSkillDef.IsOfType(hand, "t_weapon_bow_run") ? 563 : 564); //snd_WEAPONS_BLUNT01, snd_WEAPONS_CROSSBOW
			if (ammo != null) {
				if (ammo.Amount <= 1) {
					ammo.Delete();
				} else {
					ammo.Amount--;
				}
			}
		}

		/// <summary>#1070 - e_bleed_bolt, on a light crossbow (whose TDATA3 is jagged bolts): an Archery shot bleeds at
		/// points * 45 out of 1000, with the blow * (nastaveni_bleed_bolt * the bolt's coefficient / 1000) / 1000.
		/// In the saddle it can't, and says so now and then.</summary>
		internal static void TryBleedBolt(Player self, Character target, WeaponSwingArgs swing) {
			Weapon crossbow = self.Weapon;
			if ((crossbow == null) || !string.Equals(((WeaponDef) crossbow.Def).AmmoType, "t_weapon_bolt_jagged", StringComparison.OrdinalIgnoreCase)) {
				return;
			}
			if (self.Flag_Riding) {
				if (Globals.dice.Next(10) == 1) {
					self.SysMessage("V sedle nejsi " + (self.IsFemale ? "schopna" : "schopen") + " s touto kusi dobre manipulovat", ClassAbilities.ClassMessageHue);
				}
				return;
			}
			if (Globals.dice.Next(1000) >= ClassAbilities.Points(self, "bleed_bolt") * ClassAbilitySettings.instance.bleedBoltChancePerPoint) {
				return;
			}
			Projectile bolt = self.WeaponProjectile;
			int coefficient = (bolt == null) ? 0 : ((ProjectileDef) bolt.Def).BleedCoefficient;
			double power = (swing.DamageAfterAC * ((ClassAbilitySettings.instance.bleedBolt * coefficient) / 1000)) / 1000;
			if (power > 0) {
				SingletonScript<BleedingEffectPluginDef>.Instance.Strike(self, target, power);
			}
		}

		private static string CrushingMessage(int resist) {
			if (resist < 250) {
				return "Cil byl drtive zasazen!";
			} else if (resist < 500) {
				return "Cil byl drtive zasazen! Jen slabe odolal tvemu sipu.";
			}
			return (resist < 750) ? "Cil z casti odolava tvemu drtivemu sipu!" : "Cil znacne odolava tvemu drtivemu sipu!";
		}

		/// <summary>e_killingSpreeCharge's blow on a monster: each charge adds 5% and a point, one is used up.</summary>
		internal static void ApplySpree(Player self, Character target, WeaponSwingArgs swing) {
			SpreeTimer spree = self.GetTimer(SpreeTimer.Key) as SpreeTimer;
			if ((spree == null) || target.IsPlayer) {
				return;
			}
			swing.DamageAfterAC = (swing.DamageAfterAC * (100 + (spree.charges * (SpreeBaseBonus + ClassAbilities.Points(self, "killingSpree"))))) / 100;
			self.SetTag(spreeTargetTK, target);
			spree.Drop(self);
		}

		/// <summary>@finalBlow on a monster: a charge, and one more if the spree's own blow did it.</summary>
		internal static void OnKill(Player self, NPC victim) {
			if ((ClassAbilities.Points(self, "killingSpree") <= 0) || victim.IsPetOf(self)) {
				return;
			}
			SpreeTimer.Add(self);
			self.SysMessage((self.IsFemale ? "Dorazila" : "Dorazil") + " jsi cil. Chut zabijet ti vzrostla!", ClassAbilities.ClassMessageHue);
			if (self.GetTag(spreeTargetTK) == victim) {
				SpreeTimer.Add(self);
			}
			self.RemoveTag(spreeTargetTK);
		}

		/// <summary>The critical shot of @AfterSwing: x1.8, and what was readied goes off.</summary>
		internal static void CriticalShot(Player self, Character target, WeaponSwingArgs swing) {
			self.SysMessage("Skvely zasah!", ClassAbilities.ClassMessageHue);
			swing.DamageAfterAC = (swing.DamageAfterAC * ClassAbilitySettings.instance.criticalPermille) / 1000;
			if (self.HasTimer(splitShotReadyTK)) {
				self.DeleteTimer(splitShotReadyTK);
				AbilityIcons.HideSplitShot(self);
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

	/// <summary>i_mem_crushingArrow. Moving spoils it, unless the bow can be run with.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CrushingArrowTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_crushingArrow_");

		[SaveableData, CopyableData]
		public Character target;

		[SaveableData, CopyableData]
		public int fromX, fromY;

		[DeepCopyImplementation, LoadingInitializer]
		public CrushingArrowTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Player self = cont as Player;
			if (self != null) {
				AbilityIcons.HideCrushingArrow(self); //#1070 - f_crushingArrow_cancel
			}
			if ((self == null) || (this.target == null) || this.target.IsDeleted) {
				return;
			}
			Item hand = self.FindLayer(LayerNames.Hand2) as Item;
			if (((self.X != this.fromX) || (self.Y != this.fromY)) && !CraftingSkillDef.IsOfType(hand, "t_weapon_bow_run")) {
				self.RedMessage("Drtivy sip prerusen pohybem!");
				return;
			}
			RangerAbilities.FireCrushingArrow(self, this.target);
		}
	}

	/// <summary>reduceResist of sharp, slash, pierce and blunt (resists.scp's i_resistModifier records): every arrow is a
	/// record of a value and an end; the strongest running one is what's off the resists, and a weaker one that outlasts
	/// it takes over when it ends. A record weaker and shorter than one already there is dropped. What's invulnerable, a
	/// resist of 1000, stays so.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CrushedResistsTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_crushedResists_");

		[SaveableData, CopyableData]
		public int sharp, slashing, stabbing, blunt;

		/// <summary>"end,value;end,value", strongest first, each later one weaker and lasting longer.</summary>
		[SaveableData, CopyableData]
		public string records = "";

		[DeepCopyImplementation, LoadingInitializer]
		public CrushedResistsTimer() {
		}

		internal static void Apply(NPC npc, int power, double seconds) {
			CrushedResistsTimer timer = npc.GetTimer(Key) as CrushedResistsTimer;
			if (timer == null) {
				timer = new CrushedResistsTimer();
				npc.AddTimer(Key, timer);
			}
			List<KeyValuePair<double, int>> list = timer.Records;
			if (AddRecord(list, Globals.TimeInSeconds + seconds, power)) {
				timer.Records = list;
				timer.ApplyFirst(npc);
			}
			npc.Sound(0x3a8);
		}

		/// <summary>f_reduceResist_findPlacement and f_reduceResist_cleanInvalidRecords. False if nothing changed.</summary>
		internal static bool AddRecord(List<KeyValuePair<double, int>> list, double end, int value) {
			for (int i = 0; i < list.Count; i++) {
				KeyValuePair<double, int> cur = list[i];
				if (cur.Value <= value) {
					if (cur.Key < end) {
						list[i] = new KeyValuePair<double, int>(end, value);
						for (int j = list.Count - 1; j > i; j--) {
							if (list[j].Key < end) {
								list.RemoveAt(j);
							}
						}
						return true;
					}
					if (cur.Value == value) {
						return false;
					}
					list.Insert(i, new KeyValuePair<double, int>(end, value));
					return true;
				}
				if (cur.Key >= end) {
					return false;
				}
			}
			list.Add(new KeyValuePair<double, int>(end, value));
			return true;
		}

		private List<KeyValuePair<double, int>> Records {
			get {
				List<KeyValuePair<double, int>> list = new List<KeyValuePair<double, int>>();
				foreach (string record in (this.records ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
					string[] parts = record.Split(',');
					list.Add(new KeyValuePair<double, int>(double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
						int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)));
				}
				return list;
			}
			set {
				List<string> parts = new List<string>();
				foreach (KeyValuePair<double, int> record in value) {
					parts.Add(record.Key.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + record.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
				}
				this.records = string.Join(";", parts);
			}
		}

		/// <summary>f_resistModifier_applyCurrResist: the first record off the resists, until its end.</summary>
		private void ApplyFirst(NPC npc) {
			this.Restore(npc);
			List<KeyValuePair<double, int>> list = this.Records;
			if (list.Count == 0) {
				this.Delete();
				return;
			}
			int power = list[0].Value;
			this.sharp = (npc.ResistSharp >= 1000) ? 0 : power;
			this.slashing = (npc.ResistSlashing >= 1000) ? 0 : power;
			this.stabbing = (npc.ResistStabbing >= 1000) ? 0 : power;
			this.blunt = (npc.ResistBlunt >= 1000) ? 0 : power;
			npc.ResistSharp -= this.sharp;
			npc.ResistSlashing -= this.slashing;
			npc.ResistStabbing -= this.stabbing;
			npc.ResistBlunt -= this.blunt;
			this.DueInSeconds = Math.Max(0, list[0].Key - Globals.TimeInSeconds);
		}

		private void Restore(NPC npc) {
			npc.ResistSharp += this.sharp;
			npc.ResistSlashing += this.slashing;
			npc.ResistStabbing += this.stabbing;
			npc.ResistBlunt += this.blunt;
			this.sharp = this.slashing = this.stabbing = this.blunt = 0;
		}

		/// <summary>The record ran out: f_resistModifierTimer, the next one if any. A one-shot timer is already gone
		/// here, so the next record gets a new one.</summary>
		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC npc)) {
				return;
			}
			this.Restore(npc);
			List<KeyValuePair<double, int>> list = this.Records;
			list.RemoveAll(r => r.Key <= Globals.TimeInSeconds + 0.05);
			if (list.Count > 0) {
				CrushedResistsTimer next = new CrushedResistsTimer();
				next.Records = list;
				npc.AddTimer(Key, next);
				next.ApplyFirst(npc);
			}
		}
	}

	/// <summary>i_charge for killing spree: up to 3, gone after 10 s, one a time after 5.</summary>
	[SaveableClass, DeepCopyableClass]
	public class SpreeTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_killingSpree_");
		private const int Limit = 3;
		private const double Duration = 10, DropDuration = 5;

		[SaveableData, CopyableData]
		public int charges;

		[DeepCopyImplementation, LoadingInitializer]
		public SpreeTimer() {
		}

		internal static void Add(Player self) {
			SpreeTimer spree = self.GetTimer(Key) as SpreeTimer;
			if (spree == null) {
				spree = new SpreeTimer();
				self.AddTimer(Key, spree);
				spree.PeriodInSeconds = Duration;
			}
			spree.charges = System.Math.Min(Limit, spree.charges + 1);
			spree.DueInSeconds = Duration;
			AbilityIcons.ShowCharge(self, AbilityIcons.KillingSpree, spree.charges); //#1070
		}

		internal void Drop(Player self) {
			this.charges--;
			if (this.charges <= 0) {
				this.Delete();
			} else {
				this.DueInSeconds = DropDuration;
			}
			AbilityIcons.ShowCharge(self, AbilityIcons.KillingSpree, this.charges);
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Player self) {
				this.Drop(self);
			}
		}
	}

	/// <summary>#1070 - i_splitShot: the readied split shot, and its icon while it's there.</summary>
	[SaveableClass, DeepCopyableClass]
	public class SplitShotReadyTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public SplitShotReadyTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character self) {
				AbilityIcons.HideSplitShot(self);
			}
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
