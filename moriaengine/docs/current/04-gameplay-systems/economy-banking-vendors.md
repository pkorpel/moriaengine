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

Talking to a banker, its two dialogs, cheques and purses are Erebor's since #883, see
[Bankers, cheques and purses](banker.md). `BankerSpeech` (`scripts/Basic/Economy/BankerSpeech.cs`)
is a direct `AbstractSpeech` subclass that overrides the `[SpeechStub]` placeholders, see
[NPC speech](npc-speech.md). `deposit N` and `withdraw N` pull the first run of digits out of the
message (`TryExtractAmount`); `GoldUtils.TryDeposit`/`TryWithdraw` do the math against `Character.Bank`.

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

## Fixed-price shops: the alchemist and the Prodejce

Two of Erebor's NPC shops sell at fixed prices to their own realm, each one in both capitals through `.SetupWorld`
(`TownSetup.PlaceShop`):

- the alchemist (`c_alchymista`, `scripts/Basic/Economy/Alchemist`, #1017): elixirs, the empathy crystal, perception
  potions, the life candle and the soul case;
- the Prodejce (`c_drazitel`, `scripts/Basic/Economy/Prodejce`, #1070, `npc_drazitel.scp`): coal (250, up to 1000), magic
  thread (10000, up to 1000), staff crystals and bowstrings of five woods (up to 100 each).

A greeting or a buying word, or a double click within 3 tiles, opens the shop. Wares are picked on its pages, Koupit
holds the order for 10 minutes as a shopping list where a line can be struck, and paying goes through `GoldUtils.Pay`.
Stacks come as one stack; more than five single things come in a bag named Zbozi. Another realm gets "Nech me byt !".

A GM opening either also gets its sales (`d_npc_*_statistika`, `ShopStatistics`): each ware's count and the sum, kept on
the NPC, counting non-GM purchases only. Erebor's sum went wrong from a million gold on; this one doesn't.

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
