# Collector bags (t_collector)

Step 5 of the living world (#1237, #1247). It ports Erebor's `t_collector` from `systems/uzitecne_nezaraditelne_itemy.scp`.
Code: `scripts/Basic/Collectors/Collector.cs`.

A collector is a bag that counts what its owner takes from bodies or from nature, with no items made. "Vacek na stehna grizliho" is one example: aim it at a *Body of Grizzly* and its count goes up. About 20 Erebor quests use one (q006, q011, q013, q017, q035 and others), as do several errands in the [design](living-world-design.md).

## Settings

Each setting is a tag. The bag's def holds them, because the converter writes each `t_collector` item's `@create` lines there. An instance tag of the same name overrides the def's, which is how an errand makes a bag of its own.

| Tag | Meaning |
|---|---|
| `contentsinfo`, `contentstype` | A click shows "<contentsinfo> <count> <contentstype>", e.g. "Ve vacku je 3 stehen grizliho". |
| `maxcontents` | How many the bag holds. 0 means no limit. |
| `target_desc` | Told when the cursor comes up. |
| `target_id` | The name to aim at, e.g. "Body of Grizzly". A body is named after its NPC's def. |
| `value_to_check`, `absolute_success` | A try works when value_to_check >= rand(0..absolute_success), so 50/100 is about half the time. |
| `fail_msg` | Told on a failed try. |
| `target_remove` | 1: the target is removed after a try, failed or not. 0: it stays, marked as taken (`uncollectable`). |
| `repeatedcollecting` | 1: a kept target isn't marked, so it can be taken again. |
| `color` | For items: only that colour counts. |
| `contentscount` | The count, on the bag itself. Quests read it through `Collector.Collected`, `Collectors.FindIn(player, def)` or `Collectors.IsFull(player, def)`. |

## What happens

**Double click:** a dead player or a full bag is refused. Otherwise the bag tells `target_desc` and gives a cursor.

**On a target,** checked in this order:
1. **Reach:** line of sight.
2. **Already taken:** "Uz nelze ziskat vice" for a character, "Co jsi potreboval uz jsi ziskal" for an item.
3. **Colour:** when one is set.
4. **Collection source:** a source is handled as described below.
5. **A player's body:** it spoils the bag. The count drops to 0 with "Znehodnotil jsi obsah lidskou krvi...".
6. **Name:** it must match `target_id`.
7. **Roll:** a success adds one; a failure tells `fail_msg`. Either way the target is then removed or marked.

**Full bag:** a bag that filled up while the cursor was up refuses the target and leaves the body unspent. Erebor spent it anyway.

**Collection sources:** Erebor's `t_collection` is an item the bag picks from, such as a bush. It is set by tags on the item:
- `collector_id`: the bag's def;
- `pick_delay`: seconds between picks;
- `items_to_pick`: how many in all;
- `infinite`: 1 for no end.

No Erebor item used one, but errands can.

## The converter

`t_collector` becomes a `CollectorDef` with `TYPE t_normal`, so it is no longer a worn bag. Its settings are written as def tags:
- **Text settings** are quoted.
- **Numbers** follow Sphere's literals: a leading 0 is hex (`097F`), and a dot means tenths.
- **Expressions** such as `<argv(1)>` are left to whoever makes the bag.

Both the `tag.x=...` and `tag("x",...)` forms are read. The regen changed only the collector defs in 20 quest files, 67 bags in all.

## Tests

`Tests/Protocol/CollectorTests.cs` (server side `scripts/Tests/CollectorTests.cs`, account `collectortests`):
- **The converted bag:** its class and its def settings, then the click text.
- **A real double click from the client:** the target_desc message, the cursor aimed at a body, the count at 1 and the body used up.
- **The rules:**
  - a failure counts nothing and spends the body;
  - a kept body is marked and taken only once;
  - repeated collecting allows another try;
  - the wrong name is refused;
  - a player's body spoils the bag;
  - a full bag spends nothing;
  - only the right colour counts;
  - a source gives, waits, then runs out;
  - `FindIn` finds the bag.
