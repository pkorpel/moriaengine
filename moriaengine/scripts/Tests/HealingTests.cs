using System;

namespace SteamEngine.CompiledScripts {
	public static class HealingTests {
		private static Item clean, tub;
		private static NPC rabbit;
		private static Corpse animalCorpse;

		private static int Held(Character self, string defname) {
			int count = 0;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname(defname)) {
					count += item.Amount;
				}
			}
			return count;
		}

		/// <summary>A plain healer past every roll, half hurt, with bandages and a tub of blood at hand.</summary>
		[SteamFunction]
		public static void TestHealReady(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			self.Account.PLevel = 1;
			player.Profession = ProfessionDef.GetByDefname("profession_" + sa.Args.Trim('"'));
			foreach (SkillName skill in new[] { SkillName.Healing, SkillName.Veterinary, SkillName.Anatomy, SkillName.AnimalLore }) {
				self.SetRealSkillValue((int) skill, 2000);
			}
			self.Hits = (short) (self.MaxHits / 2);
			if ((clean == null) || clean.IsDeleted) {
				clean = (Item) ((ItemDef) ThingDef.GetByDefname("i_bandage")).Create(self.Backpack);
				clean.Amount = 60;
			}
			Console.WriteLine("HEAL_READY: me=" + (uint) self.FlaggedUid + " bandage=" + (uint) clean.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " hits=" + self.Hits);
		}

		[SteamFunction]
		public static void TestHealState(Character self) {
			Console.WriteLine("HEAL_STATE: hits=" + self.Hits + " clean=" + Held(self, "i_bandage") + " bloody=" + Held(self, "i_bandage_bloody"));
		}

		[SteamFunction]
		public static void TestHealTub(Character self) {
			tub = (Item) ((ItemDef) ThingDef.GetByDefname("i_bloody_tub")).Create(self.Backpack);
			tub.P((ushort) (self.X + 1), self.Y, self.Z, self.M);
			Console.WriteLine("HEAL_TUB: tub=" + (uint) tub.FlaggedUid);
		}

		/// <summary>A dead rabbit at one's feet, its body's uid.</summary>
		[SteamFunction]
		public static void TestHealAnimalCorpse(Character self) {
			rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			rabbit.CauseDeath(null);
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 2)) {
				if (thing is Corpse) {
					animalCorpse = (Corpse) thing;
				}
			}
			Console.WriteLine("HEAL_CORPSE: corpse=" + (uint) animalCorpse.FlaggedUid);
		}

		[SteamFunction]
		public static void TestHealRaised(Character self) {
			int rabbits = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 3)) {
				if (ch.Def == ThingDef.GetByDefname("c_a_jackrabbit")) {
					rabbits++;
				}
			}
			Console.WriteLine("HEAL_RAISED: rabbits=" + rabbits + " corpseGone=" + animalCorpse.IsDeleted + " bloody=" + Held(self, "i_bandage_bloody"));
		}

		/// <summary>Dead, in war mode, the body's uid: the patient's end of a resurrection.</summary>
		[SteamFunction]
		public static void TestHealDie(Character self) {
			self.Account.PLevel = 1;
			self.CauseDeath(null);
			self.Flag_WarMode = true;
			Corpse body = null;
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 2)) {
				if ((thing is Corpse) && (((Corpse) thing).Owner == self)) {
					body = (Corpse) thing;
				}
			}
			Console.WriteLine("HEAL_DIED: body=" + (uint) body.FlaggedUid + " x=" + self.X + " y=" + self.Y + " z=" + self.Z);
		}

		[SteamFunction]
		public static void TestHealGhostHere(Character self, ScriptArgs sa) {
			string[] at = sa.Args.Trim('"').Split(' ');
			self.P(ushort.Parse(at[0]), ushort.Parse(at[1]), sbyte.Parse(at[2]), self.M);
			Console.WriteLine("HEAL_GHOST_HERE:");
		}

		[SteamFunction]
		public static void TestHealAlive(Character self) {
			Console.WriteLine("HEAL_ALIVE: dead=" + self.Flag_Dead + " hits=" + self.Hits);
		}
	}
}
