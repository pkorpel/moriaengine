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
			((Character) target).Bank.OpenTo(Globals.SrcCharacter);
		}

		/// <summary>#1114 - whether the player is in jail.</summary>
		[GetMethod("Jailed", typeof(bool))]
		public static object Jailed(object target) {
			return Jail.IsJailed((Character) target);
		}

		/// <summary>#1114 - whether the player is in the labour camp.</summary>
		[GetMethod("InLabourCamp", typeof(bool))]
		public static object InLabourCamp(object target) {
			return Camp.IsInCamp((Character) target);
		}

		/// <summary>#1114 - .jail or .forgive, whichever applies.</summary>
		[Button("Jail / Forgive")]
		public static void JailOrForgive(object target) {
			Character who = (Character) target;
			Character gm = Globals.SrcCharacter as Character;
			if (Jail.IsJailed(who)) {
				Jail.Forgive(who, gm);
			} else if (Jail.Send(who)) {
				if (gm != null) {
					gm.SysMessage(string.Format(Loc<JailLoc>.Get(gm.Language).YouJailed, who.Name));
				}
			} else if (gm != null) {
				gm.SysMessage(Loc<JailLoc>.Get(gm.Language).OnlyPlayers);
			}
		}

		/// <summary>#1114 - .lagrmenu for this player.</summary>
		[Button("Labour camp")]
		public static void LabourCamp(object target) {
			Character who = (Character) target;
			if ((Globals.SrcCharacter is Player gm) && (who.Account != null)) {
				CampCommands.Carry(gm, who, CampCommands.Order.Menu);
			}
		}

		[GetMethod("Edit", typeof(ContainerView.ThingAsContainer))]
		public static object Edit(object target) {
			return new ContainerView.ThingAsContainer((Thing) target);
		}
	}
}