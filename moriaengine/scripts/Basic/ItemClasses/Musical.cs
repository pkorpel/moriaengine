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
	public partial class MusicalDef {
	}

	[ViewableClass]
	public partial class Musical : Item {
		public override void On_DClick(AbstractCharacter ac) {
			Character clicker = ac as Character;
			clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, SkillName.Musicianship, this));//select Musicianship
		}

		public void SuccessSnd() {
			this.Sound(this.TypeDef.SuccessSound);
		}

		public void FailureSnd() {
			this.Sound(this.TypeDef.FailureSound);
		}

		public override bool IsMusicalInstrument {
			get {
				return true;
			}
		}
	}
}


//500617 - What instrument shall you play?
//1062488 - The instrument you are trying to play is no longer in your backpack!

//1049541 - Choose the target for your song of discordance.
//1049535 - A song of discord would have no effect on that.
//500612 - You play poorly, and there is no effect.
//1049539 - You play the song surpressing your targets strength
//1049537 - Your target is already in discord.
//1049540 - You fail to disrupt your target
//1049535 - A song of discord would have no effect on that.
