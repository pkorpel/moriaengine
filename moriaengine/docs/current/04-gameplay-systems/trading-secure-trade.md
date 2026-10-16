# Trading & secure trade

Player-to-player secure trading (#13) - the classic two-box UO trade window, where both
sides see each other's offered items/gold live and must both check "accept" before
anything actually changes hands.

## One session, two real containers

`SecureTrade` (`scripts/Basic/Trading/SecureTrade.cs`) represents one active trade
between two characters. Each side gets its own real `Container` item (reusing the same
`0xe75` backpack itemdef `Character.AddBackpack` uses) - this lets the ordinary pickup/
drop-item packet handling move items into and out of the boxes with no special-casing on
that side at all.

The boxes are pulled out of normal view (`RemoveFromView`) immediately after creation, so
bystanders never see or interact with them. Both participants see the boxes' contents
only via an explicit `OpenedContainers.SetContainerOpened` registration for each box on
each participant - a separate channel from normal line-of-sight visibility, matching how
a real UO client is told "you may look inside this specific container" independent of
whether it's actually in the world near you.

`SecureTrade.byBoxUid` is a static lookup from either box's UID back to its owning
session - every trade-window packet handler (`TryAddItemToBox`, `SetAccepted`,
`SetGoldAndPlatinum`, `Cancel`) resolves the session this way.

## Starting and adding items

`TryStartOrAddItem` finds an existing active trade between the two characters, or starts
a new one, then adds the item. **Only real player-to-player trades go through this path**
- both sides must be an actual `Player`.

`AddItem` explicitly checks `attr_nodroptrade` before placing the item
(`SecureTrade.cs:121-130`) - this path **deliberately bypasses the normal deny-trigger
chain entirely** (its own pre-existing comment says so), so none of the `Item.On_Deny*`
hooks described in [Item ATTR flags](item-attr-flags.md) would otherwise catch a
no-drop-trade item being placed in a trade box. This is why `attr_nodroptrade` needs two
separate enforcement points in the codebase rather than one - see that page for the full
explanation.

Changing either side's offer (adding an item, or changing the gold/platinum amount)
un-checks **both** sides' accept state - a last-second change of terms requires
re-confirmation from both parties, matching the classic UO convention.

## Completing or cancelling

Once both `initiatorAccepted` and `otherAccepted` are true, `CompleteTrade` runs: gold/
platinum are granted via `GoldUtils.Grant` (merges into an existing pile rather than
always creating a new stack), then every item in each box is moved directly into the
other party's backpack via `item.Cont = recipientPack` - **not** through
`Character.PutItemInItem` (which moves whatever the *calling* character currently has
in-hand/dragging, not an arbitrary item - this is a server-initiated move, that API
doesn't apply). `Cancel` does the same item-return but back to each box's own owner.

Platinum has no equivalent concept in this engine/era - any amount entered is silently
ignored rather than guessing at an unsupported currency conversion.

Both paths finish by removing the session from `byBoxUid` and deleting both boxes.

## See also

[Item ATTR flags](item-attr-flags.md) for `attr_nodroptrade`'s full two-enforcement-point
story, and [Economy: banking & vendors](economy-banking-vendors.md) for the other places
`GoldUtils` gets used.
