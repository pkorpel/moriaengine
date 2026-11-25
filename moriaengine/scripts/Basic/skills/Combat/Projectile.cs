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

	[ViewableClass]
	public partial class ProjectileDef {

	}

	[ViewableClass]
	public partial class Projectile : IPoisonableItem {

		public ProjectileType ProjectileType {
			get {
				//TODO modify for jagged/poisoned/whatever
				return this.TypeDef.ProjectileType;
			}
		}

		public double Piercing {
			get {
				return this.TypeDef.Piercing;
			}
		}

		private static TriggerKey coupledWithWeaponTK = TriggerKey.Acquire("coupledWithWeapon");
		internal void Trigger_CoupledWithWeapon(Character self, Weapon weapon) {
			this.TryTrigger(coupledWithWeaponTK, new ScriptArgs(self, weapon));
			try {
				this.On_CoupledWithWeapon(self, weapon);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		//called when this is found at the given char and is going to be used with the given weapon as projectile
		//there's no "uncouple" trigger and it can be called at any time and any number of times, so implement accordingly
		protected virtual void On_CoupledWithWeapon(Character self, Weapon weapon) {
			
		}

		public int PoisoningDifficulty {
			get {
				return this.TypeDef.PoisoningDifficulty;
			}
		}

		public double PoisoningEfficiency {
			get {
				return this.TypeDef.PoisoningEfficiency;
			}
		}
	}
}