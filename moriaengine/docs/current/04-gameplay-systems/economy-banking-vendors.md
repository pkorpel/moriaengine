# Economy: banking & vendors

Banking, NPC vendor buy/sell, and the separate player-vendor system - three related but
independently-built pieces of the economy.

## Banking

`BankBox` (`scripts/Basic/ItemClasses/Bank/BankBox.cs`) is a real container, but
`On_DClick` is overridden to refuse direct access: "Thou must speak with a banker to
access thy bank box." - real UO bank boxes are only reachable through a banker NPC, and
`Container.On_DClick` (shared by every other container in the engine) has no such gate by
default. GMs are exempted so admin inspection via the normal container-open path still
works.

`BankerSpeech` (`scripts/Basic/Economy/BankerSpeech.cs`) is the actual banker interaction
- a direct `AbstractSpeech` subclass (mirrors `ScriptedSpeech`'s own shape, the same
`Bootstrap()`/`Register()` convention `PartyCommands`/`SecureTradeCommandsImpl` already
use elsewhere) rather than an LScript `[SPEECH]` block - there was no existing LScript
speech example anywhere in this codebase to confirm syntax against, and simple keyword
matching doesn't need LScript's pattern-trigger machinery. This was new functionality,
not restored content - the converted `vendors.def` banker NPCs had their speech trigger
commented out by the old converter with nothing left to restore (the referenced
`jobbanker.def` speech block was stripped to an empty header; since #825 it's a `[SpeechStub]`
placeholder, which `BankerSpeech` overrides - see [NPC speech](npc-speech.md)).

Recognizes 3 keywords anywhere in the spoken line (`balance`, `deposit`, `withdraw`) via
plain substring search - `deposit`/`withdraw` pull the first run of digits out of the
message (`TryExtractAmount`, e.g. "withdraw 500" -> 500) rather than requiring an exact
phrase. `GoldUtils.CountGoldIn`/`TryDeposit`/`TryWithdraw` do the actual balance math
against `Character.Bank`.

## NPC vendor buy/sell

`NpcVendorStock` (`scripts/Basic/Economy/NpcVendorStock.cs`) is a single, small, fixed
stock list **shared by every basic NPC vendor** - there was no existing NPC merchant
purchase flow at all to extend (only the separate player-vendor stub, see below), so this
deliberately stays simple rather than building a full per-NPC/restocking configuration
system. Per-vendor stock lists are a natural follow-up once real content needs them.

Each `Entry` has a `Defname` and `BuyPrice`; `SellPrice` is always exactly half the buy
price - "a simple, predictable markup rather than a general item-appraisal system this
codebase doesn't have." The 3 seeded entries (`i_torch`, `i_bandage`,
`i_dagger_verite`) were chosen because they're confirmed-active converted content, not
guessed at.

`D_VendorShop` (`scripts/Basic/Economy/VendorShopDialog.cs`) is the buy-list gump - one
row per stock entry (icon, name, price, buy button), modeled after the crafting menu
(`CraftmenuDialog.cs`) but deliberately much simpler: no paging, no GM-only columns,
since the stock list is small and fixed.

## Player vendors

`PlayerVendor` (`scripts/Optional/PlayerVendor/PlayerVendor.cs`) is a separate system - a
pet-like NPC a player can place and stock with their own items at their own prices, under
`scripts/Optional/`. `CanVendorBeControlledBy` reuses the existing pet-ownership check
(`IsPetOf`) rather than a parallel ownership concept. `CanInteractWithVendorMessage` gates
on `IsOperational` (an offline/non-functional vendor shows a message instead of opening).

This system has its own stock-management dialogs (`D_PlayerVendor_Stock`,
`D_PlayerVendor_ListStock`, `D_PlayerVendor_StockItemDetail`) and `PlayerVendorStockEntry`
data model, independent of `NpcVendorStock` above - the two vendor flows (basic NPC
vendor vs. a player's own placed vendor) don't share code, since they solve genuinely
different problems (a fixed small catalog vs. per-vendor player-configured stock).

## See also

[Trading & secure trade](trading-secure-trade.md) for the other major money-and-items
flow (`GoldUtils.Grant` is shared between both).
