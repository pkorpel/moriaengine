using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - helpers for Tests/Protocol/CoreSpellsTests.cs. Until TestCorePlayer they cast as a GM, who skips the skill requirement.</summary>
	public static class CoreSpellsTestHelpers {
		private static readonly string[] foods = { "i_bacon_slab", "i_sausages", "i_ham", "i_cake", "i_bread_loaf" };
		private static Item box, farDoor;
		private static NPC mage, rabbit;
		private static SpellBook book;
		private static Point4D ground;

		[SteamFunction]
		public static void TestCoreReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			((Player) self).Profession = Professions.Mage;
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			book = (SpellBook) ThingDef.GetByDefname("i_spellbook").Create(self.Backpack);
			foreach (string spell in new[] { "s_create_food", "s_magic_lock", "s_unlock", "s_telekinesis", "s_incognito", "s_mana_vampire", "s_polymorph" }) {
				book.AddSpell(SpellDef.GetByDefname(spell));
			}
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y + 1, self.Z, self.M);
			box = (Item) ThingDef.GetByDefname("i_crate_small").Create(self.X + 2, self.Y, self.Z, self.M);
			farDoor = (Item) ThingDef.GetByDefname("i_door_metal").Create(self.X + 9, self.Y, self.Z, self.M);
			mage = Dummy(self, 3);
			mage.SetRealSkillValue((int) SkillName.Magery, 500);
			rabbit = Dummy(self, 4);
			ground = new Point4D((ushort) (self.X - 1), self.Y, self.Z, self.M);
			Console.WriteLine("CORE_READY: name=" + self.Name + " model=" + self.Model + " str=" + self.Str + " daemon=" + ThingDef.GetByDefname("c_daemon").Model + " doorModel=" + farDoor.Model);
		}

		private static NPC Dummy(Character self, int offset) {
			NPC dummy = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + offset, self.Y, self.Z, self.M);
			dummy.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			dummy.MaxMana = 100;
			dummy.Mana = 100;
			return dummy;
		}

		private static IPoint4D Named(Character self, string name) {
			switch (name) {
				case "box":
					return box;
				case "mage":
					return mage;
				case "rabbit":
					return rabbit;
				case "door":
					return farDoor;
				case "self":
					return self;
				default:
					return ground;
			}
		}

		[SteamFunction]
		public static void TestCoreCast(Character self, ScriptArgs sa) {
			string[] words = sa.Args.Replace("\"", "").Split(' ');
			self.Mana = self.MaxMana;
			SpellDef spell = SpellDef.GetByDefname(words[0]);
			SkillSequenceArgs.Acquire(self, SkillName.Magery, Named(self, words[1]), null, book, spell, null).PhaseSelect();
			Console.WriteLine("CORE_CAST: " + words[0]);
		}

		[SteamFunction]
		public static void TestCoreLockBox(Character self) {
			box.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_container_locked");
			Console.WriteLine("CORE_LOCKED: done");
		}

		[SteamFunction]
		public static void TestCoreGiveKey(Character self) {
			Item key = (Item) ThingDef.GetByDefname("i_key_iron").Create(self.Backpack);
			key.LockUid = box.LockUid;
			Console.WriteLine("CORE_KEY: given");
		}

		[SteamFunction]
		public static void TestCoreDropKey(Character self) {
			foreach (Item item in self.Backpack) {
				if (CraftingSkillDef.IsOfType(item, "t_key")) {
					item.Delete();
					break;
				}
			}
			Console.WriteLine("CORE_KEY: gone");
		}

		private static int Scrolls(Character self) {
			int count = 0;
			foreach (Item item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname("i_scroll_unlock")) {
					count += item.Amount;
				}
			}
			return count;
		}

		[SteamFunction]
		public static void TestCoreReport(Character self) {
			int food = 0;
			foreach (Thing thing in self.GetMap().GetThingsInRange(ground.X, ground.Y, 1)) {
				if (Array.IndexOf(foods, thing.Def.PrettyDefname) >= 0) {
					food++;
				}
			}
			Console.WriteLine("CORE_REPORT: box=" + (CraftingSkillDef.IsOfType(box, "t_container_locked") ? "t_container_locked" : "t_container") + " mageMana=" + mage.Mana + " rabbitMana=" + rabbit.Mana + " name=" + self.Name + " model=" + self.Model + " str=" + self.Str + " food=" + food + " door=" + farDoor.Model + " scrolls=" + Scrolls(self));
		}

		[SteamFunction]
		public static void TestCorePlayer(Character self) {
			self.Account.PLevel = 1;
			self.MaxMana = 100;
			self.Mana = 100;
			Item scroll = (Item) ThingDef.GetByDefname("i_scroll_unlock").Create(self.Backpack);
			Console.WriteLine("CORE_PLAYER: pack=#" + self.Backpack.Uid + " scroll=#" + scroll.Uid + " box=#" + box.Uid);
		}

		[SteamFunction]
		public static void TestCoreEnd(Character self) {
			PolymorphSpellDef.End(self);
			IncognitoSpellDef.End(self);
			Console.WriteLine("CORE_END: name=" + self.Name + " model=" + self.Model + " str=" + self.Str);
		}
	}
}
