using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/AnimalLoreTests.cs.</summary>
	public static class AnimalLoreTestHelpers {
		private static NPC animal;

		[SteamFunction]
		public static void TestLorePrepare(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.AnimalLore, int.Parse(sa.Args.Trim('"'), CultureInfo.InvariantCulture));
			self.SetRealSkillValue((int) SkillName.AnimalTaming, 0);
			Console.WriteLine("LORE_PREPARED: " + self.Uid);
		}

		[SteamFunction]
		public static void TestLoreAnimal(Character self, ScriptArgs sa) {
			string[] words = sa.Args.Trim('"').Split(' ');
			animal = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			animal.Name = "Test Rabbit";
			animal.SetRealSkillValue((int) SkillName.AnimalLore, int.Parse(words[0], CultureInfo.InvariantCulture));
			if (words.Length > 1) {
				animal.ControlMaster = self;
				PetHungerTimer.LoseFood(animal, int.Parse(words[1], CultureInfo.InvariantCulture));
			}
			Console.WriteLine("LORE_ANIMAL: npc=#" + animal.Uid + " foods=" + ((NPCDef) animal.Def).FoodTypes?.Count + " max=" + ((NPCDef) animal.Def).MaxFood);
		}

		[SteamFunction]
		public static void TestLoreItem(Character self) {
			Item item = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("LORE_ITEM: #" + item.Uid);
		}
	}
}
