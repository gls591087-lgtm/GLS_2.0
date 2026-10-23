using System;
using System.Net;
using System.Net.Sockets;
using KartRider.IO;
using KartRider.IO.Packet;
using KartRider.Security;

namespace KartRider.Net;

public abstract class Session
{
    private readonly Socket _socket;
    private byte[] _buffer = Array.Empty<byte>();
    private int _bufferIndex;
    private bool _header;
    private bool _connected;
    private readonly string _label;
    private uint riv;
    private uint siv;
    private readonly object _lock;

    public string Label => _label;

    public bool IsConnected => _connected;

    public IPEndPoint RemoteEndPoint
    {
        get
        {
            try
            {
                return (IPEndPoint)_socket.RemoteEndPoint!;
            }
            catch
            {
                return new IPEndPoint(0L, 0);
            }
        }
    }

    protected Session(Socket socket)
    {
        _socket = socket ??
            throw new ArgumentNullException(nameof(socket));

        _socket.NoDelay = true;

        _label =
            _socket.RemoteEndPoint?.ToString()
            ?? "Unknown";

        _connected = true;
        _lock = new object();

        InitiateReceive(
            4u,
            header: true);
    }

    private void InitiateReceive(
        uint length,
        bool header = false)
    {
        if (!_connected)
            return;

        if (length == 0)
        {
            Close();
            return;
        }

        _header = header;

        _buffer = new byte[length];

        _bufferIndex = 0;

        BeginReceive();
    }

    private void BeginReceive()
    {
        if (!_connected)
            return;

        try
        {
            SocketError error = SocketError.Success;

            _socket.BeginReceive(
                _buffer,
                _bufferIndex,
                _buffer.Length - _bufferIndex,
                SocketFlags.None,
                out error,
                EndReceive,
                null);

            if (error != SocketError.Success)
            {
                Log.Warn(
                    "[TCP] BeginReceive failed from {0}: {1}",
                    Label,
                    error);

                Close();
            }
        }
        catch (ObjectDisposedException)
        {
            Close();
        }
        catch (SocketException ex)
        {
            Log.Warn(
                "[TCP] BeginReceive exception from {0}: {1}",
                Label,
                ex.Message);

            Close();
        }
    }

    private void EndReceive(IAsyncResult iar)
    {
        if (!_connected)
            return;

        int received;

        try
        {
            SocketError error = SocketError.Success;

            received =
                _socket.EndReceive(
                    iar,
                    out error);

            Log.Inform(
                "[TCP] EndReceive from {0}: {1} bytes",
                Label,
                received);

            if (received == 0)
            {
                Log.Warn(
                    "[TCP] Remote closed connection: {0}",
                    Label);

                Close();
                return;
            }

            if (error != SocketError.Success)
            {
                Log.Warn(
                    "[TCP] Receive error from {0}: {1}",
                    Label,
                    error);

                Close();
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            Close();
            return;
        }
        catch (SocketException ex)
        {
            Log.Warn(
                "[TCP] EndReceive exception from {0}: {1}",
                Label,
                ex.Message);

            Close();
            return;
        }

        _bufferIndex += received;

        if (_bufferIndex < _buffer.Length)
        {
            BeginReceive();
            return;
        }

        if (_header)
        {
            uint packetLength =
                BitConverter.ToUInt32(
                    _buffer,
                    0);

            Log.Inform(
                "[TCP] HEADER raw Length={0}",
                packetLength);

            if (riv != 0)
            {
                packetLength =
                    riv ^
                    packetLength ^
                    0xF834A608u;

                Log.Inform(
                    "[TCP] HEADER decoded Length={0}, RIV=0x{1:X8}",
                    packetLength,
                    riv);
            }

            /*
             * Defensive check.
             *
             * If the TCP packet length is abnormally large,
             * do not keep allocating memory due to invalid framing.
             */
            if (packetLength == 0 ||
                packetLength > 16 * 1024 * 1024)
            {
                Log.Warn(
                    "[TCP] Invalid packet length={0} from {1}.",
                    packetLength,
                    Label);

                Close();
                return;
            }

            InitiateReceive(
                packetLength);

            return;
        }

        byte[] packetBuffer = _buffer;

        if (riv != 0)
        {
            if (packetBuffer.Length < 4)
            {
                Log.Warn(
                    "[TCP] Encrypted packet too small: {0} bytes.",
                    packetBuffer.Length);

                Close();
                return;
            }

            int bufferLength =
                packetBuffer.Length - 4;

            uint originalChecksum =
                riv ^
                BitConverter.ToUInt32(
                    packetBuffer,
                    bufferLength) ^
                0xC9F84A90u;

            Array.Resize(
                ref packetBuffer,
                bufferLength);

            uint checksum =
                HashCryptograph.HashPacket(
                    packetBuffer,
                    riv,
                    pEncrypt: false,
                    (uint)bufferLength);

            Log.Inform(
                "[TCP] Checksum Original=0x{0:X8}, Calculated=0x{1:X8}",
                originalChecksum,
                checksum);

            if (originalChecksum != checksum)
            {
                Log.Warn(
                    "Different checksum while decrypting from {0}.",
                    Label);
            }

            riv += 21446425u;

            if (riv == 0)
                riv = 1u;
        }

        try
        {
            OnPacket(
                new InPacket(packetBuffer));
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[TCP] OnPacket exception from {0}: {1}",
                Label,
                ex);

            Close();
            return;
        }

        InitiateReceive(
            4u,
            header: true);
    }

    public void SendPatchData(
        Version version,
        string patchLocation)
    {
        if (patchLocation == null)
            throw new ArgumentNullException(
                nameof(patchLocation));

        uint val_first = 0u;
        uint val_second = 0u;

        short localisation =
            (short)version.Localisation;

        localisation *= 1000;

        localisation +=
            (short)(
                version.TestServer
                    ? 1
                    : 2);

        using (OutPacket outPacket =
            new OutPacket(
                RTTIValues.PcFirstMessage))
        {
            outPacket.WriteShort(
                localisation);

            outPacket.WriteShort(
                version.Major);

            outPacket.WriteShort(
                version.Minor);

            outPacket.WriteString(
                patchLocation);

            outPacket.WriteUInt(
                val_first);

            outPacket.WriteUInt(
                val_second);

            /*
             * Value confirmed with the v1.26 client.
             * 118, not 224 as in the original server pack.
             */
            outPacket.WriteByte(
                118);

            outPacket.WriteHexString(
                "03 00 09 03 00 00 03 00 00 00 00 00 " +
                "83 A3 E5 47 00 00 15 00 0D 00 00 00 " +
                "03 00 00 00 00 00 65");

            Send(outPacket);
        }

        riv =
            val_first ^
            val_second;

        siv =
            val_first ^
            val_second;

        /*
         * If both are 0, subsequent packets use plaintext framing.
         * This is intentional because val_first and val_second are currently 0.
         */
    }

    public void Send(
        OutPacket outPacket)
    {
        if (outPacket == null)
            throw new ArgumentNullException(
                nameof(outPacket));

        Log.Inform(
            "Sent {0} packet to {1}.",
            outPacket.RTTIValue.ToString(),
            Label);

        Send(
            outPacket.ToArray());
    }

    public void Send(
        byte[] buffer)
    {
        if (!_connected)
            return;

        if (buffer == null)
            throw new ArgumentNullException(
                nameof(buffer));

        lock (_lock)
        {
            if (!_connected)
                return;

            int extraLength =
                siv != 0
                    ? 8
                    : 4;

            byte[] data =
                new byte[
                    buffer.Length +
                    extraLength];

            if (siv == 0)
            {
                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        buffer.Length),
                    0,
                    data,
                    0,
                    4);
            }
            else
            {
                uint newHash =
                    HashCryptograph.HashPacket(
                        buffer,
                        siv,
                        pEncrypt: true,
                        (uint)buffer.Length);

                uint encodedLength =
                    siv ^
                    (uint)(buffer.Length + 4) ^
                    0xF834A608u;

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        encodedLength),
                    0,
                    data,
                    0,
                    4);

                uint checksum =
                    siv ^
                    newHash ^
                    0xC9F84A90u;

                Buffer.BlockCopy(
                    BitConverter.GetBytes(
                        checksum),
                    0,
                    data,
                    data.Length - 4,
                    4);

                siv += 21446425u;

                if (siv == 0)
                    siv = 1u;
            }

            Buffer.BlockCopy(
                buffer,
                0,
                data,
                4,
                buffer.Length);

            SendRaw(data);
        }
    }

    public void SendRaw(
        byte[] data)
    {
        if (!_connected)
            return;

        if (data == null)
            throw new ArgumentNullException(
                nameof(data));

        int offset = 0;

        while (offset < data.Length)
        {
            int sent;

            try
            {
                SocketError error =
                    SocketError.Success;

                sent =
                    _socket.Send(
                        data,
                        offset,
                        data.Length - offset,
                        SocketFlags.None,
                        out error);

                if (sent == 0 ||
                    error != SocketError.Success)
                {
                    throw new PacketSendException();
                }
            }
            catch
            {
                Close();
                throw;
            }

            offset += sent;
        }
    }

    public void Close()
    {
        bool shouldDisconnect = false;

        lock (_lock)
        {
            if (!_connected)
                return;

            _connected = false;
            shouldDisconnect = true;
        }

        try
        {
            if (_socket.Connected)
            {
                try
                {
                    _socket.Shutdown(
                        SocketShutdown.Both);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        try
        {
            _socket.Close();
        }
        catch
        {
        }

        if (shouldDisconnect)
        {
            Log.Warn(
                "[TCP] Disconnecting session: {0}",
                Label);

            try
            {
                OnDisconnect();
            }
            catch (Exception ex)
            {
                Log.Warn(
                    "[TCP] OnDisconnect exception for {0}: {1}",
                    Label,
                    ex);
            }
        }
    }

    public string GetRemoteAddress()
    {
        try
        {
            return
                ((IPEndPoint)_socket.RemoteEndPoint!)
                .Address
                .ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    public abstract void OnDisconnect();

    public abstract void OnPacket(
        InPacket inPacket);
}