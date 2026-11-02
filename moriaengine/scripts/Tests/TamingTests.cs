using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/TamingTests.cs.</summary>
	public static class TamingTestHelpers {
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");

		private static string[] Words(ScriptArgs sa) {
			return sa.Args.Trim('"', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
		}

		[SteamFunction]
		public static void TestTamingPrepare(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			self.Account.PLevel = 1;
			if (words.Length > 2) {
				MovementSpeedTests.TestTeleportToOpenGround(self);
				VerificationTestUtils.CleanupNearbyNpcs(self, 20);
				ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M); //or the dark can refuse
			}
			self.SetRealSkillValue((int) SkillName.AnimalTaming, int.Parse(words[0], CultureInfo.InvariantCulture));
			self.SetRealSkillValue((int) SkillName.AnimalLore, int.Parse(words[1], CultureInfo.InvariantCulture));
			Console.WriteLine("TAMING_PREPARED: " + self.GetSkill(SkillName.AnimalTaming));
		}

		[SteamFunction]
		public static void TestTamingCrook(Character self, ScriptArgs sa) {
			string defname = Words(sa)[0];
			AbstractItem held = self.FindLayer(LayerNames.Hand2);
			if (held != null) {
				held.Delete();
			}
			if (defname != "none") {
				Item crook = (Item) ThingDef.GetByDefname(defname).Create(self.Backpack);
				self.TryEquip(crook);
			}
			Console.WriteLine("TAMING_CROOK: " + (self.FindLayer(LayerNames.Hand2)?.Def.PrettyDefname ?? "none"));
		}

		[SteamFunction]
		public static void TestTamingSpawn(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			int distance = (words.Length > 1) ? int.Parse(words[1], CultureInfo.InvariantCulture) : 1;
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(words[0])).Create(self.X + distance, self.Y, self.Z, self.M);
			if (words.Length > 2) {
				npc.SetRealSkillValue((int) SkillName.AnimalTaming, int.Parse(words[2], CultureInfo.InvariantCulture));
				npc.SetRealSkillValue((int) SkillName.AnimalLore, (words.Length > 3) ? int.Parse(words[3], CultureInfo.InvariantCulture) : 0);
			}
			Console.WriteLine("TAMING_SPAWNED: npc=#" + npc.Uid + " taming=" + npc.GetSkill(SkillName.AnimalTaming) + " lore=" + npc.GetSkill(SkillName.AnimalLore));
		}

		/// <summary>On a real gem, as @spawnNow leaves it.</summary>
		[SteamFunction]
		public static void TestTamingAttachGem(Character self, ScriptArgs sa) {
			NPC npc = (NPC) Thing.UidGetCharacter(int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			Item gem = (Item) ThingDef.GetByDefname("i_worldgem_bit").Create(npc.X, npc.Y, npc.Z, npc.M);
			gem.SetTag(TagKey.Acquire("spawnInfo"), new SpawnInfo(1, (IThingFactory) npc.Def, 100000, 100001, 5));
			gem.SetTag(TagKey.Acquire("spawnedCount"), 1);
			npc.SetTag(spawnGemTK, gem);
			npc.AddTriggerGroup(TriggerGroup.GetByDefname("e_spawned_char"));
			Console.WriteLine("TAMING_GEM: #" + gem.Uid);
		}

		[SteamFunction]
		public static void TestTamingMoveGem(Character self, ScriptArgs sa) {
			Thing gem = Thing.UidGetThing(int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			gem.P(gem.X + 80, gem.Y, gem.Z);
			Console.WriteLine("TAMING_GEM_MOVED: " + gem.X);
		}

		[SteamFunction]
		public static void TestTamingRaised(Character self, ScriptArgs sa) {
			NPC npc = (NPC) Thing.UidGetCharacter(int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			ThingDef.GetByDefname("i_poresu").Create(npc.Backpack);
			Console.WriteLine("TAMING_RAISED: done");
		}

		[SteamFunction]
		public static void TestTamingSelf(Character self) {
			self.SetRealSkillValue((int) SkillName.AnimalTaming, 0);
			Console.WriteLine("TAMING_SELF: #" + self.Uid);
		}

		[SteamFunction]
		public static void TestTamingHurt(Character self) {
			DamageManager.CauseDamage(self, self, DamageType.Physical, 1);
			Console.WriteLine("TAMING_HURT: done");
		}

		[SteamFunction]
		public static void TestMountCount(Character self, ScriptArgs sa) {
			self.SetTag(MountKnowledge.Mounts[0].countTK, int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			Console.WriteLine("MOUNT_COUNTED: done");
		}

		[SteamFunction]
		public static void TestTamingShorten(Character self) {
			TamingTimer timer = (TamingTimer) self.GetTimer(TamingTimer.Key);
			timer.strokes = 0;
			Console.WriteLine("TAMING_SHORTENED: " + timer.DueInSeconds);
		}

		[SteamFunction]
		public static void TestTamingBusy(Character self) {
			Console.WriteLine("TAMING_BUSY: busy=" + (self.GetTimer(TamingTimer.Key) != null));
		}

		[SteamFunction]
		public static void TestTamingReport(Character self, ScriptArgs sa) {
			NPC npc = (NPC) Thing.UidGetCharacter(int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			Console.WriteLine("TAMING_REPORT: master=" + (npc.ControlMaster?.Name ?? "none") + " karma=" + npc.Karma
				+ " spawned=" + (TamingSkillDef.SpawnOf(npc) != null) + " skill=" + self.GetSkill(SkillName.AnimalTaming));
		}

		[SteamFunction]
		public static void TestTamingGemReport(Character self, ScriptArgs sa) {
			Thing gem = Thing.UidGetThing(int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			Console.WriteLine("TAMING_GEM_REPORT: count=" + gem.GetTag(TagKey.Acquire("spawnedCount")));
		}

		[SteamFunction]
		public static void TestMountTable(Character self) {
			MountKnowledge.Mount first = MountKnowledge.Mounts[0], last = MountKnowledge.Mounts[MountKnowledge.Mounts.Count - 1];
			int missing = MountKnowledge.Mounts.FindAll(m => m.def == null).Count;
			Console.WriteLine("MOUNT_TABLE: rows=" + MountKnowledge.Mounts.Count + " missing=" + missing + " first=" + first.name + "," + first.points + "," + first.maxPoints + "," + first.icon
				+ " last=" + last.name + " most=" + first.TimesForMost);
		}

		[SteamFunction]
		public static void TestMountKnowledge(Character self) {
			Console.WriteLine("MOUNT_KNOWLEDGE: total=" + MountKnowledge.TotalPoints(self) + " bonus=" + MountKnowledge.Bonus(self)
				+ " llama=" + MountKnowledge.CountOf(self, MountKnowledge.Mounts[0]));
		}

		[SteamFunction]
		public static void TestMountDialog(Character self) {
			D_MountsKnowledge.Show((Player) self);
		}
	}
}
