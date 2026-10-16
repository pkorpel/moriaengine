# LScript

LScript is the Sphere-derived scripting language embedded in this engine. It lives inside
trigger bodies in `.scp`/`.def` content files and inside raw-LScript dialog definitions
(`[dialog ...]` sections) - anywhere game content needs to run logic without being compiled
C#. Source text is parsed into an AST of `OpNode` subclasses (`GameCore/LScript/OpNode*.cs`)
and that AST is run once per trigger invocation; `LScriptMain.GetNewSnippetRunner` is the
entry point used both by real content loading and by this engine's own test harness when it
needs to execute a raw snippet directly.

## The `{a,b}` random-range operator only ever produces integers

`{a,b}` is LScript's inline random-range syntax. Its actual roll happens in
`OpNode_Final_RandomExpression_Simple_Variable.Run` (`GameCore/LScript/
OpNode_Final_RandomExpression.cs:71-74`):

```csharp
int lVal = Convert.ToInt32(this.leftNode.Run(vars), CultureInfo.InvariantCulture);
int rVal = Convert.ToInt32(this.rightNode.Run(vars), CultureInfo.InvariantCulture);
```

Both operands are coerced through `Convert.ToInt32` before the roll, unconditionally - there
is no code path in which this operator can return a genuinely fractional value, regardless
of what type the operands are. This was verified directly (not assumed) by running `{1,2}`
2000 times via `LScriptMain.GetNewSnippetRunner` with the compiled AST reused across calls
(mirroring how a real trigger's AST is reused across a gem's lifetime) and observing a clean
50/50 split between exactly `1` and `2` - confirming the mechanism is unbiased on a
two-outcome range, but also confirming it can never produce `1.5`. Any feature that needs a
real decimal random range (e.g. the spawn-timer decimal-minutes feature, #210) has to roll
in C# directly and cannot reuse this operator.

## Parser quirks found while building content this session

- **The local variable/argument name `info` is reserved/conflicting in LScript's grammar.**
  `arg info = SpawnInfo(...)` produces confusing, cascading parse errors that appear to point
  at unrelated code much further down (often a `for`/`endfor` block several lines later) -
  the real problem is the variable name itself. Renaming to anything else (`spawnData`,
  `result`) fixes it instantly with no other change. Avoid `info` as an LScript arg/local
  name.

- **A separately-invoked `[function]` does NOT inherit the calling trigger's `argtxt`/
  `argnum`.** Each function call gets its own fresh `ScriptArgs` (`ArgTxtHolder`/
  `ArgNumHolder` are populated per-invocation, not inherited by callees). Factoring a
  gump-response-reading loop out into a shared `[function]` silently reads back zero entries
  every time - no exception, no error, just empty data - because `argtxt[id]`/`argnum[id]`
  inside the function resolve against the function's own empty context, not the caller's
  real gump response. The fix is to inline the loop directly into each response handler
  rather than factoring it into a shared function.

- **The `<...>` eval-bracket syntax can only wrap a `DottedExpressionChain`** (a
  property/method-access chain like `a.b.c()`), not general arithmetic. `<100+arg.i*2>` is a
  parse error; the fully bare form `100+arg.i*2` (no brackets at all - `arg.i` resolves fine
  un-bracketed in an arithmetic position) is correct.

- **`for (name, start, end)` loop bounds DO accept full dotted expressions** as a bound
  (e.g. `for (arg.i, 0, someObject.SomeProperty)`) - this part works fine on its own; it's
  easy to mistakenly blame this syntax for a parse error that's actually caused by something
  else nearby (see the `info` variable-name issue above - that one has been mistaken for a
  loop-bounds problem before).

## `[SteamFunction]` dispatch and the two command parsers

A chat command (`.something`) is dispatched by `Commands.InvokeCommand`
(`GameCore/Commands.cs:168-225`), which picks between two completely different parsers based
on the connecting account's privilege level, compared against `Globals.
PlevelForLscriptCommands` (ini key `plevelForLscriptCommands`, default `2`,
`GameCore/Globals.cs:546`):

- **`commandSrc.MaxPlevel < PlevelForLscriptCommands`** → `SimpleCommandParser.
  TryRunSnippet` (`GameCore/Commands.cs:243` onward). Its regex is
  `(?<name>\w+)(\s+(?<arg>.+))?` (`GameCore/Commands.cs:244`) - everything after the command
  name becomes **one single argument string**, with no splitting on spaces at all, and
  quotes are not stripped (a quoted argument arrives containing the literal quote
  characters).
- **`commandSrc.MaxPlevel >= PlevelForLscriptCommands`** → the full LScript parser
  (`LScriptMain.GetNewSnippetRunner`, cached per `(self type, code)` pair in
  `gmCommandsCache`). This genuinely splits multiple space-separated arguments, but it does
  so by compiling and running the WHOLE command line as an LScript snippet against `self` -
  meaning full LScript member-resolution (property/method lookup on `self`) is in play, not
  just argument parsing.

### The real collision this causes (#166)

If a command's registered name collides with an existing property or method name on the
type it runs against, the full-LScript path resolves the bare word as a **property read on
`self`** instead of invoking the command at all - via LScript's general member-resolution
path (`GameCore/LScript/OpNode_Lazy_Expression.cs`), not the command dispatcher. There is no
exception: the snippet "runs" successfully (it evaluates to the property's value), the
command log shows `'account' commands 'Race'. OK`, and nothing the command was supposed to
do ever happens. `InvokeCommand` does have a trivial-result check (`GameCore/Commands.cs:
195`, rejecting `OpNode_Constant`/`OpNode_This`/`OpNode_Object` results as "the command does
nothing") but a property read doesn't match any of those node types, so it slips through
clean.

This was found for real: a new `[SteamFunction("Race")]` command silently never ran once
`Player` gained its own `Race` property, with zero indication anything was wrong beyond the
command visibly doing nothing. It was root-caused only after a literal first-line
`Console.WriteLine` inside the command method proved the method body was never entered at
all. The fix applied was renaming the command (`"Race"` → `"ChooseRace"`), not a root-cause
engine fix - the underlying shadowing behavior is still live for the next person who picks a
colliding name. **When adding a new `[SteamFunction]`, pick a name that cannot collide with
any existing property or method on the type(s) it will be invoked against** (usually
`Character`/`Player`, since most commands run with `self` bound to the calling character).

## The practical consequence for parsing multiple values in test/diagnostic commands

Because of the split above, a command reading more than one value from its arguments has to
tolerate **three different shapes** depending on who's calling it and how many values there
are:

1. A real split-args array (elevated account, genuinely multiple space-separated tokens).
2. A single un-split combined string (either account tier, when there's effectively one
   token - or any account under the Simple parser regardless of spaces).
3. A boxed primitive (elevated account, a literally single numeric argument, which the
   LScript parser evaluates to a boxed `int`/`double` rather than a string).

The practical workarounds used throughout this codebase's own `Test*` diagnostic commands:
- **Comma-join multiple values into one token with no spaces** (e.g. `.TestTeleportTo
  1234,5678,20`) - sidesteps the whole ambiguity, since there's nothing for either parser to
  split on either way.
- **Quote word-like arguments, but only for elevated accounts.** The Simple parser doesn't
  strip quotes, so quoting there corrupts a literal value by leaving the quote characters in
  it; quoting is only correct on the LScript-parsed path, and only needed there to stop a
  bareword from being resolved against a live registered object (any word matching a real
  `AbstractScript` defname - an item, a skill, etc. - gets silently resolved into that live
  object's own string form instead of staying literal text).
