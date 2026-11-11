using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1401 - Erebor's energy shield: the command's checks in their order, 59 mana and an amethyst, 45 s and half
	/// a second a point, 5 energy a point; a monster's damage let through at the table's percent and taken whole off the
	/// energy, a spell twice; a player's damage untouched; the last of it covering part of a hit, the shield still up for
	/// that blow and resting 180 s from the next tick; Recall refused while it's up, Gate not; a real step keeps it;
	/// Standcast refused only while it holds; the rest ends by itself. Protocol side: Tests/Protocol/StandcastTests.cs.</summary>
	public static class EnergyShieldTests {
		private static NPC monster;
		private static Item rune;
		private static EnergyShieldPlugin up;
		private static int stepFromX;

		private static void Check(string name, bool ok) {
			Console.WriteLine("STANDCAST_CHECK: " + name + "=" + ok);
		}

		private static bool Near(double a, double b) {
			return Math.Abs(a - b) < 0.25;
		}

		private static EnergyShieldAbilityDef Shield {
			get {
				return (EnergyShieldAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
			}
		}

		private static int Amethysts(Character self) {
			int count = 0;
			foreach (Item item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname("i_gem_amethyst")) {
					count += item.Amount;
				}
			}
			return count;
		}

		private static SpellBook BookOf(Character self) {
			SpellBook book = self.Backpack.FindByClass(typeof(SpellBook)) as SpellBook;
			if (book == null) {
				book = (SpellBook) ThingDef.GetByDefname("i_spellbook_3").Create(self.Backpack);
				foreach (SpellDef spell in SpellDef.AllSpellDefs) {
					book.AddSpell(spell);
				}
			}
			return book;
		}

		/// <summary>A real cast at the rune; true when it got under way.</summary>
		private static bool Casts(Character self, int spellId) {
			self.Mana = self.MaxMana;
			SkillSequenceArgs.Acquire(self, SkillName.Magery, rune, null, BookOf(self), SpellDef.GetById(spellId), null).PhaseSelect();
			bool casting = self.CurrentSkillName == SkillName.Magery;
			self.AbortSkill();
			self.DeletePlugin(CastFreezePlugin.key);
			return casting;
		}

		/// <summary>A monster's spell of 20 that no resist touches; returns the hits lost.</summary>
		private static int SpellHit(Character self) {
			self.Hits = self.MaxHits;
			int before = self.Hits;
			DamageManager.CauseSpellDamage(monster, self, DamageType.Irresistable, 20);
			return before - self.Hits;
		}

		/// <summary>A monster's blow-like damage (no swing roll) of 20 that no resist touches; the hits lost.</summary>
		private static int MonsterHit(Character self) {
			self.Hits = self.MaxHits;
			int before = self.Hits;
			DamageManager.CauseDamage(monster, self, DamageType.Irresistable, 20);
			return before - self.Hits;
		}

		/// <summary>The checks in order, the shield's numbers, travel, then what it takes; leaves it up with 30 energy,
		/// facing east for the client's real step.</summary>
		[SteamFunction]
		public static void TestEnergyShield(Character self) {
			Player player = (Player) self;
			EnergyShieldAbilityDef shield = Shield;
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M);
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.MaxHits = 500;
			self.MaxMana = 1000;
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is ItemDef def) && def.PrettyDefname.StartsWith("i_reag_", StringComparison.OrdinalIgnoreCase)) {
					((Item) def.Create(self.Backpack)).Amount = 50;
				}
			}
			rune = (Item) ThingDef.GetByDefname("i_rune_base").Create(self.X, self.Y - 1, self.Z, self.M);
			rune.MoreP = new Point4D(self.X + 10, self.Y, self.Z, self.M);
			monster = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y + 2, self.Z, self.M);
			NPCAiTimer.Freeze(monster, 300);

			//the checks, in Erebor's order
			player.Profession = Professions.Warrior;
			self.SetRealAbilityPoints(shield, 10);
			ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			self.Mana = 100;
			shield.Use(self);
			Check("notForWarrior", EnergyShieldPlugin.Of(self) == null);
			player.Profession = Professions.Mage;
			self.SetRealAbilityPoints(shield, 0);
			shield.Use(self);
			Check("needsPoints", EnergyShieldPlugin.Of(self) == null);
			self.SetRealAbilityPoints(shield, 10);
			self.Mana = 60;
			shield.Use(self);
			Check("sixtyManaNothing", (EnergyShieldPlugin.Of(self) == null) && (self.Mana == 60) && (Amethysts(self) == 1));
			self.Mana = 61;
			shield.Use(self);
			up = EnergyShieldPlugin.Of(self);
			Console.WriteLine("STANDCAST_SHIELD: mana=" + self.Mana + " energy=" + (up == null ? -1 : up.EffectPower) + " power=" + (up == null ? -1 : up.ShieldPower) + " timer=" + (up == null ? -1 : up.Timer));
			Check("upForManaAndAmethyst", (up != null) && (self.Mana == 2) && (Amethysts(self) == 0));
			Check("energyAndDuration", (up != null) && Near(up.EffectPower, 50) && (up.ShieldPower == 79) && Near(up.Timer, 50));
			Check("tableEnds", (EnergyShieldAbilityDef.ShieldPowerFor(50) == 24) && (EnergyShieldAbilityDef.ShieldPowerFor(25) == 54));
			if (up == null) {
				Console.WriteLine("STANDCAST_SHIELD_DONE");
				return;
			}

			//travel: Recall refused, Gate not (Erebor's argn==32 typo)
			Check("recallRefused", !Casts(self, 32) && (EnergyShieldPlugin.Of(self) == up) && !up.Resting);
			Check("gateAllowed", Casts(self, 52));

			//a monster's spell twice (@aftergetswing, then the spell rule): 20 -> 15 -> 11, 20 + 15 off the energy
			Check("spellTwice", (SpellHit(self) == 11) && Near(up.EffectPower, 15));
			//any other damage from a monster once: 20 -> 15; a player's own (as starving) whole
			up.EffectPower = 50;
			Check("monsterDamageOnce", (MonsterHit(self) == 15) && Near(up.EffectPower, 30));
			self.Hits = self.MaxHits;
			DamageManager.CauseDamage(self, self, DamageType.Irresistable, 20);
			Check("playerDamageWhole", (self.Hits == self.MaxHits - 20) && Near(up.EffectPower, 30));
			Check("standcastRefusedUnderIt", RefusesStandcast(self));
			//with a mana shield too: the energy shield first (20 -> 15), the mana shield takes the 15 (damage_final's order)
			self.AddPlugin(DamageManager.manaShieldKey, ((PluginDef) AbstractScript.GetByDefname("p_mana_shield")).Create());
			self.Mana = 100;
			int manaLost = MonsterHit(self);
			Console.WriteLine("STANDCAST_SHIELD: mana shield lost=" + manaLost + " mana=" + self.Mana + " energy=" + up.EffectPower + " hits=" + self.Hits + "/" + self.MaxHits);
			Check("manaShieldAfterEnergy", (manaLost == 0) && (self.Mana == 85) && Near(up.EffectPower, 10) && (self.Hits == self.MaxHits));
			self.DeletePlugin(DamageManager.manaShieldKey);
			up.EffectPower = 30;

			self.Hits = self.MaxHits;
			self.Stam = self.MaxStam;
			self.Direction = Direction.East;
			stepFromX = self.X;
			Console.WriteLine("STANDCAST_SHIELD_DONE");
		}

		/// <summary>After the client's step east: the shield is still up (ours used to drop on a step). Then the last 5
		/// of energy: 5 at 79 % (3) and the other 15 whole; it stays up below zero until the timer's next tick.</summary>
		[SteamFunction]
		public static void TestEnergyShieldStepped(Character self) {
			Check("stepKeepsIt", (self.X == stepFromX + 1) && (EnergyShieldPlugin.Of(self) == up) && EnergyShieldPlugin.IsUp(self));
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M); //the step left the first one's tile: no darkness rule
			rune.P(self.X, self.Y - 1, self.Z); //and the rune's reach
			up.EffectPower = 5;
			int lost = MonsterHit(self);
			Console.WriteLine("STANDCAST_SHIELD: partial lost=" + lost + " energy=" + up.EffectPower + " timer=" + up.Timer);
			Check("partialAbsorb", (lost == 18) && (up.EffectPower < 0) && !up.Resting && EnergyShieldPlugin.IsUp(self) && (up.Timer < 0.5));
			Console.WriteLine("STANDCAST_SHIELD_STEPPED");
		}

		/// <summary>A tick later: resting 180 s; spells whole; no new shield; Recall and Standcast allowed; a new shield
		/// cancelled goes into the rest, made short for TestEnergyShieldAfter.</summary>
		[SteamFunction]
		public static void TestEnergyShieldRest(Character self) {
			EnergyShieldAbilityDef shield = Shield;
			Check("restsAfterTheTick", up.Resting && (up.Timer > 170) && (up.Timer <= 180) && !EnergyShieldPlugin.IsUp(self));
			Check("restSpellWhole", SpellHit(self) == 20);
			self.Mana = 100;
			ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			shield.Use(self);
			Check("noNewWhileResting", (EnergyShieldPlugin.Of(self) == up) && (Amethysts(self) == 1) && (self.Mana == 100));
			Check("recallInRest", Casts(self, 32));
			Check("standcastInRest", !RefusesStandcast(self));

			self.DeletePlugin(PluginKey.Acquire("energyShield"));
			self.Mana = 100;
			shield.Use(self);
			up = EnergyShieldPlugin.Of(self);
			shield.Cancel(self);
			Check("cancelRests", (up != null) && up.Resting && Near(up.Timer, 180));
			if (up != null) {
				up.Timer = 1;
			}
			Console.WriteLine("STANDCAST_SHIELD_RESTING");
		}

		private static bool RefusesStandcast(Character self) {
			StandcastAbilityDef standcast = (StandcastAbilityDef) AbilityDef.GetByDefname("a_standcast");
			standcast.Toggle(self);
			bool on = standcast.IsActive(self);
			if (on) {
				standcast.Toggle(self);
			}
			return !on;
		}

		[SteamFunction]
		public static void TestEnergyShieldAfter(Character self) {
			Check("restEnds", EnergyShieldPlugin.Of(self) == null);
			if (monster != null) {
				monster.Delete();
			}
			if (rune != null) {
				rune.Delete();
			}
			Console.WriteLine("STANDCAST_SHIELD_AFTER");
		}

		/// <summary>The plain account's mage, for the player's own .energyshield.</summary>
		[SteamFunction]
		public static void TestEnergyShieldPlain(Character self) {
			StandcastAbilityDef standcast = (StandcastAbilityDef) AbilityDef.GetByDefname("a_standcast");
			if (standcast.IsActive(self)) {
				standcast.Toggle(self);
			}
			self.SetRealAbilityPoints(Shield, 10);
			ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			self.MaxMana = 100;
			self.Mana = 100;
			Console.WriteLine("STANDCAST_SHIELD_PLAIN");
		}

		[SteamFunction]
		public static void TestEnergyShieldPlainAfter(Character self) {
			Check("plainCommandShield", EnergyShieldPlugin.IsUp(self));
			Console.WriteLine("STANDCAST_SHIELD_PLAIN_DONE");
		}
	}
}
