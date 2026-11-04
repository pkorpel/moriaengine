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
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_inexhaustible (13 monsters - "dragons are fools", ordered by a GM). Archive:
	/// +3 stamina on every step it takes.</summary>
	[ViewableClass]
	public class InexhaustibleAbilityDef : PassiveAbilityDef {
		private FieldValue stamPerStep;

		public InexhaustibleAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.stamPerStep = this.InitTypedField("inexhaustibleStamPerStep", 3, typeof(int));
		}

		/// <summary>Stamina regained per step.</summary>
		public int StamPerStep {
			get { return (int) this.stamPerStep.CurrentValue; }
			set { this.stamPerStep.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static InexhaustibleAbilityDef inexhaustibleAbilityDef;
		internal static InexhaustibleAbilityDef InexhaustibleDef {
			get {
				if (inexhaustibleAbilityDef == null) {
					inexhaustibleAbilityDef = AbstractScript.GetByDefname("a_tevent_e_inexhaustible") as InexhaustibleAbilityDef;
				}
				return inexhaustibleAbilityDef;
			}
		}

		/// <summary>Character.On_StepTaken - every step a character really takes: hazard tiles
		/// it walks onto, and e_inexhaustible's stamina.</summary>
		internal static void OnStepTaken(Character stepping) {
			CheckHazardTileStep(stepping);
			CheckDragonTileStep(stepping); //#1022
			DragonBlood.CheckStep(stepping);
			CheckFrostStep(stepping); //#1022 e_frosty_wave
			CheckEggStep(stepping); //#1022 e_drop_egg
			TryApplyInexhaustible(stepping);
		}

		/// <summary>#797 e_inexhaustible - returns the stamina gained.</summary>
		public static int TryApplyInexhaustible(Character stepping) {
			InexhaustibleAbilityDef def = InexhaustibleDef;
			if (stepping.IsDeleted || !HasAbilityDef(stepping, def)) {
				return 0;
			}
			int gained = Math.Max(0, Math.Min(def.StamPerStep, stepping.MaxStam - stepping.Stam));
			stepping.Stam = (short) (stepping.Stam + gained);
			return gained;
		}
	}
}
