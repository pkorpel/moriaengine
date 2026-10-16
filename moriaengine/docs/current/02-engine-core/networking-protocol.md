# Networking & the protocol layer

## Two processes, two logins

A real UO client talks to **two separate server processes** in sequence:

1. **AuxiliaryServer** (port from `steamaux.ini`) handles account login: credentials, the
   server list, and server selection. Picking a server returns a redirect packet naming the
   real port to reconnect to.
2. **GameCore**, on that redirected port, handles the actual game session: a second login
   (`0x91`, game-server login - same credentials, different packet), the character list
   (`0xA9`), character creation/selection, and then the real game world.

Protocol tests in this repo (`Tests/Protocol/`) drive both legs explicitly - `TestLoginClient`
for the AuxiliaryServer leg, `TestGameClient` for the GameCore leg - because a real client
never skips either step.

## Disconnect, lingering, and the logout delay (#261)

A TCP close (abrupt network loss or a client-initiated `0x01` disconnect packet) only
tears down the `GameState`/connection - `GameState.On_Close` nulls its `character`/
`account` refs and fires `Trigger_LogOut()` (the `@logout` script trigger plus the
overridable `On_LogOut()`), but by itself does **not** remove the character from the
world. `AbstractCharacter.Flag_Disconnected` is the actual "not present in the world"
flag (`Map.Disconnected`/`Map.Reconnected` move a character into/out of a special per-
sector list, and `CanSeeImplementation` filters out anything `Flag_Disconnected`) - and
nothing sets it at disconnect time, only at full removal. `AbstractCharacter.IsLingering`
(`IsPlayer && !Flag_Disconnected && GameState == null`) describes exactly this in-between
state: fully present, fully visible/interactable, just orphaned from any connection.

`Player.On_LogOut()` closes that gap with a classic UO-style logout delay: a
`CharLingeringTimer` (`Player.cs`) starts for `LogoutSettings.logoutDelaySeconds` (default
60, GM-tunable via `.settings`'s "Logout" category) - its `OnTimeout` calls `Disconnect()`
if the character `IsLingering` still, which sets `Flag_Disconnected` and actually removes
it from view. A visual effect (gated by `LogoutSettings.showLogoutEffect`, default on)
plays immediately at disconnect/timer-start, not at the delayed removal - it signals to
nearby players that someone is in the process of logging out, not that someone randomly
vanished. `Player.On_LogIn()` deletes the pending timer unconditionally (a no-op if none
exists) - this fires for both a brand-new login and a reconnect to an already-lingering
character (`GameState.cs`'s `GetLingeringCharacter()` check), so reconnecting within the
window just resumes the same still-standing body with no further effect.

Real finding while building this: a complete, correct implementation of this
(`CharLingeringTimer` itself, checking `IsLingering` before calling `Disconnect()`) already
existed in `Player.cs`, just never wired up - it sat commented out with a hardcoded 5s
delay, no settings, no effect. The only work needed was wiring it to `LogoutSettings` and
adding the effect call.

## `GameState`: the per-connection session object

`GameState` (`GameCore/Networking/GameState.cs:36`) is a `TagHolder` representing one live
connection's session: the `Character` it's controlling, update/vision range bookkeeping
(`RequestedUpdateRange`/`UpdateRange`/`SyncUpdateRange`, `GameState.cs:229-271` - a player's
effective update range is the lower of their requested range and their character's
`VisionRange`), and target-cursor state.

### Target-cursor state

```csharp
private OnTargon targonDeleg;
private OnTargonCancel targonCancelDeleg;
private object targonParameters;

public void Target(bool ground, OnTargon targonDeleg, OnTargonCancel targonCancelDeleg, object targonParameters) {
    this.targonDeleg = targonDeleg;
    this.targonCancelDeleg = targonCancelDeleg;
    this.targonParameters = targonParameters;
    ...
}
```

(`GameState.cs:285-293`) - calling `Target` arms exactly one pending target-cursor response.
`targonParameters` is a plain `object`, so it can carry anything a target handler needs
across the round trip (a tuple of values, a previously-targeted object from an earlier step
in a multi-step target chain, etc).

### `HandleTarget` - and the visibility check that gates it

`HandleTarget` (`GameState.cs:307-...`) decodes the client's target-cursor response (packet
`0x6C`) and dispatches to whatever was armed via `Target`. The field order matters: cancel
responses are signaled by the client sending the sentinel `x=0xffff, y=0xffff, targetUid=0,
z=0, model=0` (`GameState.cs:318`), not a separate packet type.

The important detail for anything driving this over real protocol: for a non-ground target,
the handler is only invoked if visibility passes first -

```csharp
if (!targGround) {
    Thing thing = Thing.UidGetThing(targetUid);
    if (thing != null) {
        if (self.CanSeeForUpdate(thing).Allow) {
            targ(this, thing, parameter);
            return;
        }
    }
}
```

(`GameState.cs:326-332`). A character genuinely cannot target something they can't see via
the real protocol path - there is no bypass. This has real consequences for both gameplay and
testing: a GM needs real elevated plevel (not just a cosmetic `IsGM`-returning flag flip) to
target their own invisible test item, and any test character needs to actually be standing
near a ground item before a target/pickup attempt can succeed for the reason the test
intends - otherwise a "too far away" denial and the denial the test is actually checking for
look identical from the outside (see [known testing gotchas](../07-testing/known-gotchas.md)).

## Packet base classes

- **`GameIncomingPacket`** (`GameCore/Networking/IncomingPackets.cs:28`) - plain base for
  fixed-size incoming packets; `Read()` just decodes a known byte layout directly.
- **`DynamicLenInPacket`** (`IncomingPackets.cs:32-44`) - base for variable-length packets.
  `Read()` is sealed: it decodes a 2-byte block-size prefix, checks enough data has actually
  arrived (`NeedMoreData` if not), delegates the real decoding to the abstract
  `ReadDynamicPart(int blockSize)`, then seeks past the whole block regardless of how much
  `ReadDynamicPart` itself consumed - a subclass that reads less than the declared block size
  doesn't desync the stream.

Outgoing packets are built via pooled packet objects (`Pool<T>.Acquire()`) and often grouped
into a `PacketGroup` for one atomic send, broadcast to every connection that can currently
see the relevant object via `GameServer.SendToClientsWhoCanSee`.

### Worked example: `GumpMenuSelectionInPacket` (`0xB1`)

`GumpMenuSelectionInPacket.ReadDynamicPart` (`IncomingPackets.cs:1568-1594`) is a useful
concrete reference for the dynamic-packet shape, and for a detail that's easy to get wrong
when constructing this packet from a test harness:

```csharp
protected override ReadPacketResult ReadDynamicPart(int blockSize) {
    this.SeekFromCurrent(4);                    // focus uid, unused by the server
    this.gumpUid = this.DecodeInt();
    this.pressedButton = this.DecodeInt();

    int switchesCount = this.DecodeInt();
    this.selectedSwitches = new int[switchesCount];
    for (int i = 0; i < switchesCount; i++) {
        this.selectedSwitches[i] = this.DecodeInt();
    }

    uint entriesCount = this.DecodeUInt();
    this.responseTexts = new ResponseText[entriesCount];
    for (int i = 0; i < entriesCount; i++) {
        ushort id = this.DecodeUShort();
        int len = this.DecodeUShort() * 2;      // char count -> byte count (UTF-16BE)
        string text = this.DecodeBigEndianUnicodeString(len);
        ...
    }
}
```

The wire layout is: `focusUid(4, ignored)` → `gumpUid(4)` → `pressedButton(4)` →
`switchesCount(4)` → `switchId(4) × switchesCount` → `entriesCount(4)` →
`[id(2) + charCount(2) + UTF-16BE chars] × entriesCount`. The switches array sits **before**
the text-entries array - a test harness constructing this packet (e.g. to drive a checkbox
dialog) has to compute each entry's byte offset dynamically based on `switchesCount`, not
assume a fixed offset for where the entries begin.
