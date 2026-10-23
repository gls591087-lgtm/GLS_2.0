using KartRider.Data;
using KartRider.Game;
using KartRider.Game.Data;
using KartRider.IO;
using KartRider.IO.Packet;
using KartRider.Security;
using KartRider.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KartRider.Net;

internal static class Server
{
    private static bool isAlive;

    private static readonly ManualResetEvent AcceptDone =
        new ManualResetEvent(false);

    public static object mLock = new object();

    public static TcpListener Listener { get; private set; }

    public static UdpListener UdpListener { get; private set; }

    public static IPEndPoint RemoteEndPoint { get; private set; }

    public static Channels Channels { get; private set; }

    public static KartClients Clients { get; private set; }

    public static Garages Garages { get; private set; }

    // =========================================================
    // UDP SESSION
    // =========================================================

    // AccountId -> KartClient
    //
    // Table for keeping UDP sessions alive briefly after a TCP disconnect.
    public static Dictionary<int, KartClient> UdpClients
    {
        get;
        private set;
    }

    // AccountId -> most recent UDP endpoint
    public static Dictionary<int, IPEndPoint> UdpBackup
    {
        get;
        private set;
    }

    // AccountId -> UDP packet received before the TCP connection
    private static readonly Dictionary<int, PendingUdp> PendingUdpClients =
        new Dictionary<int, PendingUdp>();

    public static int AutoRestartTIme
    {
        get;
        private set;
    }

    public static bool IsAlive
    {
        get => isAlive;

        set
        {
            isAlive = value;

            if (!value)
            {
                AcceptDone.Set();
            }
        }
    }

    // =========================================================
    // START
    // =========================================================

    [STAThread]
    public static void Start()
    {
        while (true)
        {
            Channels = new Channels();

            Channels.Add(
                new Channel(1, "novice", 60));

            Channels.Add(
                new Channel(2, "rookieIntro", 60));

            Channels.Add(
                new Channel(3, "rookie", 60));

            Channels.Add(
                new Channel(4, "lv3", 60));

            Channels.Add(
                new Channel(5, "lv2", 60));

            Clients = new KartClients();

            Garages = new Garages();

            UdpClients =
                new Dictionary<int, KartClient>();

            UdpBackup =
                new Dictionary<int, IPEndPoint>();

            lock (mLock)
            {
                PendingUdpClients.Clear();
            }

            IsAlive = false;

            Log.Entitle(
                "KartRider v.{0}.{1}",
                Constants.Version.Major,
                Constants.Version.Minor);

            try
            {
                Settings.Initialize();

                // =================================================
                // LOCAL MODE
                // =================================================

                if (Database.Host.Equals(
                        "local",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Database.EnableLocalMode();
                }

                // =================================================
                // AUTO RESTART
                // =================================================

                AutoRestartTIme =
                    Settings.GetInt(
                        "Server/AutoRestartTime");

                Log.Inform(
                    "Automatic restart time set to {0} seconds.",
                    AutoRestartTIme);

                // =================================================
                // HANDLER
                // =================================================

                HandlerStore.Initialize();

                // =================================================
                // DATABASE
                // =================================================

                if (!Database.LocalMode)
                {
                    Database.Test();

                    Database.Analyze();

                    using (
                        Database.TemporarySchema("kcdb"))
                    {
                        DataProvider.Initialize();
                    }

                    Database.ExecuteScript(
                        Database.Host,
                        Database.Username,
                        Database.Password,
                        "UPDATE {0}.accounts SET SessionIP = ''",
                        Database.Schema);
                }
                else
                {
                    // EnableLocalMode() initializes this once,
                    // but it is initialized again here.
                    DataProvider.Initialize();
                }

                // =================================================
                // PORT
                // =================================================

                int port =
                    Settings.GetInt(
                        "Server/Port");

                // =================================================
                // EXTERNAL ENDPOINT
                // =================================================

                RemoteEndPoint =
                    new IPEndPoint(
                        Settings.GetIPAddress(
                            "Server/ExternalIP"),
                        port);

                Log.Inform(
                    "[Server] Remote endpoint = {0}.",
                    RemoteEndPoint);

                // =================================================
                // TCP
                // =================================================

                Listener =
                    new TcpListener(
                        IPAddress.Any,
                        port);

                Listener.Start();

                Log.Inform(
                    "Initialized clients listener on {0}.",
                    Listener.LocalEndpoint);

                // =================================================
                // UDP
                // =================================================

                UdpListener =
                    new UdpListener(
                        checked((ushort)port),
                        OnUDPPacketReceived);

                Log.Inform(
                    "Initialized UDP clients listener on {0}.",
                    UdpListener.LocalEndpoint);

                // =================================================
                // SERVER READY
                // =================================================

                IsAlive = true;
            }
            catch (Exception exception)
            {
                Log.Error(exception);

                IsAlive = false;
            }

            // =====================================================
            // START RESULT
            // =====================================================

            if (IsAlive)
            {
                Log.Success(
                    "Server started on thread {0}.",
                    Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                Log.Inform(
                    "Could not start server because of errors.");
            }

            Log.SkipLine();

            // =====================================================
            // ACCEPT LOOP
            // =====================================================

            while (IsAlive)
            {
                try
                {
                    AcceptDone.Reset();

                    Listener.BeginAcceptSocket(
                        OnAcceptSocket,
                        null);

                    AcceptDone.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "Accept loop failed:\n{0}",
                        ex);

                    break;
                }
            }

            // =====================================================
            // CLOSE CLIENTS
            // =====================================================

            KartClient[] remainingClients =
                Clients.ToArray();

            foreach (KartClient client in remainingClients)
            {
                try
                {
                    client.Close();
                }
                catch
                {
                }
            }

            // =====================================================
            // DISPOSE
            // =====================================================

            Dispose();

            Log.Warn(
                "Server stopped.");

            // =====================================================
            // AUTO RESTART
            // =====================================================

            if (AutoRestartTIme <= 0)
                break;

            Log.Inform(
                "Attempting auto-restart in {0} seconds.",
                AutoRestartTIme);

            Thread.Sleep(
                AutoRestartTIme * 1000);
        }

        Console.Read();
    }

    // =========================================================
    // ACCEPT
    // =========================================================

    private static void OnAcceptSocket(
        IAsyncResult ar)
    {
        try
        {
            Socket socket =
                Listener.EndAcceptSocket(ar);

            KartClient client =
                new KartClient(socket);

            Log.Inform(
                "Accepted connection from {0}.",
                client.Label);

            Clients.Add(client);
        }
        catch (Exception ex)
        {
            Log.Error(
                "Accept socket failed:\n{0}",
                ex);
        }
        finally
        {
            AcceptDone.Set();
        }
    }

    // =========================================================
    // UDP RECEIVE
    // =========================================================

    private static void OnUDPPacketReceived(
        InPacket inPacket,
        IPEndPoint endpoint)
    {
        if (inPacket == null ||
            endpoint == null)
        {
            return;
        }

        try
        {
            // =====================================================
            // DECRYPTED PAYLOAD
            //
            // 00~03 : AccountId
            // 04~07 : Value
            // 08~0B : RTTI
            // 0C~   : Handler payload
            // =====================================================

            byte[] payload =
                inPacket.ReadLeftoverBytes();

            if (payload == null ||
                payload.Length < 12)
            {
                Log.Warn(
                    "[UDP] Decrypted payload too small from {0}: {1} bytes.",
                    endpoint,
                    payload?.Length ?? 0);

                return;
            }

            uint accountId =
                BitConverter.ToUInt32(
                    payload,
                    0);

            uint value =
                BitConverter.ToUInt32(
                    payload,
                    4);

            uint rtti =
                BitConverter.ToUInt32(
                    payload,
                    8);

            string rttiName =
                Enum.IsDefined(
                    typeof(RTTIValues),
                    rtti)
                    ? ((RTTIValues)rtti).ToString()
                    : "UNKNOWN";

            Log.Inform(
                "[UDP] HEADER: AccountId={0}, Value={1}, RTTI=0x{2:X8}, Name={3}",
                accountId,
                value,
                rtti,
                rttiName);

            Log.Inform(
                "[UDP] Decrypted payload received: {0} bytes from {1}.",
                payload.Length,
                endpoint);

            Log.Hex(
                "[UDP] DECRYPTED PAYLOAD",
                payload);

            // =====================================================
            // ACCOUNT ID
            // =====================================================

            int accountKey;

            try
            {
                accountKey =
                    checked((int)accountId);
            }
            catch
            {
                Log.Warn(
                    "[UDP] Invalid AccountId={0}.",
                    accountId);

                return;
            }

            KartClient client = null;

            // =====================================================
            // 1. UDP SESSION TABLE
            //
            // It remains available here after the TCP connection closes.
            // =====================================================

            if (UdpClients != null)
            {
                lock (mLock)
                {
                    UdpClients.TryGetValue(
                        accountKey,
                        out client);
                }

                if (client != null)
                {
                    Log.Inform(
                        "[UDP] Found client from UdpClients: AccountId={0}, Client={1}",
                        accountId,
                        client.Label);
                }
            }

            // =====================================================
            // 2. CURRENT TCP CLIENT
            // =====================================================

            if (client == null &&
                Clients != null)
            {
                client =
                    Clients.GetByAccountId(
                        accountKey);

                if (client != null)
                {
                    Log.Inform(
                        "[UDP] Found client from TCP Clients: AccountId={0}, Client={1}",
                        accountId,
                        client.Label);
                }
            }

            // =====================================================
            // 3. UDP ENDPOINT
            // =====================================================

            if (client == null &&
                Clients != null)
            {
                client =
                    Clients.GetByUdpEndpoint(
                        endpoint);

                if (client != null)
                {
                    Log.Inform(
                        "[UDP] Found client from UDP endpoint: {0}",
                        endpoint);
                }
            }

            // =====================================================
            // 4. UDP SOURCE
            // =====================================================

            if (client == null &&
                Clients != null)
            {
                client =
                    Clients.GetByUdpSource(
                        endpoint);

                if (client != null)
                {
                    Log.Inform(
                        "[UDP] Found client from UDP source: {0}",
                        endpoint);
                }
            }

            // =====================================================
            // CLIENT NOT FOUND
            // =====================================================

            if (client == null)
            {
                lock (mLock)
                {
                    PendingUdpClients[accountKey] =
                        new PendingUdp(
                            endpoint,
                            payload);
                }

                Log.Inform(
                    "[UDP] Pending UDP stored: AccountId={0}, Endpoint={1}, RTTI=0x{2:X8}, Name={3}",
                    accountId,
                    endpoint,
                    rtti,
                    rttiName);

                return;
            }

            // =====================================================
            // REGISTER UDP SESSION
            // =====================================================

            if (UdpClients != null)
            {
                lock (mLock)
                {
                    UdpClients[accountKey] =
                        client;
                }
            }

            // =====================================================
            // UPDATE UDP ENDPOINT
            // =====================================================

            if (client.UDPEndPoint == null ||
                !client.UDPEndPoint.Equals(endpoint))
            {
                client.SetUDPEndPoint(endpoint);

                Log.Inform(
                    "[UDP] Auto-bound UDP endpoint: " +
                    "AccountId={0}, TCP={1}, UDP={2}",
                    accountId,
                    client.RemoteEndPoint,
                    endpoint);
            }

            // =====================================================
            // LAST UDP SOURCE
            // =====================================================

            client.SetLastUdpSource(
                endpoint);

            // =====================================================
            // UDP BACKUP
            // =====================================================

            if (UdpBackup != null)
            {
                lock (mLock)
                {
                    UdpBackup[accountKey] =
                        new IPEndPoint(
                            endpoint.Address,
                            endpoint.Port);
                }
            }

            // =====================================================
            // REMOVE PENDING UDP
            // =====================================================

            lock (mLock)
            {
                PendingUdpClients.Remove(
                    accountKey);
            }

            // =====================================================
            // HANDLER PAYLOAD
            // =====================================================

            if (payload.Length > 12)
            {
                byte[] handlerPayload =
                    new byte[
                        payload.Length - 12];

                Buffer.BlockCopy(
                    payload,
                    12,
                    handlerPayload,
                    0,
                    handlerPayload.Length);

                Log.Hex(
                    "[UDP] HANDLER PAYLOAD",
                    handlerPayload);
            }

            // =====================================================
            // DISPATCH
            // =====================================================

            Log.Inform(
                "[UDP] {0} -> {1}, RTTI=0x{2:X8}, Name={3}",
                endpoint,
                client.Label,
                rtti,
                rttiName);

            HandlerStore.DispatchUdp(
                client,
                new InPacket(payload));
        }
        catch (Exception ex)
        {
            Log.Error(
                "[UDP] Processing failed:\n{0}",
                ex);
        }
    }

    // =========================================================
    // PENDING UDP
    // =========================================================

    private sealed class PendingUdp
    {
        public IPEndPoint Endpoint { get; }

        public byte[] Payload { get; }

        public DateTime ReceivedAt { get; }

        public PendingUdp(
            IPEndPoint endpoint,
            byte[] payload)
        {
            Endpoint =
                new IPEndPoint(
                    endpoint.Address,
                    endpoint.Port);

            Payload =
                payload != null
                    ? (byte[])payload.Clone()
                    : Array.Empty<byte>();

            ReceivedAt =
                DateTime.UtcNow;
        }
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    private static void Dispose()
    {
        IsAlive = false;

        // =====================================================
        // TCP
        // =====================================================

        if (Listener != null)
        {
            try
            {
                Listener.Stop();
            }
            catch
            {
            }

            Listener = null;
        }

        // =====================================================
        // UDP
        // =====================================================

        if (UdpListener != null)
        {
            try
            {
                UdpListener.Dispose();
            }
            catch
            {
            }

            UdpListener = null;
        }

        // =====================================================
        // UDP SESSION
        // =====================================================

        lock (mLock)
        {
            if (UdpClients != null)
            {
                UdpClients.Clear();
            }

            if (UdpBackup != null)
            {
                UdpBackup.Clear();
            }

            PendingUdpClients.Clear();
        }
    }
}