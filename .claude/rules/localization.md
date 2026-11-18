---
paths:
  - "**/*.resx"
  - "moriaengine/Common/Localisation/**"
  - "**/*Loc*.cs"
---

# Localization

Read `moriaengine/docs/current/05-localization.md` first.

- **Every new loc string gets both resx entries** (`GameText.resx` and `GameText.Czech.resx`) in the same PR.
  The C# initializer is no fallback, and no test catches a missing key, so check one message's real text in a
  test.
- Use Erebor's own Czech where the text comes from the archive. **No diacritics** (`hrac`, not `hráč`).
- Don't add `Languages/*/Scripts/*.txt` overlays; they're dead.
