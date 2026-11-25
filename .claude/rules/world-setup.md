---
paths:
  - "moriaengine/scripts/Setup/**"
  - "moriaengine/scripts/Basic/**/*Setup*.cs"
---

# World setup

Read `moriaengine/docs/current/03-admin-tooling/world-setup.md` first.

- **World content goes through a `WorldSetup` and `.SetupWorld`**, never one-off placement code.
- Setups only add. **Never reuse or rename a piece id.**
- Our placements of things the archive lacks (teleports, signs, venue spots) are proposals: mark them
  unreviewed and say so in the report.
- Check that a placement really works: the region's Mapplane, and a walk from the spot that has length.
- Changing pieces means updating the counts in `WorldSetupTests` and `WorldSetupRestartTests`.
