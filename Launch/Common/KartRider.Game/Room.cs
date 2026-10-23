using System;
using System.Collections.Generic;
using System.Threading;
using KartRider.Game.Data;
using KartRider.Game.Players;
using KartRider.IO.Packet;
using KartRider.Net;
using KartRider.Net.Handler;
using KartRider.Utilities;
using KartRider.IO;

namespace KartRider.Game;

public sealed class Room : ISerializable
{
    private uint trackId;

    /*
     * Whether the first racer has finished
     */
    private bool firstFinished;

    /*
     * Environment.TickCount when the first racer finishes
     */
    private long firstFinishTick;

    /*
     * Whether the results-processing thread has already started
     */
    private bool resultProcessing;

    public Channel Parent { get; private set; }

    public ushort Id { get; set; }

    public string Name { get; set; }

    public string Password { get; set; }

    public GameRoomMode Mode { get; set; }

    public int Capacity { get; set; }

    public bool IsRacing { get; set; }

    public int StartTime { get; set; }

    public uint TrackId
    {
        get
        {
            return trackId;
        }
        set
        {
            trackId = value;
            UpdateSlotData();
        }
    }

    private Player Master { get; set; }

    private RoomSlot[] Slots { get; set; }

    private IdLooper RankCounter { get; set; }

    public bool IsProtected
    {
        get
        {
            return !string.IsNullOrEmpty(Password);
        }
    }

    public int CurrentPlayers
    {
        get
        {
            int players = 0;

            lock (Slots)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (Slots[i].Player != null)
                    {
                        players++;
                    }
                }
            }

            return players;
        }
    }

    public int AvailableSlots
    {
        get
        {
            lock (Slots)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (Slots[i].Player == null &&
                        Slots[i].State != GameRoomState.Closed)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }

    public Room(Channel parent)
    {
        Parent = parent;

        Master = null;

        Slots = new RoomSlot[16];

        for (int i = 0; i < 16; i++)
        {
            Slots[i] = new RoomSlot();
        }

        RankCounter = new IdLooper(1, 16);

        firstFinished = false;
        firstFinishTick = 0;
        resultProcessing = false;
    }

    public void Initialize(Player master, InPacket p)
    {
        Name = p.ReadString();
        Password = p.ReadString();
        Mode = (GameRoomMode)p.ReadInt();
        Capacity = p.ReadInt();

        p.ReadInt();
        p.ReadInt();
        p.ReadBytes(32);

        master.SessionData.Deserialize(p);

        Master = master;

        GameRoomState state =
            ((master.Client.Account.BitSetValue & 0x40) != 0)
            ? GameRoomState.Observer
            : GameRoomState.NotReady;

        lock (Slots)
        {
            RoomSlot slot = Slots[0];

            slot.Player = master;
            slot.State = state;
            slot.IsPrepared = false;
            slot.Finished = false;
            slot.Rank = 0;
            slot.Time = 0;
        }

        master.Room = this;
    }

    public bool Add(Player player)
    {
        if (CurrentPlayers >= Capacity)
        {
            return false;
        }

        int slotIndex = AvailableSlots;

        if (slotIndex == -1)
        {
            return false;
        }

        GameRoomState state =
            ((player.Client.Account.BitSetValue & 0x40) != 0)
            ? GameRoomState.Observer
            : GameRoomState.NotReady;

        lock (Slots)
        {
            RoomSlot slot = Slots[slotIndex];

            slot.Player = player;
            slot.State = state;
            slot.IsPrepared = false;
            slot.Finished = false;
            slot.Rank = 0;
            slot.Time = 0;
        }

        player.Room = this;

        if ((player.Client.Account.BitSetValue & 0x40) != 0)
        {
            Master = player;
        }

        UpdateSlotData();

        return true;
    }

    public bool Remove(Player player)
    {
        int slot = GetSlot(player);

        if (slot == -1)
        {
            return false;
        }

        lock (Slots)
        {
            Slots[slot].Player = null;
            Slots[slot].State = GameRoomState.None;
            Slots[slot].IsPrepared = false;
            Slots[slot].Finished = false;
            Slots[slot].Rank = 0;
            Slots[slot].Time = 0;

            if (CurrentPlayers > 0 && Master == player)
            {
                Player newMaster = null;

                for (int i = 0; i < 16; i++)
                {
                    if (Slots[i].Player != null &&
                        Slots[i].State != GameRoomState.Observer)
                    {
                        newMaster = Slots[i].Player;
                        break;
                    }
                }

                if (newMaster == null)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        if (Slots[i].Player != null)
                        {
                            newMaster = Slots[i].Player;
                            break;
                        }
                    }
                }

                Master = newMaster;
            }
            else if (CurrentPlayers == 0)
            {
                Parent.Rooms.Remove(this);
            }
        }

        player.Room = null;

        if (CurrentPlayers != 0)
        {
            UpdateSlotData();
        }

        return true;
    }

    public Player GetPlayer(int slot)
    {
        if (slot < 0 || slot >= 16)
        {
            return null;
        }

        lock (Slots)
        {
            return Slots[slot].Player;
        }
    }

    public int GetTime(Player player)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player == player)
                {
                    return Slots[i].Time;
                }
            }
        }

        return -1;
    }

    public int GetRank(Player player)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player == player)
                {
                    return Slots[i].Rank;
                }
            }
        }

        return -1;
    }

    public int GetSlot(Player player)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player == player)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public uint GetRacerPeerMask(Player sender)
    {
        lock (Slots)
        {
            uint mask = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                RoomSlot slot = Slots[i];
                if (slot.Player != null &&
                    slot.Player != sender &&
                    slot.State != GameRoomState.Observer)
                {
                    mask |= 1u << i;
                }
            }

            return mask;
        }
    }

    public bool UpdateState(Player player, GameRoomState state)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player == player)
                {
                    Slots[i].State = state;
                    return true;
                }
            }
        }

        return false;
    }

    public void UpdateStatus(
        Player rider,
        GamePlayerStatus status)
    {
        int slot = GetSlot(rider);

        if (slot == -1)
        {
            return;
        }

        switch (status)
        {
            case GamePlayerStatus.Prepared:
                {
                    lock (Slots)
                    {
                        Slots[slot].IsPrepared = true;
                    }

                    ValidatePrepared();

                    break;
                }

            case GamePlayerStatus.Finished:
                {
                    HandleFinished(rider, slot);

                    break;
                }
        }
    }

    private void HandleFinished(Player rider, int slot)
    {
        int tickCount = Environment.TickCount;
        bool startResultTimer = false;

        lock (Slots)
        {
            if (!IsRacing || resultProcessing ||
                slot < 0 || slot >= Slots.Length ||
                Slots[slot].Player != rider ||
                Slots[slot].Finished)
            {
                return;
            }

            Slots[slot].Finished = true;
            Slots[slot].Time = Math.Max(0, tickCount - StartTime);
            Slots[slot].Rank = RankCounter.Next();

            if (!firstFinished)
            {
                firstFinished = true;
                firstFinishTick = Environment.TickCount64;
                startResultTimer = true;
            }

        }

        Log.Inform(
            "[Race] Finished player={0}, slot={1}, rank={2}, time={3}",
            rider.Name,
            slot,
            GetRank(rider),
            GetTime(rider));

        using (OutPacket raceTime =
               new OutPacket(RTTIValues.GameRaceTimePacket))
        {
            raceTime.WriteInt(slot);
            raceTime.WriteInt(tickCount);
            Broadcast(raceTime);
        }

        if (startResultTimer)
        {
            // State 3 starts the client's visible retire countdown.
            BroadcastGameControl(GameControlStatus.Middle, 10_000);
            StartFinishCountdown();
        }

    }

    private void StartFinishCountdown()
    {
        long deadline = firstFinishTick + 10_000;
        Thread countdownThread = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    long remaining = deadline - Environment.TickCount64;
                    if (remaining <= 0)
                    {
                        break;
                    }

                    Thread.Sleep((int)Math.Min(remaining, 50));
                }

                FinishRace();
            }
            catch (Exception ex)
            {
                Log.Error("[Race] Retire countdown failed: {0}", ex);
            }
        })
        {
            IsBackground = true,
            Name = "KartRider Retire Countdown"
        };
        countdownThread.Start();
    }

    private void FinishRace()
    {
        lock (Slots)
        {
            if (resultProcessing || !IsRacing)
            {
                return;
            }

            resultProcessing = true;
        }

        try
        {
            List<(Player Player, int Slot, int Time, int Rank)> results = new();
            lock (Slots)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    RoomSlot slot = Slots[i];
                    Player player = slot.Player;
                    if (player == null ||
                        slot.State == GameRoomState.Observer)
                    {
                        continue;
                    }

                    if (!slot.Finished)
                    {
                        slot.Rank = 0;
                        slot.Time = int.MaxValue;
                    }

                    results.Add((player, i, slot.Time, slot.Rank));
                }
            }

            if (results.Count == 0)
            {
                ResetRaceState();
                return;
            }

            using (OutPacket resultPacket =
                   new OutPacket(RTTIValues.GameResultPacket))
            {
                resultPacket.WriteByte(0);
                resultPacket.WriteInt(results.Count);

                foreach (var result in results)
                {
                    WriteGameResultEntry(
                        resultPacket,
                        result.Player,
                        result.Slot,
                        result.Time,
                        result.Rank);
                }

                // Fixed trailer used by the matching KartRider client protocol.
                resultPacket.WriteInt(0);
                resultPacket.WriteByte(0);
                resultPacket.WriteByte(1);
                resultPacket.WriteByte(0);
                resultPacket.WriteInt(0);
                resultPacket.WriteInt(4);
                resultPacket.WriteInt(0);
                resultPacket.WriteInt(0);
                resultPacket.WriteByte(0);
                resultPacket.WriteByte(0);
                resultPacket.WriteInt(0);
                resultPacket.WriteByte(0);
                resultPacket.WriteInt(0);
                resultPacket.WriteInt(1);
                resultPacket.WriteUInt(uint.MaxValue);

                lock (Slots)
                {
                    for (int i = 0; i < Slots.Length; i++)
                    {
                        Slots[i].Player?.Client.Send(resultPacket);
                    }
                }
            }

            // The reference server gives the client four seconds to leave the result stage.
            BroadcastGameControl(GameControlStatus.Finish, 4_000);

            Thread resetThread = new Thread(() =>
            {
                try
                {
                    Thread.Sleep(4_500);
                    ResetRaceState();
                }
                catch (Exception ex)
                {
                    Log.Error("[Race] Delayed room reset failed: {0}", ex);
                    ResetRaceState();
                }
            })
            {
                IsBackground = true,
                Name = "KartRider Return To Room"
            };
            resetThread.Start();
        }
        catch (Exception ex)
        {
            Log.Error("[Race] Result processing failed: {0}", ex);
            ResetRaceState();
        }
    }

    private static void WriteGameResultEntry(
        OutPacket packet,
        Player player,
        int slot,
        int time,
        int rank)
    {
        bool retired = time == int.MaxValue;
        ushort kartId = player.Items[ItemType.Kart, true]?.GameId ?? 0;
        ushort characterId = player.Items[ItemType.Character, true]?.GameId ?? 0;

        packet.WriteInt(slot);
        packet.WriteInt(retired ? -1 : time);
        packet.WriteByte(0);
        packet.WriteUShort(kartId);
        packet.WriteInt(retired ? 0 : rank);
        packet.WriteShort(0);
        packet.WriteByte(0);

        packet.WriteInt(1);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteUShort(characterId);
        packet.WriteUShort(0);
        packet.WriteUShort(1);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(268435456);
        packet.WriteInt(1);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0xFF);
        packet.WriteByte(0);
        packet.WriteShort(0);
        packet.WriteShort(0);
        packet.WriteShort(0);
        packet.WriteInt(268435456);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteByte(0);
        packet.WriteByte(1);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteInt(0);
    }

    private void BroadcastGameControl(GameControlStatus status, int delayMilliseconds)
    {
        using OutPacket packet = new OutPacket(RTTIValues.GameControlPacket);
        packet.WriteInt((int)status);
        packet.WriteBool(false);
        packet.WriteInt(unchecked(Environment.TickCount + delayMilliseconds));
        packet.WriteInt(0);
        packet.WriteInt(0);
        packet.WriteBool(false);
        packet.WriteInt(0);
        packet.Skip(40);
        packet.WriteBytes(Convert.FromHexString(
            "0000000001000000005E71181464417C7089B0"));
        Broadcast(packet);
    }

    private void ResetRaceState()
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player == null)
                {
                    continue;
                }

                if (Slots[i].State == GameRoomState.Observer)
                {
                    continue;
                }

                Slots[i].State =
                    GameRoomState.NotReady;

                Slots[i].IsPrepared = false;
                Slots[i].Finished = false;
                Slots[i].Rank = 0;
                Slots[i].Time = 0;
            }
        }

        IsRacing = false;
        StartTime = 0;

        firstFinished = false;
        firstFinishTick = 0;

        RankCounter.Reset();

        resultProcessing = false;

        Console.WriteLine(
            "[RACE] RACE FINISHED / RETURNING TO ROOM"
        );

        UpdateSlotData();
    }

    [PacketHandler(RTTIValues.GameControlPacket)]
    public static void HandleControl(
        KartClient c,
        InPacket p)
    {
        if (c == null ||
            c.Player == null ||
            c.Player.Room == null)
        {
            return;
        }

        try
        {
            GamePlayerStatus status =
                (GamePlayerStatus)p.ReadInt();

            Log.Inform(
                "[GameControl] Player={0}, Status={1} ({2})",
                c.Player.Name,
                (int)status,
                status
            );

            /*
             * Preserve the existing packet structure.
             */
            bool hasExtra =
                p.ReadBool();

            if (hasExtra)
            {
                p.ReadInt();
                p.ReadInt();
            }

            p.ReadInt();
            p.ReadInt();
            p.ReadInt();

            bool hasSessionData =
                p.ReadBool();

            if (hasSessionData)
            {
                c.Player.SessionData.Deserialize(p);
            }

            if (status ==
                GamePlayerStatus.Prepared)
            {
                Log.Inform(
                    "[GameControl] PREPARED Player={0}",
                    c.Player.Name
                );
            }
            else if (status ==
                     GamePlayerStatus.Finished)
            {
                Log.Inform(
                    "[GameControl] FINISHED Player={0}",
                    c.Player.Name
                );
            }

            c.Player.Room.UpdateStatus(
                c.Player,
                status
            );
        }
        catch (Exception ex)
        {
            Log.Error(
                "[GameControl] Exception Player={0}: {1}",
                c.Player != null
                    ? c.Player.Name
                    : "NULL",
                ex
            );
        }
    }

    public bool CloseSlot(
        int slot,
        bool close)
    {
        if (slot < 0 || slot >= 16)
        {
            return false;
        }

        if (!close ||
            Capacity > 2)
        {
            if (close)
            {
                Capacity--;
            }
            else
            {
                Capacity++;
            }

            Slots[slot].State =
                GameRoomState.Closed;

            return true;
        }

        return false;
    }

    public void Kick(int slot)
    {
        Player target =
            GetPlayer(slot);

        if (target == null)
        {
            return;
        }

        Remove(target);

        using (OutPacket outPacket =
            new OutPacket(
                RTTIValues.ChSecedeRoomReplyPacket))
        {
            outPacket.WriteBool(true);

            target.Client.Send(
                outPacket
            );
        }
    }

    public bool ValidateMaster(
        Player player)
    {
        return Master == player;
    }

    public bool ValidateReady(
        Player master)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player != master &&
                    (
                        Slots[i].State ==
                            GameRoomState.NotReady ||
                        Slots[i].State ==
                            GameRoomState.Setup
                    ))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ValidatePrepared()
    {
        int preparedPlayers = 0;
        int racingPlayers = 0;

        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player == null)
                {
                    continue;
                }

                if (Slots[i].State ==
                    GameRoomState.Observer)
                {
                    continue;
                }

                racingPlayers++;

                if (Slots[i].IsPrepared)
                {
                    preparedPlayers++;
                }
            }
        }

        Console.WriteLine(
            "[RACE] Prepared={0}/{1}",
            preparedPlayers,
            racingPlayers
        );

        /*
         * When all active racers are Prepared,
         * start the countdown.
         */
        if (racingPlayers > 0 &&
            preparedPlayers == racingPlayers)
        {
            Race();
        }
    }

    public void Start()
    {
        if (TrackId == 0)
        {
            TrackId =
                DataProvider.Tracks
                    .GetAllRandom()
                    .Id;
        }
        else if (TrackId == 3)
        {
            TrackId =
                DataProvider.Tracks
                    .GetHotRandom()
                    .Id;
        }


        /*
         * Reset the previous race state.
         */
        firstFinished = false;
        firstFinishTick = 0;
        resultProcessing = false;

        RankCounter.Reset();

        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].State !=
                        GameRoomState.None &&
                    Slots[i].State !=
                        GameRoomState.Closed &&
                    Slots[i].State !=
                        GameRoomState.Observer)
                {
                    Slots[i].State =
                        GameRoomState.Ready;

                    Slots[i].Finished = false;
                    Slots[i].IsPrepared = false;
                    Slots[i].Rank = 0;
                    Slots[i].Time = 0;
                }
            }
        }

        /*
         * Send the race-start packet to clients.
         */
        lock (Slots)
        {
            for (int j = 0; j < 16; j++)
            {
                if (Slots[j].Player == null)
                {
                    continue;
                }

                using (OutPacket outPacket =
                    new OutPacket(
                        RTTIValues.GrCommandStartPacket))
                {
                    outPacket.WriteUInt(
                        1234044783u
                    );

                    Serialize(outPacket);

                    outPacket.WriteUInt(
                        863766061u
                    );

                    AddSlotData(outPacket);

                    outPacket.WriteUInt(
                        TrackId
                    );

                    outPacket.WriteUInt(
                        TrackId
                    );

                    PlayerHandler.EncodeKartData(
                        outPacket
                    );

                    outPacket.WriteUInt(
                        TrackId
                    );

                    outPacket.WriteByte(0);

                    Slots[j].Player.Client.Send(
                        outPacket
                    );
                }
            }
        }
    }

    public void Race()
    {
        if (IsRacing)
        {
            Console.WriteLine(
                "[RACE] Race() ignored: already racing."
            );

            return;
        }

        /*
         * 7-second countdown.
         */
        StartTime =
            Environment.TickCount + 7000;

        /*
         * Reset the new race state.
         */
        firstFinished = false;
        firstFinishTick = 0;
        resultProcessing = false;

        RankCounter.Reset();

        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player == null)
                {
                    continue;
                }

                if (Slots[i].State ==
                    GameRoomState.Observer)
                {
                    continue;
                }

                Slots[i].Finished = false;
                Slots[i].IsPrepared = false;
                Slots[i].Rank = 0;
                Slots[i].Time = 0;
            }
        }

        /*
         * Active racing state.
         */
        IsRacing = true;

        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                Player player =
                    Slots[i].Player;

                if (player == null)
                {
                    continue;
                }

                using (OutPacket outPacket =
                    new OutPacket(
                        RTTIValues.GameControlPacket))
                {
                    /*
                     * Type 1 = race-start countdown
                     */
                    outPacket.WriteInt(1);

                    outPacket.WriteBool(false);

                    outPacket.WriteInt(
                        StartTime
                    );

                    outPacket.WriteInt(i);

                    bool sendSessionData = true;

                    outPacket.WriteBool(
                        sendSessionData
                    );

                    if (sendSessionData)
                    {
                        player.SessionData.Serialize(
                            outPacket
                        );
                    }

                    outPacket.WriteInt();

                    outPacket.Skip(40);

                    outPacket.Skip(10);

                    player.Client.Send(
                        outPacket
                    );
                }
            }
        }

        Console.WriteLine(
            "[RACE] Race started. StartTime={0}",
            StartTime
        );
    }

    public void Broadcast(
        OutPacket outPacket,
        Player skipped = null)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player != skipped)
                {
                    Slots[i].Player.Client.Send(
                        outPacket
                    );
                }
            }
        }
    }

    public void BroadcastTCPPacket(byte[] data)
    {
        if (data == null)
        {
            return;
        }

        lock (Slots)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Player player = Slots[i].Player;
                if (player != null)
                {
                    // Send() frames and encrypts plaintext for each TCP session.
                    player.Client.Send(data);
                }
            }
        }
    }

    public void BroadcastTCPPacketExcept(byte[] data, Player skipped)
    {
        if (data == null)
        {
            return;
        }

        lock (Slots)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Player player = Slots[i].Player;
                if (player != null && player != skipped)
                {
                    player.Client.Send(data);
                }
            }
        }
    }

    public void BroadcastTCPPacketToMaskExcept(
        byte[] data,
        uint recipientMask,
        Player skipped)
    {
        if (data == null)
        {
            return;
        }

        lock (Slots)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if ((recipientMask & (1u << i)) == 0)
                {
                    continue;
                }

                Player player = Slots[i].Player;
                if (player != null && player != skipped)
                {
                    player.Client.Send(data);
                }
            }
        }
    }

    public void BroadcastUDPToMaskExcept(
        UdpOutPacket outPacket,
        uint recipientMask,
        Player skipped)
    {
        if (outPacket == null)
        {
            return;
        }

        lock (Slots)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if ((recipientMask & (1u << i)) == 0)
                {
                    continue;
                }

                Player player = Slots[i].Player;
                if (player != null &&
                    player != skipped &&
                    player.Client.UDPEndPoint != null)
                {
                    Server.UdpListener.BeginSend(
                        outPacket,
                        player.Client.UDPEndPoint);
                }
            }
        }
    }

    public void BroadcastUDP(
        UdpOutPacket outPacket,
        Player skipped = null)
    {
        lock (Slots)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Slots[i].Player != null &&
                    Slots[i].Player != skipped &&
                    Slots[i].Player.Client.UDPEndPoint != null)
                {
                    Server.UdpListener.BeginSend(
                        outPacket,
                        Slots[i].Player.Client.UDPEndPoint
                    );
                }
            }
        }
    }

    public void UpdateSlotData()
    {
        using (OutPacket outPacket =
            new OutPacket(
                RTTIValues.GrSlotDataPacket))
        {
            AddSlotData(outPacket);

            byte[] data =
                outPacket.ToArray();

            Log.Inform(
                "[GrSlotData] TX Length={0}, Players={1}, TrackId={2}",
                data.Length,
                CurrentPlayers,
                TrackId
            );

            Log.Hex(
                "[GrSlotData] TX HEX",
                data
            );

            Broadcast(outPacket);
        }
    }

    public void AddSlotData(
        OutPacket p)
    {
        p.WriteUInt(TrackId);

        p.WriteInt(0);

        /*
         * 32-byte padding.
         */
        for (int i = 0; i < 32; i++)
        {
            p.WriteByte(0);
        }

        p.WriteInt(0);
        p.WriteByte(0);

        /*
         * 6 ints.
         */
        for (int i = 0; i < 6; i++)
        {
            p.WriteInt(0);
        }

        int index = 0;

        lock (Slots)
        {
            for (int k = 0; k < 16; k++)
            {
                RoomSlot slot =
                    Slots[k];

                p.WriteInt(
                    (int)slot.State
                );

                if (slot.State ==
                        GameRoomState.None ||
                    slot.State ==
                        GameRoomState.Closed)
                {
                    continue;
                }

                if (slot.Player == null)
                {
                    Log.Warn(
                        "[GrSlotData] Slot={0} State={1} but Player=null",
                        k,
                        slot.State
                    );

                    continue;
                }

                p.WriteInt(
                    slot.Player.AccountId
                );

                p.WriteEndpoint(
                    slot.Player.Client.RemoteEndPoint
                );

                p.WriteEndpoint(
                    slot.Player.Client.UDPEndPoint
                );

                p.WriteString(
                    slot.Player.Name
                );

                p.WriteString(
                    "Test"
                );

                p.WriteInt(0);

                if (slot.State !=
                    GameRoomState.Observer)
                {
                    p.WriteSByte(
                        slot.Player.License
                    );

                    /*
                     * Nine ushort values.
                     */
                    for (int l = 0; l < 9; l++)
                    {
                        p.WriteUShort(0);
                    }

                    /*
                     * Currently equipped items.
                     */
                    slot.Player.Items.Serialize(
                        p
                    );

                    p.WriteString("");

                    p.WriteInt(
                        slot.Player.RP
                    );

                    p.WriteByte(0);

                    p.WriteByte(
                        (byte)index
                    );

                    p.WriteByte(0);

                    for (int i = 0; i < 9; i++)
                    {
                        p.WriteInt(0);
                    }

                    index++;
                }
            }
        }
    }

    public void SetTrackIdSilent(
        uint id)
    {
        /*
         * without calling UpdateSlotData,
         * change only TrackId.
         */
        trackId = id;
    }

    public void Serialize(
        OutPacket p)
    {
        p.WriteString(Name);

        p.WriteByte(
            (byte)Mode
        );

        p.WriteInt();

        p.WriteByte(
            (byte)Capacity
        );

        p.WriteInt();
    }

    public void Deserialize(
        InPacket p)
    {
    }
}

