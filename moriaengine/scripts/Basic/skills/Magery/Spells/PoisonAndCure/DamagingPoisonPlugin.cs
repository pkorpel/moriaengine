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
	public partial class DamagingPoisonEffectPlugin {

		public override void On_FadingEffectTick() {
			Character self = this.Cont as Character;
			if (self != null) {

				double damage = DamageManager.CauseDamage(this.SourceThing as Character, self,
					DamageType.MagicPoison, this.EffectPower);

				if (damage < 1) { //this is how we check immunity - in the first tick
					this.Delete();
					return;
				}
			} else {
				this.Delete();
				return;
			}

			//0-4 are valid messages
			this.AnnouncePoisonStrength((PoisonStrengthMessage) (this.EffectPower / 2));
		}

		//the names aren't really important, the relevant part here is that it's 0-4
		public enum PoisonStrengthMessage {
			Zero = 0, Weakest = 0, Nauseous = 0, Ill = 0,
			One = 1, Weak = 1, DisorientedAndNauseous = 1, ExtremelyIll = 1,
			Two = 2, Normal = 2, FeelingPain = 2, StumblesAround = 2,
			Three = 3, Strong = 3, ExtremelyWeak = 3, WrackedWithPain = 3,
			Four = 4, Strongest = 4, ExtremePain = 4, SpasmingUncontrollably = 4
		}

		public void AnnouncePoisonStrength(PoisonStrengthMessage strength) {
			Character self = this.Cont as Character;
			if (self != null) {
				int roundedEffect = (int) strength;
				if (roundedEffect < 0) {
					roundedEffect = 0;
				} else if (roundedEffect > 4) {
					roundedEffect = 4;
				}

				self.ClilocEmote(1042858 + (roundedEffect * 2), 0x21, self.Name); //shouldn't see one's own cliloc emote?
				self.ClilocSysMessage(1042857 + (roundedEffect * 2), 0x21);
			}
		}
	}
}


