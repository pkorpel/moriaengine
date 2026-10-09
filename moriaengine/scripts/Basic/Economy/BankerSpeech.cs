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
	/// <summary>
	/// Banker NPC speech recognition (enhancements Phase 14.5, issue #36). Banker NPCs
	/// (scripts/converted/chars/vendors.def's C_H_BANKER/C_H_BANKER_F) had their speech trigger
	/// commented out by the old converter with nothing left to restore (the referenced
	/// jobbanker.def speech block was stripped to an empty header) - this is new, not restored.
	///
	/// Written as a direct AbstractSpeech subclass (mirrors ScriptedSpeech's own shape -
	/// Bootstrap() + Register(), same convention PartyCommands/SecureTradeCommandsImpl already
	/// use elsewhere) rather than as an LScript [SPEECH] block - there's no existing LScript
	/// speech example anywhere in this codebase to confirm syntax against, and simple keyword
	/// matching doesn't need LScript's pattern-trigger machinery anyway.
	/// </summary>
	public sealed class BankerSpeech : AbstractSpeech {
		public static void Bootstrap() {
			new BankerSpeech("spk_jobbanker").Register();
		}

		private BankerSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			Character speaker = speechArgs.Speaker as Character;
			string msg = speechArgs.Speech;
			if (speaker == null || string.IsNullOrEmpty(msg)) {
				return SpeechResult.IgnoredOrActedUpon;
			}

			if (msg.IndexOf("balance", StringComparison.OrdinalIgnoreCase) >= 0) {
				ReportBalance(speaker);
				return SpeechResult.ActedUponExclusively;
			}
			if (msg.IndexOf("deposit", StringComparison.OrdinalIgnoreCase) >= 0) {
				HandleDeposit(speaker, msg);
				return SpeechResult.ActedUponExclusively;
			}
			if (msg.IndexOf("withdraw", StringComparison.OrdinalIgnoreCase) >= 0) {
				HandleWithdraw(speaker, msg);
				return SpeechResult.ActedUponExclusively;
			}

			return SpeechResult.IgnoredOrActedUpon;
		}

		private static void ReportBalance(Character speaker) {
			long balance = GoldUtils.CountGoldIn(speaker.Bank);
			speaker.SysMessage("Thy bank balance is " + balance + " gold.");
		}

		private static void HandleDeposit(Character speaker, string msg) {
			uint amount;
			if (!TryExtractAmount(msg, out amount)) {
				speaker.SysMessage("How much gold wouldst thou like to deposit? (say e.g. 'deposit 100')");
				return;
			}
			if (GoldUtils.TryDeposit(speaker, amount)) {
				speaker.SysMessage("Thou hast deposited " + amount + " gold.");
			} else {
				speaker.SysMessage("Thou dost not have that much gold on hand.");
			}
		}

		private static void HandleWithdraw(Character speaker, string msg) {
			uint amount;
			if (!TryExtractAmount(msg, out amount)) {
				speaker.SysMessage("How much gold wouldst thou like to withdraw? (say e.g. 'withdraw 100')");
				return;
			}
			if (GoldUtils.TryWithdraw(speaker, amount)) {
				speaker.SysMessage("Thou hast withdrawn " + amount + " gold.");
			} else {
				speaker.SysMessage("Thou dost not have that much gold in thy account.");
			}
		}

		/// <summary>Pulls the first run of digits out of the spoken line (e.g. "withdraw 500" -> 500).</summary>
		private static bool TryExtractAmount(string msg, out uint amount) {
			int start = -1;
			int end = -1;
			for (int i = 0; i < msg.Length; i++) {
				if (char.IsDigit(msg[i])) {
					if (start < 0) {
						start = i;
					}
					end = i;
				} else if (start >= 0) {
					break;
				}
			}

			if (start < 0) {
				amount = 0;
				return false;
			}

			return uint.TryParse(msg.Substring(start, end - start + 1), out amount) && amount > 0;
		}
	}
}
