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
using System.Reflection;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	public abstract class CompiledTargetDef : AbstractTargetDef {
		private bool allowGround;

		public CompiledTargetDef()
			: base(null, "Target.cs", -1) {

			Type type = this.GetType();
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo mi in methods) {
				if (mi.Name.Equals("On_TargonGround")) {
					this.allowGround = true;
					break;
				}
				if (mi.Name.Equals("On_TargonPoint")) {
					this.allowGround = true;
					break;
				}
			}
		}

		protected sealed override bool AllowGround {
			get {
				return this.allowGround;
			}
		}

		protected override string InternalFirstGetDefname() {
			return this.GetType().Name;
		}

		protected sealed override void On_Targon(GameState state, IPoint3D getback, object parameter) {
			Player self = state.Character as Player;
			if (self != null) {
				if (TargetResult.RestartTargetting == this.On_TargonPoint(self, getback, parameter)) {
					this.On_Start(self, parameter);
				}
			}
		}

		protected sealed override void On_TargonCancel(GameState state, object parameter) {
			Player self = state.Character as Player;
			if (self != null) {
				this.On_TargonCancel(self, parameter);
			}
		}

		protected virtual void On_TargonCancel(Player self, object parameter) {
		}

		protected virtual TargetResult On_TargonPoint(Player self, IPoint3D targetted, object parameter) {
			Thing thing = targetted as Thing;
			if (thing != null) {
				return this.On_TargonThing(self, thing, parameter);
			}
			AbstractInternalItem s = targetted as AbstractInternalItem;
			if (s != null) {
				return this.On_TargonStatic(self, s, parameter);
			}
			return this.On_TargonGround(self, targetted, parameter);
		}

		protected virtual TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Character ch = targetted as Character;
			if (ch != null) {
				return this.On_TargonChar(self, ch, parameter);
			}
			Item item = targetted as Item;
			if (item != null) {
				return this.On_TargonItem(self, item, parameter);
			}
			return TargetResult.RestartTargetting;//item nor char? huh?
		}

		protected virtual TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.ClilocSysMessage(1046439);//That is not a valid target.
			return TargetResult.RestartTargetting;
		}

		protected virtual TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.ClilocSysMessage(1046439);//That is not a valid target.
			return TargetResult.RestartTargetting;
		}

		protected virtual TargetResult On_TargonStatic(Player self, AbstractInternalItem targetted, object parameter) {
			return this.On_TargonGround(self, targetted, parameter);
		}

		protected virtual TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.ClilocSysMessage(1046439);//That is not a valid target.
			return TargetResult.RestartTargetting;
		}
	}

	public enum TargetResult {
		RestartTargetting = 1,
		Done = 0
	}
}