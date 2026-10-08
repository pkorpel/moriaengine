# MoriaEngine

This repo is the server for **Moria**, an Ultima Online shard (its kingdom/region is called "Erebor" in the shard's own lore). It's a Sphere-API-style UO server emulator, historically known as **SteamEngine**, recently ported to .NET 8 so it runs on Windows, macOS, and Linux.

Current focus: taking the engine from "boots cleanly" to a basic playable shard - login, movement, combat, skills, persistence - matching how Moria/Erebor actually played. Work is tracked via [GitHub Issues](https://github.com/pkorpel/moriaengine/issues); each issue maps to one phase or fix, closed with a reference to the commit that resolved it.

[![.NET Build](https://github.com/pkorpel/moriaengine/actions/workflows/dotnet.yml/badge.svg)](https://github.com/pkorpel/moriaengine/actions/workflows/dotnet.yml)

## Repo layout

| Path | What it is |
|---|---|
| [`moriaengine/`](moriaengine/) | **The server itself.** GameCore (gameplay) + AuxiliaryServer (login/remote-console) + the LScript scripting engine and all shard content. This is the actively developed part - see [`moriaengine/README.txt`](moriaengine/README.txt) for how to build and run it. |
| [`uo-files/`](uo-files/) | Real Ultima Online client data (`map0.mul`, `statics0.mul`, `tiledata.mul`, `sound.mul`, `multi.mul/idx`, `Bodyconv.def`) that GameCore reads at runtime (`mulPath` in `steamengine.ini`). Not something you build - just data the server needs to fully boot. |
| [`MoriaUpdater/`](MoriaUpdater/) | Python client auto-updater/patcher: distributes updated game files to players as binary diffs instead of full re-downloads, packaged into a Windows `.exe` via py2exe. |
| [`OldUpdater/`](OldUpdater/) | The predecessor to `MoriaUpdater/` - same purpose (Python-based binary-diff auto-updater), superseded by it. |
| [`Cruncher/`](Cruncher/) | A standalone C#/.NET Framework WPF desktop tool (circa 2011) for offline analysis of UO save files and server connection logs. |

**`MoriaUpdater/`, `OldUpdater/`, and `Cruncher/` are dormant** - no real changes since this repo's initial import, not covered by CI, and not part of the ongoing net8 port/shard-recovery effort. They're kept here for reference (and because `MoriaUpdater`/`OldUpdater` are what players actually used to patch their clients), but if you're here to work on the server, [`moriaengine/`](moriaengine/) is almost certainly what you want.

## Getting started

```
cd moriaengine
./build.ps1              # build everything (needs PowerShell Core - pwsh - on any OS)
./build.ps1 -Run Both    # build, then run AuxiliaryServer + GameCore together
```

Full details - first-run setup, UO client data, running tests, CI - are in [`moriaengine/README.txt`](moriaengine/README.txt).

## Project history

- Phases 0-8: ported the whole solution from .NET Framework to .NET 8 (see `git log` for the full history).
- Phase 9 onward: recovering actual gameplay - test harness, bug fixes, protocol-level tests, and eventually full content restoration. See the open/closed issues for current status.
