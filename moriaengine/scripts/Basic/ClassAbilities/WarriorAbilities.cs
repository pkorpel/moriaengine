using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the warrior's abilities from specabilities.scp, new_events.scp's e_stunattack and
	/// sphereevents.scp's @AfterSwing. Cooldowns are Erebor's serv.time tags, in seconds.</summary>
	public static class WarriorAbilities {
		private const string OnlyWarrior = "Toto muze pouzit jen valecnik.";
		private const string Frozen = "You are frozen and cannot move.";
		private const double StunSeconds = 2;

		private static readonly TagKey nextRoundAttackTK = TagKey.Acquire("_nextRoundAttack_");
		private static readonly TagKey nextLeapTK = TagKey.Acquire("_nextLeap_");
		private static readonly TagKey nextCryTK = TagKey.Acquire("_nextCry_");
		private static readonly TagKey nextStunTK = TagKey.Acquire("_omraceniNext_");
		private static readonly TagKey stunReadyTK = TagKey.Acquire("_zmrazeni_");
		private static readonly TagKey defendingTK = TagKey.Acquire("_defending_");
		private static readonly TagKey nextSingleTauntTK = TagKey.Acquire("_nextTauntSingle_");
		private static readonly TagKey nextMassTauntTK = TagKey.Acquire("_nextTauntMass_");
		internal static readonly TimerKey roundAttackTK = TimerKey.Acquire("_roundAttack_");
		private static readonly PluginKey stunKey = PluginKey.Acquire("_warriorStun_");

		internal static bool IsWarrior(Character ch) {
			return Professions.HasProfession(ch, Professions.Warrior);
		}

		internal static bool IsFrozen(Character ch) {
			return ch.Flag_Dead || ParalyzeEffectPluginDef.IsParalyzed(ch);
		}

		[SteamFunction]
		public static void Berserker(Player self) {
			if (ClassAbilities.ToggleCounter(self, Professions.Warrior)) {
				self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " zasadit kriticky uder.", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>[Function roundattack]. It hits 0.2 s or so after the 2 s windup.</summary>
		[SteamFunction]
		public static void RoundAttack(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!IsWarrior(self)) {
				self.RedMessage(OnlyWarrior);
			} else if (self.HasTimer(roundAttackTK)) {
				self.RedMessage("Jeste nelze pouzit.");
			} else if (IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (!self.IsGM && (ClassAbilities.SecondsTag(self, nextRoundAttackTK) > Globals.TimeInSeconds)) {
				self.RedMessage("Jeste nelze pouzit.");
			} else if (!HoldsRoundAttackWeapon(self)) {
				self.SysMessage("S timhle paratkem je to nemozne.");
			} else if (ClassAbilities.ReduceStamina(self, settings.roundAttackStamina)) {
				self.SysMessage("Chystas se tvrde uderit!");
				RoundAttackTimer timer = new RoundAttackTimer();
				self.AddTimer(roundAttackTK, timer);
				timer.DueInSeconds = 2;
			}
		}

		/// <summary>findlayer(2), the two handed slot, literally: a one handed weapon doesn't qualify.</summary>
		private static bool HoldsRoundAttackWeapon(Character self) {
			return CraftingSkillDef.IsOfType(self.FindLayer(LayerNames.Hand2) as Item, "t_weapon_sword", "t_weapon_mace_smith", "t_weapon_fence");
		}

		internal static void FinishRoundAttack(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			self.SetTag(nextRoundAttackTK, Globals.TimeInSeconds + settings.roundAttackDelaySeconds);
			if (Globals.dice.Next(10001) > (ClassAbilities.Points(self, "roundattack") * settings.roundAttackChance)) {
				self.RedMessage("Nepovedlo se.");
				return;
			}
			if (!HoldsRoundAttackWeapon(self)) {
				self.SysMessage("S timhle paratkem je to nemozne.");
				return;
			}
			self.Sound(self.IsFemale ? 824 : 1098);
			self.Sound(512);
			self.SysMessage("Zurive bijes vse kolem sebe!");
			self.Direction = (Direction) (((int) self.Direction + 4) % 8); //Erebor's four flips turn it full circle
			self.Direction = (Direction) (((int) self.Direction + 4) % 8);
			List<Character> around = new List<Character>();
			foreach (AbstractCharacter near in self.GetMap().GetCharsInRange(self.X, self.Y, 1)) {
				Character ch = near as Character;
				if ((ch != null) && (ch != self) && !ch.IsGM && !ch.Flag_Dead && (Math.Abs(ch.Z - self.Z) <= 6)) {
					around.Add(ch);
				}
			}
			foreach (Character victim in around) {
				double damage = DamageManager.GetWeaponSwingArgs(self, victim).DamageAfterAC * settings.roundAttackDamagePercent / 100;
				DamageManager.CauseDamage(self, victim, self.WeaponDamageType, damage);
			}
		}

		/// <summary>[function omraceni]: readies the blow that stuns. For warriors, priests and craftsmen.</summary>
		[SteamFunction]
		public static void Omraceni(Player self) {
			if (!IsWarrior(self) && !Professions.HasProfession(self, Professions.Priest) && !Professions.HasProfession(self, Professions.Craftsman)) {
				self.SysMessage("Tohle neni pro tebe");
				return;
			}
			if (ClassAbilities.SecondsTag(self, nextStunTK) > Globals.TimeInSeconds) {
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
				return;
			}
			int points = ClassAbilities.Points(self, "stuntattack");
			if (points <= 0) {
				self.SysMessage("Tohle nedokazes");
				return;
			}
			if (Globals.dice.Next(1, 11) > Math.Min(points, 10)) {
				self.Stam = (short) Math.Max(0, self.Stam - 10);
				self.SysMessage("Nepodarilo se ti pripravit omracujici uder!");
				return;
			}
			if (ClassAbilities.ReduceStamina(self, 40)) {
				self.SetTag(stunReadyTK, 1);
				self.SysMessage("Muzes zasadit omracujici uder!");
				self.SetTag(nextStunTK, Globals.TimeInSeconds + StunWindowSeconds(ClassAbilities.Points(self, "stuntattack_prodleva")));
			} else {
				self.SetTag(nextStunTK, Globals.TimeInSeconds + 5);
			}
		}

		internal static double StunWindowSeconds(int delayPoints) {
			if (delayPoints < 10) {
				return 20;
			} else if (delayPoints < 20) {
				return 17;
			}
			return (delayPoints < 30) ? 14 : 11;
		}

		/// <summary>f_stun_getfreezebyweapon: how strong a monster's mace can stun, its experience divided by ten.</summary>
		internal static int StunLimit(Material material, bool shadow, int efficiencyPoints) {
			int limit;
			if (shadow) {
				limit = 901;
			} else {
				switch (material) {
					case Material.Copper:
						limit = 51;
						break;
					case Material.Iron:
						limit = 101;
						break;
					case Material.Verite:
						limit = 151;
						break;
					case Material.Valorite:
						limit = 251;
						break;
					case Material.Obsidian:
						limit = 401;
						break;
					case Material.Adamantinum:
						limit = 601;
						break;
					case Material.Mithril:
						limit = 901;
						break;
					default:
						return 0;
				}
			}
			int koeficient = (efficiencyPoints > 10) ? 10 + ((efficiencyPoints - 10) / 2) : efficiencyPoints;
			return limit + ((limit * koeficient) / 100);
		}

		/// <summary>e_stunattack's @AfterSwing.</summary>
		internal static void TryStun(Player self, NPC target, int damage) {
			if (self.GetTag(stunReadyTK) == null) {
				return;
			}
			self.RemoveTag(stunReadyTK);
			Weapon weapon = self.Weapon;
			if ((damage <= 0) || !CraftingSkillDef.IsOfType(weapon, "t_weapon_mace_smith")) {
				return;
			}
			if (ClassAbilities.SecondsTag(self, nextStunTK) < Globals.TimeInSeconds) {
				self.SysMessage("Prilis dlouho jsi " + (self.IsFemale ? "cekala" : "cekal") + " a rana ti nevysla.");
				return;
			}
			IObjectWithMaterial material = weapon.Def as IObjectWithMaterial;
			Material made = (material == null) ? Material.None : material.Material;
			if ((target.ExperienceReward / 10) < StunLimit(made, false, ClassAbilities.Points(self, "stuntattack_ucinnost"))) {
				ParalyzeEffectPluginDef.Paralyze(target, stunKey, TimeSpan.FromSeconds(StunSeconds));
				self.SysMessage((self.IsFemale ? "Omracila" : "Omracil") + " jsi cil!", ClassAbilities.ClassMessageHue);
			}
		}

		/// <summary>[Function leap].</summary>
		[SteamFunction]
		public static void Leap(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			Character target = WeaponSkillTargetQueuePlugin.CurrentTarget(self);
			if (!IsWarrior(self)) {
				self.SysMessage(OnlyWarrior);
			} else if (target == null) {
				self.SysMessage("Lze pouzit jen pri boji");
			} else if (!target.IsPlayer) {
				self.SysMessage("Cil neni hrac");
			} else if (!self.CanSeeLOS(target).Allow || target.Flag_Hidden) {
				self.SysMessage("Nevidis na cil.");
			} else if (Point2D.GetSimpleDistance(self, target) > 9) {
				self.SysMessage("Cil je prilis daleko.");
			} else if (IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (Globals.dice.Next(10001) > (ClassAbilities.Points(self, "leap") * settings.leapChance)) {
				self.RedMessage("Utok se nepovedl.");
				self.SetTag(nextLeapTK, Globals.TimeInSeconds + settings.leapDelaySeconds);
			} else if (ClassAbilities.SecondsTag(self, nextLeapTK) > Globals.TimeInSeconds) {
				self.SysMessage("Jeste nelze pouzit.");
				self.SetTag(nextLeapTK, Globals.TimeInSeconds + settings.leapDelaySeconds);
			} else if (IsInHouse(target)) {
				self.SysMessage("Nesmis utocit do domu.");
				self.SetTag(nextLeapTK, Globals.TimeInSeconds + settings.leapDelaySeconds);
			} else {
				int distance = Point2D.GetSimpleDistance(self, target);
				if (ClassAbilities.ReduceStamina(self, distance * settings.leapStaminaPerTile)) {
					if (distance <= settings.leapRange) {
						self.Go(new Point4D(target.X, target.Y, target.Z, target.M));
					} else {
						MonsterSpecialAttacks.ForceMove(self, settings.leapRange, (x, y) => Point2D.GetDirFromTo(new Point2D(x, y), target), null);
						self.SysMessage("Nedostal ses az k cili...", ClassAbilities.ClassMessageHue);
					}
					self.SetTag(nextLeapTK, Globals.TimeInSeconds + settings.leapDelaySeconds);
				}
			}
		}

		private static bool IsInHouse(Character ch) {
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return (area != null) && (area.HouseNumber != 0);
		}

		/// <summary>[Function warcry]: everyone near is shaken out of casting, and the warrior loses its mana.</summary>
		internal static void Warcry(Character self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!IsWarrior(self)) {
				self.SysMessage(OnlyWarrior);
			} else if (self.Flag_Dead) {
				self.SysMessage("You are dead!");
			} else if (IsFrozen(self)) {
				self.SysMessage(Frozen);
			} else if (!self.IsGM && (Globals.dice.Next(10001) > (ClassAbilities.Points(self, "warcry") * settings.warcryChance))) {
				self.RedMessage("Nepovedlo se.");
				self.SetTag(nextCryTK, Globals.TimeInSeconds + settings.warcryDelaySeconds);
			} else if (!self.IsGM && (ClassAbilities.SecondsTag(self, nextCryTK) > Globals.TimeInSeconds)) {
				self.SysMessage("Jeste nelze pouzit.");
			} else if (ClassAbilities.ReduceStamina(self, Globals.dice.Next(5, 9))) {
				self.SetTag(nextCryTK, Globals.TimeInSeconds + settings.warcryDelaySeconds);
				self.Mana = 0;
				self.Sound(self.IsFemale ? 824 : 1098);
				List<Character> shaken = new List<Character>();
				foreach (AbstractCharacter near in self.GetMap().GetCharsInRange(self.X, self.Y, settings.warcryRange)) {
					if ((near is Character ch) && (ch != self)) {
						shaken.Add(ch);
					}
				}
				foreach (Character ch in shaken) {
					ch.RedMessage("Jsi v soku!");
					if (ch.CurrentSkillName == SkillName.Magery) {
						ch.AbortSkill();
					}
				}
			}
		}

		/// <summary>[function defstance]. Erebor's class list calls it .stance, which a thief's vigilance shares.</summary>
		[SteamFunction]
		public static void DefStance(Character self) {
			if (!IsWarrior(self)) {
				self.SysMessage(OnlyWarrior);
			} else if (self.GetTag(defendingTK) == null) {
				if (((Player) self).Level < 10) {
					self.SysMessage("Na to jsi jeste prilis nezkuseny");
				} else if (self.Stam < 80) {
					self.SysMessage("Tohle ted nedokazes...");
				} else {
					PayForStance(self);
					self.SetTag(defendingTK, 1);
					self.SysMessage("Zaujal jsi obranny postoj.");
				}
			} else if (self.Stam < 80) {
				self.SysMessage("Tohle ted nedokazes...");
			} else {
				PayForStance(self);
				self.RemoveTag(defendingTK);
				self.SysMessage("Zrusil jsi obranny postoj.");
			}
		}

		private static void PayForStance(Character self) {
			self.Stam = (short) Math.Max(0, self.Stam - (self.MaxStam / 2));
		}

		internal static bool InDefStance(Character ch) {
			return ch.GetTag(defendingTK) != null;
		}

		private static double DefStanceFactor(Character self) {
			return (100 - ((ClassAbilitySettings.instance.defStanceDamagePercent * ClassAbilities.Points(self, "defstance")) / 10)) / 100.0;
		}

		/// <summary>@AfterSwing of a player, on a blow that's landing.</summary>
		internal static void ModifyOwnBlow(Player self, Character target, WeaponSwingArgs swing) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (ClassAbilities.CounterFires(self) && (self.WeaponType != WeaponType.Bow) && (self.WeaponType != WeaponType.XBow)) {
				self.SysMessage("Kriticky zasah!", ClassAbilities.ClassMessageHue);
				swing.DamageAfterAC = (swing.DamageAfterAC * settings.criticalPermille) / 1000;
			}
			if (!target.IsPlayer && (self.Level >= settings.doubleSwingMinLevel)) {
				int points = ClassAbilities.Points(self, "doubleswing");
				if ((points > 0) && IsDoubleSwingWeapon(self) && (Globals.dice.Next(1, 1001) <= (100 + (points * 5))) && (self.Stam >= settings.doubleSwingStamina)) {
					self.Stam = (short) (self.Stam - settings.doubleSwingStamina);
					self.Sound(512);
					self.SysMessage("Dvojity zasah!", ClassAbilities.ClassMessageHue);
					swing.DamageAfterAC = (swing.DamageAfterAC * settings.doubleSwingPermille) / 1000;
				}
			}
			if (InDefStance(self) && !target.IsPlayer) {
				swing.DamageAfterAC = swing.DamageAfterAC * DefStanceFactor(self);
			}
		}

		private static bool IsDoubleSwingWeapon(Character self) {
			Item oneHand = self.FindLayer(LayerNames.Hand1) as Item;
			if (oneHand != null) {
				return IsModelOf(oneHand, "I_sword_long", "I_sword_broad");
			}
			return IsModelOf(self.FindLayer(LayerNames.Hand2) as Item, "I_axe_battle_large", "I_axe_double", "I_axe_two_hand");
		}

		/// <summary>typedef.dispid==: looks, so every material of a sword counts.</summary>
		private static bool IsModelOf(Item item, params string[] defnames) {
			if (item == null) {
				return false;
			}
			foreach (string defname in defnames) {
				ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
				if ((def != null) && (def.Model == item.Def.Model)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>@aftergetswing of a player: its stance, and f_shieldDodge_afterGetSwing.</summary>
		internal static void ModifyIncomingBlow(Player self, Character attacker, WeaponSwingArgs swing) {
			if (attacker.IsPlayer) {
				return;
			}
			if (InDefStance(self)) {
				swing.DamageAfterAC = swing.DamageAfterAC * DefStanceFactor(self);
			}
			if (ClassAbilities.Points(self, "vykryti") > Globals.dice.Next(100)) {
				Item shield = self.FindLayer(LayerNames.Hand2) as Item;
				if (CraftingSkillDef.IsOfType(shield, "t_shield")) {
					IObjectWithMaterial material = shield.Def as IObjectWithMaterial;
					int percent = ShieldBlockPercent((material == null) ? Material.None : material.Material);
					if (percent < 100) {
						swing.DamageAfterAC = (swing.DamageAfterAC * percent) / 100;
					}
				}
			}
		}

		internal static int ShieldBlockPercent(Material material) {
			switch (material) {
				case Material.Copper:
					return 85;
				case Material.Iron:
					return 80;
				case Material.Verite:
					return 75;
				case Material.Valorite:
					return 70;
				case Material.Obsidian:
					return 65;
				case Material.Adamantinum:
					return 60;
				case Material.Mithril:
					return 50;
				default:
					return 100;
			}
		}

		private static bool TauntAllowed(Player self, bool mass) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int points = ClassAbilities.Points(self, "taunt");
			if (!IsWarrior(self)) {
				self.SysMessage(OnlyWarrior);
				return false;
			}
			if (self.Flag_Dead) {
				return false;
			}
			if ((points <= 0) || (mass && (points < 11))) {
				self.RedMessage("Neumis pouzit tuto abilitu.");
				return false;
			}
			if (self.Stam < (mass ? settings.massTauntStamina : settings.singleTauntStamina)) {
				self.RedMessage("Nemas dostatek staminy.");
				return false;
			}
			TagKey nextTK = mass ? nextMassTauntTK : nextSingleTauntTK;
			double next = ClassAbilities.SecondsTag(self, nextTK);
			if (next > Globals.TimeInSeconds) {
				self.RedMessage("Zbrkle pouziti schopnosti jen oddalilo cas, kdy ji pujde znovu pouzit.");
				self.SetTag(nextTK, next + settings.tauntDelayPenaltySeconds);
				return false;
			}
			HidingSkillDef.UnHide(self);
			return true;
		}

		private static double TauntDelay(Character self, double seconds) {
			return InDefStance(self) ? seconds * 0.75 : seconds;
		}

		[SteamFunction]
		public static void Taunt(Player self) {
			if (TauntAllowed(self, false)) {
				self.Target(SingletonScript<Targ_Taunt>.Instance);
				self.SysMessage("Koho chces na sebe nalakat?");
			}
		}

		[SteamFunction]
		public static void MassTaunt(Player self) {
			if (!TauntAllowed(self, true)) {
				return;
			}
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			self.Sound(self.IsFemale ? 824 : 1098);
			self.Stam = (short) Math.Max(0, self.Stam - settings.massTauntStamina);
			self.SetTag(nextMassTauntTK, Globals.TimeInSeconds + TauntDelay(self, settings.massTauntDelaySeconds));
			int strength = (self.Str * 1000) / 250, stamina = ((self.Stam + 50) * 1000) / Math.Max(1, (int) self.MaxStam);
			List<NPC> near = new List<NPC>();
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, settings.tauntRange)) {
				if ((ch is NPC npc) && !npc.Flag_Dead) {
					near.Add(npc);
				}
			}
			foreach (NPC npc in near) {
				int count = TauntTimer.Count(npc);
				if ((count > 0) && (self.Stam < (count * 2))) {
					continue;
				}
				if (!CanTaunt(self, npc)) {
					self.RedMessage("Nalakani teto prisery presahuje tve moznosti.");
					continue;
				}
				int resist = TauntTimer.Resist(npc);
				if (resist >= 1000) {
					self.RedMessage("Cil odolava nalakani.");
					continue;
				}
				bool works = Globals.dice.Next(1, 10001) <= ((strength * stamina * (1000 - resist)) / 100000);
				if (works && (count > 0)) {
					self.Stam = (short) Math.Max(0, self.Stam - (count * 5));
				}
				TauntTimer.Taunt(npc, self, works);
			}
		}

		internal static bool CanTaunt(Player self, NPC npc) {
			return (self.Level * ClassAbilities.Points(self, "taunt") * 5) >= npc.ExperienceReward;
		}

		/// <summary>i_taunt_target's @targon_char.</summary>
		internal static void TauntOne(Player self, NPC target) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (Point2D.GetSimpleDistance(self, target) > settings.tauntRange) {
				self.RedMessage("Cil je mimo tvuj dosah");
				return;
			}
			if (!CanTaunt(self, target)) {
				self.SysMessage("Nalakani teto prisery presahuje tve moznosti.");
				return;
			}
			int resist = TauntTimer.Resist(target);
			if (resist >= 1000) {
				self.RedMessage("Cil odolava nalakani.");
				return;
			}
			self.Stam = (short) Math.Max(0, self.Stam - settings.singleTauntStamina);
			self.SetTag(nextSingleTauntTK, Globals.TimeInSeconds + TauntDelay(self, settings.singleTauntDelaySeconds));
			int strength = (self.Str * 1000) / 250, stamina = ((self.Stam + 50) * 1000) / Math.Max(1, (int) self.MaxStam);
			bool works = Globals.dice.Next(1, 10001) <= ((strength * stamina * (1000 - resist)) / 100000);
			self.Sound(works ? (self.IsFemale ? 824 : 1098) : (self.IsFemale ? 823 : 1097));
			if (works) {
				self.SysMessage("Nalakani se povedlo", ClassAbilities.ClassMessageHue);
			} else {
				self.RedMessage("Nalakani se nepovedlo");
			}
			TauntTimer.Taunt(target, self, works);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class RoundAttackTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public RoundAttackTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Player self) && !self.Flag_Dead) {
				WarriorAbilities.FinishRoundAttack(self);
			}
		}
	}

	public class Targ_Taunt : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (targetted is NPC npc) {
				WarriorAbilities.TauntOne(self, npc);
			} else {
				self.SysMessage("Tohle nemuzes nalakat.");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.RedMessage("Toto neni monstrum.");
			return TargetResult.Done;
		}
	}

	/// <summary>i_taunter: for a time the monster goes for the warrior and nobody else. Each taunt makes it
	/// 25% harder to taunt again, of which 5% stays after.</summary>
	[SaveableClass, DeepCopyableClass]
	public class TauntTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_taunter_");
		private static readonly TagKey countTK = TagKey.Acquire("_tauntCount_");
		private static readonly TagKey resistTK = TagKey.Acquire("_resistTaunt_");

		[SaveableData, CopyableData]
		public Character taunter;

		[SaveableData, CopyableData]
		public double secondsLeft;

		[DeepCopyImplementation, LoadingInitializer]
		public TauntTimer() {
		}

		internal static int Count(NPC npc) {
			return Convert.ToInt32(npc.GetTag(countTK) ?? 0);
		}

		internal static int Resist(NPC npc) {
			return Convert.ToInt32(npc.GetTag(resistTK) ?? 0);
		}

		internal static void Taunt(NPC npc, Character taunter, bool works) {
			if (!works) {
				return;
			}
			double seconds = ((taunter.Str / 3) * 2) / 10.0;
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai != null) {
				ai.EngageTarget(taunter);
			}
			TauntTimer running = npc.GetTimer(Key) as TauntTimer;
			if (running != null) {
				running.taunter = taunter;
				running.secondsLeft = Math.Max(seconds, 5);
				return;
			}
			npc.SetTag(countTK, Count(npc) + 1);
			npc.SetTag(resistTK, Resist(npc) + 250);
			TauntTimer timer = new TauntTimer();
			timer.taunter = taunter;
			timer.secondsLeft = Math.Max(seconds, 5);
			npc.AddTimer(Key, timer);
			timer.DueInSeconds = 1;
			timer.PeriodInSeconds = 1;
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = cont as NPC;
			if ((npc == null) || npc.Flag_Dead) {
				return;
			}
			this.secondsLeft--;
			Character who = this.taunter;
			if ((who == null) || who.IsDeleted || (this.secondsLeft <= 0) || (who.M != npc.M) || who.Flag_Hidden || who.Flag_InvisByMagic ||
					(Point2D.GetSimpleDistance(npc, who) > ClassAbilitySettings.instance.tauntRange)) {
				npc.SetTag(resistTK, Resist(npc) - 200);
				this.Delete();
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if ((ai != null) && (ai.Target != who)) {
				ai.EngageTarget(who);
			}
		}
	}
}
