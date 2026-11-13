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

using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Sphere's OnSkill_Info: a cursor with no reach check, a difficulty from the target, and what's seen after the
	/// skill's delay. Sphere's own texts stay in English, Erebor kept them; its @Fail lines are Czech. Nobody gains by use.</summary>
	public abstract class InformationSkillDef : SkillDef {
		protected const int DifficultyOfOneself = 2, MostDifficulty = 60, DifficultyScale = 10, NotApplicable = -1;

		protected InformationSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>Under 0 it's over, a message said why.</summary>
		protected abstract int DifficultyOf(Character self, Thing target);

		protected abstract void Show(Character self, Thing target);

		protected abstract string Prompt(InformationLoc texts);

		protected abstract string FailMessage(Character self, InformationLoc texts);

		protected static int Difficulty(Character self, Thing target) {
			return (target == self) ? DifficultyOfOneself : Globals.dice.Next(MostDifficulty);
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.Self is Player self) {
				self.SysMessage(this.Prompt(Loc<InformationLoc>.Get(self.Language)));
				self.Target(SingletonScript<Targ_Information>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			int difficulty = this.DifficultyOf(self, (Thing) skillSeqArgs.Target1);
			if (difficulty < 0) {
				return TriggerResult.Cancel;
			}
			skillSeqArgs.Success = (difficulty == 0) || this.CheckSuccess(self, difficulty * DifficultyScale);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			this.Show(skillSeqArgs.Self, (Thing) skillSeqArgs.Target1);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.MessageTo(self, this.FailMessage(self, Loc<InformationLoc>.Get(self.Language))); //#1502 - a stroke: no Globals.Src
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	public class Targ_Information : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return Start(targetted, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return Start(targetted, parameter);
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}

		private static TargetResult Start(Thing targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}
	}

	[ViewableClass]
	public class AnatomySkillDef : InformationSkillDef {
		private static readonly string[] strengths = { "rather feeble", "somewhat weak", "to be of normal strength", "somewhat strong", "very strong",
			"extremely strong", "extraordinarily strong", "as strong as an ox", "like one of the strongest people you have ever seen", "superhumanly strong" };
		private static readonly string[] dexterities = { "very clumsy", "somewhat uncoordinated", "moderately dexterous", "somewhat agile", "very agile",
			"extremely agile", "extraordinarily agile", "like they move like quicksilver", "like one of the fastest people you have ever seen", "superhumanly agile" };

		public AnatomySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.AnatomyPrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return self.IsFemale ? texts.AnatomyFailFemale : texts.AnatomyFailMale;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			if (target is Character) {
				return Difficulty(self, target);
			}
			self.SysMessage("That does not appear to be a living being.");
			return 1;
		}

		protected override void Show(Character self, Thing target) {
			Character ch = target as Character;
			if (ch != null) {
				ch.MessageTo(self, ch.Name + " looks " + strengths[Index(ch.Str)] + " and " + dexterities[Index(ch.Dex)] + "."); //#1502 - to the one asking
				if (SummonSpellDef.IsSummonedCreature(ch)) {
					ch.MessageTo(self, "This is a magical creature");
				}
			}
		}

		//99z8's unsigned division: nothing at all reads as the top
		private static int Index(int stat) {
			return (stat < 1) ? 9 : System.Math.Min((stat - 1) / 10, 9);
		}
	}

	[ViewableClass]
	public class ArmsLoreSkillDef : InformationSkillDef {
		private const int Fragile = 3;

		public ArmsLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.ArmsLorePrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return texts.ArmsLoreFail;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			if ((target is Weapon) || (target is Wearable)) {
				return Difficulty(null, target);
			}
			self.SysMessage("That does not appear to be a weapon or armor.");
			return NotApplicable;
		}

		protected override void Show(Character self, Thing target) {
			Destroyable item = (Destroyable) target;
			string text = (item is Weapon weapon) ? "Attack [" + (int) weapon.AttackVsP + "]." : "Defense [" + ((Wearable) item).ArmorVsP + "].";
			text += " This item is " + Repair(item) + ".";
			if ((item.Durability <= Fragile) || (item.MaxDurability <= Fragile)) {
				text += " It looks quite fragile.";
			}
			if (item.Flag_Newbied || item.Flag_NonMovable) {
				text += " It is has a slight magical aura.";
			}
			self.SysMessage(text);
		}

		private static string Repair(Destroyable item) {
			int current = item.Durability, most = item.MaxDurability;
			if (current > most) {
				return "absolutely flawless";
			}
			if (current == most) {
				return "in full repair";
			}
			if (current > (most / 2)) {
				return "a bit worn";
			}
			if (current > (most / 3)) {
				return "well worn";
			}
			return (current > Fragile) ? "badly damaged" : "about to fall apart";
		}
	}

	/// <summary>Erebor's @start says who made it and how worn it is, Sphere's skill follows. Nothing has a price yet, so
	/// no item has a resale value.</summary>
	[ViewableClass]
	public class ItemIdSkillDef : InformationSkillDef {
		private const int RollOf = 1000, DurabilityRollOf = 800, TailorKnows = 800, MostFromKnown = 20, KnowsMaterialFrom = 40;
		private static readonly TagKey identifiedTK = TagKey.Acquire("_identified_");

		public ItemIdSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.ItemIdPrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return texts.ItemIdFail;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			InformationLoc texts = Loc<InformationLoc>.Get(self.Language);
			int skill = self.GetSkill(SkillName.ItemID);
			Item item = target as Item;
			if (skill > Globals.dice.Next(RollOf)) {
				self.SysMessage(string.Format(texts.ItemIdMaker, (item == null) ? "" : CraftingSkillDef.GetCrafter(item)?.Name));
				if ((item is Destroyable worn) && (CraftingSkillDef.IsOfType(item, "t_clothing") ? (self.GetSkill(SkillName.Tailoring) > TailorKnows)
						: (CraftingSkillDef.IsOfType(item, "t_weapon_bow_run", "t_weapon_sword", "t_armor") && (skill > Globals.dice.Next(DurabilityRollOf))))) {
					self.SysMessage(string.Format(texts.ItemIdDurability, worn.Durability, worn.MaxDurability));
				}
			} else {
				self.SysMessage(texts.ItemIdUnknown);
			}
			if (target is Character) {
				return 1;
			}
			return Globals.dice.Next(target.HasTag(identifiedTK) ? MostFromKnown : MostDifficulty);
		}

		protected override void Show(Character self, Thing target) {
			if (target is Item item) {
				item.SetTag(identifiedTK, true);
				self.SysMessage("The item does not apear to have any real resale value.");
				string resources = (item.Def as ItemDef)?.Resources?.ToString();
				if ((self.GetSkill(SkillName.ItemID) > KnowsMaterialFrom) && !string.IsNullOrEmpty(resources)) {
					self.SysMessage("It is made of " + resources);
				}
			} else {
				self.SysMessage("That appears to be '" + target.Name + "'.");
			}
		}
	}

	[ViewableClass]
	public class EvalIntSkillDef : InformationSkillDef {
		private const int KnowsMagicFrom = 400, MagicStep = 200;
		private static readonly string[] minds = { "dumb as a rock", "fairly stupid", "not the brightest", "about average", "moderately intelligent",
			"very intelligent", "like a formidable intellect", "extraordinarily intelligent", "like a definite genius", "superhumanly intelligent" };
		private static readonly string[] magics = { "clueless about magic", "to have vague grasp of magic", "capable of using magic",
			"higly capable with magic", "to be adept of magic", "to have mastery of magic" };
		private static readonly string[] manas = { "nearly absent of mana", "low on mana", "half drained of mana",
			"have a high charge of mana", "full of mana", "super charged with mana" };

		public EvalIntSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.EvalIntPrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return self.IsFemale ? texts.EvalIntFailFemale : texts.EvalIntFailMale;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			if (target is Character) {
				return Difficulty(self, target);
			}
			self.SysMessage("That does not appear to be a living being.");
			return 1;
		}

		protected override void Show(Character self, Thing target) {
			Character ch = target as Character;
			if (ch == null) {
				return;
			}
			self.SysMessage(ch.Name + " looks " + minds[Clamp(ch.Int / 10, minds)] + ".");
			if (self.GetSkill(SkillName.EvalInt) > KnowsMagicFrom) {
				int magic = System.Math.Max(ch.GetSkill(SkillName.Magery), ch.GetSkill(SkillName.Necromancy));
				int mana = (ch.Int > 0) ? (ch.Mana * (manas.Length - 1)) / ch.Int : 0;
				self.SysMessage("They look " + magics[Clamp(magic / MagicStep, magics)] + " and " + manas[Clamp(mana, manas)] + ".");
			}
		}

		private static int Clamp(int index, string[] of) {
			return System.Math.Max(0, System.Math.Min(index, of.Length - 1));
		}
	}

	[ViewableClass]
	public class ForensicsSkillDef : InformationSkillDef {
		public ForensicsSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.ForensicsPrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return texts.ForensicsFail;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			Corpse corpse = target as Corpse;
			if (corpse == null) {
				self.SysMessage("Forensics must be used on a corpse.");
				return NotApplicable;
			}
			if (!self.CanReach(corpse).Allow) {
				self.SysMessage("You are too far away to tell much.");
				return NotApplicable;
			}
			return (corpse.Owner == self) ? DifficultyOfOneself : Globals.dice.Next(MostDifficulty);
		}

		protected override void Show(Character self, Thing target) {
			Corpse corpse = (Corpse) target;
			string killer = corpse.KilledBy;
			string detail = string.IsNullOrEmpty(killer) ? "You cannot determine who killed it" : "It looks to have been killed by " + killer;
			self.SysMessage("This is " + corpse.Name + " and it is " + corpse.SecondsDead.ToString(CultureInfo.InvariantCulture) + " seconds old. " + detail);
		}
	}

	/// <summary>Erebor's @success tells of the poison on a thing, Sphere's skill then says what it tastes like.</summary>
	[ViewableClass]
	public class TasteIdSkillDef : InformationSkillDef {
		private const int RollOf = 1000;

		public TasteIdSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override string Prompt(InformationLoc texts) {
			return texts.TasteIdPrompt;
		}

		protected override string FailMessage(Character self, InformationLoc texts) {
			return texts.TasteIdFail;
		}

		protected override int DifficultyOf(Character self, Thing target) {
			Item item = target as Item;
			if (item == null) {
				self.SysMessage((target == self) ? "Yummy!" : "Try tasting some item.");
				return NotApplicable;
			}
			if (!self.CanReach(item).Allow) {
				self.SysMessage("You can't use this.");
				return NotApplicable;
			}
			if (CraftingSkillDef.IsOfType(item, "t_potion", "t_fruit", "t_food", "t_food_raw", "t_meat_raw", "t_weapon_mace_sharp", "t_weapon_sword", "t_weapon_fence", "t_weapon_axe")) {
				return Globals.dice.Next(MostDifficulty);
			}
			self.SysMessage("It tastes like " + item.Name + ".");
			return 1;
		}

		protected override void Show(Character self, Thing target) {
			InformationLoc texts = Loc<InformationLoc>.Get(self.Language);
			Item item = (Item) target;
			if (self.GetSkill(SkillName.TasteID) > Globals.dice.Next(RollOf)) {
				PoisonedItemPlugin poison = PoisonedItemPlugin.GetPoisonPlugin(item);
				self.SysMessage((poison != null) ? string.Format(texts.TasteIdPoisoned, poison.PoisonName) : texts.TasteIdClean);
			} else {
				self.SysMessage(texts.TasteIdFail);
			}
		}
	}

	public class InformationLoc : CompiledLocStringCollection<InformationLoc> {
		public string AnatomyPrompt = "Koho si prejes prozkoumat?";
		public string AnatomyFailMale = "Pruzkumem anatomie jsi nic nezjistil.";
		public string AnatomyFailFemale = "Pruzkumem anatomie jsi nic nezjistila.";
		public string ArmsLorePrompt = "Jakou zbroj ci zbran si prejes prozkoumat?";
		public string ArmsLoreFail = "Nedokazes zjistit nic blizsiho.";
		public string ItemIdPrompt = "Co si prejes identifikovat?";
		public string ItemIdFail = "Nepodarilo se ti predmet identifikovat.";
		public string ItemIdMaker = "Vyrobce: {0}";
		public string ItemIdDurability = "Durabilita: {0}/{1}";
		public string ItemIdUnknown = "Puvod teto veci je ti neznamy.";
		public string EvalIntPrompt = "Vyber charakter pro odhad.";
		public string EvalIntFailMale = "Nejsi schopen nic odhadnout.";
		public string EvalIntFailFemale = "Nejsi schopna nic odhadnout.";
		public string ForensicsPrompt = "Ci telo chces prozkoumat?";
		public string ForensicsFail = "Nepodarilo se ti zjistit zadne detaily o smrti.";
		public string TasteIdPrompt = "Co si prejes ochutnat?";
		public string TasteIdFail = "Nepodarilo se ti zjistit slozeni ani stopy jedu.";
		public string TasteIdPoisoned = "Otravena : {0}";
		public string TasteIdClean = "Neni otravena.";
	}
}
