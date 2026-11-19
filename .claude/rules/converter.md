---
paths:
  - "moriaengine/scripts/converted/**"
  - "moriaengine/SphereShardConverter/**"
---

# Converted scripts and the converter

Read `moriaengine/docs/current/06-tooling/sphere-shard-converter.md` first, especially "Where a correction
goes". To regenerate, use the `converter-regen` skill.

## Never edit `scripts/converted/`

Not one line, not even moriaengine-only glue (a `TEVENTS=` line, a `src.Dialog(...)` trigger). Every
regeneration rewrites the files whole and silently drops output-only fixes; it has happened (e_banker's
TEVENTS, the race stone's @dclick, an i_hatchet layer). Fix it in the archive fork `pkorpel/erebor-sphere-archive`
(its own branch and PR) or in `SphereShardConverter`. Then regenerate and copy **whole** files. A regen of
unchanged sources diffs to 0.

- Hand-written scripts (everything in `scripts/` except `converted/`) win over the archive.
- Don't widen the converter's override tables or `passthroughTriggers` opportunistically. They take only
  entries backed by real data, or bodies this engine can run.
- Missing data is fixed in the converter, never by hand in a def.
