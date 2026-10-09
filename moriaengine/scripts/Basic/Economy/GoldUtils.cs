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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Shared gold payment/grant logic (enhancements Phase 14.5, issue #36) - every purchase
	/// path (secure trade, player vendor, NPC vendor, banker withdraw/deposit) needs the same
	/// "find and consume/create real i_gold item stacks" mechanics, so it lives here once rather
	/// than being reimplemented per caller.
	///
	/// Mirrors the architecture the crafting resource-consumption system already uses
	/// (Resources/ItemCounter.cs: enumerate a container, sum matching Amounts, Consume() across
	/// stacks) rather than inventing a new approach - just scoped to the one itemdef (i_gold)
	/// instead of ItemCounter's generic multi-resource machinery, which this doesn't need.
	/// </summary>
	public static class GoldUtils {
		private static ThingDef goldDef;
		private static ThingDef GoldDef {
			get {
				if (goldDef == null) {
					goldDef = ThingDef.GetByDefname("i_gold");
					if (goldDef == null) {
						throw new SEException("Unable to find itemdef i_gold in scripts.");
					}
				}
				return goldDef;
			}
		}

		/// <summary>Total gold (across every matching stack) directly inside this one container - does not recurse into sub-containers.</summary>
		public static long CountGoldIn(AbstractItem container) {
			long total = 0;
			foreach (AbstractItem item in container) {
				if (item.Def == GoldDef) {
					total += item.Amount;
				}
			}
			return total;
		}

		/// <summary>Consumes up to `amount` gold from this container's matching stacks. Returns how much was actually consumed (may be less than requested if the container didn't have enough).</summary>
		private static long ConsumeGoldFrom(AbstractItem container, long amount) {
			long remaining = amount;
			// Snapshot first - Item.Consume can delete a fully-drained stack, which would
			// desync a live enumerator over the container we're iterating.
			List<AbstractItem> items = new List<AbstractItem>(container);
			foreach (AbstractItem candidate in items) {
				if (remaining <= 0) {
					break;
				}
				if (candidate.Def == GoldDef) {
					Item goldItem = (Item) candidate;
					remaining -= goldItem.Consume(remaining);
				}
			}
			return amount - remaining;
		}

		/// <summary>
		/// Tries to pay `amount` gold, honoring Globals.PaymentSource. All-or-nothing - if there
		/// isn't enough available from the applicable source(s), nothing is deducted and this
		/// returns false.
		/// </summary>
		public static bool TryPay(Character payer, uint amount) {
			if (amount == 0) {
				return true;
			}

			Container backpack = payer.Backpack;
			BankBox bank = payer.Bank;

			if (Globals.PaymentSource == Globals.PaymentSourceMode.BankOnly) {
				if (CountGoldIn(bank) < amount) {
					return false;
				}
				ConsumeGoldFrom(bank, amount);
				return true;
			}

			//BackpackFirst: draw from gold on hand first, then top up from the bank.
			long backpackGold = CountGoldIn(backpack);
			long bankGold = CountGoldIn(bank);
			if (backpackGold + bankGold < amount) {
				return false;
			}

			long fromBackpack = System.Math.Min(backpackGold, amount);
			ConsumeGoldFrom(backpack, fromBackpack);
			long remainder = amount - fromBackpack;
			if (remainder > 0) {
				ConsumeGoldFrom(bank, remainder);
			}
			return true;
		}

		/// <summary>Grants `amount` gold into the recipient's backpack, merging into an existing pile there rather than fragmenting into a new stack if one's already present.</summary>
		public static void Grant(Character recipient, uint amount) {
			if (amount == 0) {
				return;
			}
			Container backpack = recipient.Backpack;
			AbstractItem goldItem = (AbstractItem) GoldDef.Create(backpack);
			goldItem.Amount = (int) amount;
			backpack.TryStackToAnyInside(goldItem);
		}

		/// <summary>Grants `amount` gold directly into the recipient's bank, same stacking behavior as Grant.</summary>
		public static void GrantToBank(Character recipient, uint amount) {
			if (amount == 0) {
				return;
			}
			BankBox bank = recipient.Bank;
			AbstractItem goldItem = (AbstractItem) GoldDef.Create(bank);
			goldItem.Amount = (int) amount;
			bank.TryStackToAnyInside(goldItem);
		}

		/// <summary>Moves `amount` gold from the backpack to the bank. Unlike TryPay, this always
		/// draws from the backpack specifically - deposit/withdraw are explicit direct transfers,
		/// not a purchase, so Globals.PaymentSource (which only governs where purchases draw
		/// from) doesn't apply here.</summary>
		public static bool TryDeposit(Character who, uint amount) {
			if (amount == 0) {
				return true;
			}
			Container backpack = who.Backpack;
			if (CountGoldIn(backpack) < amount) {
				return false;
			}
			ConsumeGoldFrom(backpack, amount);
			GrantToBank(who, amount);
			return true;
		}

		/// <summary>Moves `amount` gold from the bank to the backpack.</summary>
		public static bool TryWithdraw(Character who, uint amount) {
			if (amount == 0) {
				return true;
			}
			BankBox bank = who.Bank;
			if (CountGoldIn(bank) < amount) {
				return false;
			}
			ConsumeGoldFrom(bank, amount);
			Grant(who, amount);
			return true;
		}
	}
}
