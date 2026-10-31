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

namespace SteamEngine.CompiledScripts {

	/// <summary>#827 - Erebor's [speech spk_healer] (systems/npc_systems.scp), on 18 healer chardefs: on=*res* - a realm
	/// outcast is told they're cursed by the gods; a dead speaker the healer sees is resurrected. In practice the
	/// second half only ever reaches a GM: a healer can't hear a ghost (Erebor: a vendor-brain NPC doesn't understand
	/// ghost speech; here an NPC doesn't see a ghost), so ghosts are resurrected by clicking the healer (e_healer,
	/// MonsterSpecialAttacks.TryHealerClick). Replaces the converted [SpeechStub spk_healer].</summary>
	public sealed class HealerSpeech : AbstractSpeech {
		public new static void Bootstrap() {
			new HealerSpeech("spk_healer").Register();
		}

		private HealerSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC healer = listener as NPC;
			Character speaker = speechArgs.Speaker as Character;
			string speech = speechArgs.Speech;
			if ((healer == null) || (speaker == null) || (speech == null) || (speech.IndexOf("res", StringComparison.OrdinalIgnoreCase) < 0)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			if (Healer.IsCursed(speaker)) {
				Healer.SayCursed(speaker);
			} else if (speaker.Flag_Dead && healer.CanSeeLOS(speaker).Allow) {
				Healer.Resurrect(healer, speaker);
			}
			return SpeechResult.IgnoredOrActedUpon; //other NPCs still hear it, as each Sphere NPC hears on its own
		}
	}
}
