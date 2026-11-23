# Networking and the protocol layer

## Session lifecycle

A real client first connects to AuxiliaryServer for account login and server selection.
The redirect leads to GameCore, which receives the game-server login (`0x91`), sends the
character list (`0xA9`) and handles character selection/creation and world entry.
Protocol tests use `TestLoginClient` and `TestGameClient` for these two legs.

`GameState` is a per-connection `TagHolder`: account/character references, update range and
one pending target request. Effective update range is bounded by the character's vision
range. It is not the persistent character object.

## Disconnect, lingering, and the logout delay (#261)

Closing TCP tears down the session and raises logout hooks; it does not itself remove the
character from the world. `Flag_Disconnected` controls world presence. `IsLingering` means
a player has no session but remains present and interactable.

`Player.On_LogOut()` starts `CharLingeringTimer`, using the Logout settings delay (default
60 seconds). Its callback checks `IsLingering || IsLeftByItsAccount` before disconnecting
the character. The optional visual effect occurs when logout begins. `On_LogIn()` deletes
the timer, so reconnecting during the delay resumes the still-present character.

Keep session closure, the logout notification and actual world removal distinct when
changing reconnect logic or testing visibility.

## Targeting

`GameState.Target` stores callbacks and an arbitrary parameter object for one pending
cursor response. `HandleTarget` decodes packet `0x6C`. Cancellation uses the sentinel
coordinates `x=0xffff, y=0xffff` with zero target UID, z and model.

For a non-ground target, the server resolves the UID and checks `CanSeeForUpdate` before
invoking the callback. A real protocol request cannot select an invisible/out-of-range
object merely because test code knows its UID. Arrange position and account privileges
before asserting the behaviour of the target handler. Target visibility does not replace
operation-specific reach or permission checks.

## Packet framing

| Type | Responsibility |
|---|---|
| `GameIncomingPacket` | Base for fixed-layout incoming packets. |
| `DynamicLenInPacket` | Reads the length prefix, waits for a complete block and invokes `ReadDynamicPart`. Advances past the declared block. |
| Pooled outgoing packet | Encodes a response; acquired through the packet pool. |
| `PacketGroup` | Groups outgoing packets for sending. |

`GameServer.SendToClientsWhoCanSee` handles visibility-based broadcasts. Preserve pooling
and ownership conventions when introducing packets; follow an existing packet's send path.

## Gump response example

After its packet header, `GumpMenuSelectionInPacket` (`0xB1`) decodes:

| Field | Size |
|---|---|
| Focus UID (ignored), gump UID, pressed button | 4 bytes each |
| Switch count, followed by switch IDs | 4 bytes plus 4 per switch |
| Text-entry count | 4 bytes |
| Each text entry | 2-byte ID, 2-byte character count, then UTF-16BE text |

The character count is multiplied by two to get byte length. Text entries start after the
variable-length switch array; a test client must not use a fixed offset for them.

## Sources

[GameState](../../../GameCore/Networking/GameState.cs),
[GameServer](../../../GameCore/Networking/GameServer.cs),
[IncomingPackets](../../../GameCore/Networking/IncomingPackets.cs),
[Player](../../../scripts/Basic/CharClasses/Player.cs),
[test infrastructure](../07-testing/test-infrastructure.md).
