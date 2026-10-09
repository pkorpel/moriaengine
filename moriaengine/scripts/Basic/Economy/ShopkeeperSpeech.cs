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
	/// Basic NPC vendor speech recognition (enhancements Phase 14.5, issue #36). There is no
	/// NPC merchant purchase flow anywhere in this codebase to extend - only the player-vendor
	/// stub (#25, fixed separately) existed. "Buy" opens a shop list gump
	/// (Dialogs.D_VendorShop); "sell" starts a target so the player picks an item from their own
	/// backpack to sell back.
	///
	/// Same AbstractSpeech-subclass shape as BankerSpeech, for the same reason: no existing
	/// LScript [SPEECH] example to confirm syntax against, and simple keyword matching doesn't
	/// need LScript's regex-trigger machinery.
	/// </summary>
	public sealed class ShopkeeperSpeech : AbstractSpeech {
		public static void Bootstrap() {
			new ShopkeeperSpeech("spk_shopkeep").Register();
		}

		private ShopkeeperSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			Player speaker = speechArgs.Speaker as Player;
			string msg = speechArgs.Speech;
			if (speaker == null || string.IsNullOrEmpty(msg)) {
				return SpeechResult.IgnoredOrActedUpon;
			}

			if (msg.IndexOf("buy", StringComparison.OrdinalIgnoreCase) >= 0) {
				speaker.Dialog(SingletonScript<Dialogs.D_VendorShop>.Instance);
				return SpeechResult.ActedUponExclusively;
			}
			if (msg.IndexOf("sell", StringComparison.OrdinalIgnoreCase) >= 0) {
				speaker.SysMessage("What wouldst thou sell me?");
				speaker.Target(SingletonScript<Targ_SellToVendor>.Instance);
				return SpeechResult.ActedUponExclusively;
			}

			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>Targeting def for the "sell" speech command - validates and completes the sale of whatever item the player points at.</summary>
	public class Targ_SellToVendor : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (item == null) {
				self.SysMessage("I do not buy such things.");
				return TargetResult.Done;
			}

			NpcVendorStock.Entry entry = NpcVendorStock.FindByDef(item.Def);
			if (entry == null) {
				self.SysMessage("I do not buy that here.");
				return TargetResult.Done;
			}

			if (item.Cont != self.Backpack) {
				self.SysMessage("That is not thine to sell.");
				return TargetResult.Done;
			}

			long amount = item.Amount;
			item.Delete();
			GoldUtils.Grant(self, entry.SellPrice * (uint) amount);
			self.SysMessage("Sold for " + (entry.SellPrice * amount) + " gold.");
			return TargetResult.Done;
		}
	}
}
