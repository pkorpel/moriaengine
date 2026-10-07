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
	public class StatDrainingAbilityDef : ActivableAbilityDef {

		private FieldValue hitsDrain;
		private FieldValue stamDrain;
		private FieldValue manaDrain;

		public StatDrainingAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.hitsDrain = this.InitTypedField("hitsDrain", 0, typeof(double));
			this.stamDrain = this.InitTypedField("stamDrain", 0, typeof(double));
			this.manaDrain = this.InitTypedField("manaDrain", 0, typeof(double));
		}

		public double HitsDrain {
			get {
				return (double) this.hitsDrain.CurrentValue;
			}
			set {
				this.hitsDrain.CurrentValue = value;
			}
		}

		public double StamDrain {
			get {
				return (double) this.stamDrain.CurrentValue;
			}
			set {
				this.stamDrain.CurrentValue = value;
			}
		}

		public double ManaDrain {
			get {
				return (double) this.manaDrain.CurrentValue;
			}
			set {
				this.manaDrain.CurrentValue = value;
			}
		}

		protected override void On_Activate(Character ch, Ability ab) {
			base.On_Activate(ch, ab);

			StatDrainingEffectDurationPlugin plugin = ch.GetPlugin(this.PluginKey) as StatDrainingEffectDurationPlugin;
			if (plugin != null) {
				plugin.InitDrain(this.HitsDrain, this.StamDrain, this.ManaDrain);
			}
		}
	}
}
