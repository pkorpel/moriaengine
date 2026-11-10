using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1386 - Standcast as Erebor's lasting toggle doing its work in its plugin: real casts (PhaseSelect with the
	/// target set, as MageryRulesTests) freeze the caster and set the boost through the caster's @spellCast; SpellDef's
	/// @monsterSpellPower gives the first target the bonus, every target half for an area spell; a ground cast gets
	/// nothing; a real explosion is stronger with it on; the energy shield switches it off; a frozen caster's real step is
	/// refused before it shoves anyone; .standcast from a plain account is for mages, not warriors. Protocol side:
	/// Tests/Protocol/StandcastTests.cs.</summary>
	public static class StandcastTests {
		private static NPC dummy, blocker;
		private static int powerOn, stamBefore, blockerX;
		private static Point4D walkFrom;

		private static void Check(string name, bool ok) {
			Console.WriteLine("STANDCAST_CHECK: " + name + "=" + ok);
		}

		private static bool Near(double a, double b) {
			return Math.Abs(a - b) < 0.25;
		}

		private static StandcastAbilityDef Standcast {
			get {
				return (StandcastAbilityDef) AbilityDef.GetByDefname("a_standcast");
			}
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

		private static void Cast(Character self, string spellName, IPoint4D target) {
			self.Mana = self.MaxMana;
			SkillSequenceArgs.Acquire(self, SkillName.Magery, target, null, BookOf(self), SpellDef.GetByDefname(spellName), null).PhaseSelect();
		}

		private static bool StepRefused(Character self) {
			return !self.CheckCanMove(false);
		}

		/// <summary>Switches it on, casts at a monster, checks the freeze, the bonus and the recast times, then an area
		/// spell; leaves the freeze to end soon for TestStandcastAfter.</summary>
		[SteamFunction]
		public static void TestStandcastStart(Character self) {
			Player player = (Player) self;
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M);
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.MaxMana = 1000;
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is ItemDef def) && def.PrettyDefname.StartsWith("i_reag_", StringComparison.OrdinalIgnoreCase)) {
					((Item) def.Create(self.Backpack)).Amount = 50;
				}
			}
			player.Level = 100;
			player.Profession = Professions.Mage;
			dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;
			NPCAiTimer.Freeze(dummy, 120);
			self.AddTriggerGroup(SingletonScript<E_StandcastPowerProbe>.Instance);
			SpellDef arrow = SpellDef.GetByDefname("s_magic_arrow"), explosion = SpellDef.GetByDefname("s_explosion");

			Check("offNoBonus", arrow.MonsterLevelBonus(self, dummy) == 0);
			Standcast.Toggle(self);
			Check("toggleOn", Standcast.IsActive(self));

			Cast(self, "s_magic_arrow", dummy);
			CastFreezePlugin freeze = self.GetPlugin(CastFreezePlugin.key) as CastFreezePlugin;
			Check("freezeOnCast", (freeze != null) && Near(freeze.Timer, arrow.CastTime));
			Check("stepRefused", StepRefused(self));
			//#860 - level 100 * 0.02, the first monster target only
			double first = arrow.MonsterLevelBonus(self, dummy), second = arrow.MonsterLevelBonus(self, dummy);
			Console.WriteLine("STANDCAST_BONUS: first=" + first.ToString(CultureInfo.InvariantCulture) + " second=" + second.ToString(CultureInfo.InvariantCulture));
			Check("firstTargetOnly", Near(first, 2.0) && (second == 0));

			//f_setStandcast's recast times
			freeze.Timer = 5.0;
			CastFreezePlugin.FreezeFor(self, 2.0);
			Check("recastLongerPenalty", Near(freeze.Timer, 6.5));
			freeze.Timer = 0.5;
			CastFreezePlugin.FreezeFor(self, 2.0);
			Check("recastAlmostDone", Near(freeze.Timer, 2.0));
			freeze.Timer = 2.0;
			CastFreezePlugin.FreezeFor(self, 2.0);
			Check("recastEarly", Near(freeze.Timer, 3.0));

			E_StandcastPowerProbe.target = dummy;
			E_StandcastPowerProbe.power = 0;
			Cast(self, "s_explosion", dummy);
			double a = explosion.MonsterLevelBonus(self, dummy), b = explosion.MonsterLevelBonus(self, dummy);
			Check("areaHalfEveryTarget", Near(a, 1.0) && Near(b, 1.0));
			Console.WriteLine("STANDCAST_STARTED");
		}

		/// <summary>Polled by the protocol test: ready once the explosion has landed on the dummy and no freeze is left.
		/// A plain caster fizzles one explosion in 33 (difficulty 600 at Magery 100.0): cast again then.</summary>
		[SteamFunction]
		public static void TestStandcastPowerReady(Character self) {
			if ((E_StandcastPowerProbe.power > 0) && (self.GetPlugin(CastFreezePlugin.key) == null)) {
				Console.WriteLine("STANDCAST_POWER_READY");
				return;
			}
			if ((E_StandcastPowerProbe.power == 0) && (self.CurrentSkillName != SkillName.Magery)) {
				Cast(self, "s_explosion", dummy);
			}
			Console.WriteLine("STANDCAST_POWER_WAIT");
		}

		/// <summary>After the explosion's cast time: its real power taken, still on, the freeze over, a ground cast gets
		/// nothing; then the same explosion with Standcast off, for TestStandcastPower.</summary>
		[SteamFunction]
		public static void TestStandcastAfter(Character self) {
			SpellDef arrow = SpellDef.GetByDefname("s_magic_arrow");
			powerOn = E_StandcastPowerProbe.power;
			Check("staysOn", Standcast.IsActive(self));
			Check("freezeEnded", self.GetPlugin(CastFreezePlugin.key) == null);
			Check("stepAllowed", !StepRefused(self));

			Cast(self, "s_fire_field", new Point4D(self.X + 3, self.Y, self.Z, self.M));
			Check("groundCastRuns", self.CurrentSkillName == SkillName.Magery); //so its @spellCast came and found no target
			Check("groundNoFreeze", self.GetPlugin(CastFreezePlugin.key) == null);
			Check("groundNoBonus", arrow.MonsterLevelBonus(self, dummy) == 0);
			self.AbortSkill();

			Standcast.Deactivate(self);
			E_StandcastPowerProbe.power = 0;
			Cast(self, "s_explosion", dummy);
			Console.WriteLine("STANDCAST_SECOND_CAST");
		}

		/// <summary>The explosion without Standcast was weaker; the shield switches it off and keeps it off, the
		/// command switches it on and off; then a freeze and a sheep in the way for the client's real step.</summary>
		[SteamFunction]
		public static void TestStandcastPower(Character self) {
			int powerOff = E_StandcastPowerProbe.power;
			Console.WriteLine("STANDCAST_POWER: on=" + powerOn + " off=" + powerOff);
			Check("realCastBoosted", (powerOff > 0) && (powerOn > powerOff));
			self.RemoveTriggerGroup(SingletonScript<E_StandcastPowerProbe>.Instance);
			Standcast.Toggle(self);
			ActivableAbilityDef shield = (ActivableAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
			self.SetRealAbilityPoints(shield, 10);
			self.Mana = self.MaxMana;
			ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			shield.Activate(self);
			Check("shieldUp", shield.IsActive(self));
			Check("shieldEndsIt", !Standcast.IsActive(self));
			Standcast.Toggle(self);
			Check("noToggleUnderShield", !Standcast.IsActive(self));
			shield.Deactivate(self);

			Standcast.Toggle(self);
			bool on = Standcast.IsActive(self);
			Standcast.Toggle(self);
			Check("toggleOff", on && !Standcast.IsActive(self));

			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Direction = Direction.East;
			blocker = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(blocker, 120);
			blockerX = blocker.X;
			walkFrom = new Point4D(self.X, self.Y, self.Z, self.M);
			self.Stam = self.MaxStam;
			stamBefore = self.Stam;
			CastFreezePlugin.FreezeFor(self, 30);
			Console.WriteLine("STANDCAST_WALK_READY");
		}

		/// <summary>The client's step east into the sheep was refused before the collision: no shove, no stamina
		/// paid. Then a warrior for the plain .standcast.</summary>
		[SteamFunction]
		public static void TestStandcastWalked(Character self) {
			Check("frozenNoShove", (self.X == walkFrom.X) && (self.Y == walkFrom.Y) && (self.Stam == stamBefore) && (blocker.X == blockerX));
			self.DeletePlugin(CastFreezePlugin.key);
			Console.WriteLine("STANDCAST_WALKED");
		}

		/// <summary>A character on a plain account (not the server's first, an admin): a warrior first.</summary>
		[SteamFunction]
		public static void TestStandcastPlain(Character self) {
			((Player) self).Profession = Professions.Warrior;
			Console.WriteLine("STANDCAST_WARRIOR");
		}

		[SteamFunction]
		public static void TestStandcastAfterWarrior(Character self) {
			Check("notForWarrior", !Standcast.IsActive(self));
			((Player) self).Profession = Professions.Mage;
			Console.WriteLine("STANDCAST_MAGE");
		}

		[SteamFunction]
		public static void TestStandcastAfterMage(Character self) {
			Check("plainCommandOn", Standcast.IsActive(self));
			Console.WriteLine("STANDCAST_DONE");
		}
	}

	/// <summary>#1386 - the power of a real cast on the test's target, from the caster's @causeSpellEffect.</summary>
	public class E_StandcastPowerProbe : CompiledTriggerGroup {
		internal static Character target;
		internal static int power;

		public void On_CauseSpellEffect(Character self, SpellEffectArgs spellEffectArgs) {
			if (spellEffectArgs.CurrentTarget == target) {
				power = spellEffectArgs.SpellPower;
			}
		}
	}
}
