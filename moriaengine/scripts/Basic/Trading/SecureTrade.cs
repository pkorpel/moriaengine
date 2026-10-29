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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// One active secure-trade session between two players (issue #13, reworked to Sphere 0.99z8
	/// rules in #810 - EreborNext's CItemContainer.Trade.cs / CClientGame.CharacterDrop.cs). Each
	/// side has its own "offered items" box; the client renders both side by side once it receives
	/// the open (type 0) packet naming them.
	///
	/// Like Sphere's IT_EQ_TRADE_WINDOW, each box (i_secure_trade_box) is a real Container
	/// EQUIPPED on its owner's Special layer - invisible to everyone else, saved with the owner,
	/// never on the ground. A box with no live session (server restart mid-trade) is emptied back
	/// into its owner's pack at their next login (ReturnOrphanedBoxes).
	///
	/// Rules: a trade starts by dropping an item on another online, living player within reach.
	/// Any change of either offer unchecks both accept boxes; taking an item back out also makes
	/// the partner wait TradeStabilizeSeconds before they can accept (Sphere's anti-swap timer).
	/// Accepting needs the partner in sight within the update range. Only the box owner can take
	/// items out of a box. Death, logout or either side closing the window cancels the trade and
	/// returns everything.
	/// </summary>
	public sealed class SecureTrade {
		private const double TradeStabilizeSeconds = 2;
		private const string BoxDefname = "i_secure_trade_box";

		private static readonly Dictionary<uint, SecureTrade> byBoxUid = new Dictionary<uint, SecureTrade>();

		private readonly Character initiator, other;
		private readonly Container initiatorBox, otherBox;
		private bool initiatorAccepted, otherAccepted;
		private double initiatorBoxStableAt, otherBoxStableAt;
		private bool finalized;

		private SecureTrade(Character initiator, Character other) {
			this.initiator = initiator;
			this.other = other;
			this.initiatorBox = CreateBox(initiator);
			this.otherBox = CreateBox(other);

			byBoxUid[(uint) this.initiatorBox.Uid] = this;
			byBoxUid[(uint) this.otherBox.Uid] = this;

			this.RegisterViewers();

			//the client only opens the trade gump once it knows both boxes (TazUO's SecureTrading
			//handler drops the open packet otherwise) - RunUO's SecureTradeEquip: each box as an
			//item contained in its owner's mobile
			foreach (Character to in new[] { initiator, other }) {
				SendBox(to, this.initiatorBox, initiator);
				SendBox(to, this.otherBox, other);
			}
			//box uids are the flagged item uids the client knows the boxes by (TazUO looks them up)
			SendOpen(initiator, this.initiatorBox.FlaggedUid, this.otherBox.FlaggedUid, other.Name);
			SendOpen(other, this.otherBox.FlaggedUid, this.initiatorBox.FlaggedUid, initiator.Name);
		}

		//both parties see both boxes' contents. Re-done on every change: OpenedContainers.GetViewers
		//drops a viewer who steps out of reach, and would never add them back.
		private void RegisterViewers() {
			OpenedContainers.SetContainerOpened(this.initiator, this.initiatorBox);
			OpenedContainers.SetContainerOpened(this.initiator, this.otherBox);
			OpenedContainers.SetContainerOpened(this.other, this.initiatorBox);
			OpenedContainers.SetContainerOpened(this.other, this.otherBox);
		}

		private static ContainerDef boxDef;
		private static ContainerDef BoxDef {
			get {
				if (boxDef == null) {
					boxDef = ThingDef.GetByDefname(BoxDefname) as ContainerDef;
					if (boxDef == null) {
						throw new SEException("Unable to find ContainerDef " + BoxDefname + " (secure trade boxes) in scripts.");
					}
				}
				return boxDef;
			}
		}

		private static Container CreateBox(Character owner) {
			return (Container) BoxDef.Create(owner); //equips it on the owner's Special layer
		}

		private static bool IsTradeBox(AbstractItem item) {
			return (item != null) && (item.Def == BoxDef);
		}

		/// <summary>Accepts the client's flagged item uid as well as a plain one.</summary>
		public static SecureTrade FindByBoxUid(uint boxUid) {
			SecureTrade trade;
			byBoxUid.TryGetValue((uint) Thing.UidClearFlags(boxUid), out trade);
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

		private static List<SecureTrade> FindAllFor(Character ch) {
			List<SecureTrade> list = new List<SecureTrade>();
			foreach (SecureTrade trade in byBoxUid.Values) {
				if (!trade.finalized && (trade.initiator == ch || trade.other == ch) && !list.Contains(trade)) {
					list.Add(trade);
				}
			}
			return list;
		}

		/// <summary>An item dropped on a character. Null = not a player-to-player trade (an NPC,
		/// which goes the normal TryPutItemOnChar way).</summary>
		public static DenyResult TryStartOrAddItem(AbstractCharacter fromChar, AbstractCharacter targetChar, AbstractItem item) {
			Character from = fromChar as Character;
			Character target = targetChar as Character;
			if (from == null || target == null || !from.IsPlayer || !target.IsPlayer) {
				return null;
			}

			SecureTrade trade = FindActive(from, target);
			if (trade == null) {
				DenyResult canStart = CanStartTrade(from, target);
				if (!canStart.Allow) {
					return canStart;
				}
				if (IsNoDropTrade(item)) {
					return CannotBeTraded; //don't open an empty window for an item that can't go in it
				}
				trade = new SecureTrade(from, target);
			}
			return trade.AddItem(from, item);
		}

		private static readonly DenyResult CannotBeTraded = new DenyResult_StringSysMessage("That item cannot be traded.");

		private static bool IsNoDropTrade(AbstractItem item) {
			Item asItem = item as Item;
			return (asItem != null) && asItem.HasAttribute(ItemAttributes.attr_nodroptrade);
		}

		private static DenyResult CanStartTrade(Character from, Character target) {
			if (from.Flag_Dead) {
				return DenyResultMessages.Deny_NoMessage;
			}
			if (target.GameState == null) {
				return new DenyResult_StringSysMessage(target.Name + " is not online.");
			}
			if (target.Flag_Dead) {
				return new DenyResult_StringSysMessage("You can't trade with a ghost.");
			}
			return from.CanReach(target);
		}

		/// <summary>An item dropped into a trade box. Null = not one of ours.</summary>
		public static DenyResult TryAddItemToBox(AbstractCharacter whoChar, AbstractItem destCont, AbstractItem item) {
			SecureTrade trade = FindByBoxUid((uint) destCont.Uid);
			if (trade == null) {
				return null;
			}
			Character who = whoChar as Character;
			if (who == null || (who != trade.initiator && who != trade.other)) {
				return DenyResultMessages.Deny_ThatDoesNotBelongToYou;
			}
			return trade.AddItem(who, item);
		}

		private DenyResult AddItem(Character who, AbstractItem item) {
			if (this.finalized) {
				return DenyResultMessages.Deny_NoMessage;
			}

			//#223 - attr_nodroptrade. This path bypasses the normal deny-trigger chain, so there's
			//no Item.On_Deny* hook that would otherwise catch this - must check explicitly here.
			if (IsNoDropTrade(item)) {
				return CannotBeTraded;
			}

			//always into the dropper's own box, even if it was dropped on the partner's side
			Container box = (who == this.initiator) ? this.initiatorBox : this.otherBox;
			who.PutItemInItem(box, 0, 0, false); //no permission/reach checks - we've already established who is a legitimate trade party

			//changing either side's offer is a last-second change of terms - both sides must
			//re-confirm (Sphere's Trade_Status(false) on ContentAdd).
			this.RegisterViewers();
			this.ResetAccepted();
			return DenyResultMessages.Allow;
		}

		/// <summary>Container.On_ItemLeave - an item left a trade box. When that's the owner taking
		/// it back (not the trade completing/cancelling), both accepts reset and the partner has to
		/// wait before accepting again (Sphere's Trade_Status(false, true) on OnRemoveOb).</summary>
		internal static void OnItemLeftBox(Container box) {
			SecureTrade trade = FindByBoxUid((uint) box.Uid);
			if (trade == null || trade.finalized) {
				return;
			}
			double stableAt = Globals.TimeInSeconds + TradeStabilizeSeconds;
			if (box == trade.initiatorBox) {
				trade.initiatorBoxStableAt = stableAt;
			} else {
				trade.otherBoxStableAt = stableAt;
			}
			trade.ResetAccepted();
		}

		/// <summary>Item.On_DenyPickupItemFrom - only the box's owner may take items out of it;
		/// the partner can look but not take.</summary>
		internal static bool IsPickupFromBoxDenied(Container box, AbstractCharacter picker) {
			SecureTrade trade = FindByBoxUid((uint) box.Uid);
			if (trade == null) {
				return false;
			}
			Character owner = (box == trade.initiatorBox) ? trade.initiator : trade.other;
			return picker != owner;
		}

		private void ResetAccepted() {
			this.initiatorAccepted = false;
			this.otherAccepted = false;
			this.BroadcastAcceptedState();
		}

		public void SetAccepted(AbstractCharacter whoChar, bool accepted) {
			if (this.finalized) {
				return;
			}
			Character who = whoChar as Character;
			Character partner;
			double partnerBoxStableAt;
			if (who == this.initiator) {
				partner = this.other;
				partnerBoxStableAt = this.otherBoxStableAt;
			} else if (who == this.other) {
				partner = this.initiator;
				partnerBoxStableAt = this.initiatorBoxStableAt;
			} else {
				return;
			}

			if (accepted) {
				if (Globals.TimeInSeconds < partnerBoxStableAt) {
					who.SysMessage("Wait a second for the other trade window to stabilize.");
					this.BroadcastAcceptedState(); //un-ticks the box the client already ticked
					return;
				}
				if (!IsInTradeRange(who, partner)) {
					who.SysMessage("You are too far away to trade items.");
					this.BroadcastAcceptedState();
					return;
				}
			}

			if (who == this.initiator) {
				this.initiatorAccepted = accepted;
			} else {
				this.otherAccepted = accepted;
			}
			this.BroadcastAcceptedState();

			if (this.initiatorAccepted && this.otherAccepted) {
				this.CompleteTrade();
			}
		}

		//Sphere: the partner in sight within SPHEREMAP_VIEW_SIZE (18 = our MaxUpdateRange)
		private static bool IsInTradeRange(Character who, Character partner) {
			if (who.M != partner.M) {
				return false;
			}
			if (Point2D.GetSimpleDistance(who, partner) > Globals.MaxUpdateRange) {
				return false;
			}
			return who.GetMap().CanSeeLosFromTo(who, partner);
		}

		public void Cancel() {
			if (this.finalized) {
				return;
			}
			this.finalized = true;
			MoveAllItems(this.initiatorBox, this.initiator);
			MoveAllItems(this.otherBox, this.other);
			this.CloseAndCleanUp();
		}

		/// <summary>Cancels every trade this character is in - death, logout (Sphere's
		/// CancelAllTrades / the trade-window delete in @death). Erebor's own @death logs it.</summary>
		public static void CancelAllFor(Character ch, string reason) {
			foreach (SecureTrade trade in FindAllFor(ch)) {
				Character partner = (trade.initiator == ch) ? trade.other : trade.initiator;
				Console.WriteLine("SecureTrade: " + ch + " " + reason + " during an active trade with " + partner + " - trade cancelled.");
				trade.Cancel();
			}
		}

		/// <summary>For tests: this character's trade state in one line.</summary>
		public static string Describe(Character ch) {
			List<SecureTrade> trades = FindAllFor(ch);
			if (trades.Count == 0) {
				return "trades=0";
			}
			SecureTrade t = trades[0];
			bool isInitiator = t.initiator == ch;
			Container myBox = isInitiator ? t.initiatorBox : t.otherBox;
			return "trades=" + trades.Count +
				" myAccepted=" + (isInitiator ? t.initiatorAccepted : t.otherAccepted) +
				" partnerAccepted=" + (isInitiator ? t.otherAccepted : t.initiatorAccepted) +
				" myBox=" + myBox.Uid + " myBoxOnMe=" + (myBox.Cont == ch) + " myBoxLayer=" + myBox.Z +
				" myBoxCount=" + myBox.Count;
		}

		/// <summary>Player.On_LogIn - a trade box with no live session (the server went down
		/// mid-trade) is emptied back into its owner's pack and deleted.</summary>
		public static void ReturnOrphanedBoxes(Character ch) {
			List<AbstractItem> orphans = null;
			foreach (AbstractItem equipped in ch) {
				if (IsTradeBox(equipped) && FindByBoxUid((uint) equipped.Uid) == null) {
					if (orphans == null) {
						orphans = new List<AbstractItem>();
					}
					orphans.Add(equipped);
				}
			}
			if (orphans != null) {
				foreach (Container box in orphans) {
					MoveAllItems(box, ch);
					box.Delete();
				}
			}
		}

		private void CompleteTrade() {
			this.finalized = true;
			MoveAllItems(this.initiatorBox, this.other);
			MoveAllItems(this.otherBox, this.initiator);
			this.CloseAndCleanUp();
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

		private void CloseAndCleanUp() {
			SendClose(this.initiator, this.initiatorBox.FlaggedUid);
			SendClose(this.other, this.otherBox.FlaggedUid);
			foreach (Character to in new[] { this.initiator, this.other }) {
				SendBoxRemoved(to, this.initiatorBox);
				SendBoxRemoved(to, this.otherBox);
			}
			byBoxUid.Remove((uint) this.initiatorBox.Uid);
			byBoxUid.Remove((uint) this.otherBox.Uid);
			this.initiatorBox.Delete();
			this.otherBox.Delete();
		}

		private void BroadcastAcceptedState() {
			SendAcceptedState(this.initiator, this.initiatorBox.FlaggedUid, this.initiatorAccepted, this.otherAccepted);
			SendAcceptedState(this.other, this.otherBox.FlaggedUid, this.otherAccepted, this.initiatorAccepted);
		}

		private static void SendBox(Character to, Container box, Character owner) {
			GameState state = to.GameState;
			if (state == null) {
				return;
			}
			var p = Pool<AddItemToContainerOutPacket>.Acquire();
			p.Prepare(owner.FlaggedUid, box);
			state.Conn.SendSinglePacket(p);
		}

		//the box is on an invisible layer, so its Delete() isn't announced to anyone
		private static void SendBoxRemoved(Character to, Container box) {
			GameState state = to.GameState;
			if (state == null) {
				return;
			}
			var p = Pool<DeleteObjectOutPacket>.Acquire();
			p.Prepare(box);
			state.Conn.SendSinglePacket(p);
		}

		private static void SendOpen(Character to, uint myBoxUid, uint otherBoxUid, string otherName) {
			GameState state = to.GameState;
			if (state == null) {
				return;
			}
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareOpen(myBoxUid, otherBoxUid, otherName);
			state.Conn.SendSinglePacket(p);
		}

		private static void SendClose(Character to, uint boxUid) {
			GameState state = to.GameState;
			if (state == null) {
				return;
			}
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareClose(boxUid);
			state.Conn.SendSinglePacket(p);
		}

		private static void SendAcceptedState(Character to, uint boxUid, bool meAccepted, bool otherAccepted) {
			GameState state = to.GameState;
			if (state == null) {
				return;
			}
			var p = Pool<SecureTradeOutPacket>.Acquire();
			p.PrepareAcceptedState(boxUid, meAccepted, otherAccepted);
			state.Conn.SendSinglePacket(p);
		}
	}

	public sealed class SecureTradeCommandsImpl : SecureTradeCommands {
		public static void Bootstrap() {
			new SecureTradeCommandsImpl();
		}

		public override DenyResult TryAddItemToBox(AbstractCharacter who, AbstractItem destCont, AbstractItem item) {
			return SecureTrade.TryAddItemToBox(who, destCont, item);
		}

		public override DenyResult TryStartOrAddItem(AbstractCharacter from, AbstractCharacter target, AbstractItem item) {
			return SecureTrade.TryStartOrAddItem(from, target, item);
		}

		//Sphere's Event_SecureTrade: only the window's own owner may tick or close it
		public override void SetAccepted(AbstractCharacter who, uint tradeBoxUid, bool accepted) {
			SecureTrade trade = SecureTrade.FindByBoxUid(tradeBoxUid);
			if (trade != null && IsOwnBox(who, tradeBoxUid)) {
				trade.SetAccepted(who, accepted);
			}
		}

		public override void Cancel(AbstractCharacter who, uint tradeBoxUid) {
			SecureTrade trade = SecureTrade.FindByBoxUid(tradeBoxUid);
			if (trade != null && IsOwnBox(who, tradeBoxUid)) {
				trade.Cancel();
			}
		}

		private static bool IsOwnBox(AbstractCharacter who, uint tradeBoxUid) {
			AbstractItem box = Thing.UidGetItem((int) tradeBoxUid);
			return (box != null) && (box.Cont == who);
		}
	}
}
