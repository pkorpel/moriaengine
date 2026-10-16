# RemoteConsole

A WinForms GUI client (`RemoteConsole/`, namespace `SteamEngine.RemoteConsole`) for the
remote-console protocol - lets an administrator connect to a running
[AuxiliaryServer](auxiliary-server.md) and drive/monitor a `GameCore` instance remotely,
without a real UO game client.

## Not part of the current build graph

As of this writing, RemoteConsole has **not** been ported to .NET 8 - it still only
builds against the old .NET Framework toolchain
(`RemoteConsole/SteamEngine.RemoteConsole.csproj`), so it's not part of the normal
Debug/Release build the other projects share (see
[Architecture overview](../01-overview/architecture-overview.md)). It's WinForms-based
(`MainForm`, `ConnectionForm`, `StartGameForm`, each with a `.Designer.cs` partner file),
which is itself a reason it hasn't followed GameCore/AuxiliaryServer onto .NET 8 - WinForms
on Linux/this sandbox isn't viable at all, so this project can only realistically be
exercised on Windows.

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

Functional for its intended Windows/.NET-Framework audience, but not exercised by any
automated test in this repo (there's no headless way to drive a WinForms app, and it's
not part of the cross-platform build target). Treat changes here as untested unless
manually verified on Windows.
