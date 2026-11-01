using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helpers for Tests/Protocol/FigurineTests.cs.</summary>
	public static class FigurineTests {
		private const int DefaultModel = 0x20F6;

		/// <summary>Every creature's ICON gets read, and only the archive's broken ones fail.</summary>
		[SteamFunction]
		public static void TestFigurineIcons(Character self) {
			int ok = 0, none = 0, broken = 0;
			string brokenNames = "";
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				NPCDef def = script as NPCDef;
				if (def != null) {
					try {
						if (def.Icon == 0) {
							none++;
						} else {
							ok++;
						}
					} catch (SEException) {
						broken++;
						brokenNames += def.PrettyDefname + " ";
					}
				}
			}
			Console.WriteLine("FIGURINE_ICONS: ok=" + ok + " none=" + none + " broken=" + broken + " " + brokenNames);
		}

		/// <summary>A real pet of that kind shrunk: the figurine's model against the model of the item its ICON names.</summary>
		[SteamFunction]
		public static void TestFigurineOf(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			NPC pet = (NPC) ThingDef.GetByDefname(args[0]).Create(self.X, self.Y, self.Z, self.M);
			pet.Color = 0x21;
			PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(self.Backpack);
			figurine.Keep(pet);
			pet.Delete();
			int expected = ThingDef.GetByDefname(args[1]).Model;
			Console.WriteLine("FIGURINE_OF: " + args[0] + " matches=" + (figurine.Model == expected) + " notLlama=" + (figurine.Model != DefaultModel)
				+ " color=" + figurine.Color + " uid=" + (uint) figurine.FlaggedUid);
		}

		/// <summary>No ICON: still a figurine, with the old picture.</summary>
		[SteamFunction]
		public static void TestFigurineNoIcon(Character self) {
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				NPCDef def = script as NPCDef;
				if ((def != null)) {
					try {
						if (def.Icon != 0) {
							continue;
						}
						NPC pet = (NPC) def.Create(self.X, self.Y, self.Z, self.M);
						PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(self.Backpack);
						figurine.Keep(pet);
						pet.Delete();
						Console.WriteLine("FIGURINE_NOICON: model=" + figurine.Model);
						return;
					} catch (SEException) {
					}
				}
			}
		}

		[SteamFunction]
		public static void TestFigurineLoaded(Character self) {
			foreach (AbstractCharacter ch in self.Account.Characters) {
				foreach (AbstractItem item in ((Character) ch).Backpack) {
					PetFigurine figurine = item as PetFigurine;
					if (figurine != null) {
						Console.WriteLine("FIGURINE_LOADED: model=" + figurine.Model + " notLlama=" + (figurine.Model != DefaultModel));
					}
				}
			}
		}
	}
}
