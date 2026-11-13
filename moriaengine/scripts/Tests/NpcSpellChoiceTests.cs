using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1033 - runs ChooseSpellToCast many times per situation and reports what it picked, as
	/// "SPELL_CHOICE: name=True/False", since most of the choice has a random part.
	/// </summary>
	public static class NpcSpellChoiceTests {
		private const int Rolls = 300;

		private static readonly string[] monsterSpells = {
			"s_lightning", "s_magic_reflection", "s_fireball", "s_harm", "s_greater_heal", "s_flamestrike", "s_paralyze",
			"s_poison", "s_mass_dispel", "s_explosion", "s_energy_bolt", "s_mass_curse", "s_dispel", "s_summon_elem_air",
			"s_summon_elem_fire", "s_summon_undead", "s_mind_blast", "s_mana_drain", "s_fire_bolt", "s_summon_daemon",
			"s_blade_spirits", "s_cure", "s_meteor_swarm", "s_clumsy", "s_feeblemind", "s_magic_arrow", "s_curse",
			"s_summon_elem_earth", "s_energy_vortex", "s_weaken", "s_fire_field", "s_heal", "s_dispel_field",
			"s_summon_elem_water", "s_protection", "s_poison_field", "s_archprotection", "s_strength", "s_chain_lightning",
		};

		[SteamFunction]
		public static void TestNpcSpellChoice(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			try {
				EverySpellMonstersHoldHasAKind();
				GuardSupportsItselfOnlyWhenNeeded(self);
				CureOnlyWhenPoisoned(self);
				DamageGoesForTheWeakResist(self);
				TargetStateDecidesTheAttack(self);
				ManaDrainOnlyForAMage(self);
				SummonsReallyAppear(self);
				ACastTakesTheCastersPower(self); //#1420
				AReflectionGoesBackFromTheReflector(self); //#1420
				AParalyzeRollsAgainstTheDefense(self); //#1507
				AnAreaSpellSparesFriendsBeforeThePower(self); //#1518
			} finally {
				VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			}
			Console.WriteLine("SPELL_CHOICE_DONE");
		}

		private static void EverySpellMonstersHoldHasAKind() {
			List<string> unknown = new List<string>();
			foreach (string name in monsterSpells) {
				SpellDef spell = SpellDef.GetByDefname(name);
				if ((spell == null) || (NpcSpellChoice.KindOf(spell) == NpcSpellKind.Unknown)) {
					unknown.Add(name);
				}
			}
			Console.WriteLine("SPELL_CHOICE_INFO: unknown kinds: " + string.Join(",", unknown));
			Report("everyMonsterSpellHasAKind", unknown.Count == 0);
		}

		private static void GuardSupportsItselfOnlyWhenNeeded(Character self) {
			NPC guard = (NPC) ((NPCDef) ThingDef.GetByDefname("c_vguard_gondor_hala_pesak")).Create(self.X + 1, self.Y, self.Z, self.M);
			guard.Mana = 50;
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(guard);
			timer.EngageTarget(dummy);
			SpellDef reflection = SpellDef.GetByDefname("s_magic_reflection");
			SpellDef heal = SpellDef.GetByDefname("s_greater_heal");
			Console.WriteLine("SPELL_CHOICE_INFO: guard hits=" + guard.Hits + "/" + guard.MaxHits + " healAmount=" + heal.GetEffectForValue(heal.EstimatePower(guard, guard)));

			Report("guardHitsAreFull", guard.Hits == guard.MaxHits);
			Report("guardReflectsFirst", Counts(timer, guard, dummy).Get(reflection) == Rolls);
			timer.Fight(guard, dummy);
			Report("guardCastReflection", MagicReflectionPluginDef.HasMagicReflection(guard));
			timer.ForgetCastCooldown();

			Dictionary<SpellDef, int> fullHealth = Counts(timer, guard, dummy);
			Report("guardAtFullHealthCastsNothing", fullHealth.Get(null) == Rolls);

			guard.Hits = (short) (guard.MaxHits * 80 / 100);
			Report("guardAt80PercentCastsNothing", Counts(timer, guard, dummy).Get(null) == Rolls);

			guard.Hits = (short) (guard.MaxHits * 60 / 100);
			Report("guardAt60PercentHeals", Counts(timer, guard, dummy).Get(heal) == Rolls);
			guard.Delete();
			dummy.Delete();
		}

		private static void CureOnlyWhenPoisoned(Character self) {
			NPC mage = Mage(self);
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(mage);
			timer.EngageTarget(dummy);
			SpellDef cure = SpellDef.GetByDefname("s_cure");
			Report("noCureWithoutPoison", Counts(timer, mage, dummy).Get(cure) == 0);
			Poison.Apply(mage, dummy, 100, 30, PoisonKind.AbsoluteDamage);
			Report("mageGotPoisoned", Poison.IsPoisoned(mage));
			Report("poisonedMageCures", Counts(timer, mage, dummy).Get(cure) == Rolls);
			mage.Delete();
			dummy.Delete();
		}

		private static void DamageGoesForTheWeakResist(Character self) {
			NPC mage = Mage(self);
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(mage);
			timer.EngageTarget(dummy);
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");
			SpellDef lightning = SpellDef.GetByDefname("s_lightning");

			dummy.Hits = (short) (dummy.MaxHits / 2); //at full health the first casts are control spells
			dummy.ResistFire = 900;
			Dictionary<SpellDef, int> fireResistant = Counts(timer, mage, dummy);
			Report("fireResistantTargetGetsNoFire", (fireResistant.Get(fireball) == 0) && (fireResistant.Get(lightning) > 0));

			dummy.ResistFire = 0;
			dummy.ResistElectric = 900;
			Dictionary<SpellDef, int> shockResistant = Counts(timer, mage, dummy);
			Report("shockResistantTargetGetsNoLightning", (shockResistant.Get(lightning) == 0) && (shockResistant.Get(fireball) > 0));
			mage.Delete();
			dummy.Delete();
		}

		private static void TargetStateDecidesTheAttack(Character self) {
			NPC mage = Mage(self);
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(mage);
			timer.EngageTarget(dummy);
			SpellDef paralyze = SpellDef.GetByDefname("s_paralyze");
			SpellDef poison = SpellDef.GetByDefname("s_poison");
			SpellDef curse = SpellDef.GetByDefname("s_curse");
			SpellDef dispel = SpellDef.GetByDefname("s_dispel");
			SpellDef harm = SpellDef.GetByDefname("s_harm");

			Dictionary<SpellDef, int> fresh = Counts(timer, mage, dummy);
			Console.WriteLine("SPELL_CHOICE_INFO: fresh target: " + Describe(fresh));
			Report("freshTargetMixesControlSpells", (fresh.Get(paralyze) > Rolls / 5) && (fresh.Get(curse) > Rolls / 5) && (fresh.Get(dispel) == 0));

			dummy.Hits = (short) (dummy.MaxHits / 2);
			Report("hurtTargetIsNotCursed", Counts(timer, mage, dummy).Get(curse) == 0);
			dummy.Hits = dummy.MaxHits;

			ParalyzeEffectPluginDef.Paralyze(dummy, PluginKey.Acquire("_testChoiceParalyze_"), TimeSpan.FromSeconds(30));
			Report("targetIsParalyzed", ParalyzeEffectPluginDef.IsParalyzed(dummy));
			Report("paralyzedTargetIsNotParalyzedAgain", Counts(timer, mage, dummy).Get(paralyze) == 0);
			ParalyzeEffectPluginDef.Unparalyze(dummy);

			Poison.Apply(dummy, mage, 100, 30, PoisonKind.AbsoluteDamage);
			Report("poisonedTargetIsNotPoisonedAgain", Counts(timer, mage, dummy).Get(poison) == 0);
			Poison.RemoveAll(dummy);

			SummonSpellDef.MarkAsSummonedCreature(dummy);
			Report("summonIsDispelled", Counts(timer, mage, dummy).Get(dispel) == Rolls);
			dummy.Delete();

			dummy = Dummy(self);
			timer.EngageTarget(dummy);
			SpellDef.GetByDefname("s_magic_reflection").ApplyEffectChar(dummy, dummy, 700, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
			Report("targetReflects", MagicReflectionPluginDef.HasMagicReflection(dummy));
			Dictionary<SpellDef, int> reflecting = Counts(timer, mage, dummy);
			Console.WriteLine("SPELL_CHOICE_INFO: reflecting target: " + Describe(reflecting));
			Report("reflectionGetsTheWeakestDamage", reflecting.Get(harm) == Rolls);
			mage.Delete();
			dummy.Delete();
		}

		private static void ManaDrainOnlyForAMage(Character self) {
			NPC drainer = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_choice_drainer")).Create(self.X + 1, self.Y, self.Z, self.M);
			drainer.Mana = 1000;
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(drainer);
			timer.EngageTarget(dummy);
			SpellDef drain = SpellDef.GetByDefname("s_mana_drain");
			Report("noDrainOnANonMage", Counts(timer, drainer, dummy).Get(drain) == 0);
			dummy.SetRealSkillValue((int) SkillName.Magery, 500);
			dummy.MaxMana = 100;
			dummy.Mana = 100;
			Report("mageGetsDrained", Counts(timer, drainer, dummy).Get(drain) == Rolls);
			dummy.Mana = 10;
			Report("emptyMageIsLeftAlone", Counts(timer, drainer, dummy).Get(drain) == 0);
			drainer.Delete();
			dummy.Delete();
		}

		private static void SummonsReallyAppear(Character self) {
			NPC summoner = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_choice_summoner")).Create(self.X + 1, self.Y, self.Z, self.M);
			summoner.Mana = 1000;
			NPC dummy = Dummy(self);
			NPCAiTimer timer = NPCAiTimer.GetFor(summoner);
			timer.EngageTarget(dummy);
			for (int i = 0; i < 2; i++) {
				timer.Fight(summoner, dummy);
				timer.ForgetCastCooldown();
			}
			Console.WriteLine("SPELL_CHOICE_INFO: summons=" + SummonSpellDef.CountSummonsNear(summoner) + " casts=" + timer.CastAttempts);
			Report("twoSummonsAppeared", SummonSpellDef.CountSummonsNear(summoner) == 2);
			Report("noThirdSummon", Counts(timer, summoner, dummy).Get(null) == Rolls);
		}

		private static NPC Mage(Character self) {
			NPC mage = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_choice_mage")).Create(self.X + 1, self.Y, self.Z, self.M);
			mage.MaxMana = 1000;
			mage.Mana = 1000;
			return mage;
		}

		/// <summary>#1420 - an NPC's cast works its power out as a player's: its Eval Int is its mind power, times Erebor's
		/// monster multipliers, less the target's defense (the old fixed 700 knew none of it). The dummy's probe records the
		/// power the effect arrives with and cancels it.</summary>
		private static void ACastTakesTheCastersPower(Character self) {
			NPC caster = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_power_caster")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = Dummy(self);
			dummy.AddTriggerGroup(SingletonScript<E_SpellPowerProbe>.Instance);
			NPCAiTimer timer = NPCAiTimer.GetFor(caster);
			timer.EngageTarget(dummy);
			caster.MaxMana = 1000;
			MagerySettings settings = MagerySettings.instance;
			int[] got = new int[2], expected = new int[2];
			int[] evalInts = { 400, 900 };
			for (int i = 0; i < 2; i++) {
				caster.SetRealSkillValue((int) SkillName.EvalInt, evalInts[i]);
				caster.InvalidateCombatWeaponValues();
				caster.Mana = 500;
				timer.ForgetCastCooldown();
				E_SpellPowerProbe.power = int.MinValue;
				timer.Fight(caster, dummy);
				got[i] = E_SpellPowerProbe.power;
				expected[i] = (int) ((evalInts[i] * 10) * settings.spellPowerMonster * settings.spellPowerVsM) - CombatCalculator.GetSpellDefense(caster, dummy);
			}
			Console.WriteLine("SPELL_CHOICE_INFO: power at Eval Int 40.0=" + got[0] + " (" + expected[0] + "), at 90.0=" + got[1] + " (" + expected[1] + ")"
				+ " mindPower=" + caster.MindPowerVsM);
			Report("aCastTakesTheCastersPower", (got[0] == expected[0]) && (got[1] == expected[1]) && (got[1] > got[0]));
			dummy.RemoveTriggerGroup(SingletonScript<E_SpellPowerProbe>.Instance);
			caster.Delete();
			dummy.Delete();
		}

		/// <summary>#1420 - a reflected spell goes back as Erebor's dospellcustom sent it: what the caster brought (its mind
		/// power x 10), lowered by the reflection, through the reflector's multipliers (a player's against a monster, vMSM,
		/// not the monster's MvSM it was cast with) and less the caster's own defense. The caster's probe records the power
		/// arriving back and cancels it.</summary>
		private static void AReflectionGoesBackFromTheReflector(Character self) {
			int plevel = self.Account.PLevel;
			self.Account.PLevel = 1;
			NPC caster = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_power_caster")).Create(self.X + 1, self.Y, self.Z, self.M);
			try {
				caster.SetRealSkillValue((int) SkillName.EvalInt, 900);
				caster.InvalidateCombatWeaponValues();
				caster.AddTriggerGroup(SingletonScript<E_SpellPowerProbe>.Instance);
				SpellDef.GetByDefname("s_magic_reflection").ApplyEffectChar(self, self, 700, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
				MagicReflectionPlugin reflection = MagicReflectionPluginDef.GetMagicReflectionPlugin(self);
				double lowering = (reflection == null) ? 0 : reflection.EffectPower;
				E_SpellPowerProbe.power = int.MinValue;
				E_SpellPowerProbe.spellPowerRaised = 0;
				SpellDef.GetByDefname("s_magic_arrow").EffectChar(caster, self, EffectFlag.FromSpellBook);
				MagerySettings settings = MagerySettings.instance;
				int expected = (int) (((int) ((caster.MindPowerVsP * 10) * lowering)) * settings.spellPowerVsM) - CombatCalculator.GetSpellDefense(self, caster);
				Console.WriteLine("SPELL_CHOICE_INFO: reflected power=" + E_SpellPowerProbe.power + " (" + expected + ") lowering=" + lowering
					+ " mindPowerVsP=" + caster.MindPowerVsP + " castersOwnSpellPower=" + E_SpellPowerProbe.spellPowerRaised);
				Report("aReflectionGoesBackFromTheReflector", (reflection != null) && (lowering > 0) && (E_SpellPowerProbe.power == expected)
					&& (MagicReflectionPluginDef.GetMagicReflectionPlugin(self) == null));
				//the caster's own leg runs nothing: its @spellPower never fires for a spell turned back before the multipliers
				Report("theCastersLegSpendsNothing", (E_SpellPowerProbe.power != int.MinValue) && (E_SpellPowerProbe.spellPowerRaised == 0));
			} finally {
				caster.RemoveTriggerGroup(SingletonScript<E_SpellPowerProbe>.Instance);
				caster.Delete();
				MagicReflectionPlugin left = MagicReflectionPluginDef.GetMagicReflectionPlugin(self);
				if (left != null) {
					left.Delete(); //never used up: the check failed
				}
				self.Account.PLevel = (byte) plevel;
			}
		}

		/// <summary>#1507 - Paralyze rolls its power (cut by the target's ResistParalyse) against the defense, as Erebor's
		/// dospellcustom_donesrc does for a spell without spellflag_SM, instead of subtracting it; and it lasts 20-30 s
		/// (dospell_final's paralyze({200 300})), not 10 + 50 s per 1000 of power. On an NPC target the defense is its
		/// Magic Resist; the caster's Eval Int puts the power between the defense and twice it, where the roll (47.5-52.5 %
		/// of the power) never reaches the defense but power - defense is still above 0.</summary>
		private static void AParalyzeRollsAgainstTheDefense(Character self) {
			DurableCharEffectSpellDef paralyze = (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_paralyze");
			NPC caster = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_power_caster")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPC guarded = Dummy(self);
			NPC open = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 2, self.Y + 1, self.Z, self.M);
			try {
				guarded.SetRealSkillValue((int) SkillName.MagicResist, 1000);
				guarded.InvalidateCombatWeaponValues();
				open.SetRealSkillValue((int) SkillName.MagicResist, 0);
				open.InvalidateCombatWeaponValues();
				int defense = CombatCalculator.GetSpellDefense(caster, guarded);
				double kept = 1 - (guarded.ResistParalyse / 1000.0);
				MagerySettings settings = MagerySettings.instance;
				int evalInt = (int) Math.Ceiling((defense * 1.5) / (10 * settings.spellPowerMonster * settings.spellPowerVsM * Math.Max(kept, 0.01)));
				caster.SetRealSkillValue((int) SkillName.EvalInt, evalInt);
				caster.InvalidateCombatWeaponValues();
				int power = (int) ((caster.MindPowerVsM * 10) * settings.spellPowerMonster * settings.spellPowerVsM);
				int afterResist = power - ((power * guarded.ResistParalyse) / 1000);
				bool window = (defense > 0) && ((afterResist * 525) / 1000 < defense) && (afterResist > defense);

				int landed = 0;
				for (int i = 0; i < 20; i++) {
					paralyze.EffectChar(caster, guarded, EffectFlag.FromSpellBook);
					if (ParalyzeEffectPluginDef.IsParalyzed(guarded)) {
						landed++;
						ParalyzeEffectPluginDef.Unparalyze(guarded);
					}
				}

				paralyze.EffectChar(caster, open, EffectFlag.FromSpellBook);
				TimerPlugin onOpen = open.GetPlugin(paralyze.EffectPluginKey_Spell) as TimerPlugin;
				double seconds = (onOpen == null) ? -1 : onOpen.Timer;
				Console.WriteLine("SPELL_CHOICE_INFO: paralyze defense=" + defense + " power=" + power + " afterResist=" + afterResist + " window=" + window
					+ " landedOf20=" + landed + " openSeconds=" + seconds);
				Report("aParalyzeRollsAgainstTheDefense", window && (landed == 0));
				Report("aParalyzeLastsTwentyToThirty", (seconds >= 19.5) && (seconds <= 30.01));
			} finally {
				caster.Delete();
				guarded.Delete();
				open.Delete();
			}
		}

		/// <summary>#1518 - an area spell decides whom it spares before working out the power, as Erebor returns for friends
		/// before its multipliers: the caster's @spellPower runs for the two monsters it hits, never for a friend beside them
		/// (one of the caster's realm: Notoriety has no pet relation, #1522).</summary>
		private static void AnAreaSpellSparesFriendsBeforeThePower(Character self) {
			int plevel = self.Account.PLevel;
			self.Account.PLevel = 1;
			Realm realm = self.Realm;
			self.Realm = Realm.Gondor;
			NPC main = Dummy(self);
			NPC other = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 2, self.Y + 1, self.Z, self.M);
			NPC pet = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 3, self.Y, self.Z, self.M);
			try {
				other.MaxHits = 10000;
				other.Hits = 10000;
				pet.ControlMaster = self;
				pet.Realm = Realm.Gondor;
				foreach (NPC npc in new[] { main, other, pet }) {
					NPCAiTimer.Freeze(npc, 300);
				}
				E_AreaPowerProbe.reached.Clear();
				self.AddTriggerGroup(SingletonScript<E_AreaPowerProbe>.Instance);
				SpellDef explosion = SpellDef.GetByDefname("s_explosion");
				explosion.Trigger_Success(SkillSequenceArgs.Acquire(self, SkillName.Magery, main, null, null, explosion, null));
				Console.WriteLine("SPELL_CHOICE_INFO: area power for main=" + E_AreaPowerProbe.reached.Contains(main) + " other="
					+ E_AreaPowerProbe.reached.Contains(other) + " pet=" + E_AreaPowerProbe.reached.Contains(pet) + " petHits=" + pet.Hits + "/" + pet.MaxHits);
				Report("anAreaSpellSparesFriendsBeforeThePower", E_AreaPowerProbe.reached.Contains(main) && E_AreaPowerProbe.reached.Contains(other)
					&& !E_AreaPowerProbe.reached.Contains(pet) && (pet.Hits == pet.MaxHits));
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_AreaPowerProbe>.Instance);
				main.Delete();
				other.Delete();
				pet.Delete();
				self.Realm = realm;
				self.Account.PLevel = (byte) plevel;
			}
		}

		private static NPC Dummy(Character self) {
			NPC dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;
			return dummy;
		}

		private static Dictionary<SpellDef, int> Counts(NPCAiTimer timer, NPC caster, Character target) {
			Dictionary<SpellDef, int> counts = new Dictionary<SpellDef, int>();
			int nothing = 0;
			for (int i = 0; i < Rolls; i++) {
				SpellDef spell = timer.ChooseSpellToCast(caster, target);
				if (spell == null) {
					nothing++;
				} else {
					int count;
					counts.TryGetValue(spell, out count);
					counts[spell] = count + 1;
				}
			}
			counts[NothingCast] = nothing;
			return counts;
		}

		private static SpellDef NothingCast {
			get { return SpellDef.GetByDefname("s_clumsy"); } //no test caster knows it
		}

		private static int Get(this Dictionary<SpellDef, int> counts, SpellDef spell) {
			int count;
			counts.TryGetValue(spell ?? NothingCast, out count);
			return count;
		}

		private static string Describe(Dictionary<SpellDef, int> counts) {
			List<string> parts = new List<string>();
			foreach (KeyValuePair<SpellDef, int> pair in counts) {
				parts.Add(pair.Key.Name + "=" + pair.Value);
			}
			return string.Join(", ", parts);
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("SPELL_CHOICE: " + name + "=" + ok);
		}
	}

	/// <summary>#1518 - on the caster: whom @spellPower was worked out for.</summary>
	public class E_AreaPowerProbe : CompiledTriggerGroup {
		internal static readonly List<Character> reached = new List<Character>();

		public void On_SpellPower(Character self, SpellPowerArgs args) {
			if (args.Target is Character ch) {
				reached.Add(ch);
			}
		}
	}

	/// <summary>#1420 - records the power a spell's effect arrives with, and cancels the effect.</summary>
	public class E_SpellPowerProbe : CompiledTriggerGroup {
		internal static int power, spellPowerRaised;

		public void On_SpellPower(Character self, SpellPowerArgs args) {
			spellPowerRaised++;
		}

		public TriggerResult On_SpellEffect(Character self, SpellEffectArgs args) {
			power = args.SpellPower;
			return TriggerResult.Cancel;
		}
	}
}
