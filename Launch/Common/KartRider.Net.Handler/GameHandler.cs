using KartRider.Data;
using KartRider.Game;
using KartRider.Game.Data;
using KartRider.Game.Players;
using KartRider.IO;
using KartRider.IO.Packet;
using KartRider.Net;
using KartSpec;
using LocalServerLauncher;
using MyApp;
using Newtonsoft.Json;
using static KartRider.Kart;
namespace KartRider.Net.Handler;

public static class GameHandler
{
    [PacketHandler(RTTIValues.GameSlotPacket)]
    public static void HandleSlot(KartClient c, InPacket p)
    {
        if (c?.Player?.Room is not Room room || p == null)
        {
            RelayRacePacket(c, p, "GameSlotPacket");
            return;
        }

        byte[] rawPacket = p.ToArray();
        bool udp = p.Position >= 12;
        if (TryRelayItemPacket(c, room, rawPacket, udp) ||
            TryAwardItemPickup(c, room, rawPacket, udp) ||
            TryRelayGameSlotEvent(c, room, rawPacket, udp))
        {
            return;
        }

        if (room.IsRacing)
        {
            int slotHeaderOffset = udp ? 8 : 0;
            int slotTypeOffset = slotHeaderOffset + 12;
            if (rawPacket.Length > slotTypeOffset)
            {
                LogItemPickup(
                    "[GameSlot] Passing through unclassified subtype={0}, player={1}, transport={2}, length={3}, Packet={4}",
                    rawPacket[slotTypeOffset],
                    c.Player.Name,
                    udp ? "UDP" : "TCP",
                    rawPacket.Length - slotHeaderOffset,
                    Convert.ToHexString(rawPacket));
            }
        }

        RelayRacePacket(c, p, "GameSlotPacket");
    }
    [PacketHandler(RTTIValues.GoItemCokeRocket)]
    [PacketHandler(RTTIValues.GopCokeRocket)]
    [PacketHandler(RTTIValues.GoItemMagnet)]
    [PacketHandler(RTTIValues.GopMagnet)]
    [PacketHandler(RTTIValues.GoItemRocket)]
    [PacketHandler(RTTIValues.GopRocket)]
    [PacketHandler(RTTIValues.GoItemGoldRocket)]
    [PacketHandler(RTTIValues.GopGoldRocket)]
    public static void HandleTargetedItemPacket(KartClient client, InPacket packet)
    {
        if (client?.Player?.Room is not Room room ||
            packet == null ||
            !room.IsRacing)
        {
            return;
        }

        byte[] rawPacket = packet.ToArray();
        bool udp = packet.Position >= 12;
        if (!udp)
        {
            room.BroadcastTCPPacketExcept(rawPacket, client.Player);
        }
        else
        {
            if (rawPacket.Length < 12 ||
                BitConverter.ToInt32(rawPacket, 0) != client.Account.Id)
            {
                LogItemPickup(
                    "[Item Relay] Ignoring direct item UDP with mismatched account header. Client={0}, Packet={1}",
                    client.Account.Id,
                    Convert.ToHexString(rawPacket));
                return;
            }

            using UdpOutPacket outgoing = new UdpOutPacket(
                BitConverter.ToInt32(rawPacket, 0),
                BitConverter.ToInt32(rawPacket, 4),
                (RTTIValues)BitConverter.ToUInt32(rawPacket, 8));
            outgoing.WriteBytes(rawPacket.AsSpan(12).ToArray());
            room.BroadcastUDP(outgoing, client.Player);
        }

        LogItemPickup(
            "[Item Relay] Relayed direct item RTTI=0x{0:X8} from {1}, transport={2}, bytes={3}, Packet={4}",
            BitConverter.ToUInt32(rawPacket, udp ? 8 : 0),
            client.Player.Name,
            udp ? "UDP" : "TCP",
            rawPacket.Length,
            Convert.ToHexString(rawPacket));
    }
    [PacketHandler(RTTIValues.GameKartPacket)]
    public static void HandleKartPacket(KartClient c, InPacket p)
    {
        RelayRacePacket(c, p, "GameKartPacket");
    }

    [PacketHandler(RTTIValues.GameKartItemInfoPacket)]
    public static void HandleKartItemInfo(KartClient c, InPacket p)
    {
        RelayRacePacket(c, p, "GameKartItemInfoPacket");
    }

    [PacketHandler(RTTIValues.GameJewelPacket)]
    public static void HandleJewelPacket(KartClient c, InPacket p)
    {
        RelayRacePacket(c, p, "GameJewelPacket");
    }

    [PacketHandler(RTTIValues.GameBoosterAddPacket)]
    public static void HandleBoosterPacket(KartClient c, InPacket p)
    {
        RelayRacePacket(c, p, "GameBoosterAddPacket");
    }

    [PacketHandler(RTTIValues.GameTeamBoosterRequestAddGaugePacket)]
    [PacketHandler(RTTIValues.GameTeamBoosterSetGaugePacket)]
    [PacketHandler(RTTIValues.GameFlagRequestAddScorePacket)]
    [PacketHandler(RTTIValues.GameFlagSetScorePacket)]
    [PacketHandler(RTTIValues.GameFlagGameScorePacket)]
    [PacketHandler(RTTIValues.GameBigBonusScorePacket)]
    [PacketHandler(RTTIValues.GameNotifyBigBonusPlayerPacket)]
    [PacketHandler(RTTIValues.GameOutRunClearPacket)]
    [PacketHandler(RTTIValues.GameFinalAniPacket)]
    public static void HandleGameModeEvent(KartClient c, InPacket p)
    {
        RelayRacePacket(c, p, "game mode event");
    }

    private static readonly short[] IndividualItemPickupTable =
    {
        2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 33, 111
    };

    private static readonly short[] TeamItemPickupTable =
    {
        2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 33, 111,
        109, 110, 113, 114
    };

    private static bool TryRelayItemPacket(
        KartClient client,
        Room room,
        byte[] rawPacket,
        bool udp)
    {
        int headerOffset = udp ? 8 : 0;
        int typeOffset = headerOffset + 12;
        if (rawPacket.Length <= typeOffset ||
            rawPacket[typeOffset] is not (9 or 10 or 11 or 12))
        {
            return false;
        }

        byte packetType = rawPacket[typeOffset];
        if (!room.IsRacing)
        {
            LogItemPickup(
                "[Item Relay] Dropping type={0} outside a race. Player={1}, UDP={2}",
                packetType,
                client.Player.Name,
                udp);
            return true;
        }

        if (udp &&
            (rawPacket.Length < 12 ||
             BitConverter.ToInt32(rawPacket, 0) != client.Account.Id))
        {
            LogItemPickup(
                "[Item Relay] Ignoring UDP packet with mismatched account header. Client={0}, Type={1}, Packet={2}",
                client.Account.Id,
                packetType,
                Convert.ToHexString(rawPacket));
            return true;
        }

        int actualPlayerSlot = room.GetSlot(client.Player);
        int claimedPlayerSlot = BitConverter.ToInt32(rawPacket, 4 + headerOffset);
        uint recipientMask = BitConverter.ToUInt32(rawPacket, 8 + headerOffset);
        if (actualPlayerSlot is < 0 or > 15 ||
            claimedPlayerSlot != actualPlayerSlot ||
            rawPacket.Length > 1013 + headerOffset)
        {
            LogItemPickup(
                "[Item Relay] Rejected sender or size. Player={0}, ClaimedSlot={1}, ActualSlot={2}, Type={3}, Length={4}, UDP={5}, Packet={6}",
                client.Player.Name,
                claimedPlayerSlot,
                actualPlayerSlot,
                packetType,
                rawPacket.Length,
                udp,
                Convert.ToHexString(rawPacket));
            return true;
        }

        // Preserve the target mask when it names active racers; if the client
        // build sends a different mask shape, fall back to all race peers.
        uint availablePeers = room.GetRacerPeerMask(client.Player);
        uint routedMask = recipientMask & availablePeers;
        if (routedMask == 0)
        {
            routedMask = availablePeers;
        }

        if (!udp)
        {
            room.BroadcastTCPPacketToMaskExcept(
                rawPacket,
                routedMask,
                client.Player);
        }
        else
        {
            using UdpOutPacket outgoing = new UdpOutPacket(
                BitConverter.ToInt32(rawPacket, 0),
                BitConverter.ToInt32(rawPacket, 4),
                (RTTIValues)BitConverter.ToUInt32(rawPacket, 8));
            outgoing.WriteBytes(rawPacket.AsSpan(12).ToArray());
            room.BroadcastUDPToMaskExcept(
                outgoing,
                routedMask,
                client.Player);
        }

        LogItemPickup(
            "[Item Relay] Relayed raw type={0} from {1}, slot={2}, requestedMask=0x{3:X8}, routedMask=0x{4:X8}, bytes={5}, UDP={6}, Packet={7}",
            packetType,
            client.Player.Name,
            actualPlayerSlot,
            recipientMask,
            routedMask,
            rawPacket.Length,
            udp,
            Convert.ToHexString(rawPacket));
        return true;
    }
    private static bool TryRelayGameSlotEvent(
        KartClient client,
        Room room,
        byte[] rawPacket,
        bool udp)
    {
        int headerOffset = udp ? 8 : 0;
        int subtypeOffset = headerOffset + 12;
        if (!room.IsRacing ||
            rawPacket.Length <= subtypeOffset ||
            rawPacket[subtypeOffset] != 0)
        {
            return false;
        }

        if (udp &&
            (rawPacket.Length < 12 ||
             BitConverter.ToInt32(rawPacket, 0) != client.Account.Id))
        {
            LogItemPickup(
                "[Item Event] Dropped GameSlot UDP with mismatched account. Player={0}, Packet={1}",
                client.Player.Name,
                Convert.ToHexString(rawPacket));
            return true;
        }

        int actualSlot = room.GetSlot(client.Player);
        int claimedSlot = BitConverter.ToInt32(rawPacket, headerOffset + 4);
        uint requestedMask = BitConverter.ToUInt32(rawPacket, headerOffset + 8);
        if (actualSlot is < 0 or > 15 ||
            claimedSlot != actualSlot ||
            rawPacket.Length > 1013 + headerOffset)
        {
            LogItemPickup(
                "[Item Event] Rejected GameSlot sender or size. Player={0}, ClaimedSlot={1}, ActualSlot={2}, Length={3}, UDP={4}, Packet={5}",
                client.Player.Name,
                claimedSlot,
                actualSlot,
                rawPacket.Length,
                udp,
                Convert.ToHexString(rawPacket));
            return true;
        }

        uint availablePeers = room.GetRacerPeerMask(client.Player);
        uint routedMask = requestedMask & availablePeers;
        if (routedMask == 0)
        {
            routedMask = availablePeers;
        }

        if (routedMask == 0)
        {
            LogItemPickup(
                "[Item Event] Dropped GameSlot with no active recipient. Player={0}, RequestedMask=0x{1:X8}, Packet={2}",
                client.Player.Name,
                requestedMask,
                Convert.ToHexString(rawPacket));
            return true;
        }

        if (!udp)
        {
            room.BroadcastTCPPacketToMaskExcept(
                rawPacket,
                routedMask,
                client.Player);
        }
        else
        {
            using UdpOutPacket outgoing = new UdpOutPacket(
                BitConverter.ToInt32(rawPacket, 0),
                BitConverter.ToInt32(rawPacket, 4),
                (RTTIValues)BitConverter.ToUInt32(rawPacket, 8));
            outgoing.WriteBytes(rawPacket.AsSpan(12).ToArray());
            room.BroadcastUDPToMaskExcept(
                outgoing,
                routedMask,
                client.Player);
        }

        int nestedLengthOffset = subtypeOffset + 2;
        int nestedDataOffset = subtypeOffset + 6;
        string nestedName = "GameSlotEvent";
        if (rawPacket.Length >= nestedDataOffset &&
            nestedLengthOffset + 4 <= rawPacket.Length)
        {
            int nestedLength = BitConverter.ToInt32(rawPacket, nestedLengthOffset);
            if (nestedLength >= 4 &&
                nestedLength <= rawPacket.Length - nestedDataOffset)
            {
                uint nestedRtti = BitConverter.ToUInt32(rawPacket, nestedDataOffset);
                nestedName = nestedRtti switch
                {
                    (uint)RTTIValues.GopMagnet => "GopMagnet",
                    (uint)RTTIValues.GopRocket => "GopRocket",
                    (uint)RTTIValues.GopGoldRocket => "GopGoldRocket",
                    (uint)RTTIValues.GopCokeRocket => "GopCokeRocket",
                    _ => nestedName
                };
            }
        }

        if (nestedName != "GameSlotEvent")
        {
            LogItemPickup(
                "[Item Event] Routed {0} from {1}, slot={2}, requestedMask=0x{3:X8}, routedMask=0x{4:X8}, bytes={5}, UDP={6}, Packet={7}",
                nestedName,
                client.Player.Name,
                actualSlot,
                requestedMask,
                routedMask,
                rawPacket.Length,
                udp,
                Convert.ToHexString(rawPacket));
        }

        return true;
    }

    private static void LogItemPickup(string format, params object[] args)
    {
        string message = string.Format(format, args);

        // Item diagnostics remain in the server console; release builds do not write a log file.
        if (Log.Running)
        {
            Log.Inform(message);
        }
    }

    private static bool TryAwardItemPickup(
        KartClient client,
        Room room,
        byte[] rawPacket,
        bool udp)
    {
        int headerOffset = udp ? 8 : 0;
        int typeOffset = headerOffset + 12;
        if (rawPacket.Length <= typeOffset ||
            rawPacket[typeOffset] is not (1 or 2))
        {
            return false;
        }

        if (udp &&
            (rawPacket.Length < 12 ||
             BitConverter.ToInt32(rawPacket, 0) != client.Account.Id))
        {
            LogItemPickup(
                "[Item Pickup] Ignoring UDP request with mismatched account header. Client={0}, Packet={1}",
                client.Account.Id,
                Convert.ToHexString(rawPacket));
            return true;
        }

        byte[] gameSlotPacket = udp
            ? rawPacket.AsSpan(headerOffset).ToArray()
            : rawPacket;
        byte packetType = gameSlotPacket[12];
        LogItemPickup(
            "[Item Pickup] Received type={0}, player={1}, transport={2}, length={3}, Packet={4}",
            packetType,
            client.Player.Name,
            udp ? "UDP" : "TCP",
            gameSlotPacket.Length,
            Convert.ToHexString(gameSlotPacket));

        if (!room.IsRacing)
        {
            LogItemPickup(
                "[Item Pickup] Ignoring request outside a race. Player={0}, UDP={1}",
                client.Player.Name,
                udp);
            return true;
        }

        int actualPlayerSlot = room.GetSlot(client.Player);
        if (gameSlotPacket.Length == 54)
        {
            return AwardCompactPickup54(client, room, gameSlotPacket, udp, actualPlayerSlot);
        }

        if (gameSlotPacket.Length < 73)
        {
            LogItemPickup(
                "[Item Pickup] Unsupported/truncated request. Type={0}, UDP={1}, Length={2}, Packet={3}",
                packetType,
                udp,
                gameSlotPacket.Length,
                Convert.ToHexString(gameSlotPacket));
            return true;
        }

        int claimedPlayerSlot = BitConverter.ToInt32(gameSlotPacket, 4);
        uint peerMask = BitConverter.ToUInt32(gameSlotPacket, 8);
        uint objectId = BitConverter.ToUInt32(gameSlotPacket, 14);
        uint firstTick = BitConverter.ToUInt32(gameSlotPacket, 18);
        uint secondTick = BitConverter.ToUInt32(gameSlotPacket, 22);
        byte requestState = gameSlotPacket[40];
        uint requestedItem = BitConverter.ToUInt32(gameSlotPacket, 41);
        uint blobLength = BitConverter.ToUInt32(gameSlotPacket, 45);
        uint operationHash = BitConverter.ToUInt32(gameSlotPacket, 49);

        bool validRequest =
            claimedPlayerSlot == actualPlayerSlot &&
            actualPlayerSlot >= 0 &&
            peerMask == uint.MaxValue &&
            blobLength == 24 &&
            operationHash == 0x0A4F02A5;

        uint operationBaseHash = BitConverter.ToUInt32(gameSlotPacket, 53);
        uint nestedObjectId = BitConverter.ToUInt32(gameSlotPacket, 57);
        uint nestedState = BitConverter.ToUInt32(gameSlotPacket, 61);
        uint nestedOwner = BitConverter.ToUInt32(gameSlotPacket, 65);
        uint operationTick = BitConverter.ToUInt32(gameSlotPacket, 69);
        validRequest &=
            operationBaseHash == 0x143403C4 &&
            nestedObjectId == objectId &&
            nestedState == 1 &&
            nestedOwner == (uint)actualPlayerSlot;

        if (packetType == 1)
        {
            validRequest &=
                (objectId & 0xFFFFFF00) == 0xF0000000 &&
                requestState == 0 &&
                requestedItem == 0x0000FFFF &&
                firstTick == operationTick &&
                secondTick == unchecked(firstTick + 1500);
        }
        else
        {
            validRequest &=
                objectId == 0x00FFFFFF &&
                requestState == 8 &&
                requestedItem is >= 1 and <= 32767 &&
                secondTick == firstTick;
        }

        float x = BitConverter.ToSingle(gameSlotPacket, 26);
        float y = BitConverter.ToSingle(gameSlotPacket, 30);
        float z = BitConverter.ToSingle(gameSlotPacket, 34);
        validRequest &=
            float.IsFinite(x) &&
            float.IsFinite(y) &&
            float.IsFinite(z);

        if (!validRequest)
        {
            LogItemPickup(
                "[Item Pickup] Rejected request. Player={0}, Slot={1}, ClaimedSlot={2}, Type={3}, Object={4:X8}, Mask=0x{5:X8}, State={6}, RequestedItem={7}, Blob={8}, Hashes={9:X8}/{10:X8}, Nested={11:X8}/{12}/{13}, Ticks={14}/{15}/{16}, Compact={17}, Packet={18}",
                client.Player.Name,
                actualPlayerSlot,
                claimedPlayerSlot,
                packetType,
                objectId,
                peerMask,
                requestState,
                requestedItem,
                blobLength,
                operationHash,
                operationBaseHash,
                nestedObjectId,
                nestedState,
                nestedOwner,
                firstTick,
                secondTick,
                operationTick,
                false,
                Convert.ToHexString(gameSlotPacket));
            return true;
        }

        bool teamMode = room.Mode == GameRoomMode.ItemTeam;
        short[] table = teamMode
            ? TeamItemPickupTable
            : IndividualItemPickupTable;
        short itemId = table[Random.Shared.Next(table.Length)];
        gameSlotPacket[38] = (byte)itemId;
        gameSlotPacket[39] = (byte)(itemId >> 8);
        gameSlotPacket[40] = 1;

        room.BroadcastTCPPacket(gameSlotPacket);
        LogItemPickup(
            "[Item Pickup] Awarded item={0} to player={1}, slot={2}, type={3}, object={4:X8}, transport={5}, compact={6}, responseLength={7}",
            itemId,
            client.Player.Name,
            actualPlayerSlot,
            packetType,
            objectId,
            udp ? "UDP->TCP" : "TCP",
            false,
            gameSlotPacket.Length);
        return true;
    }

    private static bool AwardCompactPickup54(
        KartClient client,
        Room room,
        byte[] request,
        bool udp,
        int actualPlayerSlot)
    {
        byte packetType = request[12];
        int claimedPlayerSlot = BitConverter.ToInt32(request, 4);
        uint peerMask = BitConverter.ToUInt32(request, 8);
        uint objectId = BitConverter.ToUInt32(request, 14);
        uint firstTick = BitConverter.ToUInt32(request, 18);
        uint secondTick = BitConverter.ToUInt32(request, 22);
        uint blobLength = BitConverter.ToUInt32(request, 26);
        uint operationHash = BitConverter.ToUInt32(request, 30);
        uint operationBaseHash = BitConverter.ToUInt32(request, 34);
        uint nestedObjectId = BitConverter.ToUInt32(request, 38);
        uint nestedState = BitConverter.ToUInt32(request, 42);
        uint nestedOwner = BitConverter.ToUInt32(request, 46);
        uint operationTick = BitConverter.ToUInt32(request, 50);

        bool validRequest =
            packetType == 1 &&
            claimedPlayerSlot == actualPlayerSlot &&
            actualPlayerSlot >= 0 &&
            peerMask == uint.MaxValue &&
            (objectId & 0xFFFFFF00) == 0xF0000000 &&
            secondTick == unchecked(firstTick + 1500) &&
            blobLength == 24 &&
            operationHash == 0x0A4F02A5 &&
            operationBaseHash == 0x143403C4 &&
            nestedObjectId == objectId &&
            nestedState == 1 &&
            nestedOwner == (uint)actualPlayerSlot &&
            operationTick == firstTick;

        if (!validRequest)
        {
            LogItemPickup(
                "[Item Pickup] Rejected compact request. Player={0}, Slot={1}, ClaimedSlot={2}, Type={3}, Object={4:X8}, Mask=0x{5:X8}, Blob={6}, Hashes={7:X8}/{8:X8}, Nested={9:X8}/{10}/{11}, Ticks={12}/{13}/{14}, Packet={15}",
                client.Player.Name,
                actualPlayerSlot,
                claimedPlayerSlot,
                packetType,
                objectId,
                peerMask,
                blobLength,
                operationHash,
                operationBaseHash,
                nestedObjectId,
                nestedState,
                nestedOwner,
                firstTick,
                secondTick,
                operationTick,
                Convert.ToHexString(request));
            return true;
        }

        bool teamMode = room.Mode == GameRoomMode.ItemTeam;
        short[] table = teamMode
            ? TeamItemPickupTable
            : IndividualItemPickupTable;
        short itemId = table[Random.Shared.Next(table.Length)];

        // The compact client request carries only the nested GopCube event.
        // The game client expects the expanded 73-byte item-award event back.
        byte[] response = new byte[73];
        Buffer.BlockCopy(request, 0, response, 0, 26);
        response[38] = (byte)itemId;
        response[39] = (byte)(itemId >> 8);
        response[40] = 1;
        Buffer.BlockCopy(BitConverter.GetBytes(0x0000FFFFu), 0, response, 41, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(24u), 0, response, 45, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(0x0A4F02A5u), 0, response, 49, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(0x143403C4u), 0, response, 53, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(objectId), 0, response, 57, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(1u), 0, response, 61, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((uint)actualPlayerSlot), 0, response, 65, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(firstTick), 0, response, 69, 4);

        room.BroadcastTCPPacket(response);
        LogItemPickup(
            "[Item Pickup] Awarded compact item={0} to player={1}, slot={2}, object={3:X8}, transport={4}, requestLength=54, responseLength={5}, Request={6}, Response={7}",
            itemId,
            client.Player.Name,
            actualPlayerSlot,
            objectId,
            udp ? "UDP->TCP" : "TCP",
            response.Length,
            Convert.ToHexString(request),
            Convert.ToHexString(response));
        return true;
    }
    private static void RelayRacePacket(
        KartClient client,
        InPacket packet,
        string packetName)
    {
        if (client?.Player?.Room is not Room room ||
            packet == null ||
            !room.IsRacing)
        {
            return;
        }

        byte[] rawPacket = packet.ToArray();
        if (packet.Position >= 12)
        {
            if (rawPacket.Length < 12)
            {
                return;
            }

            int accountId = BitConverter.ToInt32(rawPacket, 0);
            int sequence = BitConverter.ToInt32(rawPacket, 4);
            uint rtti = BitConverter.ToUInt32(rawPacket, 8);
            if (accountId != client.Account.Id)
            {
                Log.Warn(
                    "[Race UDP] Ignoring mismatched account header. Client={0}, Packet={1}",
                    client.Account.Id,
                    accountId);
                return;
            }

            using UdpOutPacket outgoing = new UdpOutPacket(
                accountId,
                sequence,
                (RTTIValues)rtti);
            outgoing.WriteBytes(packet.GetRemainingBytes());
            room.BroadcastUDP(outgoing, client.Player);
        }
        else
        {
            room.BroadcastTCPPacket(rawPacket);
        }

        Log.Inform(
            "[Race] Relayed {0} from {1} ({2} bytes)",
            packetName,
            client.Player.Name,
            rawPacket.Length);
    }

    public static void EncodeKartData(
       OutPacket outPacket,
       int kartId)
    {
        outPacket.WriteEncryptedInt(
            Kart.SpeedSlotCapacity);

        outPacket.WriteEncryptedByte(1);

        outPacket.WriteEncryptedBool(
            Kart.motorcycleType);

        outPacket.WriteEncryptedFloat(
            Kart.Mass);

        outPacket.WriteEncryptedFloat(
            Kart.AirFriction);

        // ============================================================
        // Kart physics adjustments
        // ============================================================

        outPacket.WriteEncryptedFloat(
            Kart.DragFactor +
            ServerHostSettings.DragFactorBonus);

        outPacket.WriteEncryptedFloat(
            Kart.ForwardAccelForce +
            ServerHostSettings.ForwardAccelForceBonus);

        outPacket.WriteEncryptedFloat(
            Kart.BackwardAccelForce +
            ServerHostSettings.BackwardAccelForceBonus);

        outPacket.WriteEncryptedFloat(
            Kart.GripBrakeForce +
            ServerHostSettings.GripBrakeForceBonus);

        outPacket.WriteEncryptedFloat(
            Kart.SlipBrakeForce +
            ServerHostSettings.SlipBrakeForceBonus);

        outPacket.WriteEncryptedFloat(
            Kart.MaxSteerAngle);

        outPacket.WriteEncryptedFloat(
            Kart.SteerConstraint);

        outPacket.WriteEncryptedFloat(
            Kart.FrontGripFactor);

        outPacket.WriteEncryptedFloat(
            Kart.RearGripFactor);

        outPacket.WriteEncryptedFloat(
            Kart.DriftTriggerFactor);

        outPacket.WriteEncryptedFloat(
            Kart.DriftTriggerTime);

        outPacket.WriteEncryptedFloat(
            Kart.DriftSlipFactor);

        outPacket.WriteEncryptedFloat(
            Kart.DriftEscapeForce +
            ServerHostSettings.DriftEscapeForceBonus);

        outPacket.WriteEncryptedFloat(
            Kart.CornerDrawFactor +
            ServerHostSettings.CornerDrawFactorBonus);

        outPacket.WriteEncryptedFloat(
            Kart.DriftLeanFactor);

        outPacket.WriteEncryptedFloat(
            Kart.SteerLeanFactor);

        outPacket.WriteEncryptedFloat(
            Kart.DriftMaxGauge +
            ServerHostSettings.DriftMaxGaugeBonus);

        // Keep the original values because the UI does not expose these settings.
        outPacket.WriteEncryptedFloat(
            Kart.NormalBoosterTime);

        outPacket.WriteEncryptedFloat(
            Kart.TeamBoosterTime);

        outPacket.WriteEncryptedFloat(
            Kart.BoostAccelFactor +
            ServerHostSettings.BoostAccelFactorBonus);
    }
    private static bool ApplyKartPhysics(ushort kartId)
    {
        if (kartId > 174)
        {
            Log.Error(
                "Unsupported KartID: {0}",
                kartId);

            return false;
        }

        GameDataReset.Kart_DataReset();

        StartGameData.Kart_id = (short)kartId;

        KartSpec.Kart_01.Kart_250();

        Log.Inform(
            "Kart physics applied. KartID={0}",
            kartId);

        return true;
    }
    [PacketHandler(RTTIValues.PqStartTimeAttack)]
    public static void HandleStartTimeAttack(KartClient c, InPacket p)
    {
        if (c.Player == null)
        {
            Log.Inform("PqStartTimeAttack: Player is null.");
            return;
        }

        byte speedType = p.ReadByte();
        short kartId = p.ReadShort();
        uint trackId = p.ReadUInt();
        byte startType = p.ReadByte();

        // Ignore values sent by the client.
        StartGameData.StartTimeAttack_SpeedType = 2;

        Log.Inform(
            "PqStartTimeAttack: Player={0}, SpeedType=2(FIXED), KartID={1}, TrackID={2}, StartType={3}",
            c.Player.Name,
            kartId,
            trackId,
            startType);

        StartGameData.Kart_id = kartId;

        if (!ApplyKartPhysics((ushort)kartId))
        {
            Log.Error(
                "Failed to apply kart physics. KartID={0}",
                kartId);

            return;
        }

        Kart.SpeedTypeData();

        Log.Inform(
            "SpeedType applied. SpeedType={0}, Forward={1}, Drag={2}, Boost={3}",
            StartGameData.StartTimeAttack_SpeedType,
            SpeedType.ForwardAccelForce,
            SpeedType.DragFactor,
            SpeedType.BoostAccelFactor);

        // Calculate adjustments based on SpeedType 0/1/2.
        Kart.SpeedTypeData();

  
        using (OutPacket outPacket =
            new OutPacket(RTTIValues.PrStartTimeAttack))
        {
            outPacket.WriteByte((byte)c.Player.License);

            Log.Inform(
                "PrStartTimeAttack before EncodeKartData Length={0}",
                outPacket.Length);

            EncodeKartData(
                outPacket,
                kartId);

            Log.Inform(
                "PrStartTimeAttack after EncodeKartData Length={0}",
                outPacket.Length);

            Log.Inform(
                "PrStartTimeAttack HEX={0}",
                BitConverter.ToString(
                    outPacket.ToArray()));

            c.Send(outPacket);
        }
    }
}
