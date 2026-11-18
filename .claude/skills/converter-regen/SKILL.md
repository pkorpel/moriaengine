---
name: converter-regen
description: Regenerate moriaengine's scripts/converted/ from the Erebor archive with SphereShardConverter — clone the fork, run into a scratch dir, diff file by file, copy whole files. Use whenever converted .def output must change (a converter fix, an archive fork change, missing data).
---

# Regenerating `scripts/converted/`

Rules first: `.claude/rules/converter.md`. Never hand-edit the output. Full reference:
`moriaengine/docs/current/06-tooling/sphere-shard-converter.md`.

1. **Fix the source**: either the archive fork `pkorpel/erebor-sphere-archive` (branch + PR there), or
   `moriaengine/SphereShardConverter/`.
2. **Get the archive** (gitignored, never committed). Clone the whole fork into the converter's input
   folder. The converter reads `convert/scripts/...` and `convert/save/sphereworld.scp` (the
   `nastaveni_*` values):
   ```bash
   cd moriaengine
   git clone --depth 1 https://github.com/pkorpel/erebor-sphere-archive.git SphereShardConverter/convert
   ```
   To test an unmerged fork branch, clone that branch.
3. **Run into scratch, never straight into `scripts/converted`**:
   ```bash
   MORIA_CONVERTER_OUTPUT_DIR=<scratchpad>/conv dotnet run --project SphereShardConverter
   ```
   `git status moriaengine/scripts/converted` must stay clean during scratch runs. A new generator
   attribute or def field must build (`pwsh ./build.ps1`) before the converter can use it.
4. **Diff file by file** against `scripts/converted/`, ignoring `//` comments and blank lines. Explain
   every changed line. Expect only the lines your change targets. Index hex-keyed blocks by `DEFNAME` as
   well as the header. Before deleting an archive entry as a "dead duplicate", grep for `ID=`/`model=`
   references to it; the converter uses entries as inheritance anchors.
5. **Copy whole regenerated files** with plain `cp` (no delete, never `rsync --delete`), only the files
   that changed. A regen of unchanged sources must diff to 0.
6. **Verify**: `BootLogTests` (no new boot ERROR/WARNING), `SpeechStubTests`, plus the area's classes
   (see the `protocol-tests` skill). The PR lists the regenerated files and how many lines changed in each.

Grep the archive with `-a` (`--binary-files=text`): it's cp1250 with CRLF, and plain grep silently finds
nothing. Archive headers are hex with leading zeros and no `0x` (`[ITEMDEF 01f2d]`). File names lose a
`sphere`/`sphere_d_` prefix and can differ in case, so compare case-insensitively.
