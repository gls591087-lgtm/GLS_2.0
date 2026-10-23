using System;
using System.Text;

namespace KartRider.IO.Packet;

public class InPacket : PacketBase
{
    private readonly byte[] _buffer;
    private int _index;

    public override int Position => _index;

    public override int Length => _buffer.Length;

    public int Available => _buffer.Length - _index;

    public InPacket(byte[] packet)
    {
        _buffer = packet ??
                  throw new ArgumentNullException(nameof(packet));

        _index = 0;
    }

    private void CheckLength(int length)
    {
        if (length < 0 ||
            _index + length > _buffer.Length)
        {
            throw new PacketReadException(
                "Not enough space");
        }
    }

    public bool ReadBool()
    {
        return ReadByte() == 1;
    }

    public byte ReadByte()
    {
        CheckLength(1);

        return _buffer[_index++];
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    public byte[] ReadBytes(int count)
    {
        CheckLength(count);

        byte[] temp = new byte[count];

        Buffer.BlockCopy(
            _buffer,
            _index,
            temp,
            0,
            count);

        _index += count;

        return temp;
    }

    public unsafe short ReadShort()
    {
        CheckLength(2);

        short value;

        fixed (byte* ptr = _buffer)
        {
            value = *(short*)(ptr + _index);
        }

        _index += 2;

        return value;
    }

    public ushort ReadUShort()
    {
        return (ushort)ReadShort();
    }

    public unsafe int ReadInt()
    {
        CheckLength(4);

        int value;

        fixed (byte* ptr = _buffer)
        {
            value = *(int*)(ptr + _index);
        }

        _index += 4;

        return value;
    }

    public uint ReadUInt()
    {
        return (uint)ReadInt();
    }

    public unsafe long ReadLong()
    {
        CheckLength(8);

        long value;

        fixed (byte* ptr = _buffer)
        {
            value = *(long*)(ptr + _index);
        }

        _index += 8;

        return value;
    }

    public ulong ReadULong()
    {
        return (ulong)ReadLong();
    }

    public string ReadString(bool ascii = false)
    {
        int length = ReadInt();

        if (!ascii)
            length *= 2;

        CheckLength(length);

        if (ascii)
        {
            return Encoding.ASCII.GetString(
                ReadBytes(length));
        }

        return Encoding.Unicode.GetString(
            ReadBytes(length));
    }

    public byte[] ReadLeftoverBytes()
    {
        return ReadBytes(Available);
    }

    public void Skip(int count)
    {
        CheckLength(count);

        _index += count;
    }

    // ============================================================
    // PEEK
    // ============================================================

    public uint PeekUInt()
    {
        return PeekUIntAt(_index);
    }

    public uint PeekUIntAt(int offset)
    {
        if (offset < 0 ||
            offset + 4 > _buffer.Length)
        {
            throw new PacketReadException(
                "Not enough space");
        }

        return BitConverter.ToUInt32(
            _buffer,
            offset);
    }

    // ============================================================
    // REMAINING BYTES
    // ============================================================

    public byte[] GetRemainingBytes()
    {
        if (Available <= 0)
            return Array.Empty<byte>();

        byte[] result = new byte[Available];

        Buffer.BlockCopy(
            _buffer,
            _index,
            result,
            0,
            Available);

        return result;
    }

    // ============================================================
    // REMAINING HEX
    // ============================================================
    public byte PeekByte()
    {
        if (Available <= 0)
            throw new PacketReadException("Not enough space");

        return _buffer[_index];
    }
    public string GetRemainingHex()
    {
        if (Available <= 0)
            return string.Empty;

        return Convert.ToHexString(
            _buffer,
            _index,
            Available);
    }

    // ============================================================
    // TO ARRAY
    // ============================================================

    public override byte[] ToArray()
    {
        byte[] final =
            new byte[_buffer.Length];

        Buffer.BlockCopy(
            _buffer,
            0,
            final,
            0,
            _buffer.Length);

        return final;
    }
}