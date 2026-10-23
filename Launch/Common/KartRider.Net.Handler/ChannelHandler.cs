using KartRider.Data;
using KartRider.Game;
using KartRider.Game.Players;
using KartRider.IO;
using KartRider.IO.Packet;
using KartRider.Utilities;
using System.Net;

namespace KartRider.Net.Handler;

public static class ChannelHandler
{
    [PacketHandler(RTTIValues.ChJoinRoomRequestPacket)]
    public static void HandleJoinRoomRequest(KartClient c, InPacket p)
    {
        if (c.Player == null || c.Player.Room != null)
        {
            return;
        }
        short roomId = p.ReadShort();
        byte unk = p.ReadByte();
        string password = p.ReadString();
        if (!c.Player.Channel.Rooms.Contains(roomId))
        {
            return;
        }
        bool success = true;
        Room room = c.Player.Channel.Rooms[roomId];
        if (room.IsProtected && password != room.Password)
        {
            success = false;
        }
        else
        {
            c.Player.SessionData.A = p.ReadInt();
            c.Player.SessionData.B = p.ReadInt();
            c.Player.SessionData.C = p.ReadInt();
            c.Player.SessionData.D = p.ReadInt();
            c.Player.SessionData.E = p.ReadInt();
            c.Player.SessionData.F = p.ReadInt();
            try
            {
                lock (room)
                {
                    success = room.Add(c.Player);
                }
            }
            catch
            {
                success = false;
            }
        }
        using OutPacket outPacket = new OutPacket(RTTIValues.ChJoinRoomReplyPacket);
        outPacket.WriteBool(success);
        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.ChRqEnterMyRoomPacket)]
    public static void HandleEnterMyRoom(KartClient c, InPacket p)
    {
        Console.WriteLine("========== HandleEnterMyRoom CALLED ==========");

        Log.Inform(
            ">>> HandleEnterMyRoom from {0}",
            c.RemoteEndPoint);

        string name = p.ReadString();
        string password = p.ReadString();

        Console.WriteLine(
            $"EnterMyRoom name={name}, password empty={string.IsNullOrEmpty(password)}");

        int playerId;

        try
        {
            playerId = Database.Fetch(
                "players",
                "ID",
                "Name = '{0}'",
                name);
        }
        catch
        {
            playerId = -1;
        }

        if (playerId == -1)
            return;

        Garage garage = Server.Garages.TryGetGarage(playerId);

        if (garage.IsProtected && string.IsNullOrEmpty(password))
        {
            using (OutPacket outPacket =
                new OutPacket(RTTIValues.ChCmdPwEnterMyRoomPacket))
            {
                outPacket.WriteString(name);
                c.Send(outPacket);
                return;
            }
        }

        if (c.Player.Garage != null)
        {
            c.Player.Garage.Remove(c.Player);
        }

        GarageEntranceResponse response =
            GarageEntranceResponse.Success;

        if (!string.IsNullOrEmpty(password) &&
            password != garage.Password)
        {
            response = GarageEntranceResponse.Fail;
        }
        else
        {
            try
            {
                garage.Add(c.Player);
            }
            catch
            {
                response = GarageEntranceResponse.Fail;
            }
        }

        using OutPacket outPacket2 =
            new OutPacket(RTTIValues.ChRpEnterMyRoomPacket);

        outPacket2.WriteString(name);
        outPacket2.WriteByte((byte)response);

        if (response == GarageEntranceResponse.Success)
        {
            garage.Serialize(outPacket2);
        }

        c.Send(outPacket2);
    }

    [PacketHandler(RTTIValues.ChClientUdpAddrPacket)]
    public static void HandleClientUdpAddr(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        if (p.Available < 6)
        {
            Log.Warn(
                "[UDP] ChClientUdpAddrPacket too short: {0}",
                p.Available);

            return;
        }

        byte[] clientIp = p.ReadBytes(4);

        ushort port = p.ReadUShort();

        IPAddress actualIp =
            c.RemoteEndPoint?.Address;

        if (actualIp == null)
        {
            Log.Warn(
                "[UDP] Remote endpoint is null.");

            return;
        }

        byte[] actualIpBytes =
            actualIp.GetAddressBytes();

        if (actualIpBytes.Length != 4)
        {
            Log.Warn(
                "[UDP] Only IPv4 is supported.");

            return;
        }

        IPEndPoint endpoint =
            new IPEndPoint(
                actualIp,
                port);

        c.SetUDPEndPoint(endpoint);

        Log.Inform(
            "[UDP] Client reported IP={0}.{1}.{2}.{3}:{4}",
            clientIp[0],
            clientIp[1],
            clientIp[2],
            clientIp[3],
            port);

        Log.Inform(
            "[UDP] Using actual TCP IP={0}",
            endpoint);
    }

    [PacketHandler(RTTIValues.PqUdpTimeSync)]
    public static void HandleUdpTimeSync(
        KartClient client,
        InPacket packet)
    {
        if (client == null || packet == null)
            return;

        if (packet.Available < 4)
        {
            Log.Warn(
                "[UDP] PqUdpTimeSync payload too small: {0}",
                packet.Available);

            return;
        }

        int clientTick = packet.ReadInt();

        int accountId = client.Account.Id;
        int value = 0;

        int serverTick = Environment.TickCount;

        Log.Inform(
            "[UDP][TimeSync] Account={0}, ClientTick=0x{1:X8} ({1}), ServerTick=0x{2:X8} ({2})",
            accountId,
            clientTick,
            serverTick);

        using UdpOutPacket outPacket =
            new UdpOutPacket(
                accountId,
                value,
                RTTIValues.PrUdpTimeSync);

        outPacket.WriteInt(clientTick);
        outPacket.WriteInt(serverTick);

        byte[] raw = outPacket.ToArray();

        Log.Hex(
            "[UDP][TimeSync] TX Plain",
            raw);

        Server.UdpListener.BeginSend(
            outPacket,
            client.UDPEndPoint);
    }

    [PacketHandler(RTTIValues.PqUdpEcho)]
    public static void HandleUdpEcho(
        KartClient client,
        InPacket packet)
    {
        if (client == null || packet == null)
            return;

        if (packet.Available < 8)
        {
            Log.Warn(
                "[UDP] PqUdpEcho payload too small: {0}",
                packet.Available);

            return;
        }

        int a = packet.ReadInt();
        int b = packet.ReadInt();

        int accountId = client.Account.Id;
        int value = 0;

        using UdpOutPacket outPacket =
            new UdpOutPacket(
                accountId,
                value,
                RTTIValues.PrUdpEcho);

        outPacket.WriteInt(a);
        outPacket.WriteInt(b);

        Server.UdpListener.BeginSend(
            outPacket,
            client.UDPEndPoint);
    }

    [PacketHandler(RTTIValues.ChRequestChStaticRequestPacket)]
    public static void HandleChStaticRequest(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        if (c.Player == null)
        {
            Log.Error(
                "[ChStaticRequest] Player is NULL.");

            return;
        }

        if (p.Available < 4)
        {
            Log.Error(
                "[ChStaticRequest] Invalid payload size: {0}",
                p.Available);

            return;
        }

        int id = p.ReadInt();

        Log.Inform(
            "[ChStaticRequest] Requested ID={0}",
            id);

        Log.Inform(
            "[ChStaticRequest] Available Channels={0}",
            Server.Channels.Count);

        /*
         * Never use Server.Channels[0].
         *
         * Server.Channels is not a regular List.
         * Because it is a KeyedCollection, [0] does not access the first element;
         * it looks for the channel whose key is 0.
         */

        Channel selectedChannel = null;

        foreach (Channel channel in Server.Channels)
        {
            Log.Inform(
                "[ChStaticRequest] Channel ID={0}, Name={1}",
                channel.ID,
                channel.Name);

            if (channel.ID == id)
            {
                selectedChannel = channel;
                break;
            }
        }

        /*
         * If the requested ID does not match an actual Channel ID,
         * use the first actual element.
         *
         * do not use the KeyedCollection indexer.
         */
        if (selectedChannel == null)
        {
            foreach (Channel channel in Server.Channels)
            {
                selectedChannel = channel;
                break;
            }
        }

        if (selectedChannel == null)
        {
            Log.Error(
                "[ChStaticRequest] No channel available.");

            return;
        }

        c.Player.Channel = selectedChannel;

        Log.Inform(
            "[ChStaticRequest] Player Channel assigned: ID={0}, Name={1}",
            selectedChannel.ID,
            selectedChannel.Name);

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.ChRequestChStaticReplyPacket);

        outPacket.WriteBool(true);

        byte[] channelData;

        using (OutPacket data = new OutPacket())
        {
            /*
             * Channel count
             */
            data.WriteInt(Server.Channels.Count);

            /*
             * Basic channel information
             */
            foreach (Channel channel in Server.Channels)
            {
                data.WriteByte(
                    (byte)(channel.ID + 1));

                data.WriteString(
                    channel.Name);
            }

            /*
             * Detailed channel count
             */
            data.WriteInt(
                Server.Channels.Count);

            /*
             * Detailed channel information
             */
            foreach (Channel channel in Server.Channels)
            {
                data.WriteShort(
                    (short)channel.ID);

                data.WriteString(
                    channel.Name);

                data.WriteByte(
                    (byte)(channel.ID + 1));

                data.WriteInt(-1);
            }

            channelData =
                data.ToArray();
        }

        /*
         * RTTI Hash
         */
        uint rttiHash;

        using (OutPacket hashPacket =
            new OutPacket())
        {
            hashPacket.WriteBytes(
                channelData);

            rttiHash =
                RTTIUtilities.GenerateRTTIHash(
                    hashPacket.ToArray());
        }

        /*
         * Actual channel data
         */
        byte[] result;

        using (OutPacket data =
            new OutPacket())
        {
            data.WriteByte(83);
            data.WriteByte(0);

            data.WriteUInt(
                rttiHash);

            data.WriteBytes(
                channelData);

            result =
                data.ToArray();
        }

        outPacket.WriteInt(
            result.Length);

        outPacket.WriteBytes(
            result);

        Log.Inform(
            "[ChStaticRequest] Reply Length={0}",
            outPacket.Length);

        Log.Hex(
            "[ChStaticRequest] Reply HEX",
            outPacket.ToArray());

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.ChGetRoomListRequestPacket)]
    public static void HandleGetRoomListRequest(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        if (c.Player == null ||
            c.Player.Channel == null)
        {
            Log.Warn(
                "[GetRoomList] Player or Channel is NULL!");

            return;
        }

        if (p.Available < 8)
        {
            Log.Warn(
                "[GetRoomList] Invalid payload size: {0}",
                p.Available);

            return;
        }

        int page = p.ReadInt();
        int count = p.ReadInt();

        Log.Inform(
            "[GetRoomList] Channel={0}, Page={1}, Count={2}, Rooms={3}",
            c.Player.Channel.ID,
            page,
            count,
            c.Player.Channel.Rooms.Count);

        foreach (Room room in c.Player.Channel.Rooms)
        {
            Log.Inform(
                "[GetRoomList] Room ID={0}, Name={1}, Track={2}, Players={3}/{4}, Racing={5}",
                room.Id,
                room.Name,
                room.TrackId,
                room.CurrentPlayers,
                room.Capacity,
                room.IsRacing);
        }

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.ChGetRoomListReplyPacket);

        outPacket.WriteInt(page);

        outPacket.WriteInt();

        /*
         * Keep the existing structure.
         */
        outPacket.WriteInt(
            c.Player.Channel.Rooms.Count / 10);

        outPacket.WriteInt(
            c.Player.Channel.Rooms.Count);

        foreach (Room room in c.Player.Channel.Rooms)
        {
            outPacket.WriteUShort(
                room.Id);

            outPacket.WriteString(
                room.Name);

            outPacket.WriteUInt(
                room.TrackId);

            outPacket.WriteBool(
                room.IsProtected);

            outPacket.WriteByte(
                (byte)room.Mode);

            outPacket.WriteBool(
                room.IsRacing);

            outPacket.WriteByte(
                (byte)room.Capacity);

            outPacket.WriteByte(
                (byte)room.CurrentPlayers);
        }

        byte[] data =
            outPacket.ToArray();

        Log.Inform(
            "[GetRoomList] Reply Length={0}, HEX={1}",
            data.Length,
            BitConverter.ToString(data).Replace("-", ""));

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.ChCreateRoomRequestPacket)]
    public static void HandleCreateRoom(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        Log.Inform(
            "[CreateRoom] Player={0}, Channel={1}",
            c.Player != null,
            c.Player != null &&
            c.Player.Channel != null);

        Log.Inform(
            "[CreateRoom] RX Length={0}",
            p.Length);

        Log.Hex(
            "[CreateRoom] RX",
            p.ToArray());

        if (c.Player == null)
        {
            Log.Error(
                "[CreateRoom] Player is NULL.");

            return;
        }

        if (c.Player.Room != null)
        {
            Log.Inform(
                "[CreateRoom] Player already has a room.");

            return;
        }

        if (c.Player.Channel == null)
        {
            Log.Error(
                "[CreateRoom] Player.Channel is NULL.");

            return;
        }

        try
        {
            Channel channel =
                c.Player.Channel;

            Log.Inform(
                "[CreateRoom] Using Channel ID={0}, Name={1}, ExistingRooms={2}",
                channel.ID,
                channel.Name,
                channel.Rooms.Count);

            Room room =
                new Room(channel);

            room.Id =
                channel.AssignRoomId();

            /*
             * Initialize first.
             *
             * Previously, Initialize() was called after Rooms.Add().
             */
            room.Initialize(
                c.Player,
                p);

            channel.Rooms.Add(
                room);

            Log.Inform(
                "[CreateRoom] Created Id={0}, Name={1}, Mode={2}, Capacity={3}",
                room.Id,
                room.Name,
                room.Mode,
                room.Capacity);

            using OutPacket outPacket =
                new OutPacket(
                    RTTIValues.ChCreateRoomReplyPacket);

            outPacket.WriteBool(true);
            outPacket.WriteBool(true);
            outPacket.WriteByte(137);

            Log.Hex(
                "[CreateRoom] TX",
                outPacket.ToArray());

            c.Send(outPacket);
        }
        catch (Exception ex)
        {
            Log.Error(
                "[CreateRoom] Exception: {0}",
                ex);
        }
    }

    [PacketHandler(RTTIValues.PqChannelMovein)]
    public static void HandleChannelMovein(
        KartClient c,
        InPacket p)
    {
        Console.WriteLine(
            $"[ChannelMovein] Received from {c.RemoteEndPoint}");

        try
        {
            if (p.Available < 8)
            {
                Console.WriteLine(
                    $"[ChannelMovein] Invalid packet size: {p.Available}");

                return;
            }

            int accountId =
                p.ReadInt();

            short channelId =
                p.ReadShort();

            short unknown =
                p.ReadShort();

            Console.WriteLine(
                $"[ChannelMovein] AccountID={accountId}, ChannelID={channelId}, Unknown={unknown}");

            /*
             * Channel ID is a key, so the indexer can be used after Contains.
             */
            if (!Server.Channels.Contains(channelId))
            {
                Console.WriteLine(
                    $"[ChannelMovein] Invalid channel: {channelId}");

                return;
            }

            Channel channel =
                Server.Channels[channelId];

            /*
             * Restore the account.
             */
            Account account =
                new Account(c);

            if (!account.Load(accountId))
            {
                Console.WriteLine(
                    $"[ChannelMovein] Account.Load failed: {accountId}");

                return;
            }

            c.Account =
                account;

            /*
             * Remove the existing player.
             */
            if (c.Player != null)
            {
                if (c.Player.Channel != null)
                {
                    c.Player.Channel.Remove(
                        c.Player);
                }

                c.Player = null;
            }

            /*
             * Remove the account if it is already present in the channel.
             */
            if (channel.Contains(accountId))
            {
                Console.WriteLine(
                    $"[ChannelMovein] Removing existing player ID={accountId} from channel {channelId}");

                channel.Remove(
                    accountId);
            }

            /*
             * Create the player.
             */
            Player player =
                new Player(
                    accountId,
                    c);

            if (!player.Load())
            {
                Console.WriteLine(
                    $"[ChannelMovein] Player.Load failed: {accountId}");

                return;
            }

            c.Player =
                player;

            Console.WriteLine(
                $"[ChannelMovein] New Player={player.Name}, PlayerId={player.Id}");

            /*
             * Check channel capacity.
             */
            if (channel.Count >= channel.Capacity)
            {
                Console.WriteLine(
                    $"[ChannelMovein] Channel full: {channel.ID} Count={channel.Count}/{channel.Capacity}");

                return;
            }

            /*
             * Add the player to the actual channel.
             */
            channel.Add(
                player);

            Console.WriteLine(
                $"[ChannelMovein] AFTER ADD: Player={player.Name}, Channel={player.Channel?.ID}");

            if (player.Channel == null)
            {
                Console.WriteLine(
                    "[ChannelMovein] ERROR: Channel is NULL after channel.Add");

                return;
            }

            /*
             * Successful channel-entry response
             */
            using OutPacket outPacket =
                new OutPacket(
                    RTTIValues.PrChannelMoveIn);

            outPacket.WriteBool(true);

            outPacket.WriteEndpoint(
                Server.RemoteEndPoint);

            c.Send(
                outPacket);

            Console.WriteLine(
                $"[ChannelMovein] Sent PrChannelMoveIn to {c.RemoteEndPoint}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ChannelMovein] ERROR: {ex}");
        }
    }
}