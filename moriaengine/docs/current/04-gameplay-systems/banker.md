# Bankers, cheques and purses

Port of Erebor's bankers (#883): `speech/jobbanker.scp`, `f_banker_ok` and its two dialogs in
`speak_minas_tirith.scp`, and `systems/seky.scp`. Code is in `scripts/Basic/Economy`, texts are
Erebor's, unchanged.

## Two kinds of banker

| Banker | Speech | Talking to it |
| --- | --- | --- |
| `c_banker_g`, `c_banker_m` | `spk_banker_ok` | `*bank*`, `*sek*`, `*hi*`, `*zdravym*`, `*ahoj*`, `*dobry*` within 5 tiles open the dialog, nothing else |
| `C_H_BANKER`, `C_H_BANKER_F` | `spk_jobbanker` | the keyword lines below; `bank` opens the box directly |

Double-clicking any banker within 6 tiles opens the dialog (#509 asked for double click; Erebor
had none). The dialog first checks, as `f_banker_ok` does: a realm banker refuses another realm
("Jedes potvoro !!!!"), and anyone needs pants, a robe or a skirt on ("Tahni uchyle !!!!").

## The box and the status window

The bank box opens as the gold and silver chest (gump 0x42). The status window's gold is Erebor's: all
gold on the character, in bags and in the bank, but not in locked containers, and no cheques or purses.

## Dialogs

`D_BankerOk` (`d_banker_ok`): Bankovni sluzby, Nejake drby ?, Hledam praci, Prohlednout.. Erebor's
client-side pages are server replies here, because the examine text depends on the balance
(<= 50 000, under 1 000 000, or above) and it's rolled when shown. A Gondor player at a Gondor banker
gets the envoy hint under Hledam praci.

`D_BankerBank` (`d_banker_bank`): balance text by the same bands, Otevrit banku, Proplatit upisy.
Both refuse beyond 6 tiles ("Jsi prilis daleko od bankere !!") or after moving
("Behem hovoru se nesmis hybat !"). Erebor's `e_vendor_check` also ended on casting a spell; here the
dialog remembers where the talk started, so only moving counts. "Proplatit upisy" cashes every
cheque in the pack and bank box.

## spk_jobbanker lines

First match wins, in the archive's order: `*sek*` cashes one cheque (no value checks, its own
wording), job words, `*Balance*`, `*Bank*`, greetings, robbery, money words, `*gold*`,
goodbyes. Lines with `DORAND n` over fewer lines stay silent on the extra rolls. The archive's
blocks after those (ACCOUNT, COIN, COPPER, CURRENCY, Bank, HOLD UP) can never run. `COMPLEXITY`
isn't a property on 0.99z8, so the lines of its `ELSE` branch run.

Added to Erebor's list because Sphere had them natively: `deposit N` and `withdraw N`.

## Cheques and purses

`CashiersItem` (`[CashiersItemDef]`, set by the converter; limits come from `sphereworld.scp`).

| Item | Holds | Limit |
| --- | --- | --- |
| `i_cashiers_check` | `.check` amount, cashed by a banker | 5 000 to 10 000 000 |
| `i_cashiers_mesec` | gold without its weight | 60 000 |
| `i_cashiers_pokladnicka` | gold without its weight | 200 000 |

- `.check(x)` takes gold from the bank, `.mesec(x)` from the pack, `.mesec` alone takes all of it,
  minimum 500. A GM pays nothing.
- Single click shows `(x gp)`. Dropping gold or another purse on a purse adds to it if it's on you
  or in your bank box; over the limit it becomes full strongboxes plus the rest.
- Double click asks how much to take out. OK splits off a new purse (both sides at least 500), All pours
  out the coins into the purse's container. Coins weigh 500 to the stone, carry limit `20 + 4 * Str`:
  someone over it gets what they can lift, unless the purse is in the bank.

Differences from Erebor:

- A cheque above 10 000 000 that doesn't match its written value sends a red message but no jail,
  there is no jail here.
- Erebor's partial pour paid 500 extra when under 500 would stay, which made gold from nothing and left
  a negative purse. Here 500 stays in the purse.
- The purse weighs 50 stones and the strongbox 90, as the archive's `WEIGHT=` says, though its
  description says weight 1.

## Paying for things

Everything that takes a player's money calls `GoldUtils.Pay`, Erebor's `[function pay]`. Use it for anything new that costs gold. The lower functions are private, so there's no other way.

- A GM pays nothing, and is told so.
- It takes all of the amount or none.
- It draws on the pack, bags in it included, then the bank, or the bank alone with `PaymentSource` set to `BankOnly`. `.check` names the bank and `.mesec` the pack.
- Locked containers don't count.
- Short of money, the payer sees `Potrebujes X zlatych.`. `notEnough` replaces that, and `""` says nothing, for an NPC who answers in its own words.

Not payments, and not through it: guards paid from a town's treasury, gold dropped in a rent chest, a banker's deposit and withdrawal, and a gate's item costs.
