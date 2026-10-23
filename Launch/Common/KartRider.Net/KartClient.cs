using System;
using System.Net;
using System.Net.Sockets;
using KartRider.Game;
using KartRider.Game.Players;
using KartRider.IO;
using KartRider.IO.Packet;

namespace KartRider.Net;

public sealed class KartClient : Session
{
    // =========================================================
    // TCP
    // =========================================================

    private readonly IPEndPoint _remoteEndPoint;

    public IPEndPoint RemoteEndPoint
    {
        get
        {
            return _remoteEndPoint;
        }
    }

    // =========================================================
    // ACCOUNT / PLAYER
    // =========================================================

    public Account Account { get; set; }

    public Player Player { get; set; }

    // =========================================================
    // UDP
    // =========================================================

    public IPEndPoint LastUdpSource
    {
        get;
        private set;
    }

    public IPEndPoint UDPEndPoint
    {
        get;
        private set;
    }

    public bool IsUdpBound
    {
        get
        {
            return UDPEndPoint != null ||
                   LastUdpSource != null;
        }
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public KartClient(Socket socket)
        : base(socket)
    {
        if (socket == null)
        {
            throw new ArgumentNullException(
                nameof(socket));
        }

        _remoteEndPoint =
            socket.RemoteEndPoint as IPEndPoint;

        UDPEndPoint = null;
        LastUdpSource = null;

        Log.Inform(
            "[Client] TCP endpoint = {0}",
            _remoteEndPoint ??
            new IPEndPoint(
                IPAddress.None,
                0));

        SendPatchData(
            Constants.Version,
            "http://kart.dn.nexoncdn.co.kr/patch");
    }

    // =========================================================
    // UDP ADVERTISED ENDPOINT
    // =========================================================

    public void SetUDPEndPoint(
        IPEndPoint endpoint)
    {
        if (endpoint == null)
            return;

        UDPEndPoint =
            new IPEndPoint(
                endpoint.Address,
                endpoint.Port);

        Log.Inform(
            "[UDP] Updated UDP Endpoint for {0}: {1}",
            RemoteEndPoint,
            UDPEndPoint);
    }

    // =========================================================
    // UDP ACTUAL SOURCE
    // =========================================================

    public void SetLastUdpSource(
        IPEndPoint endpoint)
    {
        if (endpoint == null)
            return;

        LastUdpSource =
            new IPEndPoint(
                endpoint.Address,
                endpoint.Port);

        Log.Inform(
            "[UDP] {0} actual UDP source = {1}.",
            Label,
            LastUdpSource);
    }

    // =========================================================
    // UDP ENDPOINT MATCH
    // =========================================================

    public bool MatchesUdpEndpoint(
        IPEndPoint endpoint)
    {
        if (endpoint == null)
            return false;

        if (LastUdpSource != null &&
            LastUdpSource.Address.Equals(
                endpoint.Address) &&
            LastUdpSource.Port ==
                endpoint.Port)
        {
            return true;
        }

        if (UDPEndPoint != null &&
            UDPEndPoint.Address.Equals(
                endpoint.Address) &&
            UDPEndPoint.Port ==
                endpoint.Port)
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // DISCONNECT
    // =========================================================

    public override void OnDisconnect()
    {
        // =====================================================
        // Preserve the UDP binding first.
        //
        // Even if Player.Save() or another operation throws,
        // the UDP session must remain active.
        // =====================================================

        try
        {
            if (Account != null)
            {
                int accountId =
                    Account.Id;

                if (UDPEndPoint != null)
                {
                    Server.UdpBackup[accountId] =
                        new IPEndPoint(
                            UDPEndPoint.Address,
                            UDPEndPoint.Port);

                    Log.Inform(
                        "[UDP] Preserved UDP binding: " +
                        "AccountId={0}, UDP={1}",
                        accountId,
                        UDPEndPoint);
                }

                if (Server.UdpClients != null &&
                    IsUdpBound)
                {
                    Server.UdpClients[accountId] =
                        this;

                    Log.Inform(
                        "[UDP] Preserved UDP session: " +
                        "AccountId={0}, UDP={1}",
                        accountId,
                        LastUdpSource ??
                        UDPEndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                "[UDP] Failed to preserve UDP session:\n{0}",
                ex);
        }

        // =====================================================
        // Account
        // =====================================================

        if (Account != null)
        {
            try
            {
                Account.SessionIP =
                    string.Empty;

                Account.Save();
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Client] Account cleanup failed: {0}",
                    ex);
            }
        }

        // =====================================================
        // Player
        // =====================================================

        if (Player != null)
        {
            try
            {
                if (Player.Garage != null)
                {
                    Player.Garage.Remove(
                        Player);
                }
                else if (Player.Room != null)
                {
                    Player.Room.Remove(
                        Player);
                }

                if (Player.Channel != null)
                {
                    Player.Channel.Remove(
                        Player);
                }

                Player.Save();
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Client] Player cleanup failed: {0}",
                    ex);
            }
        }

        Log.Inform(
            "Lost connection from {0}.",
            Label);
    }

    // =========================================================
    // TCP PACKET
    // =========================================================

    public override void OnPacket(
        InPacket inPacket)
    {
        if (inPacket == null)
            return;

        try
        {
            Log.Inform(
                "[TCP] Dispatching packet from {0}, Length={1}",
                Label,
                inPacket.Length);

            HandlerStore.Dispatch(
                this,
                inPacket);
        }
        catch (Exception ex)
        {
            Log.Error(
                "[TCP] Handler dispatch exception:\n{0}",
                ex);
        }
    }
}