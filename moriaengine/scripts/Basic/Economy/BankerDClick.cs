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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #509 - double-click a banker NPC to talk to it (#883: Erebor's dialog, whose first button
	/// opens the bank box; the "bank" speech keyword still opens it directly), with the same
	/// BankerRange.MaxDistance=6 check as BankerSpeech.cs. A plain TriggerGroup
	/// (not AbstractSpeech, which is sealed to the hear trigger) hooking the now-cancellable
	/// TriggerKey.dClick added to Thing.Trigger_DClick for exactly this purpose - returning
	/// TriggerResult.Cancel here suppresses the banker's otherwise-default paperdoll/mount
	/// On_DClick behavior. Registered on C_H_BANKER/C_H_BANKER_F via TEVENTS in vendors.def,
	/// same convention as TSPEECH=spk_jobbanker right above it.
	/// </summary>
	public sealed class BankerDClick : TriggerGroup {
		public new static void Bootstrap() {
			new BankerDClick("e_banker").Register();
		}

		private BankerDClick(string defname)
			: base(defname) {
		}

		public override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			return this.TryRun(self, tk, sa);
		}

		public override object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			if (TriggerKey.dClick.Uid != tk.Uid) {
				return TriggerResult.Continue;
			}

			Character banker = self as Character;
			Character dclicker = sa.Argv[0] as Character;
			if ((banker == null) || (dclicker == null)) {
				return TriggerResult.Continue;
			}

			//root-caused (2026-10-26): this TriggerGroup was never the problem - the
			//TEVENTS=e_banker line that attaches it to C_H_BANKER/C_H_BANKER_F was silently
			//dropped by a full scripts/converted/ regen (PR #597) and never re-added, so this
			//class simply never ran at all. Restored as a hand-patch directly in vendors.def -
			//see that file's own comment.
			if (BankerRange.IsWithinRange(banker, dclicker)) {
				//Thing.Trigger_DClick calls TryCancellableTrigger with no surrounding
				//try/catch of its own (unlike the On_DClick fallback right below it) - catch
				//here directly, same convention as every other On_*Click override in this
				//codebase.
				try {
					Player player = dclicker as Player;
					if (player != null) {
						BankerDialog.Open(banker, player);
					} else {
						dclicker.Bank.OpenBankTo(dclicker);
					}
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(e);
				}
				return TriggerResult.Cancel;
			}
			// Out of range: unlike the "bank" speech keyword (which gets explicit feedback -
			// speech already implies the banker heard something), double-click has no inherent
			// range gate for ordinary NPCs in UO (you can dclick a distant character to view
			// their paperdoll) - fall through to that normal default instead of silently eating
			// the click or adding an unrequested distance error here.
			return TriggerResult.Continue;
		}
	}
}
