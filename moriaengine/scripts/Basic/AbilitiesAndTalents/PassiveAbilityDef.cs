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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Special abiliy class used for implementing abilities that will automatically perform their action when the 
	/// first point is assigned to them (e.g. Regenerations...). These actions can be performed using the included TriggerGroup or 
	/// Plugin that will be attached/detached when the first point is added to the ability (last point is removed from the ability) 
	/// We make this class to be child of ActivableAbilityDef because we want to have the possibility to swithc (even the passive) 
	/// ability off - e.g. hypothetical ability 'Healing nearby comrades' might be switched of in some hardcore dungeon where healing is not allowed 
	/// etc.
	/// </summary>
	[ViewableClass]
	public class PassiveAbilityDef : ActivableAbilityDef {
		public PassiveAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>
		/// This method implements the assigning of the first point to the Ability
		/// Just call the activate method from parent (this will ensure assigning all TGs and Plugins
		/// </summary>
		protected override void On_Assign(Character ch, Ability ab) {
			this.Activate(ch); //activate the ability automatically
		}

		//protected override void On_ValueChanged(Character ch, Ability ab, int previousValue) {
		//    base.On_ValueChanged(ch, ab, previousValue);

		//}
	}
}
