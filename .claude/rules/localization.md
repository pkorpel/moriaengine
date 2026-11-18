---
paths:
  - "**/*.resx"
  - "moriaengine/Common/Localisation/**"
  - "**/*Loc*.cs"
---

# Localization

- A `CompiledLocStringCollection<T>` reads every string from
  `Common/Localisation/Resources/GameText.resx` (English, the default) and `GameText.Czech.resx`, keyed
  `<Class>_<field>`. The C# field initializer is **not** a fallback: a missing key shows
  `<LocalisationEntryNotAvailable>` in game, and `LocalizationVerificationTests` doesn't catch a missing
  key. Every new Loc class or field gets **both** resx entries in the same PR. Check one message's real
  text in a test.
- Use Erebor's own Czech where the text comes from the archive.
- **Czech without diacritics**: `hrac`, not `hráč`, in resx, code and `.def`. `°` in Role/RoleKey is
  syntax and stays.
- `Languages/*/Scripts/*.txt` overlays are dead code; don't add to them. Czech is deliberately not a
  culture-named resource, and there's no ResourceManager/CultureInfo lookup (`cs-CZ` throws in the
  container anyway).
- The `defaultLanguage` ini setting is only the server default; the client's choice overrides it.
