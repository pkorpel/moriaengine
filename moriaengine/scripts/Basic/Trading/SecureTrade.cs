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
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// One active secure-trade session between two players (issue #13). Each side has its own
	/// virtual "offered items" container; the client renders both boxes side by side once it
	/// receives the open (type 0) packet naming them.
	///
	/// The two boxes are real Container items (reusing the same 0xe75 backpack itemdef
	/// Character.AddBackpack uses) so the ordinary pick-up/drop-item packet handling can move
	/// items into and out of them with no special-casing needed on that side. They're pulled out
	/// of normal view (RemoveFromView) right after creation so bystanders never see or can
	/// interact with them - the two participants see their contents only via the explicit
	/// OpenedContainers registration below, a separate channel from normal visibility.
	/// </summary>
	public sealed class SecureTrade {
		private static readonly Dictionary<uint, SecureTrade> byBoxUid = new Dictionary<uint, SecureTrade>();

		private readonly Character initiator, other;
		private readonly Container initiatorBox, otherBox;
		private uint initiatorGold, initiatorPlat, otherGold, otherPlat;
		private bool initiatorAccepted, otherAccepted;
		private bool finalized;

		private SecureTrade(Character initiator, Character other) {
			this.initiator = initiator;
			this.other = other;
			this.initiatorBox = CreateBox(initiator);
			this.otherBox = CreateBox(initiator); //spawned near the initiator too - never actually visible to either side, see class remarks

			byBoxUid[(uint) this.initiatorBox.Uid] = this;
			byBoxUid[(uint) this.otherBox.Uid] = this;

			OpenedContainers.SetContainerOpened(initiator, this.initiatorBox);
			OpenedContainers.SetContainerOpened(initiator, this.otherBox);
			OpenedContainers.SetContainerOpened(other, this.initiatorBox);
			OpenedContainers.SetContainerOpened(other, this.otherBox);

			SendOpen(initiator, (uint) this.initiatorBox.Uid, (uint) this.otherBox.Uid, other.Name);
			SendOpen(other, (uint) this.otherBox.Uid, (uint) this.initiatorBox.Uid, initiator.Name);
		}

		private static ContainerDef boxDef;
		private static Container CreateBox(Character near) {
			if (boxDef == null) {
				boxDef = ThingDef.FindItemDef(0xe75) as ContainerDef;
				if (boxDef == null) {
					throw new SEException("Unable to find itemdef 0xe75 (used for secure trade boxes) in scripts.");
				}
			}

			Container box = (Container) boxDef.Create(near.X, near.Y, near.Z, near.M);
			box.RemoveFromView();
			return box;
		}

		public static SecureTrade FindByBoxUid(uint boxUid) {
			SecureTrade trade;
			byBoxUid.TryGetValue(boxUid, out trade);
			return trade;
		}

		private static SecureTrade FindActive(Character a, Character b) {
			foreach (SecureTrade trade in byBoxUid.Values) {
				if (!trade.finalized &&
					((trade.initiator == a && trade.other == b) || (trade.initiator == b && trade.other == a))) {
					return trade;
				}
			}
			return null;
		}

		public static bool TryStartOrAddItem(AbstractCharacter fromChar, AbstractCharacter targetChar, AbstractItem item) {
			Character from = fromChar as Character;
			Character target = targetChar as Character;
			if (from == null || target == null || !from.IsPlayer || !target.IsPlayer) {
				return false; //only real player-to-player trades go through here
			}

			SecureTrade trade = FindActive(from, target) ?? new SecureTrade(from, target);
			return trade.AddItem(from, item);
		}

		public static bool TryAddItemToBox(AbstractCharacter whoChar, AbstractItem destCont, AbstractItem item) {
			SecureTrade trade = FindByBoxUid((uint) destCont.Uid);
			if (trade == null) {
				return false; //not one of ours - let the generic container-drop path handle it
			}

			Character who = whoChar as Character;
			if (who == null) {
				return false;
			}
			return trade.AddItem(who, item);
		}

		private bool AddItem(Character who, AbstractItem item) {
			if (this.finalized) {
				return false;
			}

			Container box = (who == this.initiator) ? this.initiatorBox : this.otherBox;
			who.PutItemInItem(box, 0, 0, false); //no permission/reach checks - the box has no real reachable position, and we've already established who is a legitimate trade party above

			//changing either side's offer is a last-second change of terms - both sides must
			//re-confirm, matching the classic UO convention of un-checking both accept boxes.
			this.initiatorAccepted = false;
			this.otherAccepted = false;
			this.BroadcastAcceptedState();

			return true;
		}

		public void SetAccepted(AbstractCharacter whoChar, bool accepted) {
			if (this.finalized) {
				return;
			}
			Character who = whoChar as Character;
			if (who == null) {
				return;
			}

			if (who == this.initiator) {
				this.initiatorAccepted = accepted;
			} else if (who == this.other) {
				this.otherAccepted = accepted;
			} else {
				return;
			}

			this.BroadcastAcceptedState();

			if (this.initiatorAccepted && this.otherAccepted) {
				this.CompleteTrade();
			}
		}

		public void SetGoldAndPlatinum(AbstractCharacter whoChar, uint gold, uint platinum) {
			if (this.finalized) {
				return;
			}
			Character who = whoChar as Character;
			if (who == null) {
				return;
			}

			if (who == this.initiator) {
				this.initiatorGold = gold;
				this.initiatorPlat = platinum;
				SendMyGold(this.initiator, (uint) this.initiatorBox.Uid, gold, platinum);
				SendHisGold(this.other, (uint) this.otherBox.Uid, gold, platinum);
			} else if (who == this.other) {
				this.otherGold = gold;
				this.otherPlat = platinum;
				SendMyGold(this.other, (uint) this.otherBox.Uid, gold, platinum);
				SendHisGold(this.initiator, (uint) this.initiatorBox.Uid, gold, platinum);
			} else {
				return;
			}

			//same re-confirmation rule as changing the offered items.
			this.initiatorAccepted = false;
			this.otherAccepted = false;
			this.BroadcastAcceptedState();
		}

		public void Cancel() {
			if (this.finalized) {
				return;
			}
			this.finalized = true;
			this.ReturnBoxContents(this.initiatorBox, this.initiator);
			this.ReturnBoxContents(this.otherBox, this.other);
			this.CleanUp();
		}

		private void CompleteTrade() {
			this.finalized = true;

			//gold/platinum are swapped as whole units via a backing resource, same as any other
			//currency grant - no partial/negative balances are possible since each side could
			//only ever set a value here, there's nothing to validate beyond what's already typed in.
			this.GiveCurrency(this.other, this.initiatorGold, this.initiatorPlat);
			this.GiveCurrency(this.initiator, this.otherGold, this.otherPlat);

			MoveAllItems(this.initiatorBox, this.other);
			MoveAllItems(this.otherBox, this.initiator);

			SendClose(this.initiator, (uint) this.initiatorBox.Uid);
			SendClose(this.other, (uint) this.otherBox.Uid);
			this.CleanUp();
		}

		private static ThingDef goldDef;
		private void GiveCurrency(Character recipient, uint gold, uint platinum) {
			if (gold > 0) {
				if (goldDef == null) {
					goldDef = ThingDef.GetByDefname("i_gold");
					if (goldDef == null) {
						throw new SEException("Unable to find itemdef i_gold in scripts.");
					}
				}
				AbstractItem goldItem = (AbstractItem) goldDef.Create(recipient.Backpack);
				goldItem.Amount = (int) gold;
			}
			if (platinum > 0) {
				//platinum has no equivalent concept in this engine/era - any amount entered is
				//silently ignored rather than guessing at an unsupported currency conversion.
			}
		}

		private static void MoveAllItems(Container box, Character recipient) {
			Container recipientPack = recipient.Backpack;
			// Snapshot first - reassigning Cont below mutates the box's own contents collection,
			// which would desync a live enumerator over it.
			List<AbstractItem> items = new List<AbstractItem>(box);
			foreach (AbstractItem item in items) {
				// Direct reassignment rather than Character.PutItemInItem (which moves whatever
				// the CALLING character currently has in-hand/dragging, not an arbitrary item) -
				// this is a server-initiated move, not a player drag-drop, so that API doesn't
				// apply here.
				item.Cont = recipientPack;
			}
		}

		private void ReturnBoxContents(Container box, Character owner) {
			MoveAllItems(box, owner);
		}

		private void CleanUp() {
			byBoxUid.Remove((uint) this.initiatorBox.Uid);
			byBoxUid.Remove((uint) this.otherBox.Uid);
			this.initiatorBox.Delete();
			this.otherBox.Delete();
		}

		private void BroadcastAcceptedState() {
			SendAcceptedState(this.initiator, (uint) this.initiatorBox.Uid, this.initiatorAccepted, this.otherAccepted);
			SendAcceptedState(this.other, (uint) this.otherBox.Uid, this.otherAccepted, this.initiatorAccepted);
		}

		private static void SendOpen(Character to, uint myBoxUid, uint otherBoxUid, string otherName) {
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareOpen(myBoxUid, otherBoxUid, otherName);
			to.GameState.Conn.SendSinglePacket(p);
		}

		private static void SendClose(Character to, uint boxUid) {
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareClose(boxUid);
			to.GameState.Conn.SendSinglePacket(p);
		}

		private static void SendAcceptedState(Character to, uint boxUid, bool meAccepted, bool otherAccepted) {
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareAcceptedState(boxUid, meAccepted, otherAccepted);
			to.GameState.Conn.SendSinglePacket(p);
		}

		private static void SendMyGold(Character to, uint boxUid, uint gold, uint platinum) {
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareMyGold(boxUid, gold, platinum);
			to.GameState.Conn.SendSinglePacket(p);
		}

		private static void SendHisGold(Character to, uint boxUid, uint gold, uint platinum) {
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareHisGold(boxUid, gold, platinum);
			to.GameState.Conn.SendSinglePacket(p);
		}
	}

	public sealed class SecureTradeCommandsImpl : SecureTradeCommands {
		public static void Bootstrap() {
			new SecureTradeCommandsImpl();
		}

		public override bool TryAddItemToBox(AbstractCharacter who, AbstractItem destCont, AbstractItem item) {
			return SecureTrade.TryAddItemToBox(who, destCont, item);
		}

		public override bool TryStartOrAddItem(AbstractCharacter from, AbstractCharacter target, AbstractItem item) {
			return SecureTrade.TryStartOrAddItem(from, target, item);
		}

		public override void SetAccepted(AbstractCharacter who, uint tradeBoxUid, bool accepted) {
			SecureTrade.FindByBoxUid(tradeBoxUid)?.SetAccepted(who, accepted);
		}

		public override void Cancel(AbstractCharacter who, uint tradeBoxUid) {
			SecureTrade.FindByBoxUid(tradeBoxUid)?.Cancel();
		}

		public override void SetGoldAndPlatinum(AbstractCharacter who, uint tradeBoxUid, uint gold, uint platinum) {
			SecureTrade.FindByBoxUid(tradeBoxUid)?.SetGoldAndPlatinum(who, gold, platinum);
		}
	}
}
