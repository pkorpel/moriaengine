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

	[ViewableClass]
	public partial class StatDrainingEffectDurationPlugin {

		public void InitDrain(double hitsDrain, double stamDrain, double manaDrain) {
			this.manaDrain = manaDrain;
			this.stamDrain = stamDrain;
			this.manaDrain = manaDrain;

			if (this.Cont != null) {
				this.ApplyDrains();
			}
		}

		public double HitsDrain {
			get {
				return this.hitsDrain;
			}
		}

		public double StamDrain {
			get {
				return this.stamDrain;
			}
		}

		public double ManaDrain {
			get {
				return this.manaDrain;
			}
		}

		public virtual void On_Assign() {
			this.ApplyDrains();
		}

		private void ApplyDrains() {
			Character ch = (Character) this.Cont;
			if (ch != null) {
				ch.HitsRegenSpeed -= this.hitsDrain;
				ch.StamRegenSpeed -= this.stamDrain;
				ch.ManaRegenSpeed -= this.manaDrain;
			}
		}

		public override void On_UnAssign(Character cont) {
			if (cont != null) {
				cont.HitsRegenSpeed += this.hitsDrain;
				cont.StamRegenSpeed += this.stamDrain;
				cont.ManaRegenSpeed += this.manaDrain;
			}
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class StatDrainingEffectDurationPluginDef {
	}
}