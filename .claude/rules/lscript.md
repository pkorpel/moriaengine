---
paths:
  - "**/*.def"
  - "**/*.scp"
---

# `.def`, `.scp` and LScript

- Hand-written `.def`/`.scp` only; `scripts/converted/` is generated (see `converter.md`).
- **Quote strings in `string[]` fields and arrays.** A bare word is parsed as a name. If it matches a
  class it fails ("Badly placed or invalid namespace/class name"), and because the field is lazy it fails
  only when first read (a priest's title "Healer" hit the Healer class). Test by reading every entry, not
  one sample.
- A def line whose name has no FieldValue is postponed past `LoadingFinished`. Extend def formats with
  typed FieldValue fields or `LoadScriptLine` data lines, not new formats; prefer generic fields over
  hand-written `int.Parse` cases (they resolve constants and `{min max}` ranges). Typed array fields are
  `Int32[]`. An empty array means "unrestricted".
- Hand-written `NPCDef.LoadScriptLine` wants bare enum values (`Realm = Gondor`). `[Constants ...]`
  sections aren't parsed. A Constant naming a def reads as the def, not its name.
- Each `on=@x` in a `.scp` is its own section; two in a row don't share a body. `on=@Select` lines can
  cancel before the C# runs, so read the whole section.
- LScript: `||` doesn't short-circuit. `name value` is an assignment, so random lists need commas.
  `<...>` wraps only dotted chains, not arithmetic. `{a,b}` random is integer-only. `ON=300 399` button
  ranges work. Don't name a local `info`. A separately called `[function]` doesn't inherit the caller's
  `argtxt`/`argnum`. Raw-LScript dialogs get 2 argv; custom DialogArgs are `argv(0).InputArgs[i]`. Use
  `GetByDefname`, not `Get`.
- A GM's chat text is LScript: `.goch "text"` needs the quotes.
- Erebor sources are cp1250 with CRLF. Grep with `-a`, and keep each file's encoding and line endings.
