MoriaEngine / SteamEngine
==========================

A SteamEngine/MoriaEngine server, ported to modern .NET, now .NET 10 (cross-platform: Windows,
macOS, Linux).

The server consists of two processes:

  - GameCore: where all the UO gameplay happens.
  - AuxiliaryServer: secondary tasks - serves remote console connections and
    acts as a login server for the GameCore instance(s) running on the same
    machine.

There's also a third, optional tool:

  - RemoteConsole: a GUI remote-console client (WinForms, .NET 10). It builds
    with the rest of the solution everywhere but runs only on Windows:
    RemoteConsole/bin/Debug/net10.0-windows/SteamEngine.RemoteConsole.exe
    (a Windows build; a Linux/macOS build gives only the .dll, start it on
    Windows with `dotnet SteamEngine.RemoteConsole.dll`).


Building
--------

One script builds everything, on any OS:

    ./build.ps1

This requires PowerShell Core (`pwsh`) - it runs identically on Windows,
macOS, and Linux. (Windows PowerShell 5.1 works too.) Install pwsh if you
don't have it: https://github.com/PowerShell/PowerShell

Options:

    ./build.ps1 -Configuration Release     # Debug (default) or Release
    ./build.ps1 -Clean                      # clean scripts/AuxiliaryServer dependency outputs
    ./build.ps1 -Run GameCore               # build, then run GameCore
    ./build.ps1 -Run AuxiliaryServer        # build, then run AuxiliaryServer
    ./build.ps1 -Run Both                   # build, then run both together

Script source and generated C# are built together by dotnet using Roslyn source
generators; there is no separate runtime code-generation boot. If
steamengine.ini is missing, build.ps1 starts GameCore after the build to write
the default configuration and waits for it to exit.

There is no "Sane" or "Optimized" build anymore - only the standard .NET
Debug/Release split. (Older versions of this doc described three build
types; that was from a pre-.NET-8 NAnt/old-MSBuild-era scheme that never
survived the conversion to SDK-style projects.)


Running for the first time
---------------------------

The build script creates a default `steamengine.ini` when it is missing. Open
it and adjust any settings (commented inline) before starting GameCore with
`./build.ps1 -Run GameCore` or the built binary directly. Run AuxiliaryServer
to create `steamaux.ini`.

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
