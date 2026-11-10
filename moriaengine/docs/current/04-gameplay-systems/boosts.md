# Boosts

`.boosts` shows everything affecting a player: Erebor's `.boosty` (`systems/heck.scp`), rebuilt as a framework. Code:
`scripts/Basic/EffectDuration/ActiveEffects.cs` and `BoostsDialog.cs`. A GM sees someone else's with `.boostsof`.

## The dialog

The list has sections: Pozitivni vlivy, Negativni vlivy, Ostatni, Cooldowny, Stav. An effect shows its icon, name, what
it does, time left, and where it comes from: `Bless  +10 Str, +10 Dex, +10 Int  1:42  kouzlo, od: Gandalf`.

Buttons, all remembered per player:

| Button | Does |
|---|---|
| Obnovit | Shows what's true now. |
| Kompaktni / Seznam | Erebor's strip of icons, with a coloured time under each. |
| Pozice | Where it opens. Old clients don't tell the server where a dialog was dragged to. |
| Zive | Whether it updates by itself. |

`.boosts` again closes it.

Most players run 3.0.6m clients, which can't redraw a dialog in place, so "Zive" closes it and sends it anew at the
Pozice spot, at once when an effect starts or ends and every `refreshSeconds` for the countdowns. A dragged dialog
snaps back. It's off by default (`liveByDefault` on `.settings`) until tried on a real 3.0.6m client.

## Adding to it

The dialog never changes. `ActiveEffects.Of(character)` is the whole list, from these:

- **Anything built on `EffectDurationPlugin`** shows by itself, with good or bad from its `EffectFlag`. Override
  `EffectDetail` to say what it does, and `IsShownAsEffect` to hide a part that means nothing to a player.
- **Any other plugin, or a worn item** (magic items, when they exist): implement `IEffectDescriber`.
- **Anything else**: implement `IActiveEffectSource` and call `ActiveEffects.Register`.

Spell icons come from the spell's id, an ability's from `Icon =` in its def.

## Differences from Erebor

Erebor listed 50 things by hand, twice, and didn't tell good from bad or say what most of them did, so it needed a
legend. Its other panels depend on systems that aren't ported, and plug into the Stav section when they are:

- Food: player hunger exists since #924 (`HungerPlugin`, #1396), its level shown in the Stav section; Erebor's food panel isn't ported.
- The equip list, HP sight.
