---
paths:
  - "**/*.def"
  - "**/*.scp"
---

# `.def`, `.scp` and LScript

Read `moriaengine/docs/current/02-engine-core/lscript.md` first: parser quirks, `[function]` arguments,
the two command parsers. Def fields and loading: `persistence-and-saving.md`.

- Hand-written `.def`/`.scp` only. `scripts/converted/` is generated (`converter.md`).
- **Quote strings in `.def` string arrays.** A bare word resolves as a name and fails only when first read.
  Test by reading every entry, not one sample.
- Erebor sources are cp1250 with CRLF. Grep with `-a`, and keep each file's encoding and line endings.
