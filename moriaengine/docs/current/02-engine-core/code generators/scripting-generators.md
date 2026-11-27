# Code generation for scripters

The generators turn ordinary C# declarations into the engine integration code that scripts
need: definition wiring, callable functions, trigger dispatch, saving and copying. You write
the class or method and mark what the engine should expose; the project generates the
supporting code during compilation.

You do not run a generator manually or edit its output. Add your code to the scripts project
and use the normal build/startup workflow. This guide covers the build-time generators
from a script author's perspective.

## Choose by what you are building

| Your task | Generator | Main declarations |
|---|---|---|
| Create an item, character or plugin class with a matching definition | Thing and Plugin classes | `[ThingTemplate]` / `[ThingDefTemplate]`, or the Plugin equivalents |
| Handle holder events in a stateful plugin | Plugin trigger dispatch | `partial` plugin class with `On_*` methods; see the plugin example below |
| Make a C# function callable by script name | Compiled ScriptHolder | `[SteamFunction]` |
| Handle events in a shared C# trigger group | Compiled TriggerGroup | `partial` class deriving from `CompiledTriggerGroup`, public `On_*` methods |
| Save a separate data object with the world | Save Implementor | `[SaveableClass]`, `[SaveableData]`, loading constructor |
| Copy a separate data object through the engine's copy system | DeepCopy Implementor | `[DeepCopyableClass]`, `[CopyableData]`, copy initializer |
| Show and edit an object's fields in `.info` | Viewable classes | `[ViewableClass]`, `[ViewDescriptor]`, `[NoShow]`, `[Button]`; see [The `.info` and `.settings` dialogs](../../03-admin-tooling/info-and-settings-dialogs.md) |

The examples below belong in the scripts project. They assume this file header; omit imports
that your particular example does not use:

```csharp
using SteamEngine;
using SteamEngine.CompiledScripts;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts;
```

## 1. Thing and Plugin classes: define a new content type

**Use this when** you need a new item, character or plugin class with its own state and a
matching definition containing shared configuration. If an existing class already does what
you need, a new `.def` section may be enough; no new C# template is required.

The generator supplies constructors, definition/instance wiring, registration support and
save/load/copy handling for annotated members. A `[DefField]` also gets a typed public property.

### Example: initialise item charges from its definition

The def holds the configured maximum; each item holds its own remaining charges. Connect
them in `On_Create`, which initialises a newly created item:

```csharp
[ThingTemplate]
public partial class ChargedBeacon : Item {
    [SaveableData, CopyableData]
    private int charges = 0;

    public override void On_Create() {
        base.On_Create();
        this.charges = this.TypeDef.MaxCharges;
    }
}

[ThingDefTemplate]
public partial class ChargedBeaconDef : ItemDef {
    [DefField(typeof(int), DefaultExpression = "9")]
    private FieldValue maxCharges;
}
```

The generator supplies the typed `ChargedBeacon.TypeDef` accessor and the def's
`int MaxCharges` property. It also generates save/load and copy handling for `charges`.
The assignment in `On_Create` is your code: the generator does not infer a relationship
between fields named `charges` and `maxCharges`.

Content can override the def's default:

```ini
[ChargedBeaconDef i_charged_beacon]
name = charged beacon
model = 0x0e2d
maxCharges = 12
```

A new item created from this definition gets **12 charges**. A definition that omits
`maxCharges` uses **9**, the `[DefField]` default.

| Declaration | Typical default here | Why |
|---|---|---|
| Item's `charges` | `0` | Neutral instance/storage default. `On_Create` replaces it with the configured maximum for a new item. |
| Def's `maxCharges` | A sensible content default, here `9` | Used when the definition does not explicitly configure a maximum. |

Do not repeat the configured starting count in the item's field initializer. That initializer
is also involved when constructing objects for loading; it is not the place to refill them.
The generated saver can omit a field equal to its declared default, so an exhausted item's
omitted `charges` must reconstruct as zero. Loading restores saved state, and copying preserves
the source's remaining charges; neither should refill the item from its def.

Changing `MaxCharges` later affects newly created items. It does not overwrite existing
items' remaining charges: those are independent instance state after `On_Create`.

### Example: a plugin that reacts to its holder's steps

Plugins attach **trigger-driven behaviour and per-holder state** to existing objects. This
one counts a character's completed steps. Two generators work together:

- **Thing and Plugin classes** generates the plugin/def pair and persistence/copy support.
- **Plugin trigger dispatch** connects `On_Step` to `@step`
  by generating the plugin's `Run(TriggerKey, ScriptArgs)` override at build time.

```csharp
[PluginTemplate]
public partial class StepCounterPlugin : Plugin {
    [SaveableData, CopyableData]
    private int steps = 0;

    public void On_Step(Direction direction, bool running) {
        this.steps++;
    }
}

[PluginDefTemplate]
public partial class StepCounterPluginDef : PluginDef {
}
```

Define an instance of the plugin def in content:

```ini
[StepCounterPluginDef p_step_counter]
```

After definitions have loaded, attach it to the intended character from gameplay/setup code:

```csharp
character.AddNewPlugin(
    PluginKey.Acquire("step_counter"),
    PluginDef.GetByDefname("p_step_counter"));
```

The character's `@step` now reaches the plugin's handler. Its `direction` and `running` arguments
come from the event payload (`Argv` 0 and 1; the third, the point left, isn't taken here); this
example counts walking and running alike. The event represents completed steps, not teleports
(#1558: raised after the move, GMs included). Each installed plugin keeps its own
counter, saved with its holder and copied through the marked field.

The template generator does not attach the plugin, and the dispatch generator does not invent
or raise events. Attachment and the holder's event-raising code complete the connection.
Use normal `AddNewPlugin`, not the `AsSimple` variant, when the plugin must hear holder events.
Cache the `PluginKey` in production code; adding at an occupied key replaces the old plugin.

For plugin handlers, there is **no leading `self` parameter**: use `this.Cont` for the holder.
A single `ScriptArgs` parameter receives the argument object; other parameters map from the
event's `Argv`. This differs from Compiled TriggerGroup handlers, whose first parameter is
the receiver. Plugin classes with handlers must be `partial`; keep them non-nested and use
unique event names. Public `On_*` methods, as shown, make the event interface explicit.

**Keep in mind:** both halves are `partial`, and the instance/def names form a matching pair.
Template fields may be private. Saving and copying are independent opt-ins; marking a member
with one attribute does not imply the other.
Keep template names unique and suffix class names with "Plugin" / "PluginDef" (by convention).

## 2. Compiled ScriptHolder: expose a named script function

**Use this when** LScript or the command infrastructure should be able to invoke a C# method
by name. The generator creates and registers the function holder, so you do not write a
wrapper class for each function.

### Example: report the caller's position

```csharp
public static class LocationFunctions {
    /// <summary>Show the calling character's coordinates.</summary>
    [SteamFunction("ReportLocation")]
    public static void ReportLocation(Character self) {
        self.SysMessage($"Position: {self.X}, {self.Y}, {self.Z}");
    }
}
```

With command authorization permitting it, a character can invoke `.ReportLocation`. The
method uses the character supplied as `self`; it does not look up the last ambient sender.
This example is intended for character-context calls, not a console call with another kind
of receiver.

The registered name comes from `[SteamFunction("name")]`, or the method name when no explicit
name is supplied. It must start with an ASCII letter or underscore and contain only ASCII
letters, digits and underscores. XML documentation becomes the function's description.

**Keep in mind:** methods must be `public static`, and function names are globally unique,
case-insensitively. The first parameter receives `self`; subsequent parameters normally come
from the invocation's arguments. A two-parameter `(self, ScriptArgs)` form can receive the
argument object directly. Command authorization and argument parsing remain separate concerns:
generation neither grants permissions nor makes both command parsers behave identically.
Avoid names colliding with properties or methods on the receiver.

## 3. Compiled TriggerGroup: respond to events in C#

**Use this when** you want reusable behaviour that can be attached as a trigger group—for
example a welcome message, a shared action policy or an event reaction. Use a plugin instead
when each holder needs its own mutable behaviour state.

The generator turns public `On_*` methods into trigger dispatch on the same partial class.
You write the handlers rather than a `Run` method that switches over trigger keys.

### Example: welcome a character on login

```csharp
public partial class E_Welcome : CompiledTriggerGroup {
    public TriggerResult On_Login(Character self) {
        self.SysMessage("Welcome back!");
        return TriggerResult.Continue;
    }
}
```

Attach the group to the intended character through the existing trigger-group API, for example
from server-side setup code after script registration, where `character` is a `Character`:

```csharp
character.AddTriggerGroup(SingletonScript<E_Welcome>.Instance);
```

Once attached, the group receives that holder's events. Declaring the class alone does not
make it a global login handler. `On_Login` maps to `@login`; returning `Continue` allows the
remaining cancellable login processing to continue.

**Keep in mind:** the class must be concrete, non-sealed and `partial`; enclosing types must
also be partial if it is nested. Handlers are public instance methods, with unique event
names within the group, ignoring case. Their first parameter receives the event receiver.
With exactly two parameters and a `ScriptArgs`-compatible second parameter, that parameter
receives the argument object; otherwise the remaining parameters map from `Argv`.

Use the signature for the actual event. Some events pass a specialised `ScriptArgs` object;
others wrap a payload inside it. Also check the event's cancel contract: cancel can mean
“handled”, “refused” or “skip default checks”, depending on the caller.

## 4. Save Implementor: persist a separate data object

**Use this when** an ordinary C# object needs to survive world saving and loading, but is not
itself a Thing/Plugin. Examples include quest progress, spawn configuration and other state
held by a saved object.

The generator creates the save/load implementation. You describe the data and how a fresh
instance is constructed during loading.

### Example: quest progress stored on a character

```csharp
[SaveableClass]
public sealed class QuestProgress {
    [SaveableData]
    public int CompletedStages { get; set; }

    [SaveableData]
    public int RequiredStages { get; set; } = 5;

    [LoadingInitializer]
    public QuestProgress() {
    }
}
```

Attach an instance to a saved holder from your gameplay code:

```csharp
character.SetTag(TagKey.Acquire("quest_progress"), new QuestProgress());
```

Saving the character now reaches the progress object through its tag. On load, the engine
constructs it using the loading initializer and restores the marked properties. In production
code, cache a fixed `TagKey` rather than acquiring it repeatedly.

The attribute supplies serialization support; it does not make every instance a world-save
root. An unreferenced local `new QuestProgress()` is not automatically saved.

**Keep in mind:** generic `[SaveableData]` members must be public instance fields or properties
with public getters and setters (this differs from thing/plugin template fields, which can be private).
Saveable type names must be globally unique, case-insensitively. `[SavedMember]` belongs to a
different mechanism for static state; use `[SaveableData]` here.

For custom formats, add `[Save]` and `[LoadLine]` methods alongside the loading initializer.
Use `[LoadingFinalizer]` for post-load fixups. Alternatively, `[LoadSection]` lets a constructor
or static factory receive the whole `PropsSection`; it replaces the initializer/line-loader
path rather than being combined with it. Ordinary state objects usually need only the simple
pattern above.

## 5. DeepCopy Implementor: copy a separate data object

**Use this when** a data object participates in engine copying and the copy needs its own
mutable state. This is useful for configuration or progress objects attached to items that
can be duplicated. Saving an object does not automatically make it copyable.

The generator constructs the destination through your marked initializer, then copies the
annotated members through the engine's copy system.

### Example: copy spell charges and their history

```csharp
[DeepCopyableClass]
public sealed class SpellChargeState {
    [CopyableData]
    public int Charges { get; set; }

    [CopyableData]
    public int[] History { get; set; } = new int[0];

    [DeepCopyImplementation]
    public SpellChargeState() {
    }
}
```

With the script types loaded and their generated implementors registered, copy through the
engine API:

```csharp
var original = new SpellChargeState {
    Charges = 3,
    History = new[] { 1, 2 }
};

var copy = (SpellChargeState) DeepCopyFactory.GetCopy(original);
copy.History[0] = 99;
// original.History[0] remains 1.
```

A normal C# assignment would share the object. Calling the constructor yourself would create
an empty instance. `DeepCopyFactory` is what invokes the generated copying behaviour.
However, typically you don't need to call `DeepCopyFactory` directly; the engine calls it
for you when copying a holder that owns the object.

**Keep in mind:** mark exactly one constructor or static factory with
`[DeepCopyImplementation]`. It can be parameterless, as above, or take the source object of
the same type when construction needs it. Marked members are public instance fields or
properties with public getters and setters. Keep deep-copyable type names globally unique,
case-insensitively.

“Deep copy” follows the engine's rules, not “clone every reachable object”. Definitions and
certain other shared engine objects intentionally retain their identity. For custom nested
mutable classes, provide copy support as well.

### When an object needs both saving and copying

Combine the two independent mechanisms on the same ordinary data class:

```csharp
[SaveableClass, DeepCopyableClass]
public sealed class RewardState {
    [SaveableData, CopyableData]
    public int ClaimedCount { get; set; }

    [LoadingInitializer, DeepCopyImplementation]
    public RewardState() {
    }
}
```

The same constructor can initialise a loaded object or a copy. Each member still needs the
attributes for the operations it should participate in.

## Day-to-day workflow

1. Pick the pattern matching the task and write the declarations in the scripts project.
2. Build through the normal project workflow; do not edit generated files.
3. Make the result reachable: configure a def, attach a group/plugin, call a function, or
   reference a data object from a saved/copied owner.
4. Exercise/test the relevant operation. For persistence, save and reload; for copying, mutate
   the copy and check that state intended to be independent is independent.

Generated Roslyn files can be inspected under the project's analyzer/generated-source view
in the IDE. They are implementation output,  not another source file to maintain.
