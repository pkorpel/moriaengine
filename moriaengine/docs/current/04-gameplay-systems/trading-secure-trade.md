# Trading & secure trade

Player-to-player secure trading: the classic two-box UO trade window. Dropping an item on
another player opens it; both sides see each other's offered items live and must both tick
"accept" before anything changes hands. Built in #13, reworked to Sphere 0.99z8's rules in
#810 (reference: EreborNext `CItemContainer.Trade.cs`, `CClientGame.CharacterDrop.cs`,
`CChar.ItemDrop.cs`; Sphere 0.56b `Cmd_SecureTrade`/`CancelAllTrades`; Erebor's own `@death`).

## One session, two equipped boxes

`SecureTrade` (`scripts/Basic/Trading/SecureTrade.cs`) is one active trade between two
players. Each side has its own box, `i_secure_trade_box` (`SecureTrade.def`, model 0x1e5e like
Sphere's trade window) - a real `Container` **equipped on its owner's Special layer** (30),
like Sphere's `IT_EQ_TRADE_WINDOW`. So the box is never on the ground, never shown to anyone
(layers above Mount aren't sent), and is saved with its owner. The ordinary pick-up/drop
packet handling moves items in and out of it.

Items on an invisible layer normally fail every reach/visibility check (`Item.IsNotVisible`).
A live trade box is the exception: `Container.IsReachableOnInvisibleLayer` is true while
`SecureTrade` knows the box. Both traders are registered as viewers of both boxes
(`OpenedContainers.SetContainerOpened`, redone on every change, because `GetViewers` drops
anyone who steps out of reach and never adds them back), which is how the contents reach both
clients. Nobody else is a viewer.

`SecureTrade.byBoxUid` maps either box's uid back to its session. Packets carry the boxes'
**flagged** item uids (what the client knows items by); `FindByBoxUid` accepts either form.

## What the client needs

TazUO (like the classic client) only opens the trade gump if it already knows **both** boxes
- it drops the 0x6F open packet otherwise. So a new trade first sends each box to both
traders as an item contained in its owner's mobile (0x25, RunUO's `SecureTradeEquip`), then
the 0x6F type 0 open. Closing sends 0x6F type 1 to both, plus a delete for both boxes (an
invisible box's `Delete()` isn't announced by itself).

## Rules

- **Starting**: dropping an item on another player (`TryStartOrAddItem`, called from
  `DropItemInPacket`). The target must be online and alive, the dropper alive, and the target
  within reach (`CanReach`, Sphere's `CanTouch`). Otherwise the drop is refused with a message
  and the item bounces back to the pack. Dropping on an NPC isn't a trade - it goes the normal
  `TryPutItemOnChar` way (see #807/#808). A second drop on the same partner adds to the open
  trade.
- **Adding**: into the dropper's **own** box, even when dropped on the partner's side. Checks
  `attr_nodroptrade` itself, since this path bypasses the deny-trigger chain (see
  [Item ATTR flags](item-attr-flags.md)); a no-drop-trade item never opens a window at all.
- **Taking out**: only the box's owner (`Item.On_DenyPickupItemFrom` ->
  `SecureTrade.IsPickupFromBoxDenied`, "That does not belong to you"). Erebor's scripts have
  a commented-out "no stealing in an honest trade" rule for the same thing.
- **Any change unticks both accepts.** Taking an item back out also starts Sphere's anti-swap
  timer: the partner can't accept for 2 s ("Wait a second for the other trade window to
  stabilize.") - `Container.On_ItemLeave` -> `SecureTrade.OnItemLeftBox`.
- **Accepting** needs the partner in line of sight within the update range (18), else "You are
  too far away to trade items." Walking away doesn't cancel by itself (Sphere doesn't either).
- **Only the window's owner** can tick or close it (Sphere's "Trade window weirdness" check).
- **Completing** (both ticked): each box's items go into the other side's backpack.
- **Cancelling** (either side closes the window): items go back to their owners, both windows
  close.
- **Death** cancels before the corpse is made (Sphere deletes trade windows on death, Erebor's
  `@death` logs it - here a `SecureTrade: X died during an active trade with Y` console line),
  so the items go back to the pack first.
- **Logout** cancels (Sphere's `CancelAllTrades` on client detach). Logging out also drops
  whatever the player was holding back into their pack - it used to stay "dragged" on the
  lingering body.
- **Server restart mid-trade**: the session is gone but the boxes were saved on their owners.
  `Player.On_LogIn` -> `ReturnOrphanedBoxes` empties any such box into the pack and deletes it.

No gold/platinum fields: Sphere 0.99z8 has none (gold changes hands as a pile item like
anything else). The old `SetGoldAndPlatinum` created the gold out of nothing on completion
and was never reachable from a client; it was removed in #810.

## Tests

`Tests/Protocol/SecureTradeTests.cs` (helpers in `scripts/Tests/SecureTradeTests.cs`) and
`SocialTests.TwoPlayersCanCompleteASecureTrade` (the plain completed trade);
`AttrFlagTests.NoDropTradeItemCannotBeAddedToSecureTrade`.

## See also

[Item ATTR flags](item-attr-flags.md) for `attr_nodroptrade`'s two enforcement points, and
[Economy: banking & vendors](economy-banking-vendors.md).
