---
paths:
  - "moriaengine/scripts/converted/**"
  - "moriaengine/SphereShardConverter/**"
---

# Converted scripts and the converter

## Never edit `scripts/converted/`

Not even one line, not even moriaengine-only glue such as a `TEVENTS=` line or a `src.Dialog(...)`
trigger. Every regeneration silently drops output-only fixes; it has already happened (e_banker's
TEVENTS, the race stone's @dclick, an i_hatchet layer). A fix belongs in one of two places:

- the **archive fork** `pkorpel/erebor-sphere-archive` (branch and PR there too, never a direct push), or
- **`SphereShardConverter`**, when it can't produce what's needed.

Then regenerate and copy **whole** regenerated files. A fresh regen of unchanged sources must diff to 0.
Hand-written scripts (everything in `scripts/` except `converted/`) win over the archive.

How to run a regen: the `converter-regen` skill.

## Converter conventions

- Item-specific corrections go in defname-keyed override tables in `ConvertedItemDef.ThirdStage`
  (`layerOverrides`, `toolTypeOverrides`). Whole-TYPE rules go in `HandleType`. Top-level chardef fields
  map through `LineImplTask`, and fields inside `ON=@Create` through `ProcessCreateTriggerLine`, which
  **silently drops** lines it doesn't recognise.
- Archive tags become typed def fields through the converter (`tag.hypnotizable` → `NPCDef.Hypnotizable`
  and so on). Missing data is fixed in the converter, never by hand in a def.
- Convert and assign TEVENTS for every def even when the trigger group doesn't exist yet: it warns and
  does nothing until the group is built. Legacy names resolve to stubs in `teventStubs.scp`.
- `passthroughTriggers` takes only bodies this engine can run (plain single calls). Don't widen it
  opportunistically.
- `.def` values from a `[Flags]` enum are written as `DamageType.X|DamageType.Y`, never via `ToString()`.
- Typename constants and string arrays are written quoted (see `lscript.md`).
- Archive data quirks: `COLOR=` after `ITEMNEWBIE=`/`Equip=` colours that item. `ITEMNEWBIE` gear is
  newbie and never drops. Hex-keyed blocks can carry `DEFNAME=` aliases, so index diffs by both. Loot
  lives in `@NPCRestock`. `.def WEIGHT` is stones. Type names arrive lowercased. Missing `ADDSPELL`s
  (archive typos) stay commented out.
- Hand-written items with no live recipe in the archive keep their recipe via `craftrecipes.def`, written
  by the converter.
