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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	public partial class RecallRune {

		public string TargetDescription {
			get {
				return this.targetDescription;
			}
			set {
				this.targetDescription = value;
			}
		}

		public Point4D Target {
			get {
				return this.target;
			}
			set {
				this.target = value;
				if (value == null) {
					this.targetDescription = null;
				} else {
					Region targetRegion = value.GetMap().GetRegionFor(value);
					this.targetDescription = targetRegion.Name;
				}
				this.InvalidateAosToolTips();
			}
		}

		public override Point4D MoreP {
			get {
				return this.Target;
			}
			set {
				this.Target = value;
			}
		}

		public override void On_BuildAosToolTips(AosToolTips opc, Language language) {
			RecallRuneLoc loc = Loc<RecallRuneLoc>.Get(language);
			if (string.IsNullOrEmpty(this.targetDescription)) {
				opc.AddNameColonValue(loc.target, loc.anUnknownDestination);
			} else {
				opc.AddNameColonValue(loc.target, this.targetDescription);
			}
		}

		public static bool CheckTelePermissionOut(Character caster) {
			FlaggedRegion region = caster.Region as FlaggedRegion;			
			RegionFlags regionFlags = region.Flags;

			if ((regionFlags & RegionFlags.NoTeleportingOut) == RegionFlags.NoTeleportingOut) {
				caster.RedMessage(Loc<RecallRuneLoc>.Get(caster.Language).ForbiddenTeleportingOut);
				return false;
			}
			//TODO enemyteleporting - check realm friendliness
			return true;
		}

		public static bool CheckTelePermissionIn(Character caster, IPoint4D destination) {
			destination = destination.TopPoint;
			FlaggedRegion region = destination.GetMap().GetRegionFor(destination) as FlaggedRegion;
			RegionFlags regionFlags = region.Flags;

			if ((regionFlags & RegionFlags.NoTeleportingIn) == RegionFlags.NoTeleportingIn) {
				caster.RedMessage(Loc<RecallRuneLoc>.Get(caster.Language).ForbiddenTeleportingIn);
				return false;
			}
			//TODO enemyteleporting - check realm friendliness
			return true;
		}
	}

	[ViewableClass]
	public partial class RecallRuneDef {
	}

	public class RecallRuneLoc : CompiledLocStringCollection<RecallRuneLoc> {
		public string anUnknownDestination = "an unknown destination";
		public string target = "Target";

		internal string ForbiddenTeleportingIn = "Zde je zakázáno kouzlit teleportovací kouzla";
		internal string ForbiddenTeleportingOut = "Odtud je zakázáno kouzlit teleportovací kouzla";
		internal string ForbiddenEnemyTeleportingIn = "Zde je nepøátelùm zakázáno kouzlit teleportovací kouzla";
		internal string ForbiddenEnemyTeleportingOut = "Odtud je nepøátelùm zakázáno kouzlit teleportovací kouzla";		
	}
}