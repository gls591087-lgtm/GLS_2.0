[PROTOCOL.md](https://github.com/user-attachments/files/33872228/PROTOCOL.md)

# Network and Item Protocol Notes

This document records the packet-processing behavior visible in the current source. Packet IDs and binary layouts are legacy client-protocol details and may differ between client builds.

## Transport overview

The server binds both transports to the configured port (3305 in the launcher path) on all local IPv4 interfaces. The advertised client endpoint comes from `Server/ExternalIP`.

| Transport | Used for | Source implementation |
| --- | --- | --- |
| TCP | Login, menus, room state, results, and reliable race events | `Session`, `KartClient`, packet handlers |
| UDP | Time sync, frequent race traffic, and some item events | `UdpListener`, `Server.OnUDPPacketReceived`, UDP handler dispatch |

### TCP framing

Each TCP frame begins with a four-byte little-endian payload length. `Session` reads the header, rejects zero-length frames and frames larger than 16 MiB, reads the full body, and dispatches it as an `InPacket`.

The session code supports the legacy client's rolling hash/encryption framing. The initial patch/greeting packet currently uses zero seed values, so the code leaves subsequent TCP frames on the length-prefixed plaintext path for this configuration. This is protocol compatibility behavior, not transport security; the server does not use TLS.

For normal TCP packets, the first four bytes of the payload are the RTTI value. `HandlerStore.Dispatch` looks it up and consumes it before invoking the handler.

### UDP envelope and routing

`UdpListener` expects a datagram shaped as:

```text
4-byte IV | encrypted payload | 4-byte remote checksum
```

The receiver decrypts the payload with `HashCryptograph`, verifies the checksum, and drops invalid packets before dispatch. The decoded payload begins with:

```text
4-byte AccountId | 4-byte value/sequence | 4-byte RTTI | handler payload
```

`HandlerStore.DispatchUdp` consumes the account ID, value, and RTTI before invoking the handler. `Server.OnUDPPacketReceived` associates the account with a live or briefly retained UDP session, then falls back to matching advertised or observed UDP endpoints. The session tracks both the endpoint advertised by the client and the actual datagram source, which helps with NAT and VPN address/port translation.

The checksum and XOR-based transform are protocol compatibility mechanisms. They do not provide confidentiality, peer authentication, or protection equivalent to a modern secure transport.

## Race packet routing

Race-specific handlers generally drop packets unless the sender has a player in a room that is currently racing.

`GameHandler.HandleSlot` processes the `GameSlotPacket` through these routes in order:

1. Recognized raw item events are relayed to eligible race peers.
2. Pickup requests are validated and converted into item-award events.
3. General game-slot events are relayed to the requested peer mask, bounded by the room's active racer mask.
4. Other racing `GameSlotPacket` data is passed through to the room peers.

For UDP packets, account ID in the packet header must match the authenticated TCP client. Relay handlers also compare the claimed player slot with the sender's actual room slot and limit packet size. A zero or unusable recipient mask falls back to the active race peer mask where the handler supports that fallback.

## Item pickup flow

Garage inventory and race item pickups are separate mechanisms. `PlayerItems` loads and serializes owned/equipped account items. A race pickup is a `GameSlotPacket` event handled by `GameHandler`; it awards a temporary race item and does not add it to the persistent garage inventory.

The current pickup path is:

1. The client sends a `GameSlotPacket` whose subtype indicates one of the recognized pickup request types.
2. The handler confirms that the player is in an active race. For UDP, the packet account ID must match the TCP session.
3. The request's claimed room slot and expected packet fields are checked against server state and the supported packet layout.
4. A random item ID is selected from the individual or team pickup table according to the room mode.
5. The handler inserts the item ID and award state into an expanded client event and broadcasts it to room players over TCP.

The source currently accepts a compact 54-byte pickup request for the compact single-item request type and converts it to a 73-byte award event. The expanded request path also checks request type, slot, object ID, peer mask, state, tick values, nested object ownership, and fixed operation hashes before granting an item. Invalid or unsupported packets are ignored and reported to the server console.

The item pools currently encoded in `GameHandler` are:

- Individual mode: `2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 33, 111`.
- Team mode: the individual pool plus `109, 110, 113, 114`.

These are protocol/game item identifiers as used by the implementation, not a guarantee that every identifier has identical behavior in every client revision.

## Missile, magnet, and targeted item flow

`GameHandler.HandleTargetedItemPacket` is registered for the recognized rocket, gold rocket, coke rocket, and magnet RTTIs (both `GoItem...` and `Gop...` variants).

- The sender must have a player in an active race.
- TCP packets are broadcast to other room participants, excluding the sender.
- UDP packets must carry the sender's account ID in the header. The server reconstructs the UDP packet envelope and broadcasts it to other race peers.
- `TryRelayItemPacket` also recognizes item-related `GameSlotPacket` subtypes 9–12, validates the sender slot and packet size, and routes the event to eligible race peers.
- Nested magnet/rocket RTTIs inside a general game-slot event are identified for diagnostics and the original event is relayed.

This code provides packet routing so peers can observe the item's client event. It does not calculate target selection, projectile travel, hit detection, or authoritative damage. Those behaviors remain dependent on the matching client protocol and its game simulation.

## Diagnostics

When the server console is open, relevant messages use prefixes such as `[Item Pickup]`, `[Item Relay]`, `[Item Event]`, `[UDP]`, and `[HandlerStore]`. Item packet file logging has been disabled in the release source. Packet hex dumps can contain player and session data; review and redact console output before sharing it publicly.

## Protocol troubleshooting checklist

1. Verify that the client PIN endpoint points to the server's reachable IPv4 address and port 3305.
2. Allow both TCP and UDP 3305 through the server firewall and any router or VPN policy.
3. Confirm the player completed login, joined the same room, and the race is active.
4. Confirm UDP was associated with the correct account; compare `[UDP]` endpoint and account messages in the server console.
5. For pickup failures, check whether the packet is logged as unsupported, mismatched, or rejected. The current parser only grants items for the packet shapes and validations described above.
6. For missile/magnet issues, distinguish a packet relay problem from a client-side targeting or hit simulation problem. The server currently relays recognized events; it does not implement projectile physics.
