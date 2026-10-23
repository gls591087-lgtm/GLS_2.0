using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using KartRider.IO;
using KartRider.IO.Packet;
using KartRider.Security;

namespace KartRider.Net
{
    public sealed class UdpListener : IDisposable
    {
        private const int BufferSize = 8192;

        private const uint ChecksumConstant =
            0x4F3816C3u;

        private readonly Socket _socket;

        private readonly Action<
            InPacket,
            IPEndPoint> _handler;

        private bool _disposed;

        public IPEndPoint LocalEndpoint
        {
            get;
        }

        private sealed class ReceiveState
        {
            public byte[] Buffer { get; } =
                new byte[BufferSize];

            public EndPoint EndPoint =
                new IPEndPoint(
                    IPAddress.Any,
                    0);
        }

        public UdpListener(
            ushort port,
            Action<InPacket, IPEndPoint> handler)
        {
            _handler =
                handler ??
                throw new ArgumentNullException(
                    nameof(handler));

            LocalEndpoint =
                new IPEndPoint(
                    IPAddress.Any,
                    port);

            _socket =
                new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);

            _socket.Bind(LocalEndpoint);

            Log.Inform(
                "[UDP] Socket bound to {0}.",
                _socket.LocalEndPoint);

            BeginReceive();
        }

        private void BeginReceive()
        {
            if (_disposed)
                return;

            ReceiveState state =
                new ReceiveState();

            try
            {
                _socket.BeginReceiveFrom(
                    state.Buffer,
                    0,
                    state.Buffer.Length,
                    SocketFlags.None,
                    ref state.EndPoint,
                    EndReceive,
                    state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[UDP] BeginReceive failed:\n{0}",
                    ex);
            }
        }

        private void EndReceive(
            IAsyncResult iar)
        {
            if (_disposed)
                return;

            if (iar == null)
                return;

            ReceiveState state =
                iar.AsyncState as ReceiveState;

            if (state == null)
                return;

            int received;

            try
            {
                received =
                    _socket.EndReceiveFrom(
                        iar,
                        ref state.EndPoint);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                if (!_disposed)
                {
                    Log.Error(
                        "[UDP] Receive socket error: {0}",
                        ex.Message);

                    BeginReceive();
                }

                return;
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    Log.Error(
                        "[UDP] Receive failed:\n{0}",
                        ex);

                    BeginReceive();
                }

                return;
            }

            try
            {
                if (received < 16)
                {
                    Log.Warn(
                        "[UDP] Packet too small: {0} bytes.",
                        received);

                    return;
                }

                if (state.EndPoint is not IPEndPoint endpoint)
                    return;

                byte[] raw =
                    new byte[received];

                Buffer.BlockCopy(
                    state.Buffer,
                    0,
                    raw,
                    0,
                    received);

                Log.Inform(
                    "[UDP] Received {0} bytes from {1}.",
                    received,
                    endpoint);

                ProcessPacket(
                    raw,
                    endpoint);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[UDP] Packet processing failed:\n{0}",
                    ex);
            }
            finally
            {
                BeginReceive();
            }
        }

        private void ProcessPacket(
     byte[] data,
     IPEndPoint endpoint)
        {
            if (data == null ||
                endpoint == null)
            {
                return;
            }

            try
            {
                // =====================================================
                // UDP encrypted packet
                //
                // [IV 4]
                // [Encrypted Payload N]
                // [Remote Checksum 4]
                // =====================================================

                if (data.Length < 12)
                {
                    Log.Warn(
                        "[UDP] Invalid encrypted packet length: {0}",
                        data.Length);

                    return;
                }

                // =====================================================
                // 1. IV
                // =====================================================

                uint iv =
                    BitConverter.ToUInt32(
                        data,
                        0);

                // =====================================================
                // 2. Remote checksum
                // =====================================================

                uint remoteChecksum =
                    BitConverter.ToUInt32(
                        data,
                        data.Length - 4);

                // =====================================================
                // 3. Encrypted payload
                // =====================================================

                int payloadLength =
                    data.Length - 8;

                if (payloadLength <= 0)
                {
                    Log.Warn(
                        "[UDP] Invalid payload length: {0}",
                        payloadLength);

                    return;
                }

                byte[] packetData =
                    new byte[payloadLength];

                Buffer.BlockCopy(
                    data,
                    4,
                    packetData,
                    0,
                    payloadLength);

                Log.Inform(
                    "[UDP] IV=0x{0:X8}",
                    iv);

                Log.Inform(
                    "[UDP] Remote checksum=0x{0:X8}",
                    remoteChecksum);

                Log.Hex(
                    "[UDP] ENCRYPTED PAYLOAD",
                    packetData);

                // =====================================================
                // 4. Decrypt
                // =====================================================

                uint calculatedChecksum =
                    HashCryptograph.HashDecrypt(
                        packetData,
                        (uint)packetData.Length,
                        iv);

                Log.Inform(
                    "[UDP] Calculated checksum=0x{0:X8}",
                    calculatedChecksum);

                Log.Hex(
                    "[UDP] DECRYPTED PAYLOAD",
                    packetData);

                // =====================================================
                // 5. Expected checksum
                // =====================================================

                uint expected =
                    iv ^
                    ChecksumConstant ^
                    remoteChecksum;

                Log.Inform(
                    "[UDP] Expected checksum=0x{0:X8}",
                    expected);

                // =====================================================
                // 6. Verify
                // =====================================================

                if (calculatedChecksum != expected)
                {
                    Log.Warn(
                        "[UDP] Checksum FAIL: " +
                        "Remote=0x{0:X8}, " +
                        "Calculated=0x{1:X8}, " +
                        "Expected=0x{2:X8}",
                        remoteChecksum,
                        calculatedChecksum,
                        expected);

                    // Important:
                    // Never pass a packet that fails validation to a handler.
                    return;
                }

                Log.Inform(
                    "[UDP] Checksum OK.");

                // =====================================================
                // 7. Minimum RTTI check
                // =====================================================

                if (packetData.Length < 12)
                {
                    Log.Warn(
                        "[UDP] Decrypted UDP payload too small: {0} bytes.",
                        packetData.Length);

                    return;
                }

                uint accountId =
                    BitConverter.ToUInt32(
                        packetData,
                        0);

                uint value =
                    BitConverter.ToUInt32(
                        packetData,
                        4);

                uint rtti =
                    BitConverter.ToUInt32(
                        packetData,
                        8);

                Log.Inform(
                    "[UDP] AccountId={0}, Value={1}, RTTI=0x{2:X8}",
                    accountId,
                    value,
                    rtti);

                Log.Hex(
                    "[UDP] DECRYPTED PAYLOAD",
                    packetData);

                _handler(
                    new InPacket(packetData),
                    endpoint);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[UDP] Handler packet processing failed from {0}:\n{1}",
                    endpoint,
                    ex);
            }
        }

        public static void Decrypt(
            string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            data =
                data.Replace(" ", string.Empty)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\t", string.Empty);

            if ((data.Length & 1) != 0)
            {
                throw new ArgumentException(
                    "Hex string must contain an even number of characters.",
                    nameof(data));
            }

            byte[] bytes =
                new byte[data.Length / 2];

            for (int i = 0;
                 i < data.Length;
                 i += 2)
            {
                bytes[i / 2] =
                    byte.Parse(
                        data.Substring(i, 2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture);
            }

            Decrypt(bytes);
        }

        public static void Decrypt(
            byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(
                    nameof(data));

            if (data.Length < 16)
                throw new ArgumentException(
                    "UDP packet is too short.",
                    nameof(data));

            uint iv =
                BitConverter.ToUInt32(
                    data,
                    0);

            uint remoteChecksum =
                BitConverter.ToUInt32(
                    data,
                    data.Length - 4);

            int payloadLength =
                data.Length - 8;

            byte[] packetData =
                new byte[payloadLength];

            Buffer.BlockCopy(
                data,
                4,
                packetData,
                0,
                payloadLength);

            Log.Hex(
                "[UDP TEST] RAW",
                data);

            Log.Inform(
                "[UDP TEST] IV = 0x{0:X8}",
                iv);

            Log.Inform(
                "[UDP TEST] REMOTE = 0x{0:X8}",
                remoteChecksum);

            Log.Hex(
                "[UDP TEST] ENCRYPTED",
                packetData);

            uint checksum =
                HashCryptograph.HashDecrypt(
                    packetData,
                    (uint)packetData.Length,
                    iv);

            uint expected =
                iv ^
                ChecksumConstant ^
                remoteChecksum;

            Log.Inform(
                "[UDP TEST] HASH = 0x{0:X8}",
                checksum);

            Log.Inform(
                "[UDP TEST] EXPECTED = 0x{0:X8}",
                expected);

            Log.Hex(
                "[UDP TEST] DECRYPTED",
                packetData);

            if (checksum == expected)
            {
                Log.Inform(
                    "[UDP TEST] Checksum OK");
            }
            else
            {
                Log.Inform(
                    "[UDP TEST] Checksum FAIL");
            }
        }

        public static byte[] Encrypt(
            byte[] packet)
        {
            if (packet == null)
                throw new ArgumentNullException(
                    nameof(packet));

            if (packet.Length == 0)
                return Array.Empty<byte>();

            byte[] data =
                new byte[packet.Length + 8];

            uint siv =
                (uint)new Random(
                    unchecked(
                        (int)DateTime.Now.Ticks))
                    .Next();

            uint newHash =
                HashCryptograph.HashEncrypt(
                    packet,
                    (uint)packet.Length,
                    siv);

            uint remoteChecksum =
                siv ^
                newHash ^
                ChecksumConstant;

            Buffer.BlockCopy(
                BitConverter.GetBytes(siv),
                0,
                data,
                0,
                4);

            Buffer.BlockCopy(
                BitConverter.GetBytes(remoteChecksum),
                0,
                data,
                data.Length - 4,
                4);

            Buffer.BlockCopy(
                packet,
                0,
                data,
                4,
                packet.Length);

            return data;
        }

        public void BeginSend(
            UdpOutPacket outPacket,
            IPEndPoint endpoint)
        {
            if (_disposed)
                return;

            if (outPacket == null)
                throw new ArgumentNullException(
                    nameof(outPacket));

            if (endpoint == null)
                throw new ArgumentNullException(
                    nameof(endpoint));

            byte[] buffer =
                outPacket.ToArray();

            if (buffer.Length < 16)
                return;

            byte[] data =
                new byte[buffer.Length + 8];

            uint siv =
                (uint)new Random(
                    unchecked(
                        (int)DateTime.Now.Ticks))
                    .Next();

            uint newHash =
                HashCryptograph.HashEncrypt(
                    buffer,
                    (uint)buffer.Length,
                    siv);

            uint remoteChecksum =
                siv ^
                newHash ^
                ChecksumConstant;

            Buffer.BlockCopy(
                BitConverter.GetBytes(siv),
                0,
                data,
                0,
                4);

            Buffer.BlockCopy(
                BitConverter.GetBytes(remoteChecksum),
                0,
                data,
                data.Length - 4,
                4);

            Buffer.BlockCopy(
                buffer,
                0,
                data,
                4,
                buffer.Length);

            Log.Inform(
                "[UDP] TX {0} bytes -> {1}",
                data.Length,
                endpoint);

            Log.Hex(
                "[UDP] TX RAW",
                data);

            try
            {
                _socket.BeginSendTo(
                    data,
                    0,
                    data.Length,
                    SocketFlags.None,
                    endpoint,
                    EndSend,
                    null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[UDP] Send failed to {0}: {1}",
                    endpoint,
                    ex.Message);
            }
        }

        private void EndSend(
            IAsyncResult res)
        {
            try
            {
                _socket.EndSendTo(res);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[UDP] EndSend failed: {0}",
                    ex.Message);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _socket.Close();
            }
            catch
            {
            }

            try
            {
                _socket.Dispose();
            }
            catch
            {
            }
        }
    }
}