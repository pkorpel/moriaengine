/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Test infrastructure for Mage Phase 7 (#491) - the 5 special abilities (Energy Shield,
	/// Silence, Concentration, Standcast, Magic Charge). Where a mechanic is pure calculation
	/// (Standcast's multiplier, Magic Charge's bonus, Concentration's discount), these helpers
	/// call the production helper classes directly (StandcastHelper/MagicChargeHelper are
	/// `internal`, same assembly, same as Magery.ApplyConcentrationDiscount) rather than paying
	/// for a real timed cast - same reasoning as RangerCombatTests's own GetWeaponSwingArgs-based
	/// sampling (no real swing needed to prove the math).
	/// </summary>
	public static class MageAbilityTests {

		[SteamFunction]
		public static void TestSetAbilityPoints(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			string defname = parts[0];
			int points = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			AbilityDef def = AbilityDef.GetByDefname(defname);
			self.SetRealAbilityPoints(def, points);
			Console.WriteLine("MAGEABILITY_TEST: set " + defname + "=" + points);
		}

		[SteamFunction]
		public static void TestSetLevel(Character self, ScriptArgs sa) {
			int level = Convert.ToInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			((Player) self).Level = level;
			Console.WriteLine("MAGEABILITY_TEST: level set to " + level);
		}

		/// <summary>#491 - Energy Shield's own ActivableAbilityDef.Resources (60 mana, 1 amethyst
		/// gem) are checked by AbilityDef.On_DenyActivate before activation is allowed - a fresh
		/// test character has neither by default.</summary>
		[SteamFunction]
		public static void TestGrantEnergyShieldResources(Character self) {
			self.MaxMana = 1000;
			self.Mana = 1000;
			ThingDef.GetByDefname("i_gem_amethyst").Create(self.Backpack);
			Console.WriteLine("MAGEABILITY_TEST: granted energy shield resources");
		}

		/// <summary>#491 - Energy Shield audit: a non-player attacker's damage against a
		/// shielded player should be absorbed (EnergyShieldPlugin.On_Damage only absorbs when
		/// args.attacker.IsPlayer is false) - spawns a real NPC attacker rather than using self or
		/// a player as the source, since player-on-player damage deliberately bypasses the
		/// shield per the existing (unmodified) implementation.</summary>
		[SteamFunction]
		public static void TestEnergyShieldAbsorbsNpcDamage(Character self, ScriptArgs sa) {
			int amount = Convert.ToInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);

			int before = self.Hits;
			DamageManager.CauseDamage(attacker, self, DamageType.PhysicalBlunt, amount);
			int after = self.Hits;
			Console.WriteLine("MAGEABILITY_TEST: ENERGYSHIELD hitsBefore=" + before + " hitsAfter=" + after);
		}

		[SteamFunction]
		public static void TestActivateAbility(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			((ActivableAbilityDef) AbilityDef.GetByDefname(defname)).Activate(self);
			Console.WriteLine("MAGEABILITY_TEST: activated " + defname);
		}

		[SteamFunction]
		public static void TestReportAbilityActive(Character self, ScriptArgs sa) {
			string defname = SplitArgs(sa)[0];
			bool active = ((ActivableAbilityDef) AbilityDef.GetByDefname(defname)).IsActive(self);
			Console.WriteLine("MAGEABILITY_TEST: " + defname + " active=" + active);
		}

		/// <summary>#491 - a dummy tough enough to survive Mind Blast's real EFFECT=0,70 range
		/// (a plain c_sheep_woolly has far less HP than that and would die before the silence
		/// effect gets attached to it, same class of issue as other phases' "don't kill the test
		/// target before you can observe it" gotchas).</summary>
		[SteamFunction]
		public static void TestSpawnToughDummy(Character self) {
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_sheep_woolly");
			NPC dummy = (NPC) dummyDef.Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.MaxHits = 10000;
			dummy.Hits = 10000;
			Console.WriteLine("MAGEABILITY_TEST: TOUGH_DUMMY uid=" + dummy.Uid);
		}

		/// <summary>#491 - Silence's 2 confirmed fixes: proc spell (Mind Blast, not Lightning)
		/// and base EffectDuration (1.5s, not 10s). Casts the given spell at the given target and
		/// reports whether the target ended up with the silence effect plugin.</summary>
		[SteamFunction]
		public static void TestCastAtOtherAndReportSilence(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			string defname = parts[0];
			uint targetUid = Convert.ToUInt32(parts[1], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);

			SpellDef spell = SpellDef.GetByDefname(defname);
			SpellBook book = self.FindLayer(1) as SpellBook;
			if (book == null) {
				foreach (Item i in self.Backpack.EnumShallow()) {
					book = i as SpellBook;
					if (book != null) {
						break;
					}
				}
			}
			SkillSequenceArgs magery = SkillSequenceArgs.Acquire(self, SkillName.Magery, target, null, book, spell, null);
			magery.PhaseSelect();
			Console.WriteLine("MAGEABILITY_TEST: SILENCE_CAST_STARTED castTime=" + spell.CastTime.ToString(CultureInfo.InvariantCulture));
		}

		[SteamFunction]
		public static void TestReportHasSilenceEffect(Character self, ScriptArgs sa) {
			uint targetUid = Convert.ToUInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			// Mind Blast's real EFFECT range is large enough to one-shot a sheep dummy - report
			// a clean "false" rather than NRE-ing on a deleted target.
			bool hasEffect = (target != null) && !target.IsDeleted && (target.GetPlugin(PluginKey.Acquire("silence_effect")) != null);
			Console.WriteLine("MAGEABILITY_TEST: hasSilenceEffect=" + hasEffect);
		}

		[SteamFunction]
		public static void TestReportSilenceEffectDuration(Character self) {
			AbilityDef a_silence = AbilityDef.GetByDefname("a_silence");
			Console.WriteLine("MAGEABILITY_TEST: silenceEffectDuration=" + a_silence.EffectDuration.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>#491 - Concentration: real mana-cost discount, computed via Magery's own
		/// internal helper directly (deterministic math, no need to pay for a real cast).</summary>
		[SteamFunction]
		public static void TestReportConcentrationDiscount(Character self, ScriptArgs sa) {
			int manaUse = Convert.ToInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			int discounted = MagerySkillDef.ApplyConcentrationDiscount(self, manaUse);
			Console.WriteLine("MAGEABILITY_TEST: manaUse=" + manaUse + " discounted=" + discounted);
		}

		/// <summary>#491 - Standcast: multiplier computed via the production helper directly.</summary>
		[SteamFunction]
		public static void TestReportStandcastMultiplier(Character self, ScriptArgs sa) {
			int basePower = Convert.ToInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			int boosted = (int) Math.Round(basePower * (1.0 + StandcastHelper.GetLevelBonus(self, 18)));
			Console.WriteLine("MAGEABILITY_TEST: basePower=" + basePower + " boosted=" + boosted);
		}

		/// <summary>#491 - Magic Charge: bonus computed via the production helper directly,
		/// against a real spell (so its DamageType/Flags are real, not faked).</summary>
		[SteamFunction]
		public static void TestReportMagicChargeBonus(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			string spellDefname = parts[0];
			int basePower = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			SpellDef spell = SpellDef.GetByDefname(spellDefname);
			int boosted = MagicChargeHelper.ApplyBonus(self, spell, basePower);
			Console.WriteLine("MAGEABILITY_TEST: spell=" + spellDefname + " basePower=" + basePower + " boosted=" + boosted);
		}

		[SteamFunction]
		public static void TestAddMagicCharge(Character self, ScriptArgs sa) {
			string school = SplitArgs(sa)[0];
			MagicChargeSchool s;
			if (string.Equals(school, "fire", StringComparison.OrdinalIgnoreCase)) {
				s = MagicChargeSchool.Fire;
			} else if (string.Equals(school, "energy", StringComparison.OrdinalIgnoreCase)) {
				s = MagicChargeSchool.Energy;
			} else {
				s = MagicChargeSchool.Mind;
			}
			MagicChargePlugin.AddCharge(self, s);
			Console.WriteLine("MAGEABILITY_TEST: added " + school + " charge, now=" + MagicChargePlugin.Get(self).GetCharge(s));
		}

		/// <summary>Same ScriptArgs dual-shape handling established elsewhere this session.</summary>
		private static string[] SplitArgs(ScriptArgs sa) {
			if ((sa.Argv.Length == 1) && (sa.Argv[0] is string combined)) {
				return combined.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			}
			string[] parts = new string[sa.Argv.Length];
			for (int i = 0; i < sa.Argv.Length; i++) {
				parts[i] = Convert.ToString(sa.Argv[i], CultureInfo.InvariantCulture);
			}
			return parts;
		}
	}
}
