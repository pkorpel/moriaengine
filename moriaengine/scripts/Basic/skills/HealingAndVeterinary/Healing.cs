using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - no button for Healing in the skill list, as Sphere: bandages do it all, by Erebor's healing_items.scp.</summary>
	[ViewableClass]
	public class HealingSkillDef : SkillDef {
		public HealingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(500014); //That skill cannot be used directly.
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	public class t_bandage : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player != null) {
				if (ClothMaking.Count(player.Backpack, (ItemDef) self.Def) <= 0) {
					player.SysMessage(Loc<HealingLoc>.Get(player.Language).NoBandagesOnYou);
				} else {
					player.Target(SingletonScript<Targ_Bandage>.Instance, self);
				}
			}
			return TriggerResult.Cancel;
		}
	}

	public class t_bandage_blood : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player != null) {
				player.Target(SingletonScript<Targ_Bandage>.Instance, self);
			}
			return TriggerResult.Cancel;
		}
	}

	public class Targ_Bandage : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			self.SysMessage(Bandaging.IsBloody((Item) parameter) ? texts.WashIn : texts.WhoToTreat);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Item bandage = (Item) parameter;
			if (!bandage.IsDeleted) {
				if (Bandaging.IsBloody(bandage) && !Professions.HasProfession(self, Professions.Shaman)) {
					self.SysMessage(Loc<HealingLoc>.Get(self.Language).OnlyShamans);
				} else {
					Bandaging.OnCharacter(self, targetted, Bandaging.IsBloody(bandage));
				}
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			Item bandage = (Item) parameter;
			if (!bandage.IsDeleted) {
				Bandaging.OnItem(self, targetted, bandage);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>bandaonchar and the two @targon_item of Erebor's bandages.</summary>
	public static class Bandaging {
		private const int CharReach = 6, CorpseReach = 2, GhostReach = 4, BandagesToRaise = 20, LeastToRaise = 800, FailRollOf = 1025;
		private const double SecondsToRaise = 4;

		internal static readonly TagKey healedByTK = TagKey.Acquire("isHealedBy");
		internal static readonly TagKey healedUntilTK = TagKey.Acquire("isHealedUntil");

		private static readonly TagKey carvedTK = TagKey.Acquire("rozrezano");
		private static readonly string[] beyondHelp = { "c_llama", "c_horse_tan", "c_horse_gray", "c_horse_brown_dk", "c_horse_brown_lt", "c_horse_pack", "c_llama_pack" };

		internal static bool IsBloody(Item bandage) {
			return CraftingSkillDef.IsOfType(bandage, "t_bandage_blood");
		}

		private static string BandageName(bool bloody) {
			return bloody ? "i_bandage_bloody" : "i_bandage";
		}

		private static void Consume(Character self, int amount, string defname) {
			IResourceListEntry missing;
			ResourcesListParser.Parse(amount + " " + defname).ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
		}

		private static void Interrupt(Character self, string healingText, string raisingText) {
			if (self.GetTimer(HealTimer.Key) != null) {
				self.DeleteTimer(HealTimer.Key);
				self.SysMessage(healingText);
			}
			if (self.GetTimer(RaiseTimer.Key) != null) {
				self.DeleteTimer(RaiseTimer.Key);
				self.SysMessage(raisingText);
			}
		}

		//healdisarm: both hands, but for a staff
		private static void Disarm(Character self) {
			Item held = (self.FindLayer(LayerNames.Hand1) as Item) ?? (self.FindLayer(LayerNames.Hand2) as Item);
			if ((held is Weapon) && !CraftingSkillDef.IsOfType(held, "t_weapon_mace_staff")) {
				held.Cont = self.Backpack;
			}
		}

		internal static void OnCharacter(Character self, Character target, bool bloody) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			if (ParalyzeEffectPluginDef.IsParalyzed(self)) {
				self.SysMessage(texts.Frozen);
				return;
			}
			Interrupt(self, texts.MustNotRushHealing, self.IsFemale ? texts.InterruptedRaisingFemale : texts.InterruptedRaisingMale);
			if (Point2D.GetSimpleDistance(self, target) > CharReach) {
				self.SysMessage(texts.TargetTooFar);
				return;
			}
			if (SummonSpellDef.IsSummonedCreature(target)) {
				self.SysMessage(texts.NoSummons);
				return;
			}
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage(texts.CantSeeTarget);
				return;
			}
			if ((target.Hits >= target.MaxHits) && !BleedingEffectPluginDef.IsBleeding(target)) {
				self.SysMessage(string.Format(texts.NotHurt, target.Name));
				return;
			}
			self.SysMessage(string.Format(texts.TryingToHeal, target.Name));
			Disarm(self);
			Consume(self, 1, BandageName(bloody));

			HealTimer timer = new HealTimer();
			timer.patient = target;
			double seconds = SkillDef.GetBySkillName(SkillName.Healing).GetDelayForValue((ushort) self.GetSkill(SkillName.Healing));
			if ((self != target) && (target is Player)) {
				if (HealingAbilities.IsEnlightened(self)) {
					self.SysMessage(texts.HealingFaster, ClassMessageColor);
					HidingSkillDef.Reveal(self);
					seconds = ScriptUtil.EvalRangePermille(self.GetSkill(SkillName.Healing), HealingAbilities.EnlightmentSeconds);
				} else if (bloody && HealingAbilities.IsImproved(self)) {
					self.SysMessage(texts.HealingBetter, ClassMessageColor);
					timer.doubled = true;
				}
			}
			self.AddTimer(HealTimer.Key, timer).DueInSeconds = seconds;
		}

		internal const int ClassMessageColor = 0x57;

		internal static void OnItem(Player self, Item target, Item bandage) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			bool bloody = IsBloody(bandage);
			Interrupt(self, texts.InterruptedHealing, texts.MustNotRushRaising);
			if (CraftingSkillDef.IsOfType(target, bloody ? "t_water_wash" : "t_blood")) {
				Give(self, bloody ? "i_bandage" : "i_bandage_bloody", bandage.Amount);
				bandage.Delete();
				return;
			}
			Corpse corpse = target as Corpse;
			if (corpse == null) {
				self.SysMessage(texts.CantUseOnThat);
			} else if (ClothMaking.Count(self.Backpack, (ItemDef) ThingDef.GetByDefname(BandageName(bloody))) < BandagesToRaise) {
				self.SysMessage(texts.NotEnoughBandages);
			} else if (Point2D.GetSimpleDistance(self, corpse.TopPoint) > CorpseReach) {
				self.SysMessage(texts.BodyTooFar);
			} else if (!self.CanSeeLOS(corpse).Allow) {
				self.SysMessage(texts.CantSeeBody);
			} else if (!PlayerHunger.DistractsFrom(self)) {
				bool shaman = Professions.HasProfession(self, Professions.Shaman);
				bool priest = Professions.HasProfession(self, Professions.Priest);
				if ((shaman || (priest && !bloody)) && (corpse.Owner != null)) {
					RaisePerson(self, corpse, bloody, texts);
				} else if (!bloody && Professions.HasProfession(self, Professions.Ranger) && (corpse.Owner == null)) {
					RaiseAnimal(self, corpse, texts);
				} else {
					self.RedMessage(string.Format(bloody ? texts.CantRaiseThisWay : texts.CantRaiseWithBandages, self.Profession.Name));
				}
			}
		}

		private static void RaisePerson(Player self, Corpse corpse, bool bloody, HealingLoc texts) {
			Character ghost = corpse.Owner;
			if (self.GetSkill(SkillName.Healing) < LeastToRaise) {
				self.SysMessage(texts.NotGoodHealer);
			} else if (self.GetSkill(SkillName.Anatomy) < LeastToRaise) {
				self.SysMessage(texts.NotLearnedInAnatomy);
			} else if (Point2D.GetSimpleDistance(ghost, corpse.TopPoint) > GhostReach) {
				self.SysMessage(texts.GhostTooFar);
			} else if (!ghost.Flag_WarMode) {
				self.SysMessage(texts.GhostNotInWarMode);
			} else {
				self.SysMessage(texts.ReturnToLife);
				Disarm(self);
				Consume(self, BandagesToRaise, BandageName(bloody));
				StartRaising(self, corpse, false);
			}
		}

		private static void RaiseAnimal(Player self, Corpse corpse, HealingLoc texts) {
			CharacterDef kind = corpse.CharDef;
			if (self.GetSkill(SkillName.Veterinary) < LeastToRaise) {
				self.SysMessage(texts.NotGoodVet);
			} else if (self.GetSkill(SkillName.AnimalLore) < LeastToRaise) {
				self.SysMessage(texts.NotLearnedInAnimals);
			} else if (Holds(corpse, "i_nores") || System.Array.Exists(beyondHelp, name => ThingDef.GetByDefname(name) == kind)) {
				self.SysMessage(texts.BeyondHelp);
			} else if ((kind == null) || CharModelInfo.IsHumanModel(kind.Model) || ((CharModelInfo.GetByModel(kind.Model).charAnimType & CharAnimType.Animal) != CharAnimType.Animal)) {
				self.SysMessage(texts.NotAnAnimal);
			} else if (corpse.HasTag(carvedTK)) {
				self.SysMessage(texts.Gutted);
			} else {
				self.SysMessage(texts.TryingToRaiseAnimal);
				Disarm(self);
				Consume(self, BandagesToRaise, "i_bandage");
				StartRaising(self, corpse, true);
			}
		}

		private static void StartRaising(Character self, Corpse corpse, bool animal) {
			RaiseTimer timer = new RaiseTimer();
			timer.corpse = corpse;
			timer.animal = animal;
			self.AddTimer(RaiseTimer.Key, timer).DueInSeconds = SecondsToRaise;
		}

		internal static bool Holds(Container container, string defname) {
			return ClothMaking.Count(container, (ItemDef) ThingDef.GetByDefname(defname)) > 0;
		}

		internal static void Give(Character self, string defname, int amount) {
			Item given = (Item) ThingDef.GetByDefname(defname).Create(self.Backpack);
			given.Amount = amount;
		}

		/// <summary>i_mem_healing's @timer: Healing for a person, Veterinary for anything else, fixing a bleed first.</summary>
		internal static void Finish(Character self, Character patient, bool doubled) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			bool person = CharModelInfo.IsHumanModel(patient.Model);
			SkillName skill = person ? SkillName.Healing : SkillName.Veterinary;
			if (self.GetSkill(skill) <= Globals.dice.Next(FailRollOf)) {
				self.RedMessage(person ? texts.HealingFailed : texts.TreatmentFailed);
				return;
			}
			if (person && Professions.HasProfession(self, Professions.Priest) && (self != patient)) {
				patient.SetTag(healedByTK, self);
				patient.SetTag(healedUntilTK, Globals.TimeInSeconds + HealingAbilities.SecondsHealerShares);
			}
			if (!person || !Professions.HasProfession(self, Professions.Shaman) || (Globals.dice.Next(4) > 0)) {
				Give(self, "i_bandage_bloody", 1);
			}
			if (BleedingEffectPluginDef.IsBleeding(patient)) {
				BleedingEffectPluginDef.StopBleeding(patient);
				if (!person) {
					self.SysMessage(texts.BleedingStopped);
					return;
				}
				self.SysMessage(string.Format(texts.StoppedBleeding, patient.Name), ClassMessageColor);
				if (patient != self) {
					patient.SysMessage(string.Format(texts.StoppedYourBleeding, self.Name), ClassMessageColor);
				}
				if (!doubled) {
					return;
				}
			}
			self.SysMessage(string.Format(person ? texts.TreatedPerson : texts.TreatedAnimal, patient.Name), ClassMessageColor);
			SkillDef def = SkillDef.GetBySkillName(skill);
			int sum = self.GetSkill(skill) + self.GetSkill(SkillName.Anatomy);
			int healed = (int) ScriptUtil.EvalRangePermille(sum, def.Effect);
			if (doubled) {
				healed *= 2;
			}
			healed = System.Math.Min(healed, patient.MaxHits - patient.Hits);
			patient.Hits = (short) (patient.Hits + healed);
			if (patient != self) {
				patient.SysMessage(string.Format(texts.TreatedYou, self.Name), ClassMessageColor);
			}
			HpSight.ShowChange(patient, healed, true);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class HealTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_healing_");

		[SaveableData, CopyableData]
		public Character patient;
		[SaveableData, CopyableData]
		public bool doubled;

		[LoadingInitializer, DeepCopyImplementation]
		public HealTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((this.patient != null) && !this.patient.IsDeleted) {
				Bandaging.Finish((Character) cont, this.patient, this.doubled);
			}
		}
	}

	/// <summary>i_mem_ressurect: 70.0 to 105.0 to beat, so nobody's sure of it.</summary>
	[SaveableClass, DeepCopyableClass]
	public class RaiseTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_raising_");

		private const int LeastRoll = 700, MostRoll = 1050, BandagesBack = 20;

		[SaveableData, CopyableData]
		public Corpse corpse;
		[SaveableData, CopyableData]
		public bool animal;

		[LoadingInitializer, DeepCopyImplementation]
		public RaiseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			HidingSkillDef.Reveal(self);
			if ((this.corpse == null) || this.corpse.IsDeleted) {
				return;
			}
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			int skill = self.GetSkill(this.animal ? SkillName.Veterinary : SkillName.Healing);
			if (Globals.dice.Next(LeastRoll, MostRoll + 1) >= skill) {
				self.RedMessage(self.IsFemale ? texts.RaisingFailedFemale : texts.RaisingFailedMale);
			} else if (this.animal) {
				this.RaiseAnimal(self, texts);
			} else {
				this.RaisePerson(self, texts);
			}
		}

		private void RaisePerson(Character self, HealingLoc texts) {
			Character raised = this.corpse.Owner;
			self.SysMessage(string.Format(self.IsFemale ? texts.YouRaisedFemale : texts.YouRaisedMale, raised.Name), Bandaging.ClassMessageColor);
			raised.P(this.corpse);
			raised.Resurrect(this.corpse);
			if (raised.GameState != null) {
				raised.SysMessage(string.Format(texts.RaisedYou, self.Name), Bandaging.ClassMessageColor);
			}
		}

		//spelleffect on a monster's corpse, Sphere's RaiseCorpse: the creature again, holding what the corpse held
		private void RaiseAnimal(Character self, HealingLoc texts) {
			self.SysMessage(texts.RaisedAnimal, Bandaging.ClassMessageColor);
			foreach (string mark in new[] { "i_nofame", "i_poresu" }) {
				if (!Bandaging.Holds(this.corpse, mark)) {
					ThingDef.GetByDefname(mark).Create(this.corpse);
				}
			}
			Bandaging.Give(self, "i_bandage_bloody", BandagesBack);
			NPC raised = (NPC) this.corpse.CharDef.Create(this.corpse.TopPoint);
			this.corpse.ReturnStuffToChar(raised);
			this.corpse.Delete();
		}
	}

	/// <summary>The priest's enlightment and the shaman's improvement: Erebor's class abilities that act through bandages.</summary>
	public static class HealingAbilities {
		internal const double SecondsHealerShares = 900; //nastaveni_priest_healExpDelay
		internal static readonly double[] EnlightmentSeconds = { 2.0, 1.0 }; //nastaveni_ability_enlightment

		private const int StaminaToStart = 3, ChanceOf = 100001;

		private static bool IsOn(Character self) {
			return self.GetTimer(ClassAbilityTimer.Key) != null;
		}

		private static bool Rolls(Character self) {
			Player player = self as Player;
			return (player != null) && IsOn(self) && (Globals.dice.Next(ChanceOf) < CombatCalculator.CalculateClassAbility(System.Math.Min(player.Level, 60)));
		}

		internal static bool IsEnlightened(Character self) {
			return Professions.HasProfession(self, Professions.Priest) && Rolls(self);
		}

		internal static bool IsImproved(Character self) {
			return Professions.HasProfession(self, Professions.Shaman) && Rolls(self);
		}

		private static void Toggle(Character self, ProfessionDef profession, string onlyWho, string started) {
			Player player = self as Player;
			if ((player == null) || (player.Level < 1)) {
				self.RedMessage(Loc<HealingLoc>.Get(self.Language).AtLeastFirstLevel);
			} else if (!Professions.HasProfession(self, profession)) {
				self.RedMessage(onlyWho);
			} else if (IsOn(self)) {
				self.DeleteTimer(ClassAbilityTimer.Key);
				self.SysMessage(Loc<HealingLoc>.Get(self.Language).BackToNormal, Bandaging.ClassMessageColor);
			} else if (self.Stam >= StaminaToStart) {
				self.Stam -= StaminaToStart;
				ClassAbilityTimer timer = new ClassAbilityTimer();
				self.AddTimer(ClassAbilityTimer.Key, timer);
				timer.DueInSeconds = 1;
				timer.PeriodInSeconds = 1;
				self.SysMessage(started, Bandaging.ClassMessageColor);
			}
		}

		[SteamFunction]
		public static void Enlightment(Character self) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			Toggle(self, Professions.Priest, texts.OnlyPriest, self.IsFemale ? texts.HealFasterFemale : texts.HealFasterMale);
		}

		[SteamFunction]
		public static void Improvement(Character self) {
			HealingLoc texts = Loc<HealingLoc>.Get(self.Language);
			Toggle(self, Professions.Shaman, texts.OnlyShaman, self.IsFemale ? texts.HealBetterFemale : texts.HealBetterMale);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class ClassAbilityTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_classAbility_");

		[LoadingInitializer, DeepCopyImplementation]
		public ClassAbilityTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (self.Stam < 5) {
				self.SysMessage(Loc<HealingLoc>.Get(self.Language).BackToNormal, Bandaging.ClassMessageColor);
				this.Delete();
				return;
			}
			self.Stam = (short) System.Math.Max(0, self.Stam - MagerySettings.instance.astralPowerStaminaPerSecond);
		}
	}

	internal class HealingLoc : CompiledLocStringCollection<HealingLoc> {
		public string NoBandagesOnYou = "Obvazy musis mit u sebe.";
		public string WhoToTreat = "Koho chces osetrit?";
		public string WashIn = "What do you want to wash these in?";
		public string OnlyShamans = "S timhle to umi jen samani!";
		public string Frozen = "You are frozen and cannot move.";
		public string MustNotRushHealing = "Leceni nesmis uspechat!";
		public string MustNotRushRaising = "Ozivovani nesmis uspechat!";
		public string InterruptedHealing = "Prerusil jsi leceni.";
		public string InterruptedRaisingMale = "Prerusil jsi ozivovani.";
		public string InterruptedRaisingFemale = "Prerusila jsi ozivovani.";
		public string TargetTooFar = "Cil je moc daleko.";
		public string NoSummons = "Nelze lecit summony.";
		public string CantSeeTarget = "Nevidis cil.";
		public string NotHurt = "{0} neni zranen.";
		public string TryingToHeal = "Pokousis se vylecit {0}.";
		public string HealingFaster = "Lecis rychleji!";
		public string HealingBetter = "Lecis lepe!";
		public string CantUseOnThat = "Na tohle nemuzes obvaz pouzit!";
		public string NotEnoughBandages = "Nemas dost obvazu!";
		public string BodyTooFar = "Telo je prilis daleko.";
		public string CantSeeBody = "Nevidis na telo.";
		public string CantRaiseThisWay = "Jako {0} nemuzes takhle ozivovat.";
		public string CantRaiseWithBandages = "Jako {0} nemuzes ozivovat pomoci obvazu.";
		public string NotGoodHealer = "Nejsi dost dobry lecitel.";
		public string NotLearnedInAnatomy = "Nejsi dost vzdelan v anatomii.";
		public string GhostTooFar = "Duch je prilis daleko.";
		public string GhostNotInWarMode = "Duch neni ve war modu.";
		public string ReturnToLife = "Necht se navrati k zivotu !";
		public string NotGoodVet = "Nejsi dost dobry veterinar.";
		public string NotLearnedInAnimals = "Nejsi dostatecne znalosti o zviratech.";
		public string BeyondHelp = "Tomu uz nepomuze ani svecena voda";
		public string NotAnAnimal = "Toto neni zvire.";
		public string Gutted = "Zvire je vykuchane.";
		public string TryingToRaiseAnimal = "Pokousis se ozivit mrtvolu zvirete.";
		public string HealingFailed = "Leceni se ti nepovedlo.";
		public string TreatmentFailed = "Osetreni se ti nepovedlo.";
		public string BleedingStopped = "Krvaceni zastaveno.";
		public string StoppedBleeding = "{0} prestal krvacet.";
		public string StoppedYourBleeding = "{0} zastavil tve krvaceni.";
		public string TreatedPerson = "{0} byl uspesne osetren";
		public string TreatedAnimal = "{0} byl uspesne osetren.";
		public string TreatedYou = "{0} te osetril.";
		public string RaisingFailedMale = "Selhal jsi v ozivovani.";
		public string RaisingFailedFemale = "Selhala jsi v ozivovani.";
		public string YouRaisedMale = "Ozivil jsi {0}.";
		public string YouRaisedFemale = "Ozivila jsi {0}.";
		public string RaisedYou = "Ozivil te {0}.";
		public string RaisedAnimal = "Oziveni uspesne.";
		public string AtLeastFirstLevel = "Musis byt alespon na prvni urovni.";
		public string OnlyPriest = "Toto umi pouzit pouze Knez.";
		public string OnlyShaman = "Toto umi pouzit pouze Saman.";
		public string BackToNormal = "Jsi zpatky v normalnim stavu.";
		public string HealFasterMale = "Nyni jsi schopen rychleji lecit bandazemi.";
		public string HealFasterFemale = "Nyni jsi schopna rychleji lecit bandazemi.";
		public string HealBetterMale = "Nyni jsi schopen lecit lepe.";
		public string HealBetterFemale = "Nyni jsi schopna lecit lepe.";
	}
}
