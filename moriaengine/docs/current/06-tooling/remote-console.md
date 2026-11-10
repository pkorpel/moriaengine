# RemoteConsole

A WinForms GUI client (`RemoteConsole/`, namespace `SteamEngine.RemoteConsole`) for the
remote-console protocol - lets an administrator connect to a running
[AuxiliaryServer](auxiliary-server.md) and drive/monitor a `GameCore` instance remotely,
without a real UO game client.

## Build

Since #29 it is an SDK-style project targeting `net8.0-windows` with `UseWindowsForms`, and
it is in `SteamEngine.sln`, so `build.ps1` and CI compile it with everything else.
`EnableWindowsTargeting` lets Linux and macOS compile it too (the Windows Desktop reference
pack comes from NuGet on restore); it only *runs* on Windows. A Windows build puts
`SteamEngine.RemoteConsole.exe` in `RemoteConsole/bin/<Configuration>/net8.0-windows/`; a
Linux/macOS build makes no launcher (`UseAppHost` off there), only the dll, which runs on
Windows as `dotnet SteamEngine.RemoteConsole.dll`.

At start it moves to the folder holding `steamrc.example.ini` (`moriaengine/`), looking up
from the exe, unless it was started there: `steamrc.ini` (known endpoints, link programs, saved
passwords by default, so it's in `.gitignore`) is read there, and the server log's relative
file links (`.\scripts\...`) resolve against it.

What the port changed (WinForms on .NET 8 dropped or changed these):
- `MainMenu`/`MenuItem` became `MenuStrip`/`ToolStripMenuItem` (the client area grew by the
  strip's height so the tabs keep their size), and the empty `ContextMenu` on
  `ExtendedRichTextBox` became an empty `ContextMenuStrip`.
- Fonts and colours: Common lost `System.Drawing` in the earlier .NET 8 port, so
  `LogStyleInfo` only carries an RGB `LogColor` and bold/italic; `LogStrDisplay` makes the
  `Font` (Sans Serif 8.25, as Common did before) and the `Color` itself.
- `Application.SetDefaultFont(Microsoft Sans Serif 8.25)`, the font the designer layouts were
  made for (.NET 8's default is Segoe UI 9).
- Clicking a file link opens it with `UseShellExecute = true`, the link program too (.NET 8
  doesn't use the shell by default; the shell opens a file with its program and finds programs
  registered without a path).
- `Tools.ExitBinDirectory()` gave way to the `steamrc.example.ini` lookup above: it only knew
  the old `..\build\Debug` output folder.
- `[assembly: SupportedOSPlatform("windows")]` in `AssemblyInfo.cs`, since
  `GenerateAssemblyInfo` is off.

## Structure

- `ConsoleClient` (`ConsoleClient.cs`) - the actual network connection, implementing
  `IConnectionState<TcpConnection<ConsoleClient>, ConsoleClient, IPEndPoint>` (the same
  generic connection-state interface pattern used throughout this codebase's networking
  layer - see [Networking & the protocol layer](../02-engine-core/networking-protocol.md)
  for the general shape). A static `ConnectedInstance`/`IsConnected` pair tracks the
  single active connection - this tool only ever maintains one console session at a
  time, unlike a game server handling many simultaneous player connections.
- `IncomingPackets.cs`/`OutgoingPackets.cs` - the remote-console wire protocol itself,
  mirroring the incoming/outgoing packet split convention used by every other networked
  component in this codebase.
- `MainForm`/`CommandLineDisplay`/`LogStrDisplay`/`ExtendedRichTextBox` - the actual GUI:
  a command-line-style input plus a scrolling log display per connected game server.
- `ConnectionForm`/`StartGameForm` - dialogs for connecting to an `AuxiliaryServer` and
  for starting/selecting which game server to attach to.
- `Settings.cs` - persisted client-side connection settings (last-used host, etc).

## Relationship to AuxiliaryServer

RemoteConsole connects to [AuxiliaryServer](auxiliary-server.md)'s console-server
listener (port `2594`, per `MainClass.cs`'s `ConsoleServer.Init()`), not directly to a
`GameCore` process - `AuxiliaryServer` brokers the relationship between connected
consoles and the `GameServer` instances it's tracking (see `GameServersManager` in
[AuxiliaryServer](auxiliary-server.md)).

## Current status

Compiles in every build, but no automated test runs it (there's no headless way to drive a
WinForms app), and the .NET 8 port was compiled on Linux, never started on Windows. Its wire
protocol matches `AuxiliaryServer/ConsoleServer` (same packet ids and fields both ways).
Treat changes here as untested unless manually verified on Windows. Worth checking first there:
.NET 8's RichTextBox is the newer rich edit control, and the log's `[+]` expand links rely on
`LinkClicked` reporting the hidden `#N` text after the link (`ExtendedRichTextBox.InsertLink`,
`LogStrDisplay.TryUncontract`).
