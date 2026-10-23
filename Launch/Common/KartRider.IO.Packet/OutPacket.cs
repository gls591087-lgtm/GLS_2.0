    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using KartRider.Net;

    namespace KartRider.IO.Packet
    {
        public class OutPacket : PacketBase, IDisposable
        {
            private MemoryStream m_stream;
            private bool m_disposed;

            public RTTIValues RTTIValue { get; private set; }

            public override int Length => m_stream == null ? 0 : (int)m_stream.Position;
        
            public override int Position => m_stream == null ? 0 : (int)m_stream.Position;

            public bool Disposed => m_disposed;

            public OutPacket(int size = 64)
            {
                m_stream = new MemoryStream(size);
                m_disposed = false;
            }

            public OutPacket(RTTIValues rttiValue)
                : this()
            {
                RTTIValue = rttiValue;
                WriteUInt((uint)rttiValue);
            }

            private void ThrowIfDisposed()
            {
                if (m_disposed || m_stream == null)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            private void Append(long value, int byteCount)
            {
                ThrowIfDisposed();

                for (int i = 0; i < byteCount; i++)
                {
                    m_stream.WriteByte((byte)value);
                    value >>= 8;
                }
            }

            public void WriteBool(bool value)
            {
                WriteByte((byte)(value ? 1 : 0));
            }

            public void WriteByte(byte value = 0)
            {
                ThrowIfDisposed();
                m_stream.WriteByte(value);
            }

            public void WriteSByte(sbyte value = 0)
            {
                WriteByte((byte)value);
            }

            public void WriteBytes(params byte[] value)
            {
                ThrowIfDisposed();

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                m_stream.Write(value, 0, value.Length);
            }

            public void WriteShort(short value = 0)
            {
                Append(value, 2);
            }

            public void WriteUShort(ushort value = 0)
            {
                WriteShort((short)value);
            }

            public void WriteInt(int value = 0)
            {
                Append(value, 4);
            }

            public void WriteUInt(uint value = 0)
            {
                WriteInt(unchecked((int)value));
            }

            public void WriteLong(long value = 0)
            {
                Append(value, 8);
            }

            public void WriteULong(ulong value = 0)
            {
                WriteLong(unchecked((long)value));
            }

            public void WriteFloat(float value = 0f)
            {
                WriteBytes(BitConverter.GetBytes(value));
            }

            public void WriteDateTime(ushort[] value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Length < 2)
                    throw new ArgumentException(
                        "DateTime array must contain at least 2 elements.",
                        nameof(value));

                WriteUShort(value[0]);
                WriteUShort(value[1]);
            }

            public void Skip(int count)
            {
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                for (int i = 0; i < count; i++)
                {
                    WriteByte(0);
                }
            }

            public void WriteString(string value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                WriteInt(value.Length);
                WriteString(value, value.Length);
            }

            public void WriteString(string value, int length)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (length < 0 || length > value.Length)
                    throw new ArgumentOutOfRangeException(nameof(length));

                byte[] bytes = Encoding.Unicode.GetBytes(value);

                int i;

                for (i = 0; i < value.Length && i < length; i++)
                {
                    int offset = i * 2;

                    WriteByte(bytes[offset]);
                    WriteByte(bytes[offset + 1]);
                }

                for (; i < length; i++)
                {
                    WriteShort(0);
                }
            }

            public void WriteHexString(string value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                value = value.Replace(" ", "");

                if ((value.Length & 1) != 0)
                    throw new FormatException("Hex string must contain an even number of characters.");

                for (int i = 0; i < value.Length; i += 2)
                {
                    WriteByte(
                        byte.Parse(
                            value.Substring(i, 2),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture));
                }
            }

            public void WriteEncryptedBool(bool value = false)
            {
                WriteBytes(
                    Crypto.encryptBytes(
                        new byte[]
                        {
                            (byte)(value ? 1 : 0)
                        }));
            }

            public void WriteEncryptedByte(byte value = 0)
            {
                WriteBytes(
                    Crypto.encryptBytes(
                        new byte[]
                        {
                            value
                        }));
            }

            public void WriteEncryptedFloat(float value = 0f)
            {
                WriteBytes(
                    Crypto.encryptBytes(
                        BitConverter.GetBytes(value)));
            }

            public void WriteEncryptedInt(int value = 0)
            {
                WriteBytes(
                    Crypto.encryptBytes(
                        BitConverter.GetBytes(value)));
            }

            public void WriteEndpoint(IPEndPoint endpoint)
            {
                if (endpoint == null)
                    throw new ArgumentNullException(nameof(endpoint));

                WriteEndpoint(
                    endpoint.Address,
                    (ushort)endpoint.Port);
            }

            public void WriteEndpoint(IPAddress ip, ushort port)
            {
                if (ip == null)
                    throw new ArgumentNullException(nameof(ip));

                WriteBytes(ip.GetAddressBytes());
                WriteUShort(port);
            }

            public void WriteEncodedFloat(float value)
            {
                WriteBytes(
                    CryptoConstants.Encode(
                        BitConverter.GetBytes(value)));
            }

            public void WriteEncodedInt(int value)
            {
                WriteBytes(
                    CryptoConstants.Encode(
                        BitConverter.GetBytes(value)));
            }

            public void WriteEncodedByte(byte value)
            {
                WriteBytes(
                    CryptoConstants.Encode(
                        new byte[]
                        {
                            value
                        }));
            }

            public override byte[] ToArray()
            {
                ThrowIfDisposed();
                return m_stream.ToArray();
            }

            public void Dispose()
            {
                if (m_disposed)
                    return;

                m_disposed = true;

                if (m_stream != null)
                {
                    m_stream.Dispose();
                    m_stream = null;
                }

                GC.SuppressFinalize(this);
            }
        }
    }