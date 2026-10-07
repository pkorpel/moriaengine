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

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class PoisonPotion {

		public override void On_DClick(AbstractCharacter dclicker) {
			SkillSequenceArgs args = SkillSequenceArgs.Acquire((Character) dclicker, SkillName.Poisoning, this);
			args.PhaseSelect();

			//base.On_DClick(dclicker); //empties flask
		}

		public FadingEffectDurationPluginDef PoisonType {
			get { return this.TypeDef.PoisonType; }
		}

		public int PoisonPower {
			get {
				int[] arr = this.TypeDef.PoisonPower;
				if (arr == null) {
					return 0;
				}
				switch (arr.Length) {
					case 0:
						return 0;
					case 1:
						return arr[0];
					case 2:
						return Globals.dice.Next(arr[0], arr[1]);
					default:
						Logger.WriteWarning("Poison potion " + this.TypeDef.PrettyDefname +
						                           " has > 2 numbers set as PoisonPower. Only 2 are supported, for randomization");
						goto case 2;
				}
			}
		}

		public int PoisonTickCount {
			get { return this.TypeDef.PoisonTickCount; }
		}
	}

	[ViewableClass]
	public partial class PoisonPotionDef {

	}
}