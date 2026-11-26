# Architecture overview

moriaengine is a .NET 10 port of SteamEngine, a C# Ultima Online server emulator built on
the Sphere scripting model. This page is the 30,000-foot view; deeper detail lives in the
pages linked from each section.

## The two real server processes

A running shard is **two separate OS processes**, each with its own listening port
(configured in `steamengine.ini`/`steamaux.ini`):

- **AuxiliaryServer** (`AuxiliaryServer/`) - the login server. Listens on port `2593` for
  real UO client connections (the classic "login server" protocol: account/password,
  server list, pick-a-shard) and on `2594` for RemoteConsole connections. It does not run
  any gameplay - its only job for a game client is to authenticate and then redirect the
  client to a GameCore instance.
- **GameCore** (`GameCore/`) - the actual game world. Listens on port `2595`
  (`[ports] game` in `steamengine.ini`) for the client connection AuxiliaryServer redirected.
  Everything gameplay-related - world state, scripts, items, characters, combat, the
  database of saved content - lives in this one process.

A real client's connection sequence is therefore: connect to AuxiliaryServer:2593 → log in
→ receive a server list → pick one → get redirected (new TCP connection) to that server's
GameCore:2595 → character list → enter the world. The test harness
(`Tests/Protocol/TestLoginClient.cs` / `TestGameClient.cs`) exercises this exact two-hop
flow for every protocol test that needs a real character in the world. See
[Networking & the protocol layer](../02-engine-core/networking-protocol.md) for the packet
level detail. The [component interaction diagram](../../wip/diagrams/component-interaction.md) shows the same picture.

There's also a third, optional tool, **RemoteConsole** (`RemoteConsole/`) - a GUI client for
the remote-console protocol on port 2594. It targets `net10.0-windows` (WinForms) and is in
`SteamEngine.sln` since #29, so it compiles in every build, Linux and CI included, but it
only runs on Windows.

## Top-level projects and what they're for

See the [assembly dependency diagram](../../wip/diagrams/assembly-dependencies.md) for how they reference each other.

| Project | Role |
|---|---|
| `GameCore` | The engine itself: world model, networking, LScript interpreter, region/timer/persistence infrastructure, and the script-compilation pipeline described below. |
| `Common` | Shared utility code with no engine-specific dependencies (logging, misc helpers) used by both GameCore and AuxiliaryServer. |
| `Communication` | The low-level TCP connection/packet-framing layer shared by both server processes. |
| `scripts` (`SteamEngine.Scripts` project) | Game content and gameplay logic, built into a `SteamEngine.CompiledScripts` assembly. GameCore builds it when needed or reuses the last build at boot. See [Gameplay systems](../README.md#4-gameplay-systems). |
| `Generators.Abstractions` | Attributes shared by GameCore, scripts and the source generators. |
| `Generators` | Roslyn source generators that add code during GameCore and scripts compilation. See [Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md). |
| `Generators.Tests` | xUnit tests for the source generators. |
| `Tests` | The xUnit test suite. Two tiers: fast unit-style tests at the top level, and `Tests/Protocol/` - full end-to-end tests that boot real `AuxiliaryServer`/`GameCore` child processes and drive them with a real wire-protocol client. See [Test infrastructure](../07-testing/test-infrastructure.md). |
| `AuxiliaryServer` | The login-server process described above. |
| `SphereShardConverter` | A one-time offline tool that migrated the original Erebor shard's real Sphere content into moriaengine's own `.def`/`.scp` format (the `scripts/converted/` tree is its output). Not part of the running server. |
| `RemoteConsole` | The GUI remote-console client described above. |
| `ThirdParty` | Vendored client encryption (Twofish and UO login encryption) and the Grammatica parser runtime. |
| `Languages` | Data directory, not a project: per-language localization overrides (see [Localization](../05-localization.md)). |

## The Sphere-style content model, in brief

Game content combines C# classes and logic with Sphere-style text definitions. For example:

```
[ItemDef 0x9999]
DEFNAME = i_attr_flag_test
NAME = Attr Flag Test Item
ATTR = attr_static|attr_magic
```

Two script-file extensions are used for different *kinds* of content, while template class
shape is authored in attributed C# and compiled by the
[scripting generators](../02-engine-core/code%20generators/scripting-generators.md):

- **`.def`** - *definitions*: the actual `[ItemDef ...]`/`[CharacterDef ...]`/etc blocks
  that create real, loadable content (an item type, an NPC type, a house type...).
- **`.scp`** - everything else written in raw LScript: dialogs, trigger groups, standalone
  functions, and content that doesn't fit the `.def` block-per-template shape.

At boot, every `.def`/`.scp` file under `scripts/` is parsed and the content it describes
becomes available by its `DEFNAME` (e.g. `i_attr_flag_test`), ready to be instantiated into
a real in-world `Item`/`Character`/etc via `ThingDef.Create(...)`. See
[The Thing model](../02-engine-core/thing-model.md) for how a `.def` block turns into a
live object and [Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md)
for template generation flow.

## Boot sequence

At startup, `GameCore/CompiledScripts/CompilerInvoker.cs`'s `CompileScripts` method:

1. Initializes the core classes on the first compilation (`ClassManager.InitClasses`).
2. Builds `scripts/SteamEngine.Scripts.csproj` with MSBuild when needed, or reuses the last
   build when its inputs are unchanged ("Reused compiled C# scripts"; see
   [Reusing the last build](../02-engine-core/scripting-pipeline.md#reusing-the-last-build)).
3. Registers the resulting scripts assembly's classes (`ClassManager.InitClasses`).

All generated code comes from Roslyn source generators that run inside step 2's compile
(and inside any plain `dotnet build`); none of it is written to disk or checked in. See
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md).

Once script compilation succeeds, GameCore loads the actual game world
(regions, accounts, saved items/characters from the most recent `saves/<date>/<time>/`
snapshot) and starts listening for connections.
