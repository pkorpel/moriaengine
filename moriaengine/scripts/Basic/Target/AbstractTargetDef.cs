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

using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	public abstract class AbstractTargetDef : AbstractDef {

		OnTargon targon;
		OnTargonCancel targonCancel;

		internal AbstractTargetDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.targon = this.On_Targon;
			this.targonCancel = this.On_TargonCancel;
		}

		public new static AbstractTargetDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbstractTargetDef;
		}

		internal void Assign(Player self, object parameter) {
			this.On_Start(self, parameter);
		}

		protected virtual void On_Start(Player self, object parameter) {
			GameState state = self.GameState;
			if (state != null) {
				state.Target(this.AllowGround, this.targon, this.targonCancel, parameter);
			}
		}

		protected abstract bool AllowGround { get; }

		protected abstract void On_Targon(GameState state, IPoint3D getback, object parameter);

		protected abstract void On_TargonCancel(GameState state, object parameter);
	}
}