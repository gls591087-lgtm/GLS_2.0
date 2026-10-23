using KartRider.Game;
using KartRider.IO;
using KartRider.IO.Packet;

namespace KartRider.Net.Handler;

public static class GameRoomHandler
{
    [PacketHandler(RTTIValues.GrFirstRequestPacket)]
    public static void HandleFirst(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null)
            return;

        Room room = c.Player.Room;

        // If TrackId is 0, use an existing track (UpdateSlotData is not reached).
        if (room.TrackId == 0)
        {
            room.SetTrackIdSilent(338166332u); // ice_I03
            Log.Inform("[GrFirst] TrackId forced to {0}", room.TrackId);
        }

        // 1) Update the session first.
        using (OutPacket session = new OutPacket(RTTIValues.GrSessionDataPacket))
        {
            room.Serialize(session);
            Log.Hex("[GrSessionData] TX", session.ToArray());
            c.Send(session);
        }

        // 2) Update the slot only once.
        using (OutPacket slot = new OutPacket(RTTIValues.GrSlotDataPacket))
        {
            room.AddSlotData(slot);
            Log.Inform("[GrSlotData] TX Length={0}, Players={1}, TrackId={2}",
                slot.Length, room.CurrentPlayers, room.TrackId);
            Log.Hex("[GrSlotData] TX HEX", slot.ToArray());
            c.Send(slot);
        }
    }

    [PacketHandler(RTTIValues.GrRequestSetSlotStatePacket)]
    public static void HandleSetSlotState(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null)
            return;

        GameRoomState newState = (GameRoomState)p.ReadInt();
        int slot = c.Player.Room.GetSlot(c.Player);

        Log.Inform(
            "[SetSlotState] Account={0}, Name={1}, Slot={2}, State={3} ({4})",
            c.Player.AccountId, c.Player.Name, slot, (int)newState, newState);

        bool success = c.Player.Room.UpdateState(c.Player, newState);
        Log.Inform("[SetSlotState] Result={0}", success);

        using (OutPacket outPacket = new OutPacket(RTTIValues.GrReplySetSlotStatePacket))
        {
            outPacket.WriteBool(success);
            c.Send(outPacket);
        }

        c.Player.Room.UpdateSlotData();
    }

    [PacketHandler(RTTIValues.GrRequestClosePacket)]
    public static void HandleClose(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null ||
            !c.Player.Room.ValidateMaster(c.Player))
            return;

        int slot = p.ReadInt();
        bool close = p.ReadBool();

        Log.Inform("[RoomClose] Account={0}, Slot={1}, Close={2}",
            c.Player.AccountId, slot, close);

        bool success = c.Player.Room.CloseSlot(slot, close);
        Log.Inform("[RoomClose] Result={0}", success);

        using (OutPacket outPacket = new OutPacket(RTTIValues.GrReplyClosePacket))
        {
            outPacket.WriteBool(success);
            c.Send(outPacket);
        }

        c.Player.Room.UpdateSlotData();
    }

    [PacketHandler(RTTIValues.GrRequestKickPacket)]
    public static void HandleKick(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null ||
            !c.Player.Room.ValidateMaster(c.Player))
            return;

        int slot = p.ReadInt();
        Log.Inform("[RoomKick] Account={0}, Slot={1}", c.Player.AccountId, slot);

        c.Player.Room.Kick(slot);

        using (OutPacket outPacket = new OutPacket(RTTIValues.GrReplyKickPacket))
        {
            outPacket.WriteInt();
            c.Send(outPacket);
        }
    }

    [PacketHandler(RTTIValues.GrChangeTrackPacket)]
    public static void HandleChangeTrack(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null ||
            !c.Player.Room.ValidateMaster(c.Player))
            return;

        uint id = p.ReadUInt();
        c.Player.SessionData.Deserialize(p);

        Log.Inform("[RoomTrack] Account={0}, TrackId={1}", c.Player.AccountId, id);

        c.Player.Room.TrackId = id; // The setter calls UpdateSlotData.
    }

    [PacketHandler(RTTIValues.GrRequestStartPacket)]
    public static void HandleStart(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null)
            return;

        Room room = c.Player.Room;
        GameRoomStartResponse response = GameRoomStartResponse.Success;

        bool master = room.ValidateMaster(c.Player);
        int players = room.CurrentPlayers;
        bool ready = room.ValidateReady(c.Player);
        uint trackId = room.TrackId;

        Log.Inform(
            "[RoomStart] Account={0}, Master={1}, Players={2}, Ready={3}, TrackId={4}",
            c.Player.AccountId, master, players, ready, trackId);

        if (!master)
            response = GameRoomStartResponse.NotRoomMaster;
        else if (players < 2)
            response = GameRoomStartResponse.Success;
        else if (!ready)
            response = GameRoomStartResponse.NotReady;
        else if (trackId < 10)
            response = GameRoomStartResponse.NotReady;

        Log.Inform("[RoomStart] Response={0} ({1})", (int)response, response);

        using (OutPacket outPacket = new OutPacket(RTTIValues.GrReplyStartPacket))
        {
            outPacket.WriteInt((int)response);
            c.Send(outPacket);
        }

        if (response == GameRoomStartResponse.Success)
        {
            Log.Inform("[RoomStart] Calling Room.Start()");
            room.Start();
        }
    }

    [PacketHandler(RTTIValues.GrRiderTalkPacket)]
    public static void HandleRiderTalk(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room == null)
            return;

        string text = p.ReadString();
        p.ReadInt();
        int slot = c.Player.Room.GetSlot(c.Player);

        Log.Inform("[RiderTalk] Account={0}, Slot={1}, Text={2}",
            c.Player.AccountId, slot, text);

        using (OutPacket outPacket = new OutPacket(RTTIValues.GrRiderEchoPacket))
        {
            outPacket.WriteInt(slot);
            outPacket.WriteString(text);
            c.Player.Room.Broadcast(outPacket, c.Player);
        }
    }
}