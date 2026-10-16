# GM commands

How a chat command (`.something`) gets from a typed string to a running method, who's
allowed to run what, and the handful of shapes a command's actual logic takes.

## Two parsers, one authorization gate

`Commands.InvokeCommand` routes every `.`-prefixed chat line through one of two parsers,
chosen purely by the sender's own plevel:

- **`SimpleCommandParser`** (plevel below `Globals.PlevelForLscriptCommands`, default 2) -
  treats everything after the command name as **one combined string argument**, no
  splitting, no quoting semantics. This is what an ordinary player's client goes through.
- **The full LScript parser** (plevel at or above that threshold) - real multi-argument
  splitting, string literals, and - critically - LScript's own member-resolution rules
  apply to the command name itself (see "The command/property collision" below).

Either way, before the resolved method actually runs, `E_CommandsAuthorization_Global`
(`scripts/Basic/administration/CommandsAuthorization.cs`) gets a say via its
`on_Command(Globals globals, ISrc commandSrc, string cmd)` trigger:

```csharp
public int on_Command(Globals globals, ISrc commandSrc, string cmd) {
    if (cmd.StartsWith("Test", StringComparison.OrdinalIgnoreCase)) {
        return 0; //diagnostic-only, see below
    }
    int plevel = commandSrc.MaxPlevel;
    int cmdPlevel;
    if (this.commands.TryGetValue(cmd, out cmdPlevel)) {
        if (plevel >= cmdPlevel) {
            return 0;
        }
        return 1; //explicitly listed, but sender's plevel is too low
    }
    if (plevel < plevelToAllCommands) {
        return 1; //not listed at all, and sender isn't GM-equivalent either
    }
    return 0; //not listed, but sender is plevel >= 4 - allowed by default
}
```

`commands` is a case-insensitive `Dictionary<string, int>` - an **allowlist with explicit
per-command minimum plevels**, not a denylist. The plevel constants run
`PLEVEL_VISITOR=0` through `PLEVEL_OWNER=7`; `plevelToAllCommands=4` is the real
dividing line: anyone at plevel 4 or above (GM and up) can run *any* command not
explicitly listed, while anyone below that can only run what's explicitly in the table.
This is why adding a brand-new player-facing command (not GM-only) always means adding a
line here - forgetting it is a real, already-encountered bug class (see "Historical bug"
below), not a hypothetical.

A few real allowlist entries and why they're there, to show the shape of the table:
`"Where"`/`"Sendgmpage"`/`"Resync"`/`"Messages"` at `PLEVEL_PLAYER` (basic client
utilities every player needs); 13 `Guild*` commands at `PLEVEL_PLAYER` (#84, see below);
`"show"`/`"x"`/`"Page"` at `MORE_THAN_PLAYER` (`=2`); 7 admin-only commands explicitly
pinned at `PLEVEL_ADMIN` even though plevel-4-and-up would already cover them by default
- belt-and-suspenders for commands whose accidental availability would be especially bad.

**The `"Test"` prefix bypass** exists for exactly one reason: the protocol test harness
(`Tests/Protocol/*.cs`) deliberately creates non-GM accounts to exercise real
player-level authorization paths, and needs its own diagnostic `[SteamFunction]`s
(`scripts/Tests/*.cs`) to be callable from those accounts without having to maintain a
parallel allowlist entry for every throwaway test helper (#72). No production command
uses this prefix - if you're naming a real player-facing or GM command, don't start it
with "Test" by accident, it would silently never be authorization-checked.

## Historical bug: the default-deny gap (#84)

Early in this engine's life, **every** non-explicitly-listed command below plevel 4 was
simply denied - there was no equivalent of today's nuanced per-command table, just a
blanket cutoff. This made every `.Guild*` command (and others) completely unusable by
ordinary players even though the underlying feature worked fine once invoked by a GM.
Fixed by building out the allowlist to its current shape. The lesson that outlasted the
specific bug: **a new player-facing command is invisible-broken, not loudly-broken, if
you forget to allowlist it** - it just silently fails authorization with no exception,
easy to miss in testing if you only ever test from a GM account (see the next section
for why that's doubly true).

## The command/property (or method) name collision trap

When an elevated (plevel ≥ `PlevelForLscriptCommands`) account types a command, LScript's
own member-resolution rules run **before** the `[SteamFunction]` dispatch table is
consulted. If the command name happens to also be a property or method on `self` (the
calling character), LScript resolves it as that member access instead - silently, with no
exception, and the real command method is never entered at all.

Two real, independently-discovered instances of this, both fixed the same way (rename the
colliding *member*, not the command string, since the command text is the player-facing
contract):

- **`.kill` vs `Character.Kill()`** (#166's precedent, #192) - a GM-plevel account typing
  `.kill` had LScript resolve it as a bare call to the existing instance method
  `Character.Kill()` on themselves - the GM "killed themselves," with zero error logged
  anywhere. Fixed by renaming the instance method to `Character.InstaKill()`
  (`scripts/Basic/CharClasses/Character.cs:1229`), leaving the command string `"kill"`
  untouched.
- **`.Race` vs `Player.Race`** (#166 itself) - adding a new `Player.Race` property for
  the races system collided with the pre-existing `[SteamFunction("Race")]` command the
  same way. Spent real debugging time chasing a "gump construction throws and gets
  swallowed" theory (plausible, since `CompiledGumpDef.InternalConstruct` really does
  catch-and-log-only) before a literal first-line diagnostic proved the command method
  was never entered at all. Fixed by renaming the command string to `"ChooseRace"` instead
  (no pre-existing player-facing expectation to preserve here, unlike `.kill`).

This is a standing engine-level footgun, not root-caused/fixed in LScript's own
member-resolution order (see [[lscript]] for where that logic lives) - the mitigation is
discipline (check for a same-named member before picking a `[SteamFunction]` string). GM
testing is more likely than player testing to actually exercise this path, since the
collision only fires above `PlevelForLscriptCommands` - don't rely on "it worked when I
tested it as GM" to rule this out for a non-GM-facing command added at the same time.

## Three command shapes

Every GM/player chat command in this codebase fits one of three shapes, depending on what
it needs to act on:

### 1. Self-referential (no target needed)

The command acts on the caller directly - `TagHolder self` (or `Player self`,
`Character self`) is the only parameter. Example: `.TagList`
(`scripts/Basic/administration/tags/TagList.cs:212-213`):

```csharp
[SteamFunction]
public static void TagList(TagHolder self, ScriptArgs text) {
    ...
}
```

### 2. Target-cursor (one external target)

The command needs to act on *something else* - there's no "self" context to invoke
against, so it arms a target cursor and waits for the next click. Example: `.flip`
(`scripts/Basic/administration/Commands.cs:195-219`):

```csharp
[SteamFunction]
public static void Flip(Player self) {
    self.Target(SingletonScript<Targ_Flip>.Instance);
}

public class Targ_Flip : CompiledTargetDef {
    protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
        Item item = targetted as Item;
        ...
        return TargetResult.Done;
    }
}
```

`Player.Target(AbstractTargetDef def)` (`scripts/Basic/CharClasses/Player.cs:484`) arms
the cursor; `GameState.HandleTarget` dispatches the next targeting packet into the
armed `CompiledTargetDef`'s `On_TargonThing` override. `D_AttrList`'s `.AttrList` command
(see [[dialogs-framework]]) is the same shape - it just opens a dialog instead of acting
directly once the target resolves.

**`.flip`'s own real finding**: a "no flip variants" case and a working one are
observationally different only via an explicit message - an early version silently
no-op'd when the targeted item had no `DUPEITEM` siblings converted into content, which
looked indistinguishable from the command simply not working. Fixed by adding a clear
`self.SysMessage("This item has no flip variants.")` rather than leaving the silent
no-op - a general lesson for any target-cursor command: always message back on the "valid
target, but nothing to do" case, don't let it look identical to "command broken."

### 3. Multi-step target chain

Some actions need to resolve **two** different things in sequence - e.g. "reveal this
item to that specific character" needs both the item and the viewer, and a single
`On_TargonThing` only ever receives one target per cursor arm. `.Reveal`
(`scripts/Basic/ItemClasses/ItemVisibilityCommands.cs`, #224) is the first- and
currently-only command in this codebase built this way:

```csharp
[SteamFunction]
public static void Reveal(Player self) {
    self.Target(SingletonScript<Targ_RevealItem>.Instance);
}

public class Targ_RevealItem : CompiledTargetDef {
    protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
        Item item = targetted as Item;
        ...
        self.Target(SingletonScript<Targ_RevealTarget>.Instance, item); //arm step 2, carrying the resolved item forward
        return TargetResult.Done;
    }
}

public class Targ_RevealTarget : CompiledTargetDef {
    protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
        Item item = (Item) parameter; //the item resolved in step 1
        Character viewer = targetted as Character;
        item.GrantVisibilityTo(viewer);
        return TargetResult.Done;
    }
}
```

The second overload, `Player.Target(AbstractTargetDef def, object parameter)`
(`Player.cs:488`), is what makes the chain possible - `parameter` rides along to the next
`On_TargonThing` call untouched. This pattern was confirmed safe by reading
`CompiledTargetDef`'s and `GameState.HandleTarget`'s own dispatch code directly rather
than assuming a second `Target()` call from inside a target handler would behave
sensibly - it does, each arm is a fresh, independent wait-for-next-targeting-packet state.

**A real, non-obvious gate this pattern exposed (#224)**: `GameState.HandleTarget` checks
`self.CanSeeForUpdate(thing)` **before** invoking any target handler at all - a non-GM
character cannot even initiate targeting on something they can't see (e.g. their own
`attr_invis`-flagged item). `.Reveal`'s own item owner needed real GM authority just to
target their own invisible creation in the first place, for reasons with nothing to do
with `.Reveal`'s own logic.

## See also

- [Dialog framework](dialogs-framework.md) - what a command does once it needs to show a
  GM a dialog instead of (or in addition to) acting directly.
- [LScript](../02-engine-core/lscript.md) - the member-resolution order behind the
  command/property collision trap above.
- [Networking & the protocol layer](../02-engine-core/networking-protocol.md) - the
  target-cursor wire protocol `HandleTarget` sits on top of.
