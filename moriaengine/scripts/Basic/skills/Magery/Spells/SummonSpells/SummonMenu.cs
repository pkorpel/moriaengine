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

using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Sphere's sm_summon and e_necro's @spellcast 40: a player picks the creature of Summon before the cursor,
	/// by profession, skill, staff and what it costs, in a gump or Sphere's picture menu, MenuStyles. The dearest creatures
	/// take more mana and reagents when the cast starts.</summary>
	internal static class SummonMenu {
		private const string SummonSpell = "s_summon_creature";
		private const int Spruce = 1, Chestnut = 2, Oak = 3, Teak = 4, Mahagon = 5, AnyAbility = -1;
		private static readonly TagKey choiceTK = TagKey.Acquire("_summonChoice_");

		internal sealed class Row {
			internal readonly string icon, label, creature, abilityTag;
			internal readonly bool necro;
			internal readonly int skill, staff, mana, abilityLeast, abilityMost;
			internal readonly (string reagent, int amount)[] needs;

			internal Row(bool necro, string icon, string label, string creature, int skill, int staff, int mana = 0, (string, int)[] needs = null, string abilityTag = null, int abilityLeast = AnyAbility, int abilityMost = int.MaxValue) {
				this.necro = necro;
				this.icon = icon;
				this.label = label;
				this.creature = creature;
				this.skill = skill;
				this.staff = staff;
				this.mana = mana;
				this.needs = needs ?? new (string, int)[0];
				this.abilityTag = abilityTag;
				this.abilityLeast = abilityLeast;
				this.abilityMost = abilityMost;
			}
		}

		private static readonly (string, int)[] satanNeeds = { ("i_reag_daemon_bone", 2), ("i_reag_dragon_blood", 2) };
		private static readonly (string, int)[] vampireNeeds = { ("i_reag_executioners_cap", 1), ("i_reag_dragon_blood", 2) };
		private const string SatanTag = "ra_satan_vylepseni", VampireTag = "ra_velky_vampyr_vylepseni", ArcherTag = "ra_lucistnik_vylepseni";

		internal static readonly Row[] rows = {
			new Row(false, "i_pet_horse_brown_lt", null, "c_horse_brown_mage", 400, Oak),
			new Row(false, "i_pet_bear_polar", null, "c_bear_polar_mage", 600, Oak),
			new Row(false, "i_pet_scorp", null, "c_scorpion_giant_mage", 540, Oak),
			new Row(false, "i_pet_grizzly", null, "c_bear_grizzly_mage", 480, Chestnut),
			new Row(false, "i_pet_bear", "Black Bear", "c_bear_black_mage", 420, Spruce),
			new Row(false, "i_pet_bear", "Brown Bear", "c_bear_brown_mage", 360, Spruce),
			new Row(false, "i_pet_rat_2", null, "c_rat_giant_mage", 300, Spruce),
			new Row(true, "i_pet_horse_gray", "Bone Horse", "c_horse_undead_necro", 400, Oak),
			new Row(true, "i_pet_garg", "Satan level 1", "c_satan_necro", 960, Mahagon, 100, satanNeeds, SatanTag, AnyAbility, 9),
			new Row(true, "i_pet_garg", "Satan level 2", "c_satan_necro_2", 960, Mahagon, 100, satanNeeds, SatanTag, 10, 19),
			new Row(true, "i_pet_garg", "Satan level 3", "c_satan_necro_3", 960, Mahagon, 100, satanNeeds, SatanTag, 20, 29),
			new Row(true, "i_pet_garg", "Satan level 4", "c_satan_necro_4", 960, Mahagon, 100, satanNeeds, SatanTag, 30),
			new Row(true, "i_pet_garg", "Temny Vampir Level 1", "c_vampire_great_necro", 920, Teak, 70, vampireNeeds, VampireTag, AnyAbility, 9),
			new Row(true, "i_pet_garg", "Temny Vampir Level 2", "c_vampire_great_necro_2", 920, Teak, 70, vampireNeeds, VampireTag, 10, 19),
			new Row(true, "i_pet_garg", "Temny Vampir Level 3", "c_vampire_great_necro_3", 920, Teak, 70, vampireNeeds, VampireTag, 20, 29),
			new Row(true, "i_pet_garg", "Temny Vampir Level 4", "c_vampire_great_necro_4", 920, Teak, 70, vampireNeeds, VampireTag, 30),
			new Row(true, "i_pet_lich", "Liche Lord", "c_liche_lord_necro", 880, Teak, 40, new[] { ("i_reag_blood_vial", 2) }),
			new Row(true, "i_pet_elem_water", "Death Elemental", "c_elem_death_necro", 840, Teak, 35, new[] { ("i_reag_bone", 2) }),
			new Row(true, "i_pet_zombie", "Mummy", "c_mummy_necro", 780, Spruce, 30),
			new Row(true, "i_pet_garg", "Vampire", "c_vampire_necro", 720, Spruce, 30),
			new Row(true, "i_pet_lich", "Liche", "c_liche_necro", 660, Oak, 30),
			new Row(true, "i_pet_ghoul", "Fext", "c_fext_necro", 600, Chestnut),
			new Row(true, "i_pet_skeleton", "Skeleton Mage", "c_skeleton_mage_necro", 540, Oak),
			new Row(true, "i_pet_skeleton", "Skeleton Archer Level 1", "c_skeleton_archer_necro", 480, Chestnut, 0, null, ArcherTag, AnyAbility, 9),
			new Row(true, "i_pet_skeleton", "Skeleton Archer Level 2", "c_skeleton_archer_necro_2", 480, Chestnut, 0, null, ArcherTag, 10, 19),
			new Row(true, "i_pet_skeleton", "Skeleton Archer Level 3", "c_skeleton_archer_necro_3", 480, Chestnut, 0, null, ArcherTag, 20, 29),
			new Row(true, "i_pet_skeleton", "Skeleton Archer Level 4", "c_skeleton_archer_necro_4", 480, Chestnut, 0, null, ArcherTag, 30),
			new Row(true, "i_pet_skeleton", "Skeleton Sword", "c_skeleton_sword_necro", 480, Chestnut),
			new Row(true, "i_pet_skeleton", "Skeleton Axe", "c_skeleton_axe_necro", 360, Chestnut),
			new Row(true, "i_pet_skeleton", null, "c_skeleton_necro", 300, Spruce),
			new Row(true, "i_pet_zombie", null, "c_zombie_necro", 300, Spruce),
		};

		//e_necro's @spellcast, on the first creature of each family only
		private static readonly Dictionary<string, (int mana, (string, int)[] reagents)> startCosts = new Dictionary<string, (int, (string, int)[])> {
			{ "c_satan_necro", (84, satanNeeds) },
			{ "c_vampire_great_necro", (34, vampireNeeds) },
			{ "c_liche_lord_necro", (24, new[] { ("i_reag_blood_vial", 2) }) },
			{ "c_elem_death_necro", (19, new[] { ("i_reag_bone", 2) }) },
			{ "c_vampire_necro", (16, new (string, int)[0]) },
			{ "c_mummy_necro", (14, new (string, int)[0]) },
			{ "c_liche_necro", (9, new (string, int)[0]) },
		};

		internal static bool IsOffered(Player self, Row row) {
			if (row.necro != Professions.HasProfession(self, Professions.Necromant)) {
				return false;
			}
			if ((self.GetSkill(SkillName.Magery) <= row.skill) || !SpellCastRules.HasStaff(self, row.staff) || (self.Mana <= row.mana)) {
				return false;
			}
			foreach ((string reagent, int amount) in row.needs) {
				if (CountOf(self, reagent) < amount) {
					return false;
				}
			}
			if (row.abilityTag != null) {
				int points = AnimateDeadSpellDef.Ability(self, row.abilityTag);
				return (points >= row.abilityLeast) && (points <= row.abilityMost);
			}
			return true;
		}

		private static int CountOf(Character self, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int count = 0;
			foreach (Item item in self.Backpack) {
				if (item.Def == def) {
					count += item.Amount;
				}
			}
			return count;
		}

		private static void Consume(Character self, string defname, int amount) {
			ThingDef def = ThingDef.GetByDefname(defname);
			foreach (Item item in new List<Item>(ItemsOf(self))) {
				if ((amount > 0) && (item.Def == def)) {
					int taken = System.Math.Min(amount, item.Amount);
					item.Consume(taken);
					amount -= taken;
				}
			}
		}

		private static IEnumerable<Item> ItemsOf(Character self) {
			foreach (Item item in self.Backpack) {
				yield return item;
			}
		}

		/// <summary>True when the menu took over from the cast: it starts anew from the answer.</summary>
		internal static bool Intercept(SkillSequenceArgs mageryArgs, SpellDef spell) {
			if ((mageryArgs.Self is Player self) && (spell == SpellDef.GetByDefname(SummonSpell)) && (mageryArgs.Param2 == null)) {
				Item tool = (Item) mageryArgs.Tool;
				List<int> offered = new List<int>();
				List<PictureMenuRow> pictures = new List<PictureMenuRow>();
				for (int i = 0; i < rows.Length; i++) {
					if (IsOffered(self, rows[i]) && (ThingDef.GetByDefname(rows[i].icon) is ItemDef icon)) {
						offered.Add(i);
						pictures.Add(new PictureMenuRow(icon.Model, rows[i].label ?? ThingDef.GetByDefname(rows[i].creature).Name));
					}
				}
				MenuStyles.Show(self, SingletonScript<D_SummonMenu>.Instance, new DialogArgs(tool, spell), Loc<SummonMenuLoc>.Get(self.Language).Title, pictures,
					index => {
						if (IsOffered(self, rows[offered[index]])) {
							Chose(self, tool, spell, rows[offered[index]]);
						}
					},
					null);
				return true;
			}
			return false;
		}

		internal static void Chose(Player self, Item tool, SpellDef spell, Row row) {
			if (tool.IsDeleted) {
				return;
			}
			NPCDef creature = (NPCDef) ThingDef.GetByDefname(row.creature);
			self.SetTag(choiceTK, creature);
			SkillSequenceArgs.Acquire(self, SkillName.Magery, null, null, tool, spell, creature).PhaseSelect();
		}

		internal static void PayAtStart(Character self) {
			if (!self.IsGM && (self.GetTag(choiceTK) is NPCDef creature) && startCosts.TryGetValue(creature.PrettyDefname, out var cost)) {
				self.Mana = (short) System.Math.Max(0, self.Mana - cost.mana);
				foreach ((string reagent, int amount) in cost.reagents) {
					Consume(self, reagent, amount);
				}
			}
		}

		internal static NPCDef TakeChoice(Character self, SpellDef spell) {
			if (spell != SpellDef.GetByDefname(SummonSpell)) {
				return null;
			}
			NPCDef creature = self.GetTag(choiceTK) as NPCDef;
			self.RemoveTag(choiceTK);
			return creature;
		}
	}

	public class D_SummonMenu : CompiledGumpDef {
		private const int Width = 420, RowHeight = 46, PerColumn = 8, ColumnWidth = 200;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player self = (Player) sendTo;
			List<int> offered = new List<int>();
			for (int i = 0; i < SummonMenu.rows.Length; i++) {
				if (SummonMenu.IsOffered(self, SummonMenu.rows[i])) {
					offered.Add(i);
				}
			}
			int lines = System.Math.Min(offered.Count, PerColumn);
			this.SetLocation(60, 60);
			this.AddBackground(0, 0, Width, 50 + (System.Math.Max(lines, 1) * RowHeight), 5054);
			this.AddLabel(14, 12, 0, Loc<SummonMenuLoc>.Get(self.Language).Title);
			for (int i = 0; i < offered.Count; i++) {
				int index = offered[i];
				SummonMenu.Row row = SummonMenu.rows[index];
				int x = 14 + ((i / PerColumn) * ColumnWidth);
				int y = 40 + ((i % PerColumn) * RowHeight);
				this.AddButton(x, y + 8, 2103, 2104, index + 1, GumpButtonType.Reply, 0);
				if (ThingDef.GetByDefname(row.icon) is ItemDef icon) {
					this.AddItem(x + 16, y, icon.Model);
				}
				this.AddLabel(x + 70, y + 8, 0, row.label ?? ThingDef.GetByDefname(row.creature).Name);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int index = (int) gr.PressedButton - 1;
			if ((index >= 0) && (index < SummonMenu.rows.Length)) {
				Player self = (Player) gi.Cont;
				SummonMenu.Row row = SummonMenu.rows[index];
				if (SummonMenu.IsOffered(self, row)) {
					SummonMenu.Chose(self, (Item) args[0], (SpellDef) args[1], row);
				}
			}
		}
	}

	internal class SummonMenuLoc : CompiledLocStringCollection<SummonMenuLoc> {
		public string Title = "What do you want to summon ?";
	}
}
