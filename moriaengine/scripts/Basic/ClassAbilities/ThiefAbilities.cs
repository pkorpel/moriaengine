using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017, #477, #478, #481 - the thief's abilities from specabilities.scp and e_thief: the backstab
	/// from hiding, blows from behind, the throat slit and the shield strike. Ripper is the class counter.</summary>
	public static class ThiefAbilities {
		private static readonly PluginKey shieldStunKey = PluginKey.Acquire("_thiefShieldStun_");
		private static readonly PluginKey knifeStunKey = PluginKey.Acquire("_thiefKnifeStun_");
		private static readonly TagKey knifeImmuneTK = TagKey.Acquire("_kudlyImmune_");
		private const double MinHiddenSeconds = 3, StunSeconds = 2;

		internal static bool IsThief(Character ch) {
			return Professions.HasProfession(ch, Professions.Thief);
		}

		/// <summary>i_stealthbonus: when hiding began, a backstab waits 3 s of it. A thief's, on its plugin (#1433).</summary>
		internal static void NoteHidden(Character self) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief != null) {
				thief.hiddenAt = Globals.TimeInSeconds;
			}
		}

		[SteamFunction]
		public static void Ripper(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Thief)) {
				self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " zasadit mrstnou ranu.", ClassAbilities.ClassMessageHue);
			}
		}

		private static bool IsFencing(Character ch) {
			return WeaponSkillTargetQueuePlugin.GetSkillNameByWeaponType(ch.WeaponType) == SkillName.Fencing;
		}

		/// <summary>[Function backstab]: from hiding, at one adjacent, after a tenth of a second. The class list
		/// reaches it through .usehand on Erebor; here it's .backstab and a target.</summary>
		[SteamFunction]
		public static void Backstab(Player self) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				self.SysMessage("Tato funkce neni urcena pro tebe.", ClassAbilities.ClassMessageHue);
			} else if (!self.Flag_Hidden && !self.Flag_InvisByMagic) {
				self.SysMessage("Nesmis byt videt...");
			} else if (thief.stabbing) {
				self.SysMessage("Pouzivas probodnuti prilis rychle po sobe!");
			} else {
				self.Target(SingletonScript<Targ_Backstab>.Instance);
			}
		}

		internal static void StabAt(Player self, Character target) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				return; //no longer a thief when the cursor came back
			}
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage("Nevidis na cil.");
				return;
			}
			if (Point2D.GetSimpleDistance(self, target) > 1) {
				self.SysMessage("Cil je moc daleko.");
				return;
			}
			if (Math.Abs(target.Z - self.Z) > 5) {
				self.SysMessage("Nedosahnes na cil.");
				return;
			}
			self.SysMessage("Pripravujes se k bodnuti...", ClassAbilities.ClassMessageHue);
			thief.stabbing = true;
			BackstabTimer timer = new BackstabTimer();
			timer.target = target;
			self.AddTimer(BackstabTimer.Key, timer);
			timer.DueInSeconds = 0.1;
		}

		/// <summary>i_backstab's @Equip: still close and long enough hidden, 20 stamina, then points * 48 out of
		/// 1000 against players or * 35 against monsters, for x1.8 or x1.5 of a blow. It reveals either way.</summary>
		internal static void FinishStab(Player self, Character target) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				return;
			}
			thief.stabbing = false;
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage("Nevidis na cil.");
				return;
			}
			if (Point2D.GetSimpleDistance(self, target) >= 2) {
				self.SysMessage("Cil prilis daleko.");
				return;
			}
			if ((Globals.TimeInSeconds - thief.hiddenAt) < MinHiddenSeconds) {
				self.SysMessage("Nejsi dostatecne dlouho " + (self.IsFemale ? "skryta" : "skryty") + "!");
				return;
			}
			if (target == self) {
				self.SysMessage("Pokousis se probodnout " + (self.IsFemale ? "sama" : "sam") + " sebe.");
				return;
			}
			if (!ClassAbilities.ReduceStamina(self, 20)) {
				return;
			}
			int chance = ClassAbilities.Points(self, "backstab") * (target.IsPlayer ? settings.backstabChanceVsP : settings.backstabChanceVsM);
			if (chance <= Globals.dice.Next(1000)) {
				self.SysMessage("Utok se nepovedl.");
				return;
			}
			int bonus = target.IsPlayer ? settings.backstabBonusVsP : settings.backstabBonusVsM;
			double damage = (DamageManager.GetWeaponSwingArgs(self, target).DamageAfterAC * bonus) / 1000;
			HidingSkillDef.Reveal(self);
			thief.hiddenAt = 0;
			DamageManager.CauseCustomDamage(self, target, self.WeaponDamageType, damage); //#1511 - damagecust
		}

		/// <summary>f_isBackslash: the target looks the same way the thief does, give or take one direction.</summary>
		internal static bool IsBehind(Character self, Character target) {
			int toTarget = (int) Point2D.GetDirFromTo(self, target);
			int facing = (int) target.Direction & 7;
			int difference = Math.Abs(toTarget - facing);
			return (difference <= 1) || (difference == 7);
		}

		/// <summary>Permille more from behind, by points.</summary>
		internal static int FromBehindBonus(int points) {
			return (points <= 10) ? points * 10 : 100 + ((points - 10) * 12);
		}

		/// <summary>Percent more for a throat slit, by points.</summary>
		internal static int ThroatBonus(int points) {
			return (points <= 10) ? points * 4 : 40 + points;
		}

		/// <summary>e_thief's @AfterSwing with fencing on a monster: from behind, then the throat slit (the thief's
		/// @swingLanding, #1433).</summary>
		internal static void ModifyOwnBlow(ThiefProfessionPlugin thief, WeaponSwingArgs swing) {
			Player self = (Player) thief.Cont;
			Character target = swing.defender;
			if (!IsFencing(self) || target.IsPlayer) {
				return;
			}
			int behind = ClassAbilities.Points(self, "utok_do_zad");
			int distance = Point2D.GetSimpleDistance(self, target);
			if ((behind > 0) && ((distance == 1) || (distance == 2)) && IsBehind(self, target)) {
				self.SysMessage("Uder do zad!", ClassAbilities.ClassMessageHue);
				swing.DamageAfterAC += (swing.DamageAfterAC * FromBehindBonus(behind)) / 1000;
				int throat = ClassAbilities.Points(self, "podriznuti");
				if (thief.throatReady && (throat > 0)) {
					self.SysMessage("Podriznuti!", ClassAbilities.ClassMessageHue);
					swing.DamageAfterAC += (swing.DamageAfterAC * ThroatBonus(throat)) / 100;
				}
			}
		}

		/// <summary>holdsOneHand(i_war_fork): the fork's own look.</summary>
		internal static bool HoldsWarFork(Character self) {
			Item hand = self.FindLayer(LayerNames.Hand1) as Item;
			ItemDef fork = ThingDef.GetByDefname("i_war_fork") as ItemDef;
			return (hand != null) && (((fork != null) && (hand.Def.Model == fork.Model)) || (hand.Def.Model == 0x1405));
		}

		/// <summary>#1070 - the rest of e_thief's @AfterSwing: a war fork's double swing on a monster (no level needed,
		/// unlike e_war's), and any fencing blow spends a readied throat slit.</summary>
		internal static void AfterFencingBlow(ThiefProfessionPlugin thief, WeaponSwingArgs swing) {
			Player self = (Player) thief.Cont;
			Character target = swing.defender;
			if (!IsFencing(self)) {
				return;
			}
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int points = ClassAbilities.Points(self, "doubleswing");
			if (HoldsWarFork(self) && !target.IsPlayer && (points > 0) && (Globals.dice.Next(1, 1001) <= (100 + (points * 5))) &&
					(self.Stam >= settings.doubleSwingStamina)) {
				self.Stam = (short) (self.Stam - settings.doubleSwingStamina);
				self.Sound(512);
				self.Sound(566);
				self.SysMessage("Dvojity zasah!", ClassAbilities.ClassMessageHue);
				swing.DamageAfterAC = (swing.DamageAfterAC * settings.doubleSwingPermille) / 1000;
			}
			thief.throatReady = false;
		}

		/// <summary>e_thief with a war fork: a blow the counter makes special charges a round strike, up to 3. The thief's
		/// own since #1433, as e_thief was on Erebor.</summary>
		internal static void ChargeWarFork(ThiefProfessionPlugin thief, WeaponSwingArgs swing) {
			Player self = (Player) thief.Cont;
			Character target = swing.defender;
			if (!IsFencing(self) || !HoldsWarFork(self) || !ClassAbilities.CounterFires(self)) {
				return;
			}
			if (target.IsPlayer && (target.Realm == self.Realm)) {
				return; //charge_add: nothing from your own side
			}
			StrikeChargeTimer.Add(self);
			self.SysMessage("Naprazeni k rychlemu utoku!", ClassAbilities.ClassMessageHue);
		}

		/// <summary>[function strike]: the round attack the charges paid for, (20 + 5 * (charges^2 - 1))% per
		/// charge of a blow on everyone adjacent, sparing your own realm.</summary>
		[SteamFunction]
		public static void Strike(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			StrikeChargeTimer charges = self.GetTimer(StrikeChargeTimer.Key) as StrikeChargeTimer;
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (self.Flag_Dead) {
				self.SysMessage("Jako duch nemuzes tuto abilitu pouzit!");
			} else if ((thief != null) && (thief.nextStrike > Globals.TimeInSeconds)) {
				self.RedMessage("Jeste nelze pouzit.");
				thief.nextStrike = Globals.TimeInSeconds + 1;
			} else if ((thief == null) || (charges == null) || (charges.charges <= 0)) {
				self.RedMessage("Nemas pripraveny zadny uder!");
			} else if (ClassAbilities.ReduceStamina(self, settings.strikeStamina)) {
				thief.nextStrike = Globals.TimeInSeconds + 0.5;
				int count = charges.charges;
				int percent = (count * settings.strikeBase) + (((count * count) - 1) * settings.strikeBonus);
				charges.Delete();
				AbilityIcons.HideCharge(self); //#1070
				WarriorAbilities.HitAround(self, percent, true);
			}
		}

		/// <summary>f_thief_shieldStrike, after a blow lands: from the thief's plugin's @afterHit (#1459), after the shared
		/// stun blow as before; its state is the plugin's (#1433).</summary>
		internal static void TryShieldStrike(Player self, NPC target) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if ((thief == null) || !IsFencing(self) || !thief.shieldReady) {
				return;
			}
			thief.shieldReady = false;
			Item shield = self.FindLayer(LayerNames.Hand2) as Item;
			if (!CraftingSkillDef.IsOfType(shield, "t_shield")) {
				return;
			}
			double window = thief.nextShield - ((200 - (ClassAbilities.Points(self, "uderstitem") * 5)) / 10.0);
			if (window < Globals.TimeInSeconds) {
				self.SysMessage("Prilis dlouho si cekal a rana ti nevysla");
				return;
			}
			IObjectWithMaterial made = shield.Def as IObjectWithMaterial;
			Material material = (made == null) ? Material.None : made.Material;
			if ((target.ExperienceReward / 10) < WarriorAbilities.StunLimit(material, false, 0)) {
				ParalyzeEffectPluginDef.Paralyze(target, shieldStunKey, TimeSpan.FromSeconds(StunSeconds));
				self.SysMessage("Uder stitem!", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>[Function throw]: a throwing knife at whoever you fight. Against a player it takes stamina,
		/// less for good armor, and paralyzes if none is left; against a monster 50 stamina and 1 point.
		/// The knife lands at the target's feet one time in 3, to be picked up.</summary>
		private const int KnifeEffect = 9598; //myTarget.effect(0,9598,5,0,0)

		[SteamFunction("Throw")]
		public static void ThrowKnife(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			HidingSkillDef.Reveal(self);
			Item twoHanded = self.FindLayer(LayerNames.Hand2) as Item;
			if ((twoHanded != null) && !CraftingSkillDef.IsOfType(twoHanded, "t_shield")) {
				twoHanded.Cont = self.Backpack;
			}
			Character target = WeaponSkillTargetQueuePlugin.CurrentTarget(self);
			Item knife = FindKnife(self);
			if (target == null) {
				self.SysMessage("Nemas zadny cil.");
				return;
			}
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				self.SysMessage("Toto muze pouzit jen zlodej.");
				return;
			}
			if (knife == null) {
				self.SysMessage("Nemas u sebe ani jeden vrhaci nuz!");
				return;
			}
			int distance = Point2D.GetSimpleDistance(self, target);
			if (distance >= settings.throwDistance + self.WeaponRangeModifier) {
				self.SysMessage("Cil je prilis daleko!");
				return;
			}
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage("Nevidis na cil!");
				return;
			}
			int stamina = distance * settings.throwStaminaPerTile;
			if (self.Stam < stamina) {
				self.SysMessage("Nemas dost staminy!");
				return;
			}
			self.Stam = (short) (self.Stam - stamina);
			PoisonedItemPlugin poison = PoisonedItemPlugin.GetPoisonPlugin(knife); //#1070 - f_poisoning_weapon_checkPoisoningValues
			PoisonKind poisonKind = (poison == null) ? PoisonKind.AbsoluteDamage : poison.PoisonKind;
			int poisonPower = (poison == null) ? 0 : poison.PoisonPower;
			knife.Amount--;
			if (knife.Amount <= 0) {
				knife.Delete();
			}
			double previous = thief.nextThrow;
			thief.nextThrow = Globals.TimeInSeconds + settings.throwDelaySeconds;
			if (previous >= Globals.TimeInSeconds) {
				self.SysMessage((self.IsFemale ? "Pokusila" : "Pokusil") + " jsi se hodit prilis rychle a dyka ti vypadla z ruky!", ClassAbilities.ClassMessageHue);
				DropKnifeAt(self);
				return;
			}
			self.Anim(11);
			self.Sound(0x145);
			EffectFactory.EffectFromTo(self, target, KnifeEffect, 5, 0, false, false);
			if (ThiefDefence.RollsRangedReflex(target)) { //#1070 - a magic item sends it back
				target.SysMessage("Dyka byla magicky odrazena.", ClassAbilities.ClassMessageHue);
				self.Stam = (short) ((self.Stam * 4) / 5);
				if (Globals.dice.Next(3) == 2) {
					DropKnifeAt(self);
				}
				return;
			}
			if (Globals.dice.Next(3) == 2) {
				DropKnifeAt(target);
			}
			if (ThiefDefence.RollsRangedBlock(target)) {
				target.SysMessage("Dyka byla magicky zablokovana.", ClassAbilities.ClassMessageHue);
				return;
			}
			if (Globals.dice.Next(10001) >= ClassAbilities.Points(self, "throwing") * 950) {
				self.SysMessage((self.IsFemale ? "Minula" : "Minul") + " jsi cil!", ClassAbilities.ClassMessageHue);
				return;
			}
			target.Sound(target.IsFemale ? 0x325 : 0x436);
			self.SysMessage((self.IsFemale ? "Zasahla" : "Zasahl") + " jsi cil", ClassAbilities.ClassMessageHue);
			if (target.IsPlayer) {
				if (ClassAbilities.SecondsTag(target, knifeImmuneTK) >= Globals.TimeInSeconds) {
					self.SysMessage("Cil je jeste imunni vuci utoku kudlou!", ClassAbilities.ClassMessageHue);
					return;
				}
				target.SetTag(knifeImmuneTK, Globals.TimeInSeconds + settings.throwImmuneSeconds);
				if (LongRangePlugin.LongRangeDef.IsActive(self)) {
					ClassAbilities.FizzMagicTravel(target); //#1070 - i_delayer_longrange's fizzrecall
				}
				int armor = (int) target.ArmorClassVsP / 4;
				int take = (target.MaxStam * (settings.throwStaminaLossPercent - armor)) / 100;
				if (target.Stam - take > 0) {
					target.Stam = (short) (target.Stam - take);
				} else {
					target.Stam = 0;
					ParalyzeEffectPluginDef.Paralyze(target, knifeStunKey, TimeSpan.FromSeconds(0.5));
				}
			} else {
				target.Stam = (short) Math.Max(0, target.Stam - 50);
				DamageManager.CauseDamage(self, target, DamageType.Irresistable, 1);
			}
			target.Trigger_HostileAction(self);
			if (poisonPower > 0) { //#1070 - the knife's own poison, all of it, as f_poison_doPoison
				Poison.Apply(target, self, poisonPower, PoisonEffectSettings.instance.SecondsFor(poisonKind), poisonKind);
			}
		}

		private static Item FindKnife(Character self) {
			ItemDef knife = ThingDef.GetByDefname("i_kudla") as ItemDef;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == knife) {
					return (Item) item;
				}
			}
			return null;
		}

		/// <summary>throw_dropDaggerAt: a knife on the ground there, decaying in 5 minutes.</summary>
		private static void DropKnifeAt(Character at) {
			Item dropped = (Item) ((ItemDef) ThingDef.GetByDefname("i_kudla")).Create(at.X, at.Y, at.Z, at.M);
			dropped.SetAttribute(ItemAttributes.attr_decay, true);
		}

		/// <summary>[function podriznuti]. The next fencing blow spends the readied slit (e_thief's @AfterSwing).</summary>
		[SteamFunction]
		public static void Podriznuti(Player self) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				self.SysMessage("Tohle neni pro tebe");
			} else if (self.Stam < 50) {
				self.SysMessage("Tohle ted nedokazes...");
			} else if (thief.nextThroat > Globals.TimeInSeconds) {
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
			} else if (ClassAbilities.ReduceStamina(self, 45)) {
				thief.throatReady = true;
				self.SysMessage("Muzes monstrum podriznout!");
				thief.nextThroat = Globals.TimeInSeconds + 15;
			}
		}

		/// <summary>[function uderstitem].</summary>
		[SteamFunction]
		public static void UderStitem(Player self) {
			int points = ClassAbilities.Points(self, "uderstitem");
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			if (thief == null) {
				self.SysMessage("Tohle neni pro tebe");
			} else if (thief.nextShield > Globals.TimeInSeconds) {
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
			} else if (points <= 0) {
				self.SysMessage("Tohle nedokazes");
			} else if (Globals.dice.Next(1, 11) > Math.Min(points, 10)) {
				self.SysMessage("Nepodarilo se ti pripravit stit.");
			} else if (ClassAbilities.ReduceStamina(self, 30)) {
				thief.shieldReady = true;
				self.SysMessage("Muzes zasadit uder stitem!");
				thief.nextShield = Globals.TimeInSeconds + ((350 - (points * 5)) / 10.0);
			}
		}
	}

	/// <summary>i_charge for round strikes: 3.5 s, then one a time every 1.8 s.</summary>
	[SaveableClass, DeepCopyableClass]
	public class StrikeChargeTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_strikeCharge_");
		private const int Limit = 3;
		private const double Duration = 3.5, DropDuration = 1.8;

		[SaveableData, CopyableData]
		public int charges;

		[DeepCopyImplementation, LoadingInitializer]
		public StrikeChargeTimer() {
		}

		internal static void Add(Player self) {
			StrikeChargeTimer timer = self.GetTimer(Key) as StrikeChargeTimer;
			if (timer == null) {
				timer = new StrikeChargeTimer();
				self.AddTimer(Key, timer);
				timer.PeriodInSeconds = Duration;
			}
			timer.charges = Math.Min(Limit, timer.charges + 1);
			timer.DueInSeconds = Duration;
			AbilityIcons.ShowCharge(self, AbilityIcons.RoundAttack, timer.charges); //#1070
		}

		protected override void OnTimeout(TagHolder cont) {
			this.charges--;
			if (this.charges <= 0) {
				this.Delete();
			} else {
				this.DueInSeconds = DropDuration;
			}
			if (cont is Character self) {
				AbilityIcons.ShowCharge(self, AbilityIcons.RoundAttack, this.charges);
			}
		}
	}

	public class Targ_Backstab : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Koho chces probodnout?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			ThiefAbilities.StabAt(self, targetted);
			return TargetResult.Done;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class BackstabTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_backstab_");

		[SaveableData, CopyableData]
		public Character target;

		[DeepCopyImplementation, LoadingInitializer]
		public BackstabTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Player self)) {
				return;
			}
			if ((this.target != null) && !this.target.IsDeleted) {
				ThiefAbilities.FinishStab(self, this.target);
			} else {
				ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
				if (thief != null) {
					thief.stabbing = false; //#1436 - the target is gone, the thief may stab again
				}
			}
		}
	}
}
