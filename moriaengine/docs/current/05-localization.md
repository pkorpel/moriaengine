# Localization

Resx-based localization (#104, "Part B"), replacing an earlier scattered-file approach.
Covers both the compiled C# message classes used throughout the engine and the handful
of inline LScript localization blocks.

## Two known languages, looked up directly - no `CultureInfo`

`GameTextResources` (`Common/Localisation/GameTextResources.cs`) is the backing store:
two plain embedded `.resx` resources (`GameText.resx` for English/Default,
`GameText.Czech.resx` for Czech), looked up directly by the `Language` enum rather than
via `ResourceManager`'s `CultureInfo`-based satellite-assembly fallback mechanism.

**Why not the standard `ResourceManager`+`CultureInfo` approach**: this sandbox's
`dotnet` CLI requires `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` just to run at all (no
ICU installed), which means MSBuild's culture-splitting logic for a `*.cs-CZ.resx`-named
file can't be reliably exercised or validated locally - it behaved inconsistently
(silently falling back to embedding the file as a *plain* non-culture resource under a
literal manifest name, not a real satellite assembly) when first tried. There's also no
real need for general multi-culture fallback machinery - this server has exactly two
known languages. The fix was to name the Czech file `GameText.Czech.resx` (deliberately
**not** culture-pattern-shaped, so MSBuild always treats both files as plain embedded
resources, in any environment) and look both up directly keyed off `Language`, always
queried with `InvariantCulture` so the lookup reads straight from each resource's own
embedded data with no culture-fallback probing involved.

Flat keys throughout: `{ClassName}_{FieldName}` (e.g. `TamingLoc_whatToTame`) for
compiled classes, `{defname}_{EntryName}` (e.g. `loc_kick_Kicks`) for the LScript-side
entries described below.

## Compiled message classes: `CompiledLocStringCollection<T>`

Every per-feature message class (`TamingLoc`, `HidingLoc`, etc - ~39 of them) extends
`CompiledLocStringCollection<T>` (`Common/Localisation/CompiledLocStringCollection.cs`).
All ~90+ call sites (`Loc<TamingLoc>.Get(lang).fieldName`) across the codebase are
unchanged by this migration - only `GetEntriesFromCode` (`CompiledLocStringCollection.
cs:34-42`) changed what it reads from:

```csharp
protected override IEnumerable<KeyValuePair<string, string>> GetEntriesFromCode() {
    foreach (FieldInfo fi in typeof(T).GetFields(...)) {
        if (fi.FieldType == typeof(string) && !fi.DeclaringType.IsAbstract) {
            string flatKey = typeof(T).Name + "_" + fi.Name;
            yield return new KeyValuePair<string, string>(fi.Name,
                GameTextResources.GetEntry(flatKey, this.Language));
        }
    }
}
```

This used to read the field's own literal default value via `fi.GetValue(this)` - but
that ran *before* this class's own field initializers in the ctor chain anyway, so the
literal defaults were really just documentation, not a real fallback path. Now it reads
straight from `GameTextResources`, keyed by the flat `{ClassName}_{FieldName}` name.

**A new field on an existing class needs a matching resx entry in *both* files, not just
the C# field** - this is easy to miss since the code compiles fine either way; a missing
entry resolves to a literal `<LocalisationEntryNotAvailable>` sentinel at runtime instead
of a build error. See [Professions](04-gameplay-systems/professions.md) for a concrete
case where this was hit while adding new deny-result messages.

## The LScript-side mechanism: `ScriptedLocStringCollection`

A second, independent localization mechanism exists for inline LScript blocks defined
directly in `.scp`/`.def` content files (several header spellings - `[Loc ...]`,
`[ScriptedLoc ...]`, `[Localisation ...]` - all route to the same
`ScriptedLocStringCollection.Load()`, see
[LScript](02-engine-core/lscript.md)/[Scripting & code generation pipeline]
(02-engine-core/scripting-pipeline.md)). 6 confirmed-live defnames existed this way
before the migration: `loc_ability_functions`, `loc_kick`, `loc_longrange`,
`loc_silence`, `loc_mage`, `loc_playervendor`.

These inline blocks are **gone now** - their content was migrated into the same
`GameText.resx`/`.Czech.resx` files, under the `{defname}_{EntryName}` key scheme.
`ScriptedLocResxBridge` (`GameCore/LScript/ScriptedLocResxBridge.cs`) is a small,
hardcoded, closed set (found by a direct reference search across the whole repo, not
expected to grow organically) that registers each one directly from resx at boot
(`RegisterAll()`, called once from `Main.cs` right after `ClassManager.InitScripts()`),
instead of relying on script loading to trigger `ScriptedLocStringCollection.Load()` the
way the old inline blocks did. This is deliberately **not** tied to script hot-reload -
their content lives in the resx files now, not the reloadable script files, so there's
nothing for a script reload to re-trigger.

## Server-wide default language, overridable per-player

`Globals.DefaultLanguage` (an ini setting, `text` section, default `English`) sets the
server-wide default a `GameState`'s `language` field initializes to. A player's own
in-client language switch (`SetLanguageInPacket`) always overrides this per-connection,
exactly as before the migration - the ini setting only affects what a brand-new
connection starts as.

**Real bug found and fixed while wiring this up**: `ConvertTools.ConvertTo`'s enum
branch only caught `InvalidCastException` as the signal to fall back to `Enum.Parse` on
the string form - but a symbolic ini value like `"English"`/`"Czech"` makes
`ConvertTools.ToInt64` throw `FormatException` instead (uncaught). This would have
crashed on every boot after the first, since a missing ini key gets written out as its
default's `ToString()` (a symbolic name, not a number) the first time it's saved. Fixed
by also catching `FormatException`.

## Migration source: the running engine's own resolved output, not the old files directly

The resx content was **not** generated by parsing the old `.txt`/`.cs` files by hand -
that risked subtly reproducing whatever encoding/parsing bugs those older formats
already had. Instead, a throwaway diagnostic dumped the **actual resolved value** for
every live class/field and every live `ScriptedLoc` defname/entry, in both languages,
straight from the running engine (`Loc<T>.Get(language)`/`LocManager.GetLoc(defname,
language)` - i.e. whatever the engine's own existing resolution logic already produced).
This guarantees the resx content matches exactly what was live before the migration,
bugs and all - e.g. one class's "English/Default" values are themselves confirmed
garbled Czech text (a legacy encoding issue baked into that class's own literal
defaults), carried forward faithfully rather than silently "fixed" as a side effect of
an unrelated migration.

10 `ScriptedLoc`-style `.txt` files were confirmed genuinely orphaned (zero references
anywhere, even with an encoding-safe grep) and deliberately **not** imported - dead data
shouldn't get carried into the new format just because it existed in the old one.

## Current status

Fully migrated and live: `CompiledLocStringCollection<T>`'s read path, the 6
`ScriptedLoc` defnames, the server-wide default-language ini setting. A permanent
regression test (`scripts/Tests/LocalizationVerificationTests.cs` +
`Tests/Protocol/LocalizationVerificationTests.cs`) enumerates every live class/field and
defname/entry and asserts a real (non-sentinel) value exists for both languages.

Remaining cleanup, not yet done: deleting the now-dead old `.txt` override files, and
stripping the now-vestigial literal field defaults from the ~39 compiled classes (both
low-risk, deferred rather than urgent).
