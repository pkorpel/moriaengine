using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017, #477, #478, #481 - the thief's abilities from specabilities.scp and e_thief: the backstab
	/// from hiding, blows from behind, the throat slit and the shield strike. Ripper is the class counter.</summary>
	public static class ThiefAbilities {
		private static readonly TagKey hiddenAtTK = TagKey.Acquire("_hiddenAt_");
		private static readonly TagKey stabbingTK = TagKey.Acquire("_isStabbing_");
		private static readonly TagKey throatReadyTK = TagKey.Acquire("_podriznuti_");
		private static readonly TagKey nextThroatTK = TagKey.Acquire("_podriznutiNext_");
		private static readonly TagKey shieldReadyTK = TagKey.Acquire("_shieldZmrazeni_");
		private static readonly TagKey nextShieldTK = TagKey.Acquire("_uderstitemNext_");
		private static readonly PluginKey shieldStunKey = PluginKey.Acquire("_thiefShieldStun_");
		private const double MinHiddenSeconds = 3, StunSeconds = 2;

		internal static bool IsThief(Character ch) {
			return Professions.HasProfession(ch, Professions.Thief);
		}

		/// <summary>i_stealthbonus: when hiding began, a backstab waits 3 s of it.</summary>
		internal static void NoteHidden(Character self) {
			self.SetTag(hiddenAtTK, Globals.TimeInSeconds);
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
			if (!IsThief(self)) {
				self.SysMessage("Tato funkce neni urcena pro tebe.", ClassAbilities.ClassMessageHue);
			} else if (!self.Flag_Hidden && !self.Flag_InvisByMagic) {
				self.SysMessage("Nesmis byt videt...");
			} else if (self.GetTag(stabbingTK) != null) {
				self.SysMessage("Pouzivas probodnuti prilis rychle po sobe!");
			} else {
				self.Target(SingletonScript<Targ_Backstab>.Instance);
			}
		}

		internal static void StabAt(Player self, Character target) {
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
			self.SetTag(stabbingTK, 1);
			BackstabTimer timer = new BackstabTimer();
			timer.target = target;
			self.AddTimer(BackstabTimer.Key, timer);
			timer.DueInSeconds = 0.1;
		}

		/// <summary>i_backstab's @Equip: still close and long enough hidden, 20 stamina, then points * 48 out of
		/// 1000 against players or * 35 against monsters, for x1.8 or x1.5 of a blow. It reveals either way.</summary>
		internal static void FinishStab(Player self, Character target) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			self.RemoveTag(stabbingTK);
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage("Nevidis na cil.");
				return;
			}
			if (Point2D.GetSimpleDistance(self, target) >= 2) {
				self.SysMessage("Cil prilis daleko.");
				return;
			}
			if ((Globals.TimeInSeconds - ClassAbilities.SecondsTag(self, hiddenAtTK)) < MinHiddenSeconds) {
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
			self.RemoveTag(hiddenAtTK);
			DamageManager.CauseDamage(self, target, self.WeaponDamageType, damage);
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

		/// <summary>e_thief's @AfterSwing with fencing on a monster: from behind, then the shield strike.</summary>
		internal static void ModifyOwnBlow(Player self, Character target, WeaponSwingArgs swing) {
			if (!IsFencing(self) || target.IsPlayer) {
				return;
			}
			int behind = ClassAbilities.Points(self, "utok_do_zad");
			int distance = Point2D.GetSimpleDistance(self, target);
			if ((behind > 0) && ((distance == 1) || (distance == 2)) && IsBehind(self, target)) {
				self.SysMessage("Uder do zad!", ClassAbilities.ClassMessageHue);
				swing.DamageAfterAC += (swing.DamageAfterAC * FromBehindBonus(behind)) / 1000;
				int throat = ClassAbilities.Points(self, "podriznuti");
				if ((self.GetTag(throatReadyTK) != null) && (throat > 0)) {
					self.SysMessage("Podriznuti!", ClassAbilities.ClassMessageHue);
					swing.DamageAfterAC += (swing.DamageAfterAC * ThroatBonus(throat)) / 100;
				}
			}
		}

		/// <summary>f_thief_shieldStrike, after a blow lands.</summary>
		internal static void TryShieldStrike(Player self, NPC target) {
			if (!IsFencing(self) || (self.GetTag(shieldReadyTK) == null)) {
				return;
			}
			self.RemoveTag(shieldReadyTK);
			Item shield = self.FindLayer(LayerNames.Hand2) as Item;
			if (!CraftingSkillDef.IsOfType(shield, "t_shield")) {
				return;
			}
			double window = ClassAbilities.SecondsTag(self, nextShieldTK) - ((200 - (ClassAbilities.Points(self, "uderstitem") * 5)) / 10.0);
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

		/// <summary>[function podriznuti]. Erebor never clears the readied slit, so once used it stays ready;
		/// kept as it is.</summary>
		[SteamFunction]
		public static void Podriznuti(Player self) {
			if (!IsThief(self)) {
				self.SysMessage("Tohle neni pro tebe");
			} else if (self.Stam < 50) {
				self.SysMessage("Tohle ted nedokazes...");
			} else if (ClassAbilities.SecondsTag(self, nextThroatTK) > Globals.TimeInSeconds) {
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
			} else if (ClassAbilities.ReduceStamina(self, 45)) {
				self.SetTag(throatReadyTK, 1);
				self.SysMessage("Muzes monstrum podriznout!");
				self.SetTag(nextThroatTK, Globals.TimeInSeconds + 15);
			}
		}

		/// <summary>[function uderstitem].</summary>
		[SteamFunction]
		public static void UderStitem(Player self) {
			int points = ClassAbilities.Points(self, "uderstitem");
			if (!IsThief(self)) {
				self.SysMessage("Tohle neni pro tebe");
			} else if (ClassAbilities.SecondsTag(self, nextShieldTK) > Globals.TimeInSeconds) {
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
			} else if (points <= 0) {
				self.SysMessage("Tohle nedokazes");
			} else if (Globals.dice.Next(1, 11) > Math.Min(points, 10)) {
				self.SysMessage("Nepodarilo se ti pripravit stit.");
			} else if (ClassAbilities.ReduceStamina(self, 30)) {
				self.SetTag(shieldReadyTK, 1);
				self.SysMessage("Muzes zasadit uder stitem!");
				self.SetTag(nextShieldTK, Globals.TimeInSeconds + ((350 - (points * 5)) / 10.0));
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
			if ((cont is Player self) && (this.target != null) && !this.target.IsDeleted) {
				ThiefAbilities.FinishStab(self, this.target);
			}
		}
	}
}
