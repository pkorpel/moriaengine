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

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_bleed given real
	/// behavior. Same shape as the player-facing BleedingEffectPlugin (landed-hit DoT, stacks
	/// onto itself up to a cap, fades out over its duration, auto-removes once it deals 0
	/// damage - same real archive intent: a monster bite that stops being worth tracking once
	/// fully resisted), but a deliberately SEPARATE def/plugin pair (not sharing
	/// BleedingEffectPluginDef/p_bleeding_effect) so monster-bite bleed tunes independently
	/// from the thief "Bleeding Strike" ability - see NpcBleedEffect.ct's own doc comment.
	/// Resist is handled automatically - DamageType.PhysicalBleed already flows through
	/// DamageManager.GetResistModifier's existing ResistBleed handling, same as the player
	/// mechanism, no extra work needed here.
	/// </summary>
	[ViewableClass]
	public partial class NpcBleedEffectPlugin {
		public override void On_Assign() {
			Character self = (Character) this.Cont;
			self.RedMessage(Loc<BleedingLoc>.Get(self.Language).YoureBleeding);
			base.On_Assign();
		}

		public override void On_FadingEffectTick() {
			Character self = this.Cont as Character;
			if (self == null) {
				this.Delete();
				return;
			}
			double damage = DamageManager.CauseDamage(this.SourceThing as Character, self,
				DamageType.PhysicalBleed, this.EffectPower);
			if (damage < 1) {
				this.Delete();
			}
		}
	}

	[ViewableClass]
	public partial class NpcBleedEffectPluginDef {
	}
}
