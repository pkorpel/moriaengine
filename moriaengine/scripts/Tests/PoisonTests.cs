using System;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/PoisonTests.cs.</summary>
	public static class PoisonTestHelpers {
		private static string[] Words(ScriptArgs sa) {
			return sa.Args.Trim('"', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
		}

		private static int Number(string word) {
			return int.Parse(word, CultureInfo.InvariantCulture);
		}

		private static Item Give(Character self, string defname, int amount = 1) {
			Item item = (Item) ThingDef.GetByDefname(defname).Create(self.Backpack);
			if (amount > 1) {
				item.Amount = amount;
			}
			return item;
		}

		[SteamFunction]
		public static void TestPoisonPrepare(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M);
			self.SetRealSkillValue((int) SkillName.Poisoning, Number(Words(sa)[0]));
			Poison.RemoveAll(self);
			self.MaxHits = 200;
			self.Hits = 200;
			Console.WriteLine("POISON_PREPARED: pack=#" + self.Backpack.Uid);
		}

		[SteamFunction]
		public static void TestPoisonGive(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			Item item = Give(self, words[0], (words.Length > 1) ? Number(words[1]) : 1);
			Console.WriteLine("POISON_GIVEN: #" + item.Uid + " kind=" + (item as PoisonPotion)?.Kind);
		}

		[SteamFunction]
		public static void TestPoisonNpc(Character self) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			npc.MaxHits = 1000;
			npc.Hits = 1000;
			npc.Str = 100;
			npc.ResistMagic = 0;
			npc.ResistPoison = 0;
			Console.WriteLine("POISON_NPC: npc=#" + npc.Uid);
		}

		[SteamFunction]
		public static void TestPoisonApply(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			Character target = (Character) Thing.UidGetCharacter(Number(words[0]));
			bool took = Poison.Apply(target, self, Number(words[1]), double.Parse(words[2], CultureInfo.InvariantCulture), Poison.ParseKind(words[3]));
			Console.WriteLine("POISON_APPLIED: took=" + took);
		}

		[SteamFunction]
		public static void TestPoisonReport(Character self, ScriptArgs sa) {
			Character target = (Character) Thing.UidGetCharacter(Number(Words(sa)[0]));
			string strengths = "";
			foreach (PoisonKind kind in Enum.GetValues(typeof(PoisonKind))) {
				if (Poison.Running(target, kind) is DamagingPoisonEffectPlugin poison) {
					strengths += kind + "=" + poison.Strength + ",";
				}
			}
			Console.WriteLine("POISON_REPORT: poisoned=" + Poison.IsPoisoned(target) + " green=" + target.Flag_GreenHealthBar + " hits=" + target.Hits
				+ " str=" + target.Str + " stam=" + target.Stam + " para=" + ParalyzeEffectPluginDef.IsParalyzed(target) + " " + strengths);
		}

		[SteamFunction]
		public static void TestPoisonItem(Character self, ScriptArgs sa) {
			Item item = (Item) Thing.UidGetThing(Number(Words(sa)[0]));
			PoisonedItemPlugin plugin = (item == null) ? null : PoisonedItemPlugin.GetPoisonPlugin(item);
			Console.WriteLine("POISON_ITEM: exists=" + (item != null) + " power=" + plugin?.PoisonPower + " doses=" + plugin?.PoisonDoses + " kind=" + plugin?.PoisonKind);
		}

		[SteamFunction]
		public static void TestPoisonArrows(Character self) {
			foreach (AbstractItem item in self.Backpack) {
				if ((item.Def.PrettyDefname == "i_arrow_piercing_copper_poison") && (item is Item arrows)) {
					PoisonedItemPlugin plugin = PoisonedItemPlugin.GetPoisonPlugin(arrows);
					Console.WriteLine("POISON_ARROWS: amount=" + arrows.Amount + " power=" + plugin?.PoisonPower);
					return;
				}
			}
			Console.WriteLine("POISON_ARROWS: none");
		}

		[SteamFunction]
		public static void TestPoisonBlade(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			Item blade = (Item) Thing.UidGetThing(Number(words[0]));
			Character target = (Character) Thing.UidGetCharacter(Number(words[1]));
			PoisonedItemPlugin plugin = PoisonedItemPlugin.GetPoisonPlugin(blade);
			plugin.Hit(self, target);
			plugin.WearDown();
			Console.WriteLine("POISON_BLADE: done");
		}

		[SteamFunction]
		public static void TestPoisonMonster(Character self, ScriptArgs sa) {
			NPC npc = (NPC) Thing.UidGetCharacter(Number(Words(sa)[0]));
			npc.SetRealSkillValue((int) SkillName.Poisoning, 1000);
			Poison.FromMonster(npc, self);
			Console.WriteLine("POISON_MONSTER: done");
		}

		/// <summary>#1422 - a running poison is its plugin: under its kind's key, listed by .boosts with its strength, the
		/// green bar shared with bleeding (the bar stays while still bleeding), and a death ends it.</summary>
		[SteamFunction]
		public static void TestPoisonPlugin(Character self, ScriptArgs sa) {
			Character target = (Character) Thing.UidGetCharacter(Number(Words(sa)[0]));
			Poison.Apply(target, self, 7, 60, PoisonKind.AbsoluteDamage, true);
			DamagingPoisonEffectPlugin poison = target.GetPlugin(DamagingPoisonEffectPlugin.KeyOf(PoisonKind.AbsoluteDamage)) as DamagingPoisonEffectPlugin;
			string name = Loc<PoisonLoc>.Get(target.Language).Poisoned;
			bool listed = ActiveEffects.Of(target).Exists(effect => (effect.Name == name) && (effect.Detail == "7") && string.IsNullOrEmpty(effect.Source)); //no dealer named
			Console.WriteLine("POISON_CHECK: isPlugin=" + ((poison != null) && (poison.Kind == PoisonKind.AbsoluteDamage) && (poison.Strength == 7) && (poison.Dealer == self)));
			Console.WriteLine("POISON_CHECK: listed=" + listed);

			BleedingEffectPluginDef bleeding = (BleedingEffectPluginDef) PluginDef.GetByDefname("p_bleeding_effect");
			bleeding.Strike(self, target, 5);
			bool bothGreen = target.Flag_GreenHealthBar;
			Poison.RemoveAll(target);
			Console.WriteLine("POISON_CHECK: bleedingKeepsGreen=" + (bothGreen && !Poison.IsPoisoned(target) && target.Flag_GreenHealthBar));
			target.DeletePlugin(bleeding.PluginKey);
			Console.WriteLine("POISON_CHECK: greenGoesAfter=" + !target.Flag_GreenHealthBar);

			Poison.Apply(self, self, 7, 60, PoisonKind.Stamina, true);
			bool selfPoisoned = Poison.IsPoisoned(self);
			self.CauseDeath(null);
			bool endedAtDeath = !Poison.IsPoisoned(self);
			self.Resurrect();
			Console.WriteLine("POISON_CHECK: deathEndsIt=" + (selfPoisoned && endedAtDeath && !self.Flag_GreenHealthBar));
			Console.WriteLine("POISON_PLUGIN_DONE");
		}

		[SteamFunction]
		public static void TestPoisonCure(Character self, ScriptArgs sa) {
			Character target = (Character) Thing.UidGetCharacter(Number(Words(sa)[0]));
			Poison.CurePoison(target);
			Poison.SetImmunity(target, 60);
			Console.WriteLine("POISON_CURED: done");
		}
	}
}
