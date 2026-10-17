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

using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// GM-tunable skill-advancement rates (#327/#330), shown as a "Skill Advancement" category
	/// in the .settings dialog - same pattern as DecaySettings/LevelingSettings. Covers exactly
	/// the 13 use-trained skills from #140/#327/#328/#329, not the other 40 from #284 (those are
	/// correctly leveling-point-only, nothing to tune here).
	///
	/// The 11 native-curve skills (3 fields each - low/mid/high) are plain pass-through
	/// PROPERTIES, not stored fields - each getter/setter reads/writes the live
	/// SkillDef.AdvRate array directly, so an edit here takes effect immediately with zero
	/// extra sync step, and persists for free via AbstractDef's own existing FieldValue save
	/// mechanism (a GM-edited .def field is already marked changed and written to globals.sav -
	/// confirmed working end to end by the #168 fix). ViewableClassGenerator doesn't care
	/// whether a member is a field or a property - it reflects over both identically and calls
	/// whatever setter exists, confirmed by reading GenerateFieldIDFW's own codegen directly
	/// before relying on this.
	///
	/// The 2 custom-formula skills (Lockpicking/Taming, 1 field each) have no other backing
	/// store to write through to, so they're real [SaveableData] fields here - this class IS
	/// the source of truth for those two, read directly by Lockpicking.cs/TamingSkillDef.cs.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class SkillAdvancementSettings : SettingsMetaCategory {

		[SavedMember]
		public static SkillAdvancementSettings instance = new SkillAdvancementSettings();

		[LoadingInitializer]
		public SkillAdvancementSettings() {
		}

		[SaveableData]
		public double lockpickingAdvRate = 600;

		[SaveableData]
		public double tamingAdvRate = 600;

		public double alchemyLow { get { return GetAdvRateComponent(SkillName.Alchemy, 0); } set { SetAdvRateComponent(SkillName.Alchemy, 0, value); } }
		public double alchemyMid { get { return GetAdvRateComponent(SkillName.Alchemy, 1); } set { SetAdvRateComponent(SkillName.Alchemy, 1, value); } }
		public double alchemyHigh { get { return GetAdvRateComponent(SkillName.Alchemy, 2); } set { SetAdvRateComponent(SkillName.Alchemy, 2, value); } }

		public double blacksmithingLow { get { return GetAdvRateComponent(SkillName.Blacksmith, 0); } set { SetAdvRateComponent(SkillName.Blacksmith, 0, value); } }
		public double blacksmithingMid { get { return GetAdvRateComponent(SkillName.Blacksmith, 1); } set { SetAdvRateComponent(SkillName.Blacksmith, 1, value); } }
		public double blacksmithingHigh { get { return GetAdvRateComponent(SkillName.Blacksmith, 2); } set { SetAdvRateComponent(SkillName.Blacksmith, 2, value); } }

		public double bowcraftLow { get { return GetAdvRateComponent(SkillName.Fletching, 0); } set { SetAdvRateComponent(SkillName.Fletching, 0, value); } }
		public double bowcraftMid { get { return GetAdvRateComponent(SkillName.Fletching, 1); } set { SetAdvRateComponent(SkillName.Fletching, 1, value); } }
		public double bowcraftHigh { get { return GetAdvRateComponent(SkillName.Fletching, 2); } set { SetAdvRateComponent(SkillName.Fletching, 2, value); } }

		public double carpentryLow { get { return GetAdvRateComponent(SkillName.Carpentry, 0); } set { SetAdvRateComponent(SkillName.Carpentry, 0, value); } }
		public double carpentryMid { get { return GetAdvRateComponent(SkillName.Carpentry, 1); } set { SetAdvRateComponent(SkillName.Carpentry, 1, value); } }
		public double carpentryHigh { get { return GetAdvRateComponent(SkillName.Carpentry, 2); } set { SetAdvRateComponent(SkillName.Carpentry, 2, value); } }

		public double cookingLow { get { return GetAdvRateComponent(SkillName.Cooking, 0); } set { SetAdvRateComponent(SkillName.Cooking, 0, value); } }
		public double cookingMid { get { return GetAdvRateComponent(SkillName.Cooking, 1); } set { SetAdvRateComponent(SkillName.Cooking, 1, value); } }
		public double cookingHigh { get { return GetAdvRateComponent(SkillName.Cooking, 2); } set { SetAdvRateComponent(SkillName.Cooking, 2, value); } }

		public double fishingLow { get { return GetAdvRateComponent(SkillName.Fishing, 0); } set { SetAdvRateComponent(SkillName.Fishing, 0, value); } }
		public double fishingMid { get { return GetAdvRateComponent(SkillName.Fishing, 1); } set { SetAdvRateComponent(SkillName.Fishing, 1, value); } }
		public double fishingHigh { get { return GetAdvRateComponent(SkillName.Fishing, 2); } set { SetAdvRateComponent(SkillName.Fishing, 2, value); } }

		public double inscriptionLow { get { return GetAdvRateComponent(SkillName.Inscribe, 0); } set { SetAdvRateComponent(SkillName.Inscribe, 0, value); } }
		public double inscriptionMid { get { return GetAdvRateComponent(SkillName.Inscribe, 1); } set { SetAdvRateComponent(SkillName.Inscribe, 1, value); } }
		public double inscriptionHigh { get { return GetAdvRateComponent(SkillName.Inscribe, 2); } set { SetAdvRateComponent(SkillName.Inscribe, 2, value); } }

		public double lumberjackingLow { get { return GetAdvRateComponent(SkillName.Lumberjacking, 0); } set { SetAdvRateComponent(SkillName.Lumberjacking, 0, value); } }
		public double lumberjackingMid { get { return GetAdvRateComponent(SkillName.Lumberjacking, 1); } set { SetAdvRateComponent(SkillName.Lumberjacking, 1, value); } }
		public double lumberjackingHigh { get { return GetAdvRateComponent(SkillName.Lumberjacking, 2); } set { SetAdvRateComponent(SkillName.Lumberjacking, 2, value); } }

		public double miningLow { get { return GetAdvRateComponent(SkillName.Mining, 0); } set { SetAdvRateComponent(SkillName.Mining, 0, value); } }
		public double miningMid { get { return GetAdvRateComponent(SkillName.Mining, 1); } set { SetAdvRateComponent(SkillName.Mining, 1, value); } }
		public double miningHigh { get { return GetAdvRateComponent(SkillName.Mining, 2); } set { SetAdvRateComponent(SkillName.Mining, 2, value); } }

		public double tailoringLow { get { return GetAdvRateComponent(SkillName.Tailoring, 0); } set { SetAdvRateComponent(SkillName.Tailoring, 0, value); } }
		public double tailoringMid { get { return GetAdvRateComponent(SkillName.Tailoring, 1); } set { SetAdvRateComponent(SkillName.Tailoring, 1, value); } }
		public double tailoringHigh { get { return GetAdvRateComponent(SkillName.Tailoring, 2); } set { SetAdvRateComponent(SkillName.Tailoring, 2, value); } }

		public double tinkeringLow { get { return GetAdvRateComponent(SkillName.Tinkering, 0); } set { SetAdvRateComponent(SkillName.Tinkering, 0, value); } }
		public double tinkeringMid { get { return GetAdvRateComponent(SkillName.Tinkering, 1); } set { SetAdvRateComponent(SkillName.Tinkering, 1, value); } }
		public double tinkeringHigh { get { return GetAdvRateComponent(SkillName.Tinkering, 2); } set { SetAdvRateComponent(SkillName.Tinkering, 2, value); } }

		private static double GetAdvRateComponent(SkillName skill, int index) {
			return SkillDef.GetBySkillName(skill).AdvRate[index];
		}

		private static void SetAdvRateComponent(SkillName skill, int index, double value) {
			SkillDef def = SkillDef.GetBySkillName(skill);
			double[] rate = (double[]) def.AdvRate.Clone();
			rate[index] = value;
			def.AdvRate = rate;
		}
	}
}
