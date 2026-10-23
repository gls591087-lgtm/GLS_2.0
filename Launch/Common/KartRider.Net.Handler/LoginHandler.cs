using KartRider.Data;
using KartRider.Game;
using KartRider.Game.Players;
using KartRider.IO.Packet;
using KartRider.IO;
using Newtonsoft.Json;
namespace KartRider.Net.Handler;

public static class LoginHandler
{
    [PacketHandler(RTTIValues.LoPingRequestPacket)]
    public static void HandlePing(KartClient c, InPacket p)
    {
        using OutPacket outPacket = new OutPacket(RTTIValues.LoPingReplyPacket);
        c.Send(outPacket);
    }

    // =========================================================
    // Get Rider
    //
    // Rebuilt to match the original PlayerHandler.HandleGetRider
    // field layout exactly (this project only had a truncated
    // version here, which caused the client to disconnect right
    // after PrGetRider was sent).
    // =========================================================
    [PacketHandler(RTTIValues.PqGetRider)]
    public static void HandleGetRider(
    KartClient c,
    InPacket p)
    {
        if (c == null)
            return;

        if (c.Account == null)
        {
            Log.Error(
                "[GetRider] Account is null.");

            return;
        }

        Log.Inform(
            "[GetRider] Account ID={0}, Username={1}",
            c.Account.Id,
            c.Account.Username);

        c.Player =
            new Player(
                c.Account.Id,
                c);

        Log.Inform(
            "[GetRider] Created new Player object for AccountID={0}",
            c.Account.Id);

        bool loadResult = c.Player.Load();

        Log.Inform(
            "[GetRider] Player.Load() returned: {0}",
            loadResult);

        if (!loadResult)
        {
            Log.Error(
                "[GetRider] Player.Load failed. AccountID={0}",
                c.Account.Id);

            Log.Error(
                "[GetRider] Dumping LocalDatabase...");

            if (Database.LocalMode)
            {
                Database.DumpLocalDatabase();
            }

            c.Player = null;
        }
        else
        {
            Log.Inform(
                "[GetRider] Player loaded successfully. PlayerID={0}, Name={1}",
                c.Player.Id,
                c.Player.Name);
        }

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.PrGetRider);

        outPacket.WriteBool(
            c.Player != null);

        Log.Inform(
            "[PrGetRider] Sending rider exists={0}",
            c.Player != null);

        if (c.Player != null)
        {
            outPacket.WriteString(
                c.Player.Name);

            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);

            outPacket.WriteSByte(
                c.Player.License);

            outPacket.WriteUShort(
                c.Player.NewbieStatus);

            outPacket.WriteUShort(
                c.Player.RookieStatus);

            outPacket.WriteUShort(
                c.Player.L3Status);

            outPacket.WriteUShort(
                c.Player.L2Status);

            outPacket.WriteUShort(
                c.Player.L1Status);

            outPacket.WriteUShort(
                c.Player.PROStatus);

            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            outPacket.WriteShort(0);

            c.Player.Items.Serialize(
                outPacket);

            outPacket.WriteString(
                string.Empty);

            outPacket.WriteInt();

            outPacket.WriteInt(
                c.Player.RP);

            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();

            outPacket.WriteInt(
                c.Player.Lucci);

            outPacket.WriteInt();

            outPacket.WriteString(
                string.Empty);

            outPacket.WriteInt();

            outPacket.WriteShort(0);

            outPacket.WriteString(
                c.Player.Name);

            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);

            outPacket.WriteSByte(0);

            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);

            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            for (int i = 0; i <= 9; i++)
            {
                outPacket.WriteUShort(0);
            }
            for (int j = 10; j <= 16; j++)
            {
                outPacket.WriteUShort(0);
            }

            outPacket.WriteString(
                string.Empty);

            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteInt();
            outPacket.WriteString(string.Empty);
            outPacket.WriteInt();

            outPacket.WriteShort(0);
        }

        byte[] payload =
            outPacket.ToArray();

        Log.Inform(
            "[PrGetRider] Length={0}",
            payload.Length);

        Log.Inform(
            "[PrGetRider] HEX={0}",
            Convert.ToHexString(payload));

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.LoRqNewRiderPacket)]
    public static void HandleNewRider(KartClient c, InPacket p)
    {
        string name = p.ReadString();
        ushort character = p.ReadUShort();
        ushort color = p.ReadUShort();
        bool valid = true;
        if (Database.Exists("players", "Name = '{0}'", name))
        {
            valid = false;
        }
        if (valid)
        {
            c.Player = new Player(c.Account.Id, c)
            {
                Name = name
            };
            c.Player.Items.Add(new Item(character, ItemType.Character, 1, -1L, isEquipped: true));
            c.Player.Items.Add(new Item(color, ItemType.Color, 1, -1L, isEquipped: true));
            c.Player.Lucci = 1000;
            c.Player.Cash = 0;
            c.Player.Save();
        }
        using OutPacket outPacket = new OutPacket(RTTIValues.LoRpNewRiderPacket);
        outPacket.WriteBool(valid);
        if (valid)
        {
            outPacket.WriteInt();
        }
        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.LoRqGetRiderItemPacket)]
    public static void HandleGetRiderItems(KartClient c, InPacket p)
    {
        if (c.Player == null)
        {
            Log.Warn("[GetRiderItems] Player is null.");
            return;
        }

        Log.Inform(
            "[GetRiderItems] PlayerID={0}, ItemCount={1}",
            c.Player.Id,
            c.Player.Items.Count);

        using OutPacket outPacket =
            new OutPacket(RTTIValues.LoRpGetRiderItemPacket);

        outPacket.WriteInt(1);
        outPacket.WriteInt(1);
        outPacket.WriteInt(c.Player.Items.Count);

        foreach (Item item in c.Player.Items)
        {
            Log.Inform(
                "[GetRiderItems] Type={0}, GameID={1}, Quantity={2}, Equipped={3}",
                item.Type,
                item.GameId,
                item.Quantity,
                item.IsEquipped);

            outPacket.WriteShort((short)item.Type);
            outPacket.WriteUShort(item.GameId);
            outPacket.WriteShort(item.Quantity);
            outPacket.WriteByte(0);
            outPacket.WriteByte(2);
            outPacket.WriteUShort(ushort.MaxValue);
            outPacket.WriteShort(0);
        }

        byte[] payload = outPacket.ToArray();

        Log.Inform(
            "[GetRiderItems] Response Length={0}",
            payload.Length);

        Log.Inform(
            "[GetRiderItems] Response HEX={0}",
            Convert.ToHexString(payload));

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.LoRqUpdateCbPacket)]
    public static void HandleUpdateCb(KartClient c, InPacket p)
    {
        if (c.Player != null)
        {
            sbyte license = p.ReadSByte();
            c.Player.NewbieStatus = p.ReadUShort();
            ushort previousRookieStatus = c.Player.RookieStatus;
            c.Player.RookieStatus = p.ReadUShort();
            if ((previousRookieStatus & 0x40) == 0 && (c.Player.RookieStatus & 0x40) != 0)
            {
                c.Player.Items.Add(new Item(1, ItemType.SlotChanger, 50, -1L));
            }
            if ((previousRookieStatus & 0x100) != 0 || (c.Player.RookieStatus & 0x100) != 0)
            {
            }
            ushort previousL3Status = c.Player.L3Status;
            c.Player.L3Status = p.ReadUShort();
            if ((previousL3Status & 0x70) == 0 && (c.Player.L3Status & 0x70) != 0)
            {
                c.Player.Items.Add(new Item(2, ItemType.Balloon, 50, -1L));
            }
            c.Player.L2Status = p.ReadUShort();
            c.Player.L1Status = p.ReadUShort();
            c.Player.PROStatus = p.ReadUShort();
            if (c.Player.RookieStatus >= 63)
            {
                c.Player.License = 1;
            }
            if (c.Player.L3Status >= 63)
            {
                c.Player.License = 2;
            }
        }
    }

    [PacketHandler(RTTIValues.LoRqGetTrackRankPacket)]
    public static void HandleGetTrackRank(KartClient c, InPacket p)
    {
        if (c.Player != null)
        {
            uint trackId = p.ReadUInt();
            sbyte license = p.ReadSByte();
        }
    }

    [PacketHandler(RTTIValues.LoRqAddLucciPacket)]
    public static void HandleLucciAdd(KartClient c, InPacket p)
    {
        if (c.Player != null)
        {
            byte type = p.ReadByte();
            int amount = p.ReadInt();
            c.Player.Lucci += amount;
        }
    }

    [PacketHandler(RTTIValues.LoRqDecLucciPacket)]
    public static void HandleLucciDecrease(KartClient c, InPacket p)
    {
        if (c.Player != null)
        {
            byte type = p.ReadByte();
            int amount = p.ReadInt();
            c.Player.Lucci -= amount;
        }
    }

    [PacketHandler(RTTIValues.LoRqSetRiderItemOnPacket)]
    public static void HandleSetRiderItemOn(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        try
        {
            // Read all 16 selected values first for logging.
            ushort[] selections = new ushort[16];
            for (int s = 0; s < 16; s++)
                selections[s] = p.ReadUShort();

            Log.Inform("[SetRiderItemOn] Selections: {0}", string.Join(", ", selections));

            // Process each slot.
            for (int i = 1; i <= 16; i++)
            {
                ItemType type = ItemConstants.GetItemType(i);
                ushort gameId = selections[i - 1];

                bool foundMatch = false;

                foreach (Item item in c.Player.Items)
                {
                    if (item.Type != type)
                        continue;

                    if (item.IsEquipped)
                        item.IsEquipped = false;

                    if (item.GameId == gameId)
                    {
                        item.IsEquipped = true;
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch && gameId != 0)
                {
                    Log.Warn("[SetRiderItemOn] No owned item matched Type={0}, GameID={1}", type, gameId);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("[SetRiderItemOn] Exception handling SetRiderItemOn", ex);
        }

        // Wrap UpdateSlotData in its own try/catch so a broadcast exception does not crash the server.
        try
        {
            if (c.Player.Garage != null)
                c.Player.Garage.UpdateSlotData();
            else if (c.Player.Room != null)
                c.Player.Room.UpdateSlotData();
        }
        catch (Exception ex)
        {
            Log.Error("[SetRiderItemOn] UpdateSlotData exception", ex);
        }
    }

    public static void EncodeKartData(
        OutPacket outPacket,
        Statistics s)
    {
        try
        {
            if (outPacket == null)
            {
                Log.Warn("[EncodeKartData] OutPacket is null.");
                return;
            }

            if (s == null)
            {
                Log.Warn("[EncodeKartData] Statistics is null.");
                return;
            }

            // Safe adjustment helper.
            float SafeFloat(float v)
            {
                if (float.IsNaN(v) ||
                    float.IsInfinity(v) ||
                    Math.Abs(v) > 1e8f)
                {
                    return 0f;
                }

                return v;
            }

            // Sanitize values before sending.
            s.Mass = SafeFloat(s.Mass);
            s.AirFriction = SafeFloat(s.AirFriction);
            s.DragFactor = SafeFloat(s.DragFactor);
            s.ForwardAccelForce = SafeFloat(s.ForwardAccelForce);
            s.BackwardAccelForce = SafeFloat(s.BackwardAccelForce);
            s.GripBrakeForce = SafeFloat(s.GripBrakeForce);
            s.SlipBrakeForce = SafeFloat(s.SlipBrakeForce);
            s.MaxSteerAngle = SafeFloat(s.MaxSteerAngle);
            s.SteerConstraint = SafeFloat(s.SteerConstraint);
            s.FrontGripFactor = SafeFloat(s.FrontGripFactor);
            s.RearGripFactor = SafeFloat(s.RearGripFactor);
            s.DriftTriggerFactor = SafeFloat(s.DriftTriggerFactor);
            s.DriftTriggerTime = SafeFloat(s.DriftTriggerTime);
            s.DriftSlipFactor = SafeFloat(s.DriftSlipFactor);
            s.DriftEscapeForce = SafeFloat(s.DriftEscapeForce);
            s.CornerDrawFactor = SafeFloat(s.CornerDrawFactor);
            s.DriftLeanFactor = SafeFloat(s.DriftLeanFactor);
            s.SteerLeanFactor = SafeFloat(s.SteerLeanFactor);
            s.DriftMaxGauge = SafeFloat(s.DriftMaxGauge);
            s.NormalBoosterTime = SafeFloat(s.NormalBoosterTime);
            s.TeamBoosterTime = SafeFloat(s.TeamBoosterTime);
            s.BoostAccelFactor = SafeFloat(s.BoostAccelFactor);

            // Verification log.
            Log.Inform(
                "[EncodeKartData] " +
                "SpeedSlot={0}, Motorcycle={1}, " +
                "Mass={2}, AirFriction={3}, DragFactor={4}, " +
                "ForwardAccel={5}, BackwardAccel={6}, " +
                "GripBrake={7}, SlipBrake={8}, " +
                "MaxSteer={9}, SteerConstraint={10}, " +
                "FrontGrip={11}, RearGrip={12}, " +
                "DriftTrigger={13}, DriftTime={14}, " +
                "DriftSlip={15}, DriftEscape={16}, " +
                "CornerDraw={17}, DriftLean={18}, " +
                "SteerLean={19}, DriftMaxGauge={20}, " +
                "NormalBoostTime={21}, TeamBoostTime={22}, " +
                "BoostAccel={23}",
                s.SpeedSlotCapacity,
                s.motorcycleType,
                s.Mass,
                s.AirFriction,
                s.DragFactor,
                s.ForwardAccelForce,
                s.BackwardAccelForce,
                s.GripBrakeForce,
                s.SlipBrakeForce,
                s.MaxSteerAngle,
                s.SteerConstraint,
                s.FrontGripFactor,
                s.RearGripFactor,
                s.DriftTriggerFactor,
                s.DriftTriggerTime,
                s.DriftSlipFactor,
                s.DriftEscapeForce,
                s.CornerDrawFactor,
                s.DriftLeanFactor,
                s.SteerLeanFactor,
                s.DriftMaxGauge,
                s.NormalBoosterTime,
                s.TeamBoosterTime,
                s.BoostAccelFactor
            );

            // ----------------------------------------
            // Packet Encode
            // ----------------------------------------

            outPacket.WriteEncryptedInt(
                s.SpeedSlotCapacity);

            outPacket.WriteEncryptedByte(1);

            outPacket.WriteEncryptedBool(
                s.motorcycleType);

            outPacket.WriteEncryptedFloat(
                s.Mass);

            outPacket.WriteEncryptedFloat(
                s.AirFriction);

            outPacket.WriteEncryptedFloat(
                s.DragFactor);

            outPacket.WriteEncryptedFloat(
                s.ForwardAccelForce);

            outPacket.WriteEncryptedFloat(
                s.BackwardAccelForce);

            outPacket.WriteEncryptedFloat(
                s.GripBrakeForce);

            outPacket.WriteEncryptedFloat(
                s.SlipBrakeForce);

            outPacket.WriteEncryptedFloat(
                s.MaxSteerAngle);

            outPacket.WriteEncryptedFloat(
                s.SteerConstraint);

            outPacket.WriteEncryptedFloat(
                s.FrontGripFactor);

            outPacket.WriteEncryptedFloat(
                s.RearGripFactor);

            outPacket.WriteEncryptedFloat(
                s.DriftTriggerFactor);

            outPacket.WriteEncryptedFloat(
                s.DriftTriggerTime);

            outPacket.WriteEncryptedFloat(
                s.DriftSlipFactor);

            outPacket.WriteEncryptedFloat(
                s.DriftEscapeForce);

            outPacket.WriteEncryptedFloat(
                s.CornerDrawFactor);

            outPacket.WriteEncryptedFloat(
                s.DriftLeanFactor);

            outPacket.WriteEncryptedFloat(
                s.SteerLeanFactor);

            outPacket.WriteEncryptedFloat(
                s.DriftMaxGauge);

            outPacket.WriteEncryptedFloat(
                s.NormalBoosterTime);

            outPacket.WriteEncryptedFloat(
                s.TeamBoosterTime);

            outPacket.WriteEncryptedFloat(
                s.BoostAccelFactor);

            // ----------------------------------------
            // Inspect the payload.
            // ----------------------------------------

            byte[] payload = outPacket.ToArray();

            Log.Inform(
                "[EncodeKartData] Encoded length={0}",
                payload.Length);

            Log.Inform(
                "[EncodeKartData] HEX={0}",
                Convert.ToHexString(payload));
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[EncodeKartData] {0}",
                ex);
        }
    }

    public static void EncodeKartData(
        OutPacket outPacket,
        int kartId)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "karts",
            $"{kartId}.json");

        Log.Inform(
            "Loading kart data. KartID={0}, Path={1}",
            kartId,
            path);

        if (!File.Exists(path))
        {
            Log.Error(
                $"Kart data not found. KartID={kartId}, Path={path}");

            throw new FileNotFoundException(
                "Kart data not found.",
                path);
        }

        string json = File.ReadAllText(path);

        Statistics? s =
            JsonConvert.DeserializeObject<Statistics>(json);

        if (s == null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize kart data: {path}");
        }

        // Logging and sanitization (same logic).
        Log.Inform("[EncodeKartData] Loaded stats (kart {0}): {1}", kartId, JsonConvert.SerializeObject(s));

        static float SafeFloat(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || Math.Abs(v) > 1e8f)
                return 0f;
            return v;
        }

        s.Mass = SafeFloat(s.Mass);
        s.AirFriction = SafeFloat(s.AirFriction);
        s.DragFactor = SafeFloat(s.DragFactor);
        s.ForwardAccelForce = SafeFloat(s.ForwardAccelForce);
        s.BackwardAccelForce = SafeFloat(s.BackwardAccelForce);
        s.GripBrakeForce = SafeFloat(s.GripBrakeForce);
        s.SlipBrakeForce = SafeFloat(s.SlipBrakeForce);
        s.MaxSteerAngle = SafeFloat(s.MaxSteerAngle);
        s.SteerConstraint = SafeFloat(s.SteerConstraint);
        s.FrontGripFactor = SafeFloat(s.FrontGripFactor);
        s.RearGripFactor = SafeFloat(s.RearGripFactor);
        s.DriftTriggerFactor = SafeFloat(s.DriftTriggerFactor);
        s.DriftTriggerTime = SafeFloat(s.DriftTriggerTime);
        s.DriftSlipFactor = SafeFloat(s.DriftSlipFactor);
        s.DriftEscapeForce = SafeFloat(s.DriftEscapeForce);
        s.CornerDrawFactor = SafeFloat(s.CornerDrawFactor);
        s.DriftLeanFactor = SafeFloat(s.DriftLeanFactor);
        s.SteerLeanFactor = SafeFloat(s.SteerLeanFactor);
        s.DriftMaxGauge = SafeFloat(s.DriftMaxGauge);
        s.NormalBoosterTime = SafeFloat(s.NormalBoosterTime);
        s.TeamBoosterTime = SafeFloat(s.TeamBoosterTime);
        s.BoostAccelFactor = SafeFloat(s.BoostAccelFactor);

        outPacket.WriteEncryptedInt(
            s.SpeedSlotCapacity);

        outPacket.WriteEncryptedByte(1);

        outPacket.WriteEncryptedBool(
            s.motorcycleType);

        outPacket.WriteEncryptedFloat(s.Mass);
        outPacket.WriteEncryptedFloat(s.AirFriction);
        outPacket.WriteEncryptedFloat(s.DragFactor);
        outPacket.WriteEncryptedFloat(s.ForwardAccelForce);
        outPacket.WriteEncryptedFloat(s.BackwardAccelForce);
        outPacket.WriteEncryptedFloat(s.GripBrakeForce);
        outPacket.WriteEncryptedFloat(s.SlipBrakeForce);
        outPacket.WriteEncryptedFloat(s.MaxSteerAngle);
        outPacket.WriteEncryptedFloat(s.SteerConstraint);
        outPacket.WriteEncryptedFloat(s.FrontGripFactor);
        outPacket.WriteEncryptedFloat(s.RearGripFactor);
        outPacket.WriteEncryptedFloat(s.DriftTriggerFactor);
        outPacket.WriteEncryptedFloat(s.DriftTriggerTime);
        outPacket.WriteEncryptedFloat(s.DriftSlipFactor);
        outPacket.WriteEncryptedFloat(s.DriftEscapeForce);
        outPacket.WriteEncryptedFloat(s.CornerDrawFactor);
        outPacket.WriteEncryptedFloat(s.DriftLeanFactor);
        outPacket.WriteEncryptedFloat(s.SteerLeanFactor);
        outPacket.WriteEncryptedFloat(s.DriftMaxGauge);
        outPacket.WriteEncryptedFloat(s.NormalBoosterTime);
        outPacket.WriteEncryptedFloat(s.TeamBoosterTime);
        outPacket.WriteEncryptedFloat(s.BoostAccelFactor);

        byte[] payload = outPacket.ToArray();
        Log.Inform("[EncodeKartData] Encoded length={0}", payload.Length);
        Log.Inform("[EncodeKartData] HEX={0}", BitConverter.ToString(payload).Replace("-", ""));
    }
}