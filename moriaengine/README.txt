MoriaEngine / SteamEngine
==========================

A SteamEngine/MoriaEngine server, ported to .NET 8 (cross-platform: Windows,
macOS, Linux).

The server consists of two processes:

  - GameCore: where all the UO gameplay happens.
  - AuxiliaryServer: secondary tasks - serves remote console connections and
    acts as a login server for the GameCore instance(s) running on the same
    machine.

There's also a third, optional tool:

  - RemoteConsole: a GUI remote-console client. Not yet ported to .NET 8 -
    it still only builds against the old .NET Framework toolchain
    (RemoteConsole/SteamEngine.RemoteConsole.csproj).


Building
--------

One script builds everything, on any OS:

    ./build.ps1

This requires PowerShell Core (`pwsh`) - it runs identically on Windows,
macOS, and Linux. (Windows PowerShell 5.1 works too.) Install pwsh if you
don't have it: https://github.com/PowerShell/PowerShell

Options:

    ./build.ps1 -Configuration Release     # Debug (default) or Release
    ./build.ps1 -Clean                      # wipe bin/obj first
    ./build.ps1 -Run GameCore               # build, then run GameCore
    ./build.ps1 -Run AuxiliaryServer        # build, then run AuxiliaryServer
    ./build.ps1 -Run Both                   # build, then run both together

The first build takes a bit longer than later ones: scripts/Generated's code
is produced at runtime by GameCore itself (ClassTemplateParser, compiling
the .ct templates under scripts/), not checked into source control, so the
script runs GameCore briefly to generate it before building the full
solution. This only happens once - subsequent runs skip it automatically.

There is no "Sane" or "Optimized" build anymore - only the standard .NET
Debug/Release split. (Older versions of this doc described three build
types; that was from a pre-.NET-8 NAnt/old-MSBuild-era scheme that never
survived the conversion to SDK-style projects.)


Running for the first time
---------------------------

Run GameCore once (`./build.ps1 -Run GameCore`, or just the built binary
directly). It will write a default `steamengine.ini` and exit - open it,
adjust anything you want (settings are commented inline), then run GameCore
again. Run AuxiliaryServer the same way to get `steamaux.ini`.

For a real game server, run both together: `./build.ps1 -Run Both`. Then
connect with either a UO client or RemoteConsole - with either, logging in
with any username/password creates that account with admin privileges.

RemoteConsole supports adding buttons as shortcuts for common commands; see
steamrc.example.ini for a recommended starter set you can copy from.


UO client data
---------------

GameCore needs real Ultima Online client files (map0.mul, statics0.mul,
tiledata.mul, etc.) to fully boot - see `mulPath` in steamengine.ini. This
repo's `uo-files/` directory holds a set of these; point `mulPath` at it
(e.g. `mulPath = ../uo-files` when running from the moriaengine/ directory)
or supply your own.


CI
--

`.github/workflows/dotnet.yml` runs the exact same `build.ps1` plus the
test suite on every push/PR to main.
