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
	/// Server-side setup helper for #606's protocol test (Tests/Protocol/NpcResistTests.cs) -
	/// creates npc_test_resist (scripts/Tests/NpcResistTest.def) and reports the resulting
	/// Character.ResistX values, proving the real engine ResistX mechanism (already live,
	/// consumed in DamageManager.GetResistModifier - just never had any .def content behind
	/// it before this issue's converter mapping) actually applies the exact field names the
	/// converter now emits from the real archive's tag.resist_X tags.
	/// </summary>
	public static class NpcResistTests {
		/// <summary>#868 - a real monster's blow on a player under Reactive Armor.</summary>
		[SteamFunction]
		public static void TestReactiveArmorReport(Character self) {
			NPC monster = (NPC) ThingDef.GetByDefname("c_titan").Create(self.X + 1, self.Y, self.Z, self.M);
			self.MaxHits = 5000;
			self.Hits = 5000;
			int monsterHits = monster.Hits;
			DamageManager.ProcessSwing(monster, self);
			int without = 5000 - self.Hits;
			int returnedWithout = monsterHits - monster.Hits;

			((SpellDef) SpellDef.GetByDefname("s_reactive_armor")).ApplyEffectChar(self, self, 1000, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
			bool active = ReactiveArmorPluginDef.HasReactiveArmor(self);
			self.Hits = 5000;
			DamageManager.ProcessSwing(monster, self);
			int with = 5000 - self.Hits;
			int returned = monsterHits - monster.Hits;
			monster.Delete();
			Console.WriteLine("REACTIVE_ARMOR: active=" + active + " without=" + without + " returnedWithout=" + returnedWithout + " with=" + with + " returned=" + returned);
		}

		/// <summary>#860 - armor by Sphere's body part coverage, on a player and on a monster, the
		/// PvP profession change, and how often a_weapon_dodge dodges.</summary>
		[SteamFunction]
		public static void TestArmorCoverageReport(Character self) {
			System.Collections.Generic.List<Item> worn = new System.Collections.Generic.List<Item>();
			foreach (Item item in self.VisibleEquip) {
				if (item is Wearable) {
					worn.Add(item);
				}
			}
			foreach (Item item in worn) {
				item.Delete();
			}
			self.InvalidateCombatArmorValues();
			int bare = self.ArmorClassVsM;
			self.NewEquip(ThingDef.GetByDefname("i_iron_platemail")); //chest + back, 36%
			self.InvalidateCombatArmorValues();
			int chest = self.ArmorClassVsM;
			self.NewEquip(ThingDef.GetByDefname("i_iron_plate_helm")); //head, 14%
			self.InvalidateCombatArmorValues();
			int helm = self.ArmorClassVsM;

			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_resist");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			int npcBare = npc.ArmorClassVsM;
			npc.NewEquip(ThingDef.GetByDefname("i_iron_platemail"));
			npc.InvalidateCombatArmorValues();
			int npcWorn = npc.ArmorClassVsM;

			Player player = (Player) self;
			ProfessionDef original = player.Profession;
			player.Profession = Professions.Warrior;
			AbilityDef dodge = AbilityDef.GetByDefname("a_weapon_dodge");
			self.SetRealAbilityPoints(dodge, 20);
			int dodged = 0;
			for (int i = 0; i < 5000; i++) {
				if (DamageManager.TryDodge(npc, self, DamageType.PhysicalBlunt)) {
					dodged++;
				}
			}
			int playerSwing = DamageManager.GetPvPArmorChange(self);
			bool playerDodged = DamageManager.TryDodge(self, self, DamageType.PhysicalBlunt);
			bool spellDodged = DamageManager.TryDodge(npc, self, DamageType.MagicFire);
			self.SetRealAbilityPoints(dodge, 0);
			player.Profession = original;
			npc.Delete();
			NPC caster = (NPC) ThingDef.GetByDefname("c_titan_mage").Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("MIND_VALUES: titanMagePower=" + caster.MindPowerVsP + " titanMageEvalInt=" + caster.GetSkill(SkillName.EvalInt) +
				" titanMageDefense=" + caster.MindDefenseVsM + " resist=" + caster.GetSkill(SkillName.MagicResist));
			caster.Delete();
			Console.WriteLine("ARMOR_COVERAGE: bare=" + bare + " chest=" + chest + " helm=" + helm + " npcBare=" + npcBare + " npcWorn=" + npcWorn +
				" dodged=" + dodged + " pvpChange=" + playerSwing + " playerDodged=" + playerDodged + " spellDodged=" + spellDodged);
		}

		[SteamFunction]
		public static void TestCreateResistNpcAndReport(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_resist");
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("NPC_RESIST_TEST_RESULT: poison=" + npc.ResistPoison +
				" fire=" + npc.ResistFire + " electric=" + npc.ResistElectric +
				" slashing=" + npc.ResistSlashing + " stabbing=" + npc.ResistStabbing +
				" paralyse=" + npc.ResistParalyse + " curse=" + npc.ResistCurse + " sharp=" + npc.ResistSharp);
			Console.WriteLine("NPC_RESIST_MODIFIER:" +
				" slashing=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalSlashing).ToString(CultureInfo.InvariantCulture) +
				" sharp=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalSharp).ToString(CultureInfo.InvariantCulture) +
				" two=" + DamageManager.GetResistModifier(npc, DamageType.Physical | DamageType.Slashing | DamageType.Blunt).ToString(CultureInfo.InvariantCulture) +
				" fire=" + DamageManager.GetResistModifier(npc, DamageType.MagicFire).ToString(CultureInfo.InvariantCulture) +
				" both=" + DamageManager.GetResistModifier(npc, DamageType.PhysicalBlunt | DamageType.Fire).ToString(CultureInfo.InvariantCulture) +
				" plain=" + DamageManager.GetResistModifier(npc, DamageType.Physical).ToString(CultureInfo.InvariantCulture));
		}
	}
}
