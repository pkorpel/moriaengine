# Copilot instructions for MoriaEngine

## Repository shape

- Most active work is in `moriaengine/`, not in the dormant `Cruncher/`, `MoriaUpdater/`, or `OldUpdater/` folders.
- The core server is split into:
  - `GameCore/`: gameplay, world simulation, scripting, server logic
  - `AuxiliaryServer/`: login server and remote-console support
  - `Common/`, `Communication/`, `ThirdParty/`: shared infrastructure and dependencies
  - `Generated/` and `scripts/Generated/`: generated source output, created during the build
  - `Tests/`: xUnit suite; includes the fast unit tier and the slower full-boot protocol tests
  - `RemoteConsole/`: Windows GUI remote-console client
- The canonical solution file is `moriaengine/SteamEngine.sln`.

## Build, test, and validation

Use the repo’s build script as the source of truth for local builds. The script is cross-platform and handles the one-time script-generation step.

```bash
cd moriaengine
pwsh ./build.ps1
pwsh ./build.ps1 -Configuration Release
pwsh ./build.ps1 -Run Both
```

The project does not use a separate “Sane/Optimized” build model; the current build split is simply `Debug` vs `Release`.

Fast validation (default CI gate):

```bash
cd /workspaces/moriaengine
scripts/test-fast.sh
```

This runs:
- `./build.ps1 -Configuration Debug`
- `./build.ps1 -Configuration Release`
- the fast xUnit tier only (`FullyQualifiedName!~SteamEngine.Tests.Protocol&FullyQualifiedName!~SteamEngine.Tests.Perf.`)

Full regression suite (real server boot tests; slower and intentionally not the default pre-push path):

```bash
cd /workspaces/moriaengine
scripts/test-full.sh
```

Single test example:

```bash
cd /workspaces/moriaengine
dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug --filter "FullyQualifiedName~SomeSpecificTestName"
```

For a filtered fast-tier run, use a pattern like:

```bash
cd /workspaces/moriaengine
dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug --filter "FullyQualifiedName!~SteamEngine.Tests.Protocol&FullyQualifiedName!~SteamEngine.Tests.Perf."
```

There is no dedicated repo-wide lint command; build and test scripts are the project’s validation gates. If you add a fix or behavior change, prefer the existing script-based checks rather than inventing ad hoc commands.

## High-level architecture

The engine is a multi-process UO shard server:

- `GameCore` does the actual shard simulation: NPCs, world state, combat, skills, scripting, persistence.
- `AuxiliaryServer` handles login and remote console plumbing for the GameCore process.
- `RemoteConsole` is a WinForms client used to connect to the server; it is optional and Windows-only at runtime.
- `scripts/` contains the game’s scripting and generated outputs. The generated code comes from `.ct` templates and `[SteamFunction]`-annotated methods; it is not meant to be edited by hand.
- `uo-files/` contains checked-in Ultima Online assets used by the server at runtime (`map0.mul`, `tiledata.mul`, etc.). These aren’t build artifacts; they are required for a real boot.

The build script is intentionally opinionated and is part of the architecture:

- it restores and builds the solution
- it regenerates `scripts/Generated` when templates or script sources changed
- it patches `steamengine.ini` to point `mulPath` at `../uo-files` on fresh clones
- it can run the server locally with `./build.ps1 -Run Both`

## Key repo conventions and gotchas

- `scripts/Generated` and other generated files are runtime output, not hand-maintained sources. If you change a `.ct` template or update a script with `[SteamFunction]`, rebuild so the generator refreshes the generated code.
- `steamengine.ini` and `steamaux.ini` are local runtime config files, not intended to be committed. The build script auto-adjusts the default mul path to the repo’s checked-in `uo-files/` folder.
- The repo intentionally keeps a local “first-boot” flow: `GameCore` writes default config and exits once, then you adjust settings and start again.
- `scripts/run-local.sh` is the local shard runner. It starts `AuxiliaryServer` on port `2593` and `GameCore` on port `2595`; the full-boot tests use the same ports and expect them to be free.
- The fast/full split is real and important: `SteamEngine.Tests.Protocol` is the slow real-server tier and is intentionally excluded from the default fast path. Do not assume the whole suite is cheap.
- The repo’s local automation already expects `scripts/test-fast.sh` to be a standard quick validation hook (`.claude/settings.json` / `.mcp.json` reference it). Keep changes consistent with that workflow.

## Working conventions

- Prefer repo-native scripts and build entry points over custom ad hoc commands.
- Keep changes focused inside `moriaengine/` unless the task is clearly about the dormant legacy tooling.
- If a change affects boot behavior, protocol behavior, or generated script code, validate with the matching test tier instead of only running a single smoke check.
- Do not hand-edit generated code; fix the source template or method and let the generator recreate it.
