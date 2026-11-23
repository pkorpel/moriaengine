# LScript

LScript is the embedded Sphere-derived language used in `.scp`/`.def` trigger bodies and
raw dialog definitions. Source is parsed into an `OpNode` tree reused across invocations;
`LScriptMain.GetNewSnippetRunner` creates a runner for an arbitrary snippet.

## Execution context

The receiver (`self`), trigger arguments and ambient sender are different concepts.
`ScriptArgs` belongs to an invocation. A separately invoked `[function]` does not inherit
the caller's gump-response `argtxt`/`argnum` collections. Read those collections in the response
handler or pass the needed values explicitly.

Use [trigger architecture](trigger-system-architecture.md) for handler argument mapping.
Do not assume a timer has a player sender: timer dispatch clears `Globals.Src`.

## Language constraints

| Construct | Rule |
|---|---|
| `{a,b}` | Integer random range. Both operands are converted through `Convert.ToInt32`; it cannot return fractional values. |
| Random lists with identifiers | Separate elements with commas: `{ colors_white, 9, color_gray_lt, 1 }`. Whitespace can be parsed as assignment. |
| `<...>` evaluation | Wraps a dotted expression chain, not general arithmetic. Write `100+arg.i*2`, not `<100+arg.i*2>`. |
| `for (arg.i, 0, obj.Count)` | Dotted expressions are supported as bounds. |
| Local/argument name `info` | Conflicts with the grammar; choose another name. The reported parse error can occur later than the cause. |

For fractional random timing, calculate in C# rather than relying on `{a,b}`.

## Commands use two parsers

`Commands.InvokeCommand` selects a parser using the caller's `MaxPlevel` and
`Globals.PlevelForLscriptCommands` (`plevelForLscriptCommands`, default 2).

| Caller | Parsing behaviour |
|---|---|
| Below the threshold | `SimpleCommandParser`: everything after the command name is one string; quotes remain literal. |
| At/above the threshold | Full LScript: evaluates the command against `self`, resolves members and can split/evaluate arguments. |

A command name colliding with a property or method on `self` can resolve as a member read
instead of invoking the command, even while the log reports success. Choose unambiguous
`[SteamFunction]` names. `ChooseRace`, rather than `Race`, avoids the `Player.Race` collision.

## Diagnostic command arguments

A command may receive multiple argument slots, one combined string, or a boxed numeric
primitive. Do not cast every argument to `string` or assume spaces were split.

Existing diagnostic commands often use one comma-joined token, such as
`.TestTeleportTo 1234,5678,20`, and parse its components explicitly. Quoting word-like values
is useful on the elevated LScript path to prevent defname resolution, but the simple parser
retains those quotes. Test with the privilege level that will actually call the command.

## Sources

[Commands](../../../GameCore/Commands.cs),
[LScript implementation](../../../GameCore/LScript),
[RandomExpressionParseTests](../../../Tests/RandomExpressionParseTests.cs),
[testing gotchas](../07-testing/known-gotchas.md).
