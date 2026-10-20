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

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(Character), "Character")]
	public static class CharacterDescriptor {
		/*We will add here a ressurect, kill, dismount etc...
		 */

		[Button("Dismount")]
		public static void Dismount(object target) {
			((Character) target).Dismount();
		}

		[Button("Disarm")]
		public static void Disarm(object target) {
			((Character) target).DisArm();
		}

		[Button("Kill")]
		public static void Kill(object target) {
			((Character) target).InstaKill();
		}

		[Button("Resurrect")]
		public static void Resurrect(object target) {
			((Character) target).Resurrect();
		}

		[Button("Bank")]
		public static void Bank(object target) {
			// TEMPORARY diagnostic logging (#509 live-testing - clicking this did nothing for
			// the user). Remove once root-caused.
			Logger.WriteWarning("[BANKDEBUG] CharacterDescriptor.Bank: target=" + target + " Globals.SrcCharacter=" + Globals.SrcCharacter);
			try {
				((Character) target).Bank.OpenTo(Globals.SrcCharacter);
				Logger.WriteWarning("[BANKDEBUG] Bank.OpenTo succeeded.");
			} catch (Exception e) {
				Logger.WriteError("[BANKDEBUG] Bank.OpenTo threw: " + e);
			}
		}

		[GetMethod("Edit", typeof(ContainerView.ThingAsContainer))]
		public static object Edit(object target) {
			return new ContainerView.ThingAsContainer((Thing) target);
		}
	}
}