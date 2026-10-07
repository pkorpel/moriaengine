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
						spell.Trigger_Select(skillSeqArgs);
						return TriggerResult.Cancel; //cancel here, the rest of Select is being done by SpellDef
					}
				}

				SpellScroll scroll = skillSeqArgs.Tool as SpellScroll;
				if (scroll != null) {
					spell.Trigger_Select(skillSeqArgs);
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

				int manaUse = spell.GetManaUse(isFromScroll);
				int mana = self.Mana;
				if (mana >= manaUse) {
					if (!isFromScroll) {
						ResourcesList req = spell.Requirements;
						IResourceListEntry missingItem;
						if ((req != null) && (!req.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingItem))) {
							self.SysMessage(missingItem.GetResourceMissingMessage(self.Language));
							return TriggerResult.Cancel; ;
						}

						ResourcesList res = spell.Resources;
						if ((res != null) && (!res.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missingItem))) {
							self.SysMessage(missingItem.GetResourceMissingMessage(self.Language));
							return TriggerResult.Cancel; ;
						}
					} else {
						skillSeqArgs.Tool.Consume(1);
					}

					if (TriggerResult.Cancel == spell.Trigger_Start(skillSeqArgs)) {
						return TriggerResult.Cancel;
					}

					self.Mana = (short) (mana - manaUse);
					AnimCalculator.PerformAnim(self, GenericAnim.Cast);

					self.AbortSkill();
					skillSeqArgs.DelayInSeconds = spell.CastTime;
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

			skillSeqArgs.Success = this.CheckSuccess(self, spell.GetDifficulty(isFromScroll));

			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			SpellDef spell = (SpellDef) skillSeqArgs.Param1;
			spell.Trigger_Success(skillSeqArgs);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Fizzle(skillSeqArgs.Self);
		}

		private static void Fizzle(Character self) {
			EffectFactory.StationaryEffect(self, 0x3735, 6, 30);
			self.Sound((int) SoundNames.Fizzle);
			self.ClilocMessage(502632); // The spell fizzles.
		}
	}
}