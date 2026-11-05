# Persistence & saving

## `FieldValue`: the generic typed-field mechanism

Every def-level field declared in a `.ct` file's `Def:` section (see
[scripting-pipeline.md](scripting-pipeline.md)) is backed by a `FieldValue`
(`GameCore/FieldValue.cs`), created via `InitTypedField(name, defaultValue, type)` in the
generated def constructor. A `FieldValue` tracks two things at once: a `DefaultValue` (what
the `.def`/code-declared baseline is) and a `CurrentValue` (what it's actually set to right
now) - these start out equal and only diverge once something actually changes the value
away from the baseline.

`CurrentValue`'s setter (`FieldValue.cs:410`) always marks the field `isChangedManually =
true` as a side effect of being set through that path. This flag feeds `ShouldBeSaved()`
(`FieldValue.cs:359`): a field whose current value still equals its default is never written
out on save (nothing to persist - the `.def` baseline already supplies it on the next boot),
but **any** field marked `isChangedManually`, even if it happens to currently equal its
default again, gets saved anyway. This is the crux of the real bug described below.

### `ConvertTools.ConvertTo` and the pipe-separated-enum trick

`Common/ConvertTools.cs`'s enum-conversion branch (`ConvertTools.cs:132`) is what makes
Sphere-style `.def` lines like `ATTR = attr_static|attr_magic` parse into a C# `[Flags]`
enum with **zero custom parsing code**, for any `Def:` field of that shape. The mechanism:
it first tries treating the string as a raw number (`Enum.ToObject`), and when that throws
(a genuinely symbolic value like `"attr_static|attr_magic"` or `"Czech"`), it falls back to
`Enum.Parse(type, asString.Replace('|', ','), true)` - swapping Sphere's pipe-separated
syntax for C#'s comma-separated `Enum.Parse` syntax before parsing. This was confirmed
directly while building the item ATTR flag system (#217) - no new parsing path needed to be
written at all, the existing generic conversion pipeline already handled it because
`ATTR = attr_x|attr_y` is structurally identical to any other pipe-separated `[Flags]`
`.def` value this engine already supported.

## The real #168 bug: why `isLoadingFromScript` exists

`AbstractDef` (`GameCore/AbstractDef.cs`) has a private `isLoadingFromScript` flag
(`AbstractDef.cs:45`), set to `true` around `LoadScriptLines`/`LoadPostponedScriptLines` -
the two entry points that apply a def's own `.def` content at boot - and `false` again once
that's done.

Before this flag existed, a real, previously-undiscovered engine bug (tracked as #168)
corrupted tag-backed def properties like `NPCDef.TamingDifficulty` across a save/reload
cycle:

1. Loading `.def` content at boot calls `SetTag`, which went through the *same*
   `FieldValue.CurrentValue` setter a live GM edit would use - unconditionally marking the
   field `isChangedManually`, even though nothing was actually "changed," it was just the
   baseline being applied for the first time.
2. Every server save therefore wrote that baseline value out as if it were real state (e.g.
   `tag._tamingDifficulty_=0` into `globals.sav`), purely because of step 1's spurious flag.
3. On the *next* boot, the save-path manager loads the most recent save folder instead of the
   committed `.def` baseline, and that saved `tag._tamingDifficulty_` line gets re-parsed
   through the **generic** `"tag."`-prefix path in `AbstractDef`'s script-line loader (not
   the def's own typed `"tamingdifficulty"` branch, since the saved key is a different
   string) - which resolves a plain `"0"` via a number parser that prefers `int` over
   `double`. The property's getter did a strict `tag is double` check, silently failed
   against the now-`int` value, and fell back to its "unconfigured" sentinel.
4. This is self-perpetuating: once a tag has been mis-typed once, every later save
   re-persists the wrong type, so it never self-corrects.

**The fix**: `SetTag` (`AbstractDef.cs:712`) now checks `isLoadingFromScript` and, while true,
routes through `FieldValue.SetFromCode` (`FieldValue.cs:338`, made `internal` for this)
instead of the normal `CurrentValue` path - `SetFromCode` sets *both* current and default to
the same value without ever touching `isChangedManually`. Outside of loading (a genuine
runtime/GM edit), `SetTag` is unchanged. Net effect: `ShouldBeSaved()` now correctly sees
baseline `.def` content as unchanged-from-default and skips persisting it, so the corrupting
write this bug depended on never happens.

This class of bug is also *why* a long-lived local dev sandbox can accumulate stale
`saves/<date>/` snapshots whose presence silently changes test behavior (a different, later
symptom of the same underlying "what counts as save-worthy" question) - see
[../07-testing/known-gotchas.md](../07-testing/known-gotchas.md) for the operational side of
that.

## Generic instance Save/Load

For `Item`/`Character`-family classes, the `Vars:` section of a `.ct` file
(see [scripting-pipeline.md](scripting-pipeline.md)) drives auto-generated Save/Load code
per class - each instance field gets written and re-read with no hand-written persistence
code at all.

For hand-written classes that aren't `.ct`-generated - settings categories
(`SettingsMetaCategory` subclasses, see
[../03-admin-tooling/settings-dialog.md](../03-admin-tooling/settings-dialog.md)) and
`BoundTimer` subclasses (see [tags-and-timers.md](tags-and-timers.md)) are the two recurring
examples - the same save/load machinery is opted into explicitly via `[SaveableClass]` on the
class, `[SavedMember]` on fields that should persist, and a `[LoadingInitializer]`
constructor the loader calls to reconstruct an instance before populating its saved fields.

## Defs made in code, and what a save refers to by name

A saved plugin writes its def as a name (`def=#p_paralyze`), and so does a saved role. Loading resolves the name with
`AbstractScript.GetByDefname`, so the def has to be registered **before** `WorldSaver.Load` runs. Otherwise
`Plugin.LoadingFinished` logs `No PluginDef reference loaded ... Deleting.` and the plugin is dropped together with its
effect.

Defs from `.def`/`.scp` files are registered by `ScriptLoader.Load`, before the world loads. A def made in code is a
static field, `instance = (XDef) new XDef("p_x", "C# scripts", -1).Register()`. A static field initializer runs only when
the field is first read, and loading a save doesn't read it. Until #1163, 11 such defs weren't there when a save loaded:
the combat queue and tracker, paralyze, discordance, warcry, magic charge, ignition, poisoned item, crafting, snooping and
party. Two of them never called `.Register()` at all.

`ClassManager.InitStaticDefs` (#1163) closes this in one place:
- **When:** right after the scripts load and before the world does, on a boot and on a recompile.
- **What:** it runs the static initializers of every scripts class deriving from `AbstractDef` or `Plugin` that holds a def
  in a static field.
- **The check:** it then logs an ERROR for any def made in code (header line −1) that isn't registered under its own name.
  BootLogTests fails on that ERROR, so a new def built without `.Register()` is caught at once.

**Rule for a new plugin or role def made in code:** keep it in a static field of its def or plugin class and call
`.Register()` on it. No `Bootstrap()` is needed for it any more. `BootLogTests` saves a world holding one of every such
plugin and checks they all come back. Add a new one there.
