using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1409 - the movement holds as plugins answering @denyStep (#1558; was @denyMove): a GM's freeze (the client's real step is refused
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
			Check("frozenIsAPlugin", (self.GetPlugin(GmFreezePlugin.key) is GmFreezePlugin) && GmTools.IsFrozen(self) && self.IsHeld(false));
			Console.WriteLine("HOLDS_FROZEN");
		}

		[SteamFunction]
		public static void TestHoldsUnfreeze(Character self) {
			GmFreezePlugin.Freeze(self, false);
			Check("unfrozen", !GmTools.IsFrozen(self) && !self.IsHeld(false));
			self.Stam = self.MaxStam;
			Console.WriteLine("HOLDS_UNFROZEN");
		}

		[SteamFunction]
		public static void TestHoldsOthers(Character self) {
			ImmobilePlugin.Set(self, true);
			bool immobile = self.IsHeld(false) && MysticMind.CheckCanMove(self) == false;
			ImmobilePlugin.Set(self, false);
			Check("immobile", immobile && !self.IsHeld(false) && !ImmobilePlugin.IsImmobile(self));

			LootHoldPlugin.Extend(self, 0.5);
			LootHoldPlugin.Extend(self, 0.5); //a second pick adds
			LootHoldPlugin hold = self.GetPlugin(LootHoldPlugin.key) as LootHoldPlugin;
			Check("lootHeld", (hold != null) && (hold.Timer > 0.8) && self.IsHeld(false) && !CorpseLooting.CheckCanMove(self));

			//a strangler vine on the player's tile
			StranglerVineAbilityDef def = (StranglerVineAbilityDef) AbstractScript.GetByDefname("a_tevent_e_ovinovac");
			vine = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(vine, 300);
			vine.MaxHits = 30000;
			vine.Hits = 30000;
			vine.SetRealAbilityPoints(def, 1);
			MonsterSpecialAttacks.TryEntangle(vine, self);
			VineHoldPlugin vineHold = vine.GetPlugin(VineHoldPlugin.key) as VineHoldPlugin;
			Check("entangled", (vineHold != null) && (self.GetPlugin(EntangledPlugin.key) is EntangledPlugin) && vine.IsHeld(false));
			Console.WriteLine("HOLDS_OTHERS");
		}

		/// <summary>After the loot hold's second: free of it; the player's blows on the vine until it lets go; then a
		/// hold broken by moving the victim off the vine's tile is released at the next step check.</summary>
		[SteamFunction]
		public static void TestHoldsAfter(Character self) {
			Check("lootHoldRanOut", !LootHoldPlugin.IsHeld(self));
			Check("entangledHolds", self.IsHeld(false));
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
				&& (swings >= def.HitsToBreak) && !self.IsHeld(false));

			MonsterSpecialAttacks.TryEntangle(vine, self);
			bool heldAgain = self.IsHeld(false);
			self.P(self.X + 1, self.Y, self.Z);
			bool vineGoes = !vine.IsHeld(false); //the vine's own check finds the hold broken and lets go
			Check("leavingTheTileFrees", heldAgain && vineGoes && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && !self.IsHeld(false));

			//#1412 - a spell's damage counts as a blow does (Erebor's damage_final raised @aftergetswing for both), 6 in all
			vine.Flag_Invulnerable = false;
			vine.P(self.X, self.Y, self.Z, self.M);
			MonsterSpecialAttacks.TryEntangle(vine, self);
			NPC mage = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 2, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(mage, 300);
			bool heldAfterFive = true;
			for (int i = 0; i < 6; i++) {
				if (i == 5) {
					heldAfterFive = (vine.GetPlugin(VineHoldPlugin.key) != null) && self.IsHeld(false);
				}
				DamageManager.CauseSpellDamage(mage, vine, DamageType.MagicFire, 1);
				vine.Hits = vine.MaxHits;
			}
			Console.WriteLine("HOLDS_SPELLS: hitsToBreak=" + def.HitsToBreak + " heldAfterFive=" + heldAfterFive);
			Check("spellsFreeAfterSix", (def.HitsToBreak == 6) && heldAfterFive && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && !self.IsHeld(false));
			mage.Delete();

			//#1511 - an ability's hit through Erebor's damagecust counts too: a warrior's hit around, 6 of them
			MonsterSpecialAttacks.TryEntangle(vine, self);
			bool heldAfterFiveHits = true;
			for (int i = 0; i < 6; i++) {
				if (i == 5) {
					heldAfterFiveHits = (vine.GetPlugin(VineHoldPlugin.key) != null) && self.IsHeld(false);
				}
				WarriorAbilities.HitAround((Player) self, 100, false);
				vine.Hits = vine.MaxHits;
			}
			Console.WriteLine("HOLDS_ABILITY: heldAfterFive=" + heldAfterFiveHits);
			Check("abilityHitsFreeAfterSix", heldAfterFiveHits && (vine.GetPlugin(VineHoldPlugin.key) == null)
				&& (self.GetPlugin(EntangledPlugin.key) == null) && !self.IsHeld(false));
			vine.Delete();
			Console.WriteLine("HOLDS_DONE");
		}

		private static readonly PluginKey gmTestParalysisPK = PluginKey.Acquire("_gmTestParalysis_");

		/// <summary>#1558 - the one GM rule (Character.IsImmuneToHostileEffects): a GM is never paralysed, held by a vine, hurt,
		/// swung at or reached by a harmful spell, so the holds asked for GMs now never stop one; the same effects still land
		/// on a player.</summary>
		[SteamFunction]
		public static void TestHoldsGmImmune(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			NPC foe = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(foe, 300);
			self.Account.PLevel = 4;
			self.Hits = self.MaxHits;

			ParalyzeEffectPluginDef.Paralyze(self, gmTestParalysisPK, TimeSpan.FromSeconds(30), foe, EffectFlag.FromAbility);
			ParalyzeEffectPluginDef.Paralyze(self, gmTestParalysisPK, TimeSpan.FromSeconds(30)); //a trap or a monster's freeze: no source
			Check("gmNotParalysed", !ParalyzeEffectPluginDef.IsParalyzed(self) && !self.IsHeld(false));

			StranglerVineAbilityDef def = (StranglerVineAbilityDef) AbstractScript.GetByDefname("a_tevent_e_ovinovac");
			NPC gmVine = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(gmVine, 300);
			gmVine.SetRealAbilityPoints(def, 1);
			MonsterSpecialAttacks.TryEntangle(gmVine, self);
			Check("gmNotEntangled", (self.GetPlugin(EntangledPlugin.key) == null) && (gmVine.GetPlugin(VineHoldPlugin.key) == null));
			gmVine.Delete();

			int hits = self.Hits;
			int dealt = DamageManager.CauseDamage(foe, self, DamageType.Physical, 50);
			int dealtByNobody = DamageManager.CauseDamage(null, self, DamageType.Physical, 50); //lava, a trap
			Check("gmTakesNoDamage", (dealt == 0) && (dealtByNobody == 0) && (self.Hits == hits));
			Check("swingAtAGmEnds", SwingDrawnAt(foe, self) == null);

			int pluginsBefore = CountPlugins(self);
			((SpellDef) SpellDef.GetByDefname("s_weaken")).ApplyEffectChar(foe, self, 1000, EffectFlag.FromAbility | EffectFlag.HarmfulEffect);
			Check("gmNoHarmfulSpell", CountPlugins(self) == pluginsBefore);

			self.Account.PLevel = 1; //the same effects on a player
			ParalyzeEffectPluginDef.Paralyze(self, gmTestParalysisPK, TimeSpan.FromSeconds(30), foe, EffectFlag.FromAbility);
			Check("playerParalysed", ParalyzeEffectPluginDef.IsParalyzed(self) && self.IsHeld(false));
			self.DeletePlugin(gmTestParalysisPK);
			Check("playerSwungAt", foe.Trigger_DenySwing(self).Allow && (SwingDrawnAt(foe, self) != null));
			foe.AbortSkill();

			NPC playerVine = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(playerVine, 300);
			playerVine.SetRealAbilityPoints(def, 1);
			MonsterSpecialAttacks.TryEntangle(playerVine, self);
			Check("playerEntangled", (self.GetPlugin(EntangledPlugin.key) != null) && (playerVine.GetPlugin(VineHoldPlugin.key) != null));
			playerVine.Delete();
			self.DeletePlugin(EntangledPlugin.key);

			self.Hits = self.MaxHits;
			hits = self.Hits;
			dealt = DamageManager.CauseDamage(foe, self, DamageType.Physical, 10);
			Check("playerTakesDamage", (dealt > 0) && (self.Hits < hits));
			self.Hits = self.MaxHits;

			System.Collections.Generic.List<Plugin> had = new System.Collections.Generic.List<Plugin>(self.GetAllPlugins());
			int resist = self.GetSkill(SkillName.MagicResist);
			self.SetRealSkillValue((int) SkillName.MagicResist, 0); //no resisting it
			((SpellDef) SpellDef.GetByDefname("s_weaken")).ApplyEffectChar(foe, self, 1000, EffectFlag.FromAbility | EffectFlag.HarmfulEffect);
			Check("playerHarmfulSpell", CountPlugins(self) > had.Count);
			foreach (Plugin p in new System.Collections.Generic.List<Plugin>(self.GetAllPlugins())) {
				if (!had.Contains(p)) {
					p.Delete();
				}
			}
			self.SetRealSkillValue((int) SkillName.MagicResist, resist);
			foe.Delete();
			self.Hits = self.MaxHits;
			Console.WriteLine("HOLDS_GM_DONE");
		}

		/// <summary>#1558 - the attacker draws a swing at the target (the stroke WeaponSkillDef.On_Stroke takes in reach): its
		/// skill afterwards, null when the swing ended.</summary>
		private static SkillSequenceArgs SwingDrawnAt(NPC attacker, Character target) {
			SkillSequenceArgs drawn = SkillSequenceArgs.Acquire(attacker, SkillName.Wrestling, target, null, null, WeaponSkillDef.WeaponSkillPhase.Drawing, null);
			drawn.PhaseStroke();
			return attacker.CurrentSkillArgs;
		}

		private static int CountPlugins(Character ch) {
			int n = 0;
			foreach (Plugin p in ch.GetAllPlugins()) {
				n++;
			}
			return n;
		}
	}
}
