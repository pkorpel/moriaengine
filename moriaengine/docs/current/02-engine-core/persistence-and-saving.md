# Persistence & saving

## Def values: scripts give the default, a save keeps live changes

A def (an `ItemDef`, `NPCDef`, `SkillDef`, a plugin def...) gets its values from two places,
and they mean different things:

- **Script files** (`.def`, `.scp`) say what a def is. They are read at every boot, and again
  at a script reload, so editing them changes the game.
- **A save** (`globals.sav`'s def sections) holds only what was changed while the server ran,
  typically a GM editing a def through `.info`. It is loaded after the scripts.

So a def's value at runtime is the script's value unless someone changed it live, and a
live change survives a restart without blocking later script edits to other values.

### `FieldValue`

Each def value is a `FieldValue` (`GameCore/FieldValue.cs`), holding a `DefaultValue` and
a `CurrentValue`:

| Where the value comes from | Call | Default | Current | Saved? |
|---|---|---|---|---|
| a script line `name = value` | `SetFromScripts` | the script's value | the same, unless changed live | no |
| a live change (GM, code at runtime) | `CurrentValue` setter | unchanged | the new value | if it differs from the default |
| a save, after the scripts | `CurrentValue` setter (`LoadField_Delayed`) | the script's value | the saved value | again at the next save, while it differs |
| a script reload after a live change | `SetFromScripts` | the new script value | kept (the live change wins) | while it differs |

(A value a def class parses itself and stores with `SetTag` while loading, below, is an
exception: `SetFromCode` refuses a field already changed live, so on a reload after a live
change that line is skipped with a warning and its default isn't updated.)

`ShouldBeSaved()` (`FieldValue.cs:359`) is "current differs from default", so a field never
changed live, or changed back, isn't written. A script value is first stored as text and
resolved on first use (`ResolveTemporaryState`), so a value can name a def defined later.

### Typed fields and tags

A def has two kinds of values:

- **Typed fields**: declared in a `.ct` file's `Def:` section (or with `InitTypedField` in a
  hand-written def's constructor, as `ActivableAbilityDef`'s `pluginDef` and `pluginKey`).
  The field has a C# type, and both script text and saved text are converted to it
  (`ConvertTools.ConvertTo`). A `double` field is a `double` whatever the text looks like.
  This is the normal place for a def's own properties.
- **Tags**: `tag.name = value` lines, and `GetTag`/`SetTag` in code. A tag has no type: the
  value is whatever the parser makes of the text. A number is an `int` when it looks like
  one and a `double` otherwise (`ConvertTools.TryParseAnyNumber`), and a saved `double` of
  `1.0` is written as `1` (text starting with `0`, such as `010`, is read as Sphere hex). So code reading a numeric tag must not test for one exact type:
  `ConvertTools.ToDouble(tag)` / `ToInt32(tag)` take any number. (Enums are saved with their
  type's name, #407, so they come back as the enum.)

A value the code itself defines and reads (aggro mode, move rate, a realm) belongs in a typed
field, not a tag. `NPCDef`'s single values (aggro mode, realm, the taught skill, move rate,
spell cast range, boss, dragon brain, no-animate, hypnotizable, silence resist, monster types,
magic horse, serpent form) are `Defs:` fields in `NPC.ct` since #1375, and its {min, max}
ranges (`int[]` fields, about 108: stats, experience, every monster ability's numbers) since
#1377. `NPCDef.LoadScriptLine` only turns a range's script form (`min max`, or one value for
both) into the comma list FieldValue reads an array from, and maps the keywords that differ
from their field (`str` to `strRange`, `experience` to `experienceWorth`...); a value of another
shape is warned about and left unset. The dragon, boss, Halda Molle and HCN tomb abilities read
their numbers by field name through `NPCDef.AbilityTuning`; the rest read the properties. Its lists are still tags until they
move too (see `CLAUDE.md`).

### Code that sets a def value while scripts load

Some def classes parse a script keyword themselves (`NPCDef.LoadScriptLine`'s `"can"`,
`"experience"`, `"str"`). Such a value is the script's, so it must become the default, not a
live change. For tags this is `AbstractDef.SetTag` checking `isLoadingFromScript` (set
around `LoadScriptLines` and `LoadPostponedScriptLines`) and using `FieldValue.SetFromCode`,
which sets default and current together. Without it every such value was written into each
save and read back from there as an untyped number (#168).

### Pipe-separated enums

A script line for a typed enum field, such as Sphere's `ATTR = attr_static|attr_magic`, is
parsed by `FieldValue.ResolveStringWithoutLScript` (`FieldValue.cs:236-243`): `|` and `+`
become `,`, a `TypeName.` prefix is dropped, and `Enum.Parse` reads it; a number is tried
after that. `ConvertTools.ConvertTo` (`Common/ConvertTools.cs:132-148`) does the reverse
order for other values (saved ones, code): a number first, then the names.

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

Defs from `.def`/`.scp` files are registered by `ScriptLoader.Load`, before the world loads. A def made in code must
exist by then too. The engine's pattern is a static field of the def's own class:

```csharp
public partial class CraftingProcessPluginDef {
	public static CraftingProcessPluginDef instance = (CraftingProcessPluginDef)
		new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1).Register();
}
```

The def class has a `Bootstrap()`, which `ClassManager.InitClasses` calls right after the scripts compile, before
anything loads. A `.ct`-generated plugin def gets one with `RegisterPluginDef`; a hand-written plugin def writes the same
(`TimerPluginDef`, `MagicChargePluginDef`); any other def class with such a field has an empty one, "to ensure calling the
static initialiser" (`PartyDef`, `GuildDef`). The original code assumed a class's static field initializers had run by
that call; .NET 8 (checked with a small program) runs them only when a field is first read, unless the class has a static
constructor of its own. #1163 found 11 code-made defs missing when a save loaded (combat queue and tracker, paralyze,
discordance, warcry, magic charge, ignition, poisoned item, crafting, snooping, party): some on their def classes, missing
for that reason, some on plugin classes, which nothing reads early on any runtime. #1163 ran every such class's static
initializer before the load; since #1369 every one is on its def class and `InitClasses` runs the def class's static
constructor before its `Bootstrap()`.

A def made this way exists before its compiled `On_x` triggers are attached: those are registered when the Generated
assembly loads, after the scripts', and attached by `PluginDef.Init()` after the world has loaded, as in the original
engine. So a plugin's compiled trigger doesn't run during the load or `@afterLoad`.

The engine's other pattern, for a class that makes several defs, is a static constructor of its own plus an empty
`Bootstrap()` "to ensure calling the static initialiser" (`StatModSpellsUtils`, five stat spell plugin defs).

A def made in code anywhere else, such as a plugin class's static field, exists only once that field is first read.
`ClassManager.CheckCodeMadeDefs`, run after the world loads (reading a field makes its def, so earlier it would hide one
made too late), looks at the static fields of def and plugin classes (not of other classes) and logs an ERROR for such a
def, for a plugin def whose class has no `Bootstrap()` (a role def's class needs one
too, which the engine can't check: roles live in the scripts), and for one not registered under its name. `BootLogTests` fails on any ERROR
and saves a world holding one of every plugin made this way, checking they all come back. Add a new one there.

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
then writes the bytes. Since #1360 a load fills that pool up front, to the size of the files it read plus an eighth
(`SaveBuffer.Reserve`), so the first save after a restart doesn't allocate its buffers while the world waits:
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
