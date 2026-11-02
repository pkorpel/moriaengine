using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's f_alore, run from @start: at once, with no stroke and no gain. Anyone can be looked at, and
	/// what's said comes overhead to the one looking. What riding an animal gives comes with mount powers, #925.</summary>
	[ViewableClass]
	public class AnimalLoreSkillDef : SkillDef {
		private const int RollOf = 1001, MostFoodTypesShown = 4;

		public AnimalLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_AnimalLore>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = skillSeqArgs.Target1 as Character;
			if (target == null) {
				self.RedMessage(Loc<AnimalLoreLoc>.Get(self.Language).NotAnAnimal);
			} else {
				Look(self, target);
			}
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

		private static void Look(Character self, Character target) {
			AnimalLoreLoc texts = Loc<AnimalLoreLoc>.Get(self.Language);
			self.Message((target.ControlMaster != null) ? string.Format(texts.BelongsTo, target.Name, target.ControlMaster.Name) : string.Format(texts.OwnMaster, target.Name));
			int lore = self.GetSkill(SkillName.AnimalLore);
			NPC mount = self.Mount as NPC;
			if ((lore > Globals.dice.Next(RollOf)) && (target.IsAnimal || (target.GetSkill(SkillName.AnimalTaming) > 0))) {
				self.Message(FoodLine(target, target.Name, texts));
				if (lore >= target.GetSkill(SkillName.AnimalLore)) {
					self.Message(string.Format(texts.Eats, target.Name, FoodNames(target)));
				} else {
					self.Message(texts.OnlyYavannaKnows);
				}
			} else if ((target == self) && (mount != null)) {
				self.Message(FoodLine(mount, mount.Name, texts));
			} else {
				self.Message(self.IsFemale ? texts.NothingFemale : texts.NothingMale);
			}
		}

		private static string FoodLine(Character eater, string name, AnimalLoreLoc texts) {
			int food, most;
			if (eater is NPC npc) {
				most = ((NPCDef) npc.Def).MaxFood;
				food = PetHungerTimer.FoodOf(npc, most);
			} else {
				most = PlayerHunger.MaxFood;
				food = PlayerHunger.FoodOf(eater);
			}
			string line = (food < (most / 4)) ? texts.VeryHungry : (food < (most / 2)) ? texts.NotFull : (food < most) ? texts.CouldEat : texts.Stuffed;
			return string.Format(line, name);
		}

		//f_showfoodtype: Erebor's names for types, an item by its own
		private static string FoodNames(Character target) {
			List<string> names = new List<string>();
			List<NPCDef.FoodTypeEntry> eats = (target.Def as NPCDef)?.FoodTypes;
			if (eats != null) {
				foreach (NPCDef.FoodTypeEntry entry in eats) {
					if (names.Count < MostFoodTypesShown) {
						Constant name = Constant.GetByName("typename_" + entry.CategoryOrDefname);
						names.Add((name != null) ? name.Value.ToString() : (ThingDef.GetByDefname(entry.CategoryOrDefname)?.Name ?? entry.CategoryOrDefname));
					}
				}
			}
			return string.Join(",", names);
		}
	}

	public class Targ_AnimalLore : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<AnimalLoreLoc>.Get(self.Language).TargetWho);
			base.On_Start(self, parameter);
		}

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

	public class AnimalLoreLoc : CompiledLocStringCollection<AnimalLoreLoc> {
		public string TargetWho = "Jake zvire chces prozkoumat?";
		public string NotAnAnimal = "To neni zvire.";
		public string BelongsTo = "{0} patri osobe jmenem {1}.";
		public string OwnMaster = "{0} je svym vlastnim panem.";
		public string VeryHungry = "{0} ma velky hlad.";
		public string NotFull = "{0} jeste zdaleka nema plny zaludek.";
		public string CouldEat = "{0} jeste nejake to jidlo zvladne.";
		public string Stuffed = "{0} ma dost a funi.";
		public string Eats = "{0} se zivi vecmi jako: {1}.";
		public string OnlyYavannaKnows = "ale snad jen Yavanna vi, co ta potvora zere.";
		public string NothingMale = "Nic jsi nezjistil.";
		public string NothingFemale = "Nic jsi nezjistila.";
	}
}
