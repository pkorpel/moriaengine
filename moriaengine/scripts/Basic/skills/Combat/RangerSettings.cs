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

	/// <summary>One consolidated settings page for Ranger (#454 follow-up - requested directly
	/// rather than scattering fields across CombatSettings/CombatSettings.Ranges/elsewhere).
	/// Range fields are pass-through properties wrapping the EXISTING CombatSettings.Ranges.
	/// bow/crossbow/heavyCrossbow fields, not new storage - avoids duplicating data and avoids
	/// the Generated-code field-removal landmine (see CLAUDE.md) by not touching RangesSettings
	/// at all. Damage/speed are deliberately settings-only, NOT written into the 21 bow/
	/// crossbow .def content entries - the user is planning a full content rework later using
	/// newer Erebor def items, so editing that content now would just be thrown away.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class RangerSettings : SettingsMetaCategory {

		[SavedMember]
		public static RangerSettings instance = new RangerSettings();

		[LoadingInitializer]
		public RangerSettings() {
		}

		public int BowRange {
			get { return RangesSettings.instance.bow; }
			set { RangesSettings.instance.bow = value; }
		}

		public int CrossbowRange {
			get { return RangesSettings.instance.crossbow; }
			set { RangesSettings.instance.crossbow = value; }
		}

		public int HeavyCrossbowRange {
			get { return RangesSettings.instance.heavyCrossbow; }
			set { RangesSettings.instance.heavyCrossbow = value; }
		}

		/// <summary>#456 - confirmed real Erebor DAM ratio (bow weakest, heavy crossbow
		/// strongest - source-confirmed, not a guess), applied as a settings-only override of
		/// whatever the weapon's own (still converter-placeholder) AttackVsP/VsM content says -
		/// see CombatCalculator.CalculateCombatWeaponValues.</summary>
		[SaveableData]
		public double bowDamage = 5;

		[SaveableData]
		public double lightCrossbowDamage = 7;

		[SaveableData]
		public double heavyCrossbowDamage = 13;

		/// <summary>#456 - confirmed real Erebor ratio (bow/light crossbow equally fast, heavy
		/// crossbow ~10% slower - source values 58/58/52). The absolute scale is NOT a direct
		/// copy of Erebor's raw numbers - moriaengine's own weapon Speed field is a rate
		/// multiplier (higher = faster swings, see CombatCalculator's delay formula), the
		/// opposite shape from Erebor's own tick-count-like speed stat, so these values instead
		/// preserve Erebor's real ~52:58 ratio centered on this engine's existing melee-weapon
		/// baseline of 100.</summary>
		[SaveableData]
		public double bowSpeed = 100;

		[SaveableData]
		public double lightCrossbowSpeed = 100;

		[SaveableData]
		public double heavyCrossbowSpeed = 90;

		/// <summary>Confirmed real Erebor default (combat_storepos/restorepos) - a crossbow
		/// user gets snapped back to where they started firing from if they try to walk away;
		/// a bow user has no such restriction. Independent toggles so a GM can flip either one
		/// for testing/balance.</summary>
		[SaveableData]
		public bool bowMustStandStill = false;

		[SaveableData]
		public bool crossbowMustStandStill = true;

		/// <summary>#457 - explicit placeholder, NOT a real Erebor port. The real
		/// nastaveni_arrowdamage_N settings do have real, varied values in the Erebor archive,
		/// but the mapping from arrow material tier to index N isn't recoverable from the
		/// public archive (the base arrow item has no tdata1 set in what's available) - rather
		/// than guess a mapping and present it as faithful, this is a flat per-material-tier
		/// damage increment (e.g. 0.05 = +5% per tier above copper) applied via
		/// CombatCalculator - tune freely, this number carries no Erebor authority.</summary>
		[SaveableData]
		public double arrowDamageBonusPerTier = 0.05;
	}
}
