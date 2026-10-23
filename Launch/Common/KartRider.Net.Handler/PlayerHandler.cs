using KartRider.Data;
using KartRider.Game;
using KartRider.Game.Players;
using KartRider.IO;
using KartRider.IO.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace KartRider.Net.Handler;

public static class PlayerHandler
{
    // =========================================================
    // LOGIN
    // =========================================================

    [PacketHandler(RTTIValues.PqLogin)]
    public static void HandleLogin(
        KartClient c,
        InPacket p)
    {
        string username = string.Empty;
        string password = string.Empty;

        try
        {
            Log.Inform(
                "[Login] ========================================");

            Log.Inform(
                "[Login] PqLogin received.");

            Log.Inform(
                "[Login] Available={0}",
                p.Available);

            // -------------------------------------------------
            // Header
            // -------------------------------------------------

            if (p.Available < 13)
            {
                Log.Warn(
                    "[Login] Packet too short. Available={0}",
                    p.Available);

                return;
            }

            p.ReadInt();
            p.ReadInt();

            RTTIValues handlerType =
                (RTTIValues)p.ReadInt();

            p.ReadByte();

            Log.Inform(
                "[Login] HandlerType={0}",
                handlerType);

            // -------------------------------------------------
            // AccountDataProfile
            // -------------------------------------------------

            if (handlerType ==
                RTTIValues.AccountDataProfile)
            {
                Func<string, object[]> readNode = null!;

                readNode = delegate (string parentPath)
                {
                    string nodeName =
                        p.ReadString();

                    string nodeValue =
                        p.ReadString();

                    string currentPath =
                        string.IsNullOrEmpty(parentPath)
                            ? "/" + nodeName
                            : parentPath + "/" + nodeName;

                    Log.Inform(
                        "[Login] Node Path={0}",
                        currentPath);

                    Log.Inform(
                        "[Login] Node Name={0}",
                        nodeName);

                    Log.Inform(
                        "[Login] Node Value={0}",
                        nodeValue);

                    // -------------------------------------------------
                    // USERNAME
                    // -------------------------------------------------

                    if (nodeName.Equals(
                            "username",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        username =
                            nodeValue;

                        Log.Inform(
                            "[Login] Username found: {0}",
                            username);
                    }

                    // -------------------------------------------------
                    // PASSWORD
                    // -------------------------------------------------

                    else if (nodeName.Equals(
                                 "password",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        password =
                            nodeValue;

                        Log.Inform(
                            "[Login] Password found. Length={0}",
                            password.Length);
                    }

                    // -------------------------------------------------
                    // Values
                    // -------------------------------------------------

                    List<object[]> values =
                        new List<object[]>();

                    int valueCount =
                        p.ReadInt();

                    Log.Inform(
                        "[Login] ValueCount={0}",
                        valueCount);

                    for (int i = valueCount; i > 0; i--)
                    {
                        string valueName =
                            p.ReadString();

                        string valueValue =
                            p.ReadString();

                        values.Add(
                            new object[]
                            {
                                i,
                                valueName,
                                valueValue
                            });
                    }

                    // -------------------------------------------------
                    // Children
                    // -------------------------------------------------

                    List<object[]> children =
                        new List<object[]>();

                    int childCount =
                        p.ReadInt();

                    Log.Inform(
                        "[Login] ChildCount={0}",
                        childCount);

                    for (int i = childCount; i > 0; i--)
                    {
                        children.Add(
                            readNode(currentPath));
                    }

                    return new object[]
                    {
                        nodeName,
                        nodeValue,
                        values.ToArray(),
                        children.ToArray()
                    };
                };

                readNode(string.Empty);
            }
            else
            {
                Log.Warn(
                    "[Login] Unexpected login type: {0}",
                    handlerType);
            }

            // -------------------------------------------------
            // FINAL LOGIN DATA
            // -------------------------------------------------

            Log.Inform(
                "[Login] Parsed username = '{0}'",
                username);

            Log.Inform(
                "[Login] Parsed password length = {0}",
                password.Length);

            Log.Inform(
                "[Login] Database.LocalMode = {0}",
                Database.LocalMode);

            // -------------------------------------------------
            // Check the local database directly.
            // -------------------------------------------------

            if (Database.LocalMode)
            {
                Dictionary<string, object>? localPlayer =
                    Database.GetLocalPlayer();

                if (localPlayer == null)
                {
                    Log.Warn(
                        "[Login] Local player does not exist.");
                }
                else
                {
                    Log.Inform(
                        "[Login] Local player exists.");

                    if (localPlayer.TryGetValue(
                            "ID",
                            out object? playerId))
                    {
                        Log.Inform(
                            "[Login] PlayerID={0}",
                            playerId);
                    }

                    if (localPlayer.TryGetValue(
                            "AccountID",
                            out object? accountId))
                    {
                        Log.Inform(
                            "[Login] AccountID={0}",
                            accountId);
                    }

                    if (localPlayer.TryGetValue(
                            "Name",
                            out object? playerName))
                    {
                        Log.Inform(
                            "[Login] PlayerName={0}",
                            playerName);
                    }
                }
            }

            // -------------------------------------------------
            // ACCOUNT
            // -------------------------------------------------

            c.Account =
                new Account(c);

            LoginResponse response =
                LoginResponse.Valid;

            byte internetCafeResponse =
                0;

            bool accountLoaded =
                c.Account.Load(
                    username,
                    password);

            Log.Inform(
                "[Login] Account.Load() = {0}",
                accountLoaded);

            // -------------------------------------------------
            // ACCOUNT VALID
            // -------------------------------------------------

            if (accountLoaded)
            {
                Log.Inform(
                    "[Login] Account loaded successfully.");

                Log.Inform(
                    "[Login] Account ID={0}",
                    c.Account.Id);

                Log.Inform(
                    "[Login] Account Username={0}",
                    c.Account.Username);

                Log.Inform(
                    "[Login] Account IsBanned={0}",
                    c.Account.IsBanned);

                Log.Inform(
                    "[Login] Account SessionIP='{0}'",
                    c.Account.SessionIP);

                if (c.Account.IsBanned)
                {
                    response =
                        LoginResponse.Banned;

                    Log.Warn(
                        "[Login] Account is banned.");
                }
                else if (
                    c.Account.SessionIP !=
                    string.Empty)
                {
                    response =
                        LoginResponse.AlreadyLoggedIn;

                    Log.Warn(
                        "[Login] Account already logged in.");
                }
                else
                {
                    Log.Inform(
                        "[Login] Account accepted.");
                }
            }
            else
            {
                response =
                    LoginResponse.InvalidID;

                Log.Warn(
                    "[Login] Account.Load() FAILED.");

                Log.Warn(
                    "[Login] Username='{0}'",
                    username);

                Log.Warn(
                    "[Login] PasswordLength={0}",
                    password.Length);

                c.Account = null;
            }

            // -------------------------------------------------
            // LOGIN RESPONSE
            // -------------------------------------------------

            using OutPacket outPacket =
                new OutPacket(
                    RTTIValues.PrLogin);

            outPacket.WriteInt(
                (int)response);

            outPacket.WriteUShort(
                41885);

            outPacket.WriteUShort(
                26);

            outPacket.WriteInt(
                c.Account != null
                    ? c.Account.Id
                    : 0);

            outPacket.WriteString(
                c.Account != null
                    ? c.Account.Username
                    : string.Empty);

            outPacket.WriteByte(2);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);

            // -------------------------------------------------
            // Login result
            // -------------------------------------------------

            if (response ==
                LoginResponse.Valid)
            {
                outPacket.WriteByte(0);

                outPacket.WriteInt(
                    int.MaxValue);
            }
            else
            {
                outPacket.WriteByte(
                    internetCafeResponse);

                outPacket.WriteInt(0);
            }

            outPacket.WriteByte(0);

            // -------------------------------------------------
            // BitSet
            // -------------------------------------------------

            uint bitSet =
                c.Account != null
                    ? c.Account.BitSetValue
                    : 0u;

            for (int i = 0; i < 9; i++)
            {
                outPacket.WriteUInt(
                    bitSet);
            }

            // -------------------------------------------------
            // Unknown fields
            // -------------------------------------------------

            outPacket.WriteUShort(0);
            outPacket.WriteUShort(0);
            outPacket.WriteByte(0);

            outPacket.WriteInt(0);
            outPacket.WriteInt(0);

            // -------------------------------------------------
            // Server endpoint
            // -------------------------------------------------

            outPacket.WriteEndpoint(
                Server.RemoteEndPoint);

            outPacket.WriteByte(0);
            outPacket.WriteByte(0);
            outPacket.WriteByte(0);

            byte[] loginData =
                outPacket.ToArray();

            Log.Inform(
                "[PrLogin] Response={0}",
                response);

            Log.Inform(
                "[PrLogin] AccountID={0}",
                c.Account != null
                    ? c.Account.Id
                    : 0);

            Log.Inform(
                "[PrLogin] Length={0}",
                loginData.Length);

            Log.Hex(
                "[PrLogin] HEX",
                loginData);

            // -------------------------------------------------
            // SEND
            // -------------------------------------------------

            c.Send(outPacket);

            Log.Inform(
                "[Login] PrLogin sent.");

            Log.Inform(
                "[Login] ========================================");
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[Login] Exception:");

            Log.Warn(
                ex.ToString());

            throw;
        }
    }

    // =========================================================
    // KART DATA
    // =========================================================


    public static void EncodeKartData(
        OutPacket outPacket)
    {
        try
        {
            // Apply kart data matching the current Kart_id.
            KartSpec.Kart_01.Kart_250();

            outPacket.WriteEncryptedInt(
                Kart.SpeedSlotCapacity);

            outPacket.WriteEncryptedByte(1);

            outPacket.WriteEncryptedBool(
                Kart.motorcycleType);

            outPacket.WriteEncryptedFloat(
                Kart.Mass);

            outPacket.WriteEncryptedFloat(
                Kart.AirFriction);

            outPacket.WriteEncryptedFloat(
                Kart.DragFactor);

            outPacket.WriteEncryptedFloat(
                Kart.ForwardAccelForce);

            outPacket.WriteEncryptedFloat(
                Kart.BackwardAccelForce);

            outPacket.WriteEncryptedFloat(
                Kart.GripBrakeForce);

            outPacket.WriteEncryptedFloat(
                Kart.SlipBrakeForce);

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
                Kart.DriftEscapeForce);

            outPacket.WriteEncryptedFloat(
                Kart.CornerDrawFactor);

            outPacket.WriteEncryptedFloat(
                Kart.DriftLeanFactor);

            outPacket.WriteEncryptedFloat(
                Kart.SteerLeanFactor);

            outPacket.WriteEncryptedFloat(
                Kart.DriftMaxGauge);

            outPacket.WriteEncryptedFloat(
                Kart.NormalBoosterTime);

            outPacket.WriteEncryptedFloat(
                Kart.TeamBoosterTime);

            outPacket.WriteEncryptedFloat(
                Kart.BoostAccelFactor);

            Log.Inform(
                "[EncodeKartData] KartId={0} encoded. DragFactor={1}, ForwardAccelForce={2}",
                Kart.StartGameData.Kart_id,
                Kart.DragFactor,
                Kart.ForwardAccelForce);
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[EncodeKartData] {0}",
                ex);
        }
    }
    // =========================================================
    // START SCENARIO
    // =========================================================

    [PacketHandler(RTTIValues.PqStartScenario)]
    public static void HandleStartScenario(
        KartClient c,
        InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.PrStartScenario);

        outPacket.WriteShort(
            p.ReadShort());

        outPacket.WriteByte(
            p.ReadByte());

        outPacket.WriteByte(
            p.ReadByte());

        outPacket.WriteByte(0);

        c.Send(outPacket);
    }

    // =========================================================
    // START CHALLENGE
    // =========================================================

    [PacketHandler(RTTIValues.PqStartChallenge)]
    public static void HandleStartChallenge(
        KartClient c,
        InPacket p)
    {
        if (c.Player == null)
            return;

        byte challengeId =
            p.ReadByte();

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.PrStartChallenge);

        outPacket.WriteByte(
            challengeId);

        EncodeKartData(
            outPacket);

        c.Send(outPacket);
    }

    // =========================================================
    // START SINGLE
    // =========================================================

    [PacketHandler(RTTIValues.LoRqStartSinglePacket)]
    public static void HandleStartSingle(
        KartClient c,
        InPacket p)
    {
        try
        {
            byte[] leftover =
                p.ReadLeftoverBytes();

            Log.Hex(
                "[LoRqStartSingle] Payload",
                leftover);

            using OutPacket outPacket =
                new OutPacket(
                    RTTIValues.LoRqStartSinglePacket);

            outPacket.WriteBool(true);

            c.Send(outPacket);
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[LoRqStartSingle] {0}",
                ex);
        }
    }

    // =========================================================
    // COMPLETE SCENARIO SINGLE
    // =========================================================

    [PacketHandler(RTTIValues.PqCompleteScenarioSingle)]
    public static void HandleCompleteScenarioSingle(
        KartClient c,
        InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.PrCompleteScenarioSingle);

        outPacket.WriteShort(
            p.ReadShort());

        outPacket.WriteByte(
            p.ReadByte());

        outPacket.WriteByte(
            p.ReadByte());

        outPacket.WriteByte(0);

        c.Send(outPacket);
    }

    // =========================================================
    // CHANNEL SWITCH
    // =========================================================

    [PacketHandler(RTTIValues.PqChannelSwitch)]
    public static void HandleChannelSwitch(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        if (c.Account == null)
        {
            Log.Warn(
                "[ChannelSwitch] Account is null.");

            return;
        }

        if (c.Player == null)
        {
            Log.Warn(
                "[ChannelSwitch] Player is null.");

            return;
        }

        if (p.Available < 3)
        {
            Log.Warn(
                "[ChannelSwitch] Invalid packet length.");

            return;
        }

        byte rawId =
            p.ReadByte();

        byte id =
            (byte)(rawId - 1);

        short unk =
            p.ReadShort();

        ChannelSwitchResponse response =
            ChannelSwitchResponse.Success;

        // -------------------------------------------------
        // MAIN
        // -------------------------------------------------

        if (id == 0)
        {
            response =
                ChannelSwitchResponse.GoToMain;

            if (c.Player.Channel != null)
            {
                c.Player.Channel.Remove(
                    c.Player);
            }
        }

        // -------------------------------------------------
        // INVALID
        // -------------------------------------------------

        else if (!Server.Channels.Contains(id))
        {
            response =
                ChannelSwitchResponse.Fail;
        }

        // -------------------------------------------------
        // FULL
        // -------------------------------------------------

        else
        {
            Channel channel =
                Server.Channels[id];

            if (channel.Count >=
                channel.Capacity)
            {
                response =
                    ChannelSwitchResponse.Full;
            }
        }

        // -------------------------------------------------
        // UDP BACKUP
        // -------------------------------------------------

        if (c.UDPEndPoint != null)
        {
            IPEndPoint endpoint =
                new IPEndPoint(
                    c.UDPEndPoint.Address,
                    c.UDPEndPoint.Port);

            lock (Server.UdpBackup)
            {
                Server.UdpBackup[
                    c.Account.Id] =
                    endpoint;
            }

            Log.Inform(
                "[UDP] Backup Account={0} Endpoint={1}",
                c.Account.Id,
                endpoint);
        }
        else
        {
            Log.Inform(
                "[UDP] No UDP endpoint for Account={0}",
                c.Account.Id);
        }

        // -------------------------------------------------
        // SAVE
        // -------------------------------------------------

        if (response ==
            ChannelSwitchResponse.Success)
        {
            c.Account.Save();

            c.Player.Save();

            if (c.Player.Channel != null)
            {
                c.Player.Channel.Remove(
                    c.Player);
            }
        }

        // -------------------------------------------------
        // RESPONSE
        // -------------------------------------------------

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.PrChannelSwitch);

        outPacket.WriteInt(
            (int)response);

        outPacket.WriteShort(
            (short)(
                id != 1
                    ? id
                    : 0));

        outPacket.WriteShort(
            unk);

        outPacket.WriteEndpoint(
            Server.RemoteEndPoint);

        c.Send(outPacket);
    }

    // =========================================================
    // UPDATE TB
    // =========================================================

    [PacketHandler(RTTIValues.UpdateTbPacket)]
    public static void HandleUpdateTb(
        KartClient c,
        InPacket p)
    {
        if (c == null)
            return;

        if (p.Available < 12)
            return;

        uint d =
            p.ReadUInt();

        uint e =
            p.ReadUInt();

        uint f =
            p.ReadUInt();

        string g =
            p.ReadString();

        using OutPacket outPacket =
            new OutPacket(
                RTTIValues.UpdateTbPacket);

        outPacket.WriteUInt(d);
        outPacket.WriteUInt(e);
        outPacket.WriteUInt(f);
        outPacket.WriteString(g);

        byte[] data =
            outPacket.ToArray();

        Log.Inform(
            "[DEBUG] UpdateTb Length={0}",
            data.Length);

        Log.Inform(
            "[DEBUG] UpdateTb HEX={0}",
            Convert.ToHexString(data));

        c.Send(outPacket);
    }
}