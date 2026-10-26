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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side helpers for ".info"/D_Info regression scenarios driven by real packets from
	/// Tests/Protocol/InfoDialogTests.cs.
	/// </summary>
	public static class InfoDialogTests {

		/// <summary>Real user-reported bug: a monster's Skills field in `.info` shows as
		/// empty/unusable even though the monster genuinely has skills set (e.g. a titan's
		/// real Wrestling/Parry values). Root cause: Character.Skills used to return a bare
		/// SkillsEnumerator (IEnumerable&lt;ISkill&gt;, not IList) - DataViewProvider only has
		/// a registered IDataView for IList (ListDataView, HandleSubclasses=true), so clicking
		/// the field always hit "No DataView found for the given type" regardless of how many
		/// real skills were set. Spawns a titan (real content, real skill ranges), finds the
		/// Skills field via the same reflection path D_Info itself uses, and reports whether a
		/// DataView now resolves for its value plus how many skill entries it holds.</summary>
		[SteamFunction]
		public static void TestCheckTitanSkillsDataView(Character self) {
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.P());

			IDataView viewCls = DataViewProvider.FindDataViewByType(titan.GetType());
			if (viewCls == null) {
				Console.WriteLine("SKILLS_DATAVIEW: no top-level DataView for " + titan.GetType());
				return;
			}

			object skillsValue = null;
			foreach (IDataFieldView field in viewCls.GetDataFieldsPage(0, titan)) {
				if (string.Equals(field.GetName(titan), "Skills", StringComparison.Ordinal)) {
					skillsValue = field.GetValue(titan);
					break;
				}
			}

			if (skillsValue == null) {
				Console.WriteLine("SKILLS_DATAVIEW: Skills field not found or null value");
				return;
			}

			IDataView skillsViewCls = DataViewProvider.FindDataViewByType(skillsValue.GetType());
			if (skillsViewCls == null) {
				Console.WriteLine("SKILLS_DATAVIEW: no DataView for Skills value type " + skillsValue.GetType());
				return;
			}

			int skillCount = 0;
			foreach (ISkill skill in titan.Skills) {
				skillCount++;
			}
			Console.WriteLine("SKILLS_DATAVIEW: found DataView, real skill count=" + skillCount);
		}

		/// <summary>Same bug class as TestCheckTitanSkillsDataView above, for Character.Abilities
		/// (#651, found while investigating #4). Grants self a real ability (a_weapon_dodge,
		/// the existing unimplementedAbilities.scp stub) then checks the Abilities field the
		/// same way.</summary>
		[SteamFunction]
		public static void TestCheckAbilitiesDataView(Character self) {
			AbilityDef dodgeDef = (AbilityDef) AbstractScript.GetByDefname("a_weapon_dodge");
			self.SetRealAbilityPoints(dodgeDef, 10);

			IDataView viewCls = DataViewProvider.FindDataViewByType(self.GetType());
			if (viewCls == null) {
				Console.WriteLine("ABILITIES_DATAVIEW: no top-level DataView for " + self.GetType());
				return;
			}

			object abilitiesValue = null;
			foreach (IDataFieldView field in viewCls.GetDataFieldsPage(0, self)) {
				if (string.Equals(field.GetName(self), "Abilities", StringComparison.Ordinal)) {
					abilitiesValue = field.GetValue(self);
					break;
				}
			}

			if (abilitiesValue == null) {
				Console.WriteLine("ABILITIES_DATAVIEW: Abilities field not found or null value");
				return;
			}

			IDataView abilitiesViewCls = DataViewProvider.FindDataViewByType(abilitiesValue.GetType());
			if (abilitiesViewCls == null) {
				Console.WriteLine("ABILITIES_DATAVIEW: no DataView for Abilities value type " + abilitiesValue.GetType());
				return;
			}

			int abilityCount = 0;
			foreach (Ability ability in self.Abilities) {
				abilityCount++;
			}
			Console.WriteLine("ABILITIES_DATAVIEW: found DataView, real ability count=" + abilityCount);
		}

		/// <summary>#664 - a List's own entries (e.g. opening the Skills/Abilities field found
		/// by TestCheckTitanSkillsDataView/TestCheckAbilitiesDataView above) used to label every
		/// single row as a generic "[0](Obj)", "[1](Obj)", etc, with zero indication of what was
		/// actually in that slot. Confirms a titan's real Skills list rows now show the real
		/// SkillName (e.g. "[0] Wrestling") and a granted ability's row shows its real Defname
		/// (e.g. "[0] a_weapon_dodge"), not just a bare index.</summary>
		[SteamFunction]
		public static void TestCheckListEntryLabels(Character self) {
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.P());

			IDataView skillsViewCls = DataViewProvider.FindDataViewByType(titan.Skills.GetType());
			bool skillLabelHasName = false;
			foreach (IDataFieldView field in skillsViewCls.GetDataFieldsPage(0, titan.Skills)) {
				string name = field.GetName(titan.Skills);
				if (name.StartsWith("[0] ", StringComparison.Ordinal) && !name.Contains("[0] [0]")) {
					skillLabelHasName = true;
					break;
				}
			}

			AbilityDef dodgeDef = (AbilityDef) AbstractScript.GetByDefname("a_weapon_dodge");
			self.SetRealAbilityPoints(dodgeDef, 10);
			IDataView abilitiesViewCls = DataViewProvider.FindDataViewByType(self.Abilities.GetType());
			bool abilityLabelHasDefname = false;
			foreach (IDataFieldView field in abilitiesViewCls.GetDataFieldsPage(0, self.Abilities)) {
				string name = field.GetName(self.Abilities);
				if (name.Contains(dodgeDef.Defname)) {
					abilityLabelHasDefname = true;
					break;
				}
			}

			Console.WriteLine("LIST_ENTRY_LABELS: skillLabelHasName=" + skillLabelHasName +
				" abilityLabelHasDefname=" + abilityLabelHasDefname);
		}

		/// <summary>#4 of the 2026-10-26 monster-testing batch - a titan's TEVENTS lines
		/// (e_monster/e_character/e_npc_knockback) never resolve to real TriggerGroups (this
		/// engine deliberately never built those legacy Sphere AI-hook names). Confirms each
		/// one now grants a real, inspectable stub ability instead of just logging a warning.</summary>
		[SteamFunction]
		public static void TestCheckTitanTeventAbilities(Character self) {
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.P());

			bool hasMonster = false, hasCharacter = false, hasKnockback = false;
			int totalAbilities = 0;
			foreach (Ability ability in titan.Abilities) {
				totalAbilities++;
				string defname = ((AbilityDef) ability.Def).Defname;
				if (string.Equals(defname, "a_tevent_e_monster", StringComparison.OrdinalIgnoreCase)) {
					hasMonster = true;
				} else if (string.Equals(defname, "a_tevent_e_character", StringComparison.OrdinalIgnoreCase)) {
					hasCharacter = true;
				} else if (string.Equals(defname, "a_tevent_e_npc_knockback", StringComparison.OrdinalIgnoreCase)) {
					hasKnockback = true;
				}
			}

			Console.WriteLine("TEVENT_ABILITIES: total=" + totalAbilities +
				" hasMonster=" + hasMonster + " hasCharacter=" + hasCharacter + " hasKnockback=" + hasKnockback);
		}

		/// <summary>Kills and immediately resurrects self (same pattern as AttrFlagTests'/
		/// NotorietyTests' own self-death helpers) to produce a real Corpse at self's own
		/// position - the real-user-reported ".info on a corpse" crash needs an actual Corpse
		/// instance, not a throwaway item.
		///
		/// Real bug found writing this: the parameterless Character.Resurrect() auto-detects
		/// and loots+DELETES any corpse within 1 tile owned by the resurrecting character
		/// (Corpse.ReturnStuffToChar's own this.Delete() call) - exactly where this corpse sits.
		/// Calling the explicit Resurrect(Corpse) overload with null instead skips that
		/// auto-detection entirely, so the corpse survives for the test to actually open .info
		/// on.</summary>
		[SteamFunction]
		public static void TestSetupCorpseForInfo(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player victim = (Player) self;
			int x = victim.X;
			int y = victim.Y;

			NPCDef killerDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC killer = (NPC) killerDef.Create(victim.P());

			victim.CauseDeath(killer);

			Corpse foundCorpse = null;
			foreach (AbstractItem item in victim.GetMap().GetItemsInRange(x, y, 5)) {
				Corpse corpse = item as Corpse;
				if ((corpse != null) && (corpse.Owner == victim)) {
					foundCorpse = corpse;
					break;
				}
			}

			victim.Resurrect(null);

			if (foundCorpse != null) {
				Console.WriteLine("INFO_CORPSE_SETUP: uid=" + foundCorpse.Uid);
			} else {
				Console.WriteLine("INFO_CORPSE_SETUP: none");
			}
		}
	}
}
