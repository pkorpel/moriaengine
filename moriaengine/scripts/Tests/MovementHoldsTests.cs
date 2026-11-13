using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1409 - the movement holds as plugins answering @denyMove: a GM's freeze (the client's real step is refused
	/// in Tests/Protocol/MovementHoldsTests.cs), flag_immobile, a looter's hold that runs out, a strangler vine that lets go
	/// after landed blows and when its victim isn't on its tile any more.</summary>
	public static class MovementHoldsTests {
		private static NPC vine;

		private static void Check(string name, bool ok) {
			Console.WriteLine("HOLDS_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestHoldsFreeze(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Stam = self.MaxStam;
			self.Direction = Direction.East;
			GmFreezePlugin.Freeze(self, true);
			Check("frozenIsAPlugin", (self.GetPlugin(GmFreezePlugin.key) is GmFreezePlugin) && GmTools.IsFrozen(self) && !self.CheckCanMove(false));
			Console.WriteLine("HOLDS_FROZEN");
		}

		[SteamFunction]
		public static void TestHoldsUnfreeze(Character self) {
			GmFreezePlugin.Freeze(self, false);
			Check("unfrozen", !GmTools.IsFrozen(self) && self.CheckCanMove(false));
			self.Stam = self.MaxStam;
			Console.WriteLine("HOLDS_UNFROZEN");
		}

		[SteamFunction]
		public static void TestHoldsOthers(Character self) {
			ImmobilePlugin.Set(self, true);
			bool immobile = !self.CheckCanMove(false) && MysticMind.CheckCanMove(self) == false;
			ImmobilePlugin.Set(self, false);
			Check("immobile", immobile && self.CheckCanMove(false) && !ImmobilePlugin.IsImmobile(self));

			LootHoldPlugin.Extend(self, 0.5);
			LootHoldPlugin.Extend(self, 0.5); //a second pick adds
			LootHoldPlugin hold = self.GetPlugin(LootHoldPlugin.key) as LootHoldPlugin;
			Check("lootHeld", (hold != null) && (hold.Timer > 0.8) && !self.CheckCanMove(false) && !CorpseLooting.CheckCanMove(self));

			//a strangler vine on the player's tile
			StranglerVineAbilityDef def = (StranglerVineAbilityDef) AbstractScript.GetByDefname("a_tevent_e_ovinovac");
			vine = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(vine, 300);
			vine.MaxHits = 30000;
			vine.Hits = 30000;
			vine.SetRealAbilityPoints(def, 1);
			MonsterSpecialAttacks.TryEntangle(vine, self);
			VineHoldPlugin vineHold = vine.GetPlugin(VineHoldPlugin.key) as VineHoldPlugin;
			Check("entangled", (vineHold != null) && (self.GetPlugin(EntangledPlugin.key) is EntangledPlugin) && !vine.CheckCanMove(false));
			Console.WriteLine("HOLDS_OTHERS");
		}

		/// <summary>After the loot hold's second: free of it; the player's blows on the vine until it lets go; then a
		/// hold broken by moving the victim off the vine's tile is released at the next step check.</summary>
		[SteamFunction]
		public static void TestHoldsAfter(Character self) {
			Check("lootHoldRanOut", !LootHoldPlugin.IsHeld(self));
			Check("entangledHolds", !self.CheckCanMove(false));
			int swings = 0;
			vine.Flag_Invulnerable = true; //#1411 - every landed blow counts, also one CauseDamage refuses (@getHit, not @damage)
			while ((vine.GetPlugin(VineHoldPlugin.key) != null) && (swings < 500)) {
				DamageManager.ProcessSwing(self, vine);
				vine.Hits = vine.MaxHits;
				swings++;
			}
			StranglerVineAbilityDef def = (StranglerVineAbilityDef) AbstractScript.GetByDefname("a_tevent_e_ovinovac");
			Console.WriteLine("HOLDS_SWINGS: " + swings + " hitsToBreak=" + def.HitsToBreak);
			Check("blowsFree", (vine.GetPlugin(VineHoldPlugin.key) == null) && (self.GetPlugin(EntangledPlugin.key) == null)
				&& (swings >= def.HitsToBreak) && self.CheckCanMove(false));

			MonsterSpecialAttacks.TryEntangle(vine, self);
			bool heldAgain = !self.CheckCanMove(false);
			self.P(self.X + 1, self.Y, self.Z);
			bool vineGoes = vine.CheckCanMove(false); //the vine's own check finds the hold broken and lets go
			Check("leavingTheTileFrees", heldAgain && vineGoes && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && self.CheckCanMove(false));

			//#1412 - a spell's damage counts as a blow does (Erebor's damage_final raised @aftergetswing for both), 6 in all
			vine.Flag_Invulnerable = false;
			vine.P(self.X, self.Y, self.Z, self.M);
			MonsterSpecialAttacks.TryEntangle(vine, self);
			NPC mage = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 2, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(mage, 300);
			bool heldAfterFive = true;
			for (int i = 0; i < 6; i++) {
				if (i == 5) {
					heldAfterFive = (vine.GetPlugin(VineHoldPlugin.key) != null) && !self.CheckCanMove(false);
				}
				DamageManager.CauseSpellDamage(mage, vine, DamageType.MagicFire, 1);
				vine.Hits = vine.MaxHits;
			}
			Console.WriteLine("HOLDS_SPELLS: hitsToBreak=" + def.HitsToBreak + " heldAfterFive=" + heldAfterFive);
			Check("spellsFreeAfterSix", (def.HitsToBreak == 6) && heldAfterFive && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && self.CheckCanMove(false));
			mage.Delete();

			//#1511 - an ability's hit through Erebor's damagecust counts too: a warrior's hit around, 6 of them
			MonsterSpecialAttacks.TryEntangle(vine, self);
			bool heldAfterFiveHits = true;
			for (int i = 0; i < 6; i++) {
				if (i == 5) {
					heldAfterFiveHits = (vine.GetPlugin(VineHoldPlugin.key) != null) && !self.CheckCanMove(false);
				}
				WarriorAbilities.HitAround((Player) self, 100, false);
				vine.Hits = vine.MaxHits;
			}
			Console.WriteLine("HOLDS_ABILITY: heldAfterFive=" + heldAfterFiveHits);
			Check("abilityHitsFreeAfterSix", heldAfterFiveHits && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && self.CheckCanMove(false));
			vine.Delete();
			Console.WriteLine("HOLDS_DONE");
		}
	}
}
