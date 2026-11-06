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

## When the world is saved

- **Automatically**, every `autosaveMinutes` (60 by default), set in the `[files]` section of `steamengine.ini`. 0 turns
  the autosave off. The timer lives in `scripts/Basic/PeriodicSave.scp`, and a GM's `.nextsave` says when the next save is
  due. An older `steamengine.ini` without the line gets the default; the line only appears when the file is written anew.
- **On demand**, with a GM's `.save`.
- **On a deliberate shutdown** (#1173): an admin's `.exit` (plevel 5), the remote console's `exit`, and a line typed at
  GameCore's own console all save first (`MainClass.SaveAndExit`). A save that fails or throws only logs a WARNING, and
  the server goes down anyway: the user chose that over refusing to exit.
- **Never** on `.exitnosave` (plevel 5; console: `exitnosave`). It logs a WARNING that the world wasn't saved. A crash, a
  killed process, and the `exit` typed while scripts are waiting for a successful recompile don't save either. A killed
  process includes `scripts/run-local.sh stop` and `docker stop` (SIGTERM): nothing handles that signal, so they don't save.

Both commands need plevel 5, the console's too (a console login needs only 4, so a plevel 4 console account can't shut
down any more). The check runs inside `Globals.Exit`/`ExitNoSave` (`Commands.AuthorizeAnywhereThrow`), not only on the
typed word: #406 checks only a command's first word, so `.show exitnosave` or `.serv.exit` used to reach them unchecked.

The run level drops from Running while the shutdown still holds `globalLock`, so a `.save` or an autosave queued behind it
can't save a world `Main.Exit` is already taking apart.

A save that fails leaves a half-written folder. A boot tries the newest folders first and gives up after three
(`WorldSaver.Load`), so a few failed saves in a row could keep the server from starting. `E_BackupsManager_Global` renames
the folder of a failed save to `<time>.failed`. That name isn't read as a backup, and the folder stays for an admin to look
at. Only the folder made for that very save is renamed; when the save failed before making one, nothing is.

Until #1173 a GM's `.exit` in the game did nothing ("This code line does nothing"). A command typed in the game resolves on
the character typing it, and only the console's commands resolve on `Globals`, so `exit` read as a bare word. `.save` worked
because it has its own `[SteamFunction]`; `.exit` and `.exitnosave` have one now too (`scripts/Basic/administration/Commands.cs`).

A save whose folder can't be made fails the usual way: `@afterSave` still runs with success false, so the autosave timer
is set again for the next try. A GM's `.save` says when it failed. `autosaveMinutes =` with no number reads as 0, no autosave.

Every save logs where its time went (#1203), one line, `Save timings: pause=... total=... format=... write=...`, then each
file as `name open format write ms size`. The same numbers are in `WorldSaver.LastTimings` (`SaveTimings`).

Since #1209 a save formats each file into memory (`SaveBuffer`, pooled 1 MB chunks, UTF-8 already) while the world is held,
then writes the bytes:
- **`Globals.Save` / `TrySave`** (`.save`, the shutdown save, the save before a recompile, tests) write before they return,
  as before.
- **`Globals.SaveInBackground`** (the autosave, `scripts/Basic/PeriodicSave.scp`) returns after the formatting: the world
  is held only for that (`pause`), and a worker thread writes the files meanwhile. `@afterSave` runs once they are
  written, under `globalLock`, with whether they were. The next save, a recompile and the shutdown wait for that write
  first (`WorldSaver.FinishPendingWrite`).
- **Writing**: to what `@openSaveStream` handed. `scripts/Basic/BackupsManager.cs`'s `NormalFileManager` opens each `.sav`
  straight in the save's folder with big buffers (`SaveStream.CreateFileWriter`: a plain `StreamWriter(File.Create)` buffered
  1K chars and 4 KB, so a 94 MB save was tens of thousands of write calls, half its pause). With no script answering, the
  engine writes `name.sav.tmp` and renames it only once it's whole.
- **A failed write fails the save** (#1205): a write, flush or close that throws makes `@afterSave` get false, so
  BackupsManager sets the folder aside as `.failed` and no backup is pruned. Before, a failed final flush only warned and
  the truncated file counted.
- `WorldSaver.TestWriteFailureFile` (tests only) makes one file's write fail.
