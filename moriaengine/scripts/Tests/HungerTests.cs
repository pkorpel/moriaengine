using System;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	public static class HungerTests {
		/// <summary>A plain player with given food, a fish and a loaf to eat, hurt so regeneration shows.</summary>
		[SteamFunction]
		public static void TestHungerReady(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			PlayerHunger.SetFood(self, int.Parse(sa.Args.Trim('"')));
			Item fish = (Item) ((ItemDef) ThingDef.GetByDefname("i_fish_cooked")).Create(self.Backpack);
			fish.Amount = 3;
			Console.WriteLine("HUNGER_READY: fish=" + (uint) fish.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " weight=" + fish.Def.Weight);
		}

		[SteamFunction]
		public static void TestHungerFood(Character self, ScriptArgs sa) {
			string set = sa.Args.Trim('"');
			if (set.Length > 0) {
				PlayerHunger.SetFood(self, int.Parse(set));
			}
			Console.WriteLine("HUNGER_FOOD: food=" + PlayerHunger.FoodOf(self) + " level=" + PlayerHunger.LevelText(self));
		}

		/// <summary>How often hunger gets in the way, of 1000.</summary>
		[SteamFunction]
		public static void TestHungerRolls(Character self, ScriptArgs sa) {
			PlayerHunger.SetFood(self, int.Parse(sa.Args.Trim('"')));
			int hungry = 0;
			for (int i = 0; i < 1000; i++) {
				if (PlayerHunger.IsHungry(self)) {
					hungry++;
				}
			}
			Console.WriteLine("HUNGER_ROLLS: hungry=" + hungry);
		}

		/// <summary>100 sure Hidings through the real success phase, on no food: how many it left the player hidden.</summary>
		[SteamFunction]
		public static void TestHungerHiding(Character self) {
			PlayerHunger.SetFood(self, 0);
			self.SetRealSkillValue((int) SkillName.Hiding, 1000);
			int hidden = 0;
			for (int i = 0; i < 100; i++) {
				self.Flag_Hidden = false;
				SkillSequenceArgs.Acquire(self, SkillName.Hiding).PhaseSuccess();
				if (self.Flag_Hidden) {
					hidden++;
				}
			}
			self.Flag_Hidden = false;
			PlayerHunger.SetFood(self, 10);
			Console.WriteLine("HUNGER_HIDING: hidden=" + hidden);
		}

		/// <summary>Whether a starving player's hits regenerate, given a second.</summary>
		[SteamFunction]
		public static void TestHungerHurtMe(Character self) {
			self.HitsRegenSpeed = 20;
			self.Hits = (short) (self.MaxHits / 2);
			Console.WriteLine("HUNGER_HITS: hits=" + self.Hits);
		}

		[SteamFunction]
		public static void TestHungerHits(Character self) {
			Console.WriteLine("HUNGER_HITS: hits=" + self.Hits);
		}

		/// <summary>A blow tried on nothing in the stomach costs 10.</summary>
		[SteamFunction]
		public static void TestHungerSwing(Character self) {
			NPC rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			self.Hits = self.MaxHits;
			int before = self.Hits;
			PlayerHunger.SetFood(self, 0);
			DamageManager.ProcessSwing(self, rabbit);
			Console.WriteLine("HUNGER_SWING: lost=" + (before - self.Hits));
			rabbit.Delete();
			PlayerHunger.SetFood(self, 10);
		}

		/// <summary>The clock at 1 s a point, as the settings page would set it.</summary>
		[SteamFunction]
		public static void TestHungerFast(Character self) {
			HungerSettings.instance.secondsPerFoodPoint = 1;
			self.DeleteTimer(PlayerHungerTimer.Key);
			PlayerHunger.StartTicking((Player) self);
			Console.WriteLine("HUNGER_FAST:");
		}

		[SteamFunction]
		public static void TestHungerSlow(Character self) {
			HungerSettings.instance.secondsPerFoodPoint = 1800;
			self.DeleteTimer(PlayerHungerTimer.Key);
			PlayerHunger.StartTicking((Player) self);
			string lines = "";
			foreach (ActiveEffect effect in ActiveEffects.Of(self)) {
				lines += effect.Name + "=" + effect.Detail + ";";
			}
			Console.WriteLine("HUNGER_SLOW: " + lines);
		}
	}
}
