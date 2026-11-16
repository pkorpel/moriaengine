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
using SteamEngine.Generators.Abstractions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	[ThingDefTemplate]
	public partial class WearableDef : DestroyableDef {
		[DefField(typeof(int), DefaultExpression = "0")]
		private FieldValue armorVsP;

		[DefField(typeof(int), DefaultExpression = "0")]
		private FieldValue mindDefenseVsP;

		[DefField(typeof(int), DefaultExpression = "0")]
		private FieldValue armorVsM;

		[DefField(typeof(int), DefaultExpression = "0")]
		private FieldValue mindDefenseVsM;

		[DefField(typeof(WearableType), DefaultExpression = "WearableType.Clothing")]
		private FieldValue wearableType;

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "armor":
				case "minddefense":
					base.LoadScriptLine(filename, line, param + "vsp", args);
					base.LoadScriptLine(filename, line, param + "vsm", args);
					return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}
	}

	[ViewableClass]
	[ThingTemplate]
	public partial class Wearable : Destroyable {

		public int ArmorVsP {
			get {
				return (int) ((this.TypeDef.ArmorVsP * (double) this.Durability) / this.MaxDurability);
			}
		}

		public int MindDefenseVsP {
			get {
				return (int) ((this.TypeDef.MindDefenseVsP * (double) this.Durability) / this.MaxDurability);
			}
		}

		public int ArmorVsM {
			get {
				return (int) ((this.TypeDef.ArmorVsM * (double) this.Durability) / this.MaxDurability);
			}
		}

		public int MindDefenseVsM {
			get {
				return (int) ((this.TypeDef.MindDefenseVsM * (double) this.Durability) / this.MaxDurability);
			}
		}
	}
}