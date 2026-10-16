# Glossary

Short definitions for terms used throughout these docs. See the linked page for full detail.

**ItemDef / CharacterDef** - the *template* object for an item or character type (e.g. "a
longsword", "a jackrabbit"). Created once per `DEFNAME` from a `.def` block at boot; real
in-world instances are created from it via `ThingDef.Create(...)`. See
[The Thing model](../02-engine-core/thing-model.md).

**Thing / AbstractItem / AbstractCharacter** - the C# class hierarchy backing every
in-world object. `Thing` is the common base (position, UID, deletion); `AbstractItem` and
`AbstractCharacter` split off the item-specific and character-specific behavior
respectively. `Item`/`Character` (note: no "Abstract" prefix) are the next layer down,
where most real gameplay-facing properties/overrides actually live. See
[The Thing model](../02-engine-core/thing-model.md).

**TagHolder / TagKey** - the generic "arbitrary named value storage" mechanism almost
every persistent object in this engine is built on. A `TagKey` is an interned, reusable key
(`TagKey.Acquire("some_name")`); `TagHolder` is the base class providing `GetTag`/`SetTag`/
`RemoveTag` against those keys. Plugins and **timers** are also stored in the same
underlying table as ordinary tags - see [Tags & timers](../02-engine-core/tags-and-timers.md).

**FieldValue** - the generic mechanism backing *typed*, `.def`-settable fields on a
`ThingDef` (e.g. an `ItemDef`'s configured default value for some property). Handles
parsing a `.def` line's raw string into the real target type, including the
pipe-separated `[Flags]`-enum convention (`ATTR = attr_x|attr_y`). See
[Persistence & saving](../02-engine-core/persistence-and-saving.md).

**DupeItem** - the Sphere-style mechanism for one `ItemDef` to say "I'm really just a
variant of that other one" (`AbstractItemDef.DupeItem`), and for cycling an item through a
group of visually-related models (`AbstractItemDef.GetNextFlipModel`, what the `.flip` GM
command drives).

**plevel** - an account's permission level, a plain integer (`AbstractAccount.PLevel`).
Different parts of the engine gate on different thresholds: `Character.IsGM` is simply
`PLevel > 1`; the default-deny fallback for any command not explicitly listed in
`CommandsAuthorization` requires `PLevel >= 4`. The two checks are related but not the
same number - a character can be "a GM" (`IsGM == true`) without having enough plevel to
run every unlisted command. See [GM commands](../03-admin-tooling/gm-commands.md).

**cliloc** - a numeric ID identifying a string the real UO *client* renders from its own
built-in string table (optionally with embedded arguments), rather than literal text sent
over the wire. Used throughout `DenyResult`/system-message code
(`DenyResult_ClilocSysMessage`, `GetNameCliloc`, etc).

**UID** - the unique integer identifier every `Thing` gets on creation
(`Thing.Uid`/`Thing.UidGetItem`/`Thing.UidGetThing`), stable across saves/reloads.

**Region / MultiRegion / FlaggedRegion** - `Region` is the base concept (an area with
some associated behavior); `StaticRegion` is baked into the map at boot;
`DynamicRegion` is created/placed at runtime (houses, ships) and is always checked before
static regions at the same point; `FlaggedRegion` adds a generic bitflag set (safe/guarded/
no-pvp/no-decay/...) on top of either. `MultiRegion` is the specific `DynamicRegion`
subtype backing a placed house or ship. See [Regions](../02-engine-core/regions.md).

**Trigger / deny-trigger** - the generic "something is about to happen, and any number of
handlers (scripted or compiled) get a chance to object" pattern used for nearly every
state-changing action in the engine (pickup, drop, trade, equip, door open/close, ...). A
"deny" trigger specifically produces a `DenyResult` that can block the action and carry a
reason message. See [Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md).

**SteamFunction** - the `[SteamFunction]` attribute marking a static method as a real
chat/console command, dispatched by name through the generated command table (see
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md)). Used both
for real GM/player commands and for the large family of `Test*`-prefixed diagnostic
commands protocol tests drive over the wire.

**LScript** - the embedded scripting language `.scp`/`.def` content and raw-LScript
dialogs are written in; also the language real in-game "speech" commands get parsed
through for sufficiently-elevated accounts. See [LScript](../02-engine-core/lscript.md).

**`.ct` / `.def` / `.scp`** - three content file conventions, all ultimately loaded by the
same LScript machinery, distinguished by what they're *for*: a `.ct` file is a class
*template* co-located with a C# gameplay class, driving code generation for that class
(e.g. `Item.ct` next to `Item.cs`); a `.def` file contains real `[ItemDef ...]`/
`[CharacterDef ...]`-style content blocks; a `.scp` file holds everything else written in
raw LScript (dialogs, trigger groups, standalone functions). See
[Architecture overview](architecture-overview.md) and
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md).

**BoundTimer** - a `Timer` subclass that lives attached to a specific `TagHolder` (stored
in that holder's own tag table, which is why it persists across saves for free). See
[Tags & timers](../02-engine-core/tags-and-timers.md).

**ViewableClass** - the `[ViewableClass]` attribute marking a class for reflection-based
property discovery by the generic `.info` GM dialog. See
[ViewableClasses & the `.info` dialog](../02-engine-core/viewable-classes-and-info-dialog.md).

**CompiledGumpDef** - the base class for a hand-written (as opposed to raw-LScript
`[dialog ...]`) dialog. See [Dialog framework](../03-admin-tooling/dialogs-framework.md).

**GUTATable** (and `GUTAButton`/`GUTACheckBox`/`GUTAText`/...) - the layout-building
components used inside a `CompiledGumpDef` to lay out a dialog's rows/columns. See
[Dialog framework](../03-admin-tooling/dialogs-framework.md).
