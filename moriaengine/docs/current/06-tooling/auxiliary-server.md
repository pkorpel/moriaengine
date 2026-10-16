# AuxiliaryServer

The login-server process (`AuxiliaryServer/`, namespace `SteamEngine.AuxiliaryServer`) -
a separate OS process from [GameCore](../02-engine-core/thing-model.md), run as its own
executable. See [Architecture overview](../01-overview/architecture-overview.md) for how
this fits into a running shard's two-process topology; this page covers the process
itself in more detail.

## Responsibilities

`MainClass.cs` is the entry point. `Init()` starts four independent listeners/services:

1. **`LoginServer.LoginServer`** (`LoginServer/LoginServer.cs`) - the real UO client-facing
   login protocol: account/password, server list, pick-a-shard. A thin
   `TcpServer<LoginClient>` subclass (the same generic TCP server base class used
   throughout this codebase's networking layer). This is the *only* piece a real game
   client ever talks to directly on this process - once a client picks a server, it's
   redirected to that `GameCore` instance's own port and AuxiliaryServer is out of the
   picture for that connection.
2. **`ConsoleServer.ConsoleServer`** - the remote-console protocol listener (port
   `2594`), what [RemoteConsole](remote-console.md) connects to.
3. **`SEGameServerServer`** (`SEGameServers/`) - the side of the protocol a running
   `GameCore` process itself connects back to, so AuxiliaryServer knows which game
   servers currently exist and can route login redirects to them.
4. **`SphereServerClientFactory`** (`SphereServers/`) - support for the original
   Sphere-protocol server type, kept alongside the SE (SteamEngine/GameCore) protocol -
   a legacy compatibility path from before this was a from-scratch rewrite.

`GameServersManager` is the in-memory registry tying these together: which `GameServer`
instances exist (`gameServersByUid`/`gameServersByIniID`), and which `ConsoleClient`
sessions are currently "logged in" to which game server (`consoles`/`gameServers`
dictionaries) - a many-to-many relationship, since one console can watch multiple
servers and one server can be watched by multiple consoles.

## Process lifecycle

`MainClass.Main()` runs `Init()`, then blocks on a background thread reading stdin for a
plain `Console.ReadLine()`-triggered exit, backed by a shared `CancellationTokenSource`
(`ExitSignalToken`) - the same cancellation-token-based shutdown signal pattern other
long-running loops in this codebase (test fixtures, background timers) key off of.
`Exit()` disposes the three network services in order.

## Starting a GameCore instance

`SEGameServerSetup`/`SphereServerSetup` + `AuxServMsBuildProjectStarter` handle actually
launching a `GameCore` child process (building it via MSBuild first if needed) - this is
how `AuxiliaryServer` can offer a fresh, not-yet-running shard as a login destination
rather than only brokering already-running ones. The test harness
(`Tests/Protocol/GameServerPairFixture`, see
[Test infrastructure](../07-testing/test-infrastructure.md)) uses a similar real-process-
spawning approach for its own full-boot-tier tests, independent of this production
starter code.

## Current status

This is the piece of the stack a real deployed shard actually runs continuously
alongside `GameCore` - not a one-time tool like
[SphereShardConverter](sphere-shard-converter.md), and (unlike
[RemoteConsole](remote-console.md)) fully part of the current .NET 8 build and the
protocol-test suite's own two-hop login flow.
