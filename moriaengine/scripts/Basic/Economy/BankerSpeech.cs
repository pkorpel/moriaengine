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
using System.Text.RegularExpressions;
using SteamEngine.Common;

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
		public new static void Bootstrap() {
			new BankerSpeech("spk_jobbanker").Register();
			new BankerOkSpeech("spk_banker_ok").Register();
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

			Character banker = listener as Character;
			if (banker == null) {
				return SpeechResult.IgnoredOrActedUpon;
			}

			//Sphere's native money words, not in the archive's speech: a number makes it a deposit
			if (Regex.IsMatch(msg, "deposit.*[0-9]", RegexOptions.IgnoreCase)) {
				HandleDeposit(speaker, msg);
				return SpeechResult.ActedUponExclusively;
			}
			if (msg.IndexOf("withdraw", StringComparison.OrdinalIgnoreCase) >= 0) {
				HandleWithdraw(speaker, msg);
				return SpeechResult.ActedUponExclusively;
			}
			return HandleErebor(banker, speaker, msg) ? SpeechResult.ActedUponExclusively : SpeechResult.IgnoredOrActedUpon;
		}

		/// <summary>[SPEECH spk_jobbanker] in its order, first match wins. The archive's later
		/// ACCOUNT, COIN, COPPER, CURRENCY, Bank and HOLD UP blocks never run, an earlier *x* takes
		/// them. COMPLEXITY isn't a property on 0.99z8 (none in EreborNext), so the ELSE lines run.</summary>
		private static bool HandleErebor(Character banker, Character speaker, string msg) {
			string milord = speaker.IsFemale ? "milady" : "milord";
			if (Matches(msg, "*sek*")) {
				BankerDialog.CashOneCheck(banker, speaker);
			} else if (Matches(msg, "*job*", "*work*", "*what*do*do*", "*profession*", "*occupation*")) {
				SayOneOf(banker, 3, "Zde je banka, " + milord + ".", "Radi Vam zde " + RegionName(banker) + ", " + milord + " poskytneme nase sluzby.");
			} else if (Matches(msg, "*Balance*")) {
				banker.Say("Na vasich depozitnich uctech je " + GoldUtils.CountGoldIn(speaker.Bank) + " zlatych.");
			} else if (Matches(msg, "*Bank*")) {
				// #509 - kept to its own tighter BankerRange.MaxDistance (6) rather than whatever
				// Globals.SpeechDistance happens to be configured to - same range enforced by
				// BankerDClick, so both ways behave consistently regardless of that global setting.
				if (BankerRange.IsWithinRange(speaker, banker)) {
					speaker.Bank.OpenBankTo(speaker);
				} else {
					speaker.SysMessage("Thou must stand closer to the banker to do that.");
				}
			} else if (Matches(msg, "hi", "hello*", "greetings*", "good*see*thee*", "Hail*", "Greet")) {
				banker.Say("Zdravim Vas, nase penezni sluzby Vam jsou k dispozici?");
			} else if (Matches(msg, "*rob*", "*steal*", "*hold up*")) {
				SayOneOf(banker, 4, "Straze, pomoooc bankovni prepadeni.");
			} else if (Matches(msg, "*coin*", "*money*", "*currency*", "*deposit*", "*transaction*", "*account*")) {
				SayOneOf(banker, 4, "Jiste radi vam uschovame bezpecne vase cennosti, ci provedeme financni transakce.");
			} else if (Matches(msg, "*gold*", "*silver*", "*copper*")) {
				SayOneOf(banker, 3, "Penize, si u nas muzete kdykoli uschovat i vyzvednout, i jine cennosti");
			} else if (Matches(msg, "Bye", "Goodbye", "Farewell")) {
				banker.Say("Dekujeme ze jste vyuzily nasich sluzeb ci se k tomu brzy odhodlate.");
			} else {
				return false;
			}
			return true;
		}

		private static bool Matches(string msg, params string[] patterns) {
			foreach (string pattern in patterns) {
				if (SpeechStub.GetRegex(pattern).IsMatch(msg)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>DORAND n: one roll of n, and rolling past the lines says nothing.</summary>
		private static void SayOneOf(Character banker, int rolls, params string[] lines) {
			int roll = Globals.dice.Next(rolls);
			if (roll < lines.Length) {
				banker.Say(lines[roll]);
			}
		}

		private static string RegionName(Character banker) {
			return (banker.Region != null) ? banker.Region.Name : "";
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

	/// <summary>#883 - [SPEECH spk_banker_ok]: the realm bankers (c_banker_g, c_banker_m) only
	/// talk through their dialog.</summary>
	public sealed class BankerOkSpeech : AbstractSpeech {
		private const int MaxDistance = 5;
		private static readonly string[] patterns = { "*bank*", "*sek*", "*hi*", "*zdravym*", "*ahoj*", "*dobry*" };

		internal BankerOkSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			Player speaker = speechArgs.Speaker as Player;
			Character banker = listener as Character;
			string msg = speechArgs.Speech;
			if ((speaker == null) || (banker == null) || string.IsNullOrEmpty(msg)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			foreach (string pattern in patterns) {
				if (SpeechStub.GetRegex(pattern).IsMatch(msg)) {
					if (Point2D.GetSimpleDistance(banker, speaker) > MaxDistance) {
						speaker.SysMessage("Na takovou vzdalenost s tebou jednat nebude");
					} else {
						BankerDialog.Open(banker, speaker);
					}
					return SpeechResult.ActedUponExclusively;
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
