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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class MagerySkillDef : SkillDef {
		//skillTarget1: spell target
		//skillTarget2: scroll or spellbook item
		//skillParam1: SpellDef instance
		//skillParam2: spell param (summoned creature def?)

		private static readonly TriggerKey tkSpellCast = TriggerKey.Acquire("spellCast");
		private static readonly TriggerKey tkSpellCastTry = TriggerKey.Acquire("spellCastTry");
		private static readonly TriggerKey tkDenySpellCast = TriggerKey.Acquire("denySpellCast");

		public static void TryCastSpellFromBook(Character ch, int spellid) {
			SpellDef sd = SpellDef.GetById(spellid);
			if (sd != null) {
				TryCastSpellFromBook(ch, sd);
			} else {
				ch.ClilocMessage(502345); // This spell has been temporarily disabled.
			}
		}

		public static void TryCastSpellFromBook(Character ch, SpellDef spellDef) {
			Sanity.IfTrueThrow(spellDef == null, "spellDef == null");

			SpellBook book = ch.FindLayer(1) as SpellBook;
			if (book != null) {
				if (!book.HasSpell(spellDef)) {
					book = null;
				}
			}
			if (book == null) {
				foreach (Item i in ch.Backpack) {
					book = i as SpellBook;
					if (book != null) {
						if (book.HasSpell(spellDef)) {
							break;
						}
					}
				}
			}
			if (book != null) {
				SkillSequenceArgs magery = SkillSequenceArgs.Acquire(ch, SkillName.Magery, book, spellDef);
				magery.PhaseSelect();
			} else {
				ch.ClilocSysMessage(500015); // You do not have that spell!
			}
		}

		public static void TryCastSpellFromScroll(Character ch, SpellScroll scroll) {
			Sanity.IfTrueThrow(scroll == null, "scroll == null");

			if (scroll.TopObj() == ch) {
				SpellDef spellDef = scroll.SpellDef;
				if (spellDef != null) {
					SkillSequenceArgs magery = SkillSequenceArgs.Acquire(ch, SkillName.Magery, scroll, spellDef);
					magery.PhaseSelect();
				} else {
					ch.ClilocMessage(502345); // This spell has been temporarily disabled.
				}
			} else {
				ch.ClilocSysMessage(1042001); // That must be in your pack for you to use it.
			}
		}

		public MagerySkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;

			if (skillSeqArgs.Tool != null) {
				SpellBook book = skillSeqArgs.Tool as SpellBook;
				if (book != null) {
					if ((book.TopObj() == self) && book.HasSpell(spell)) {
						if (!SummonMenu.Intercept(skillSeqArgs, spell) && !PolymorphMenu.Intercept(skillSeqArgs, spell)) {
							spell.Trigger_Select(skillSeqArgs);
						}
						return TriggerResult.Cancel; //cancel here, the rest of Select is being done by SpellDef
					}
				}

				SpellScroll scroll = skillSeqArgs.Tool as SpellScroll;
				if (scroll != null) {
					if (!SummonMenu.Intercept(skillSeqArgs, spell) && !PolymorphMenu.Intercept(skillSeqArgs, spell)) {
						spell.Trigger_Select(skillSeqArgs);
					}
					return TriggerResult.Cancel; //cancel here, the rest of Select is being done by SpellDef
				}
			}

			throw new SEException("Casting without book or scroll not implemented yet."); //would apply for NPCs perhaps?
		}

		//checking and consuming resources
		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.CanInteractWithMessage(skillSeqArgs.Target1)) {
				SpellDef spell = (SpellDef) skillSeqArgs.Param1;

				bool isFromScroll = skillSeqArgs.Tool is SpellScroll;

				int manaUse = ApplyConcentrationDiscount(self, spell.GetManaUse(isFromScroll));
				int mana = self.Mana;
				if ((mana >= manaUse) || self.IsGM) {
					if (!isFromScroll && !self.IsGM) {
						ResourcesList req = spell.Requirements;
						IResourceListEntry missingItem;
						if ((req != null) && (!req.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingItem))) {
							self.SysMessage(Loc<MageryRulesLoc>.Get(self.Language).LackSkill);
							return TriggerResult.Cancel;
						}

						ResourcesList res = spell.Resources;
						if ((res != null) && (!res.HasResourcesPresent(self, ResourcesLocality.Backpack, out missingItem))) {
							self.SysMessage(missingItem.GetResourceMissingMessage(self.Language));
							return TriggerResult.Cancel;
						}
					}

					//#1401 - Erebor's @spellcast in two: its first lines (hungryhurts) ran before its own refusals (SpellCastRules)
					self.TryTrigger(tkSpellCastTry, skillSeqArgs.scriptArgs);
					if (self.Flag_Dead) {
						return TriggerResult.Cancel; //starved to death by it
					}
					if (!SpellCastRules.Allow(self, spell, skillSeqArgs.Target1, out bool fizzle)) {
						if (fizzle) {
							Fizzle(self);
						}
						return TriggerResult.Cancel;
					}
					//#1401 - the refusals of a caster's other events (the energy shield's Recall), after Erebor's e_allplayers
					if (TriggerResult.Cancel == self.TryCancellableTrigger(tkDenySpellCast, skillSeqArgs.scriptArgs)) {
						return TriggerResult.Cancel;
					}
					if (skillSeqArgs.Param2 is NPCDef) {
						SummonMenu.PayAtStart(self);
					}

					if (TriggerResult.Cancel == spell.Trigger_Start(skillSeqArgs)) {
						return TriggerResult.Cancel;
					}
					self.TryTrigger(tkSpellCast, skillSeqArgs.scriptArgs); //#1386 - Sphere's @spellCast, the cast is under way
					if (self.Flag_Dead) {
						return TriggerResult.Cancel;
					}

					SpellCastRules.DropManaShield(self, spell, skillSeqArgs.Target1);
					AnimCalculator.PerformAnim(self, GenericAnim.Cast);

					self.AbortSkill();
					skillSeqArgs.DelayInSeconds = self.IsGM ? GmCastSeconds : spell.CastTime;
					skillSeqArgs.DelayStroke();
					string runeWords = spell.GetRuneWords();
					if (!string.IsNullOrEmpty(runeWords)) {
						self.Speech(runeWords, 0, SpeechType.Spell, -1, ClientFont.Unified, null, null);
					}

					return TriggerResult.Cancel; ; //default = set delay by magery skilldef, which we don't want
				}
				self.ClilocSysMessage(502625); // Insufficient mana for this spell.
			}

			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;

			bool isFromScroll;
			Item tool = skillSeqArgs.Tool;
			if (tool != null) {
				SpellBook book = skillSeqArgs.Tool as SpellBook;
				if (book != null) {
					isFromScroll = false;
					if (book.IsDeleted || (book.TopObj() != self)) {
						self.ClilocSysMessage(501608);	//You don't have a spellbook.
						return TriggerResult.Cancel;
					}
					if (!book.HasSpell(spell)) {
						self.ClilocSysMessage(501902);	//You don't know that spell.
						return TriggerResult.Cancel;
					}
				} else if (tool is SpellScroll) { //it might be deleted by now, but we can still check for it's type...
					isFromScroll = true;
				} else {
					throw new SEBugException("Magery tool is neither book nor scroll?");
				}
			} else {
				throw new SEBugException("No magery tool - not implemented");
			}

			if (!self.CanInteractWithMessage(skillSeqArgs.Target1)) {
				return TriggerResult.Cancel;
			}

			skillSeqArgs.Success = spell.IsCustom ? SpellCastRules.CustomCastHolds(self, spell) : this.CheckSuccess(self, spell.GetDifficulty(isFromScroll));

			return TriggerResult.Continue;
		}

		private const double GmCastSeconds = 0.1;
		/// <summary>#962 - Sphere pays at the end of a cast (Spell_CanCast). A failed one costs half the mana and a bit more,
		/// and its reagents, REAGENTLOSSFAIL=1. False when there's not enough left to pay.</summary>
		private static bool Pay(SkillSequenceArgs skillSeqArgs, bool failed) {
			Character self = skillSeqArgs.Self;
			if (self.IsGM) {
				return true;
			}
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;
			bool isFromScroll = skillSeqArgs.Tool is SpellScroll;
			int manaUse = ApplyConcentrationDiscount(self, spell.GetManaUse(isFromScroll));
			if (self.Mana < manaUse) {
				self.ClilocSysMessage(502625); // Insufficient mana for this spell.
				return false;
			}
			if (failed) {
				manaUse = spell.IsCustom ? (manaUse / 2) : ((manaUse / 2) + Globals.dice.Next((manaUse / 2) + (manaUse / 4)));
			}
			self.Mana = (short) (self.Mana - manaUse);
			if (isFromScroll) {
				if (!skillSeqArgs.Tool.IsDeleted) {
					skillSeqArgs.Tool.Consume(1);
				}
			} else {
				ResourcesList res = spell.Resources;
				IResourceListEntry missingItem;
				if ((res != null) && !res.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missingItem)) {
					self.SysMessage(missingItem.GetResourceMissingMessage(self.Language));
					return false;
				}
			}
			return true;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;
			if (!Pay(skillSeqArgs, false)) {
				return;
			}
			spell.Trigger_Success(skillSeqArgs);
			if ((spell.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				CombatWear.OnHarmfulCast(self); //#865
			}
			if (self is Player caster) {
				MagicChargePlugin.AfterCast(caster, spell, skillSeqArgs.Target1 as Character); //#1017
				NecroAbilities.SpellDone(caster, spell, skillSeqArgs.Tool is SpellScroll);
			}
			if (!self.IsDeleted) { //#1419 - @spellSuccess, after the effect and the inline after-cast work
				self.TryTrigger(tkSpellSuccess, new ScriptArgs(new SpellSuccessArgs(self, spell, skillSeqArgs.Target1, skillSeqArgs.Tool is SpellScroll)));
			}
		}

		private static readonly TriggerKey tkSpellSuccess = TriggerKey.Acquire("spellSuccess");

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
			Pay(skillSeqArgs, true);
		}

		private static AbilityDef concentrationDef;
		private static AbilityDef ConcentrationDef {
			get {
				if (concentrationDef == null) {
					concentrationDef = AbilityDef.GetByDefname("a_concentration");
				}
				return concentrationDef;
			}
		}

		/// <summary>#483/#491 - Concentration: mananeed = mananeed * nastaveni_ability_concentration
		/// / 1000 in source, i.e. a flat per-point mana-cost discount. No-op (returns manaUse
		/// unchanged) for any caster without the ability, which is every pre-existing test -
		/// critical regression-safety property for this phase's own cross-cutting changes.</summary>
		internal static int ApplyConcentrationDiscount(Character self, int manaUse) {
			AbilityDef def = ConcentrationDef;
			if (def == null) {
				return manaUse;
			}
			int points = self.GetAbility(def);
			if (points <= 0) {
				return manaUse;
			}
			double discount = System.Math.Min(points * MagerySettings.instance.concentrationDiscountPerPoint, 0.9);
			return System.Math.Max(1, (int) System.Math.Round(manaUse * (1.0 - discount)));
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
			Pay(skillSeqArgs, true);
		}

		internal static void Fizzle(Character self) {
			EffectFactory.StationaryEffect(self, 0x3735, 6, 30);
			self.Sound((int) SoundNames.Fizzle);
			self.ClilocMessage(502632); // The spell fizzles.
		}
	}
}