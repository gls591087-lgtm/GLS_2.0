using System;
using System.Net;

namespace KartRider.Net;

public sealed class UdpPacket
{
    public uint Header { get; }
    public uint SessionId { get; }
    public uint Rtti { get; }
    public uint Value { get; }
    public IPEndPoint Endpoint { get; }

    private UdpPacket(
        uint header,
        uint sessionId,
        uint rtti,
        uint value,
        IPEndPoint endpoint)
    {
        Header = header;
        SessionId = sessionId;
        Rtti = rtti;
        Value = value;
        Endpoint = endpoint;
    }

    public static bool TryParse(
        byte[] data,
        IPEndPoint endpoint,
        out UdpPacket packet)
    {
        packet = null!;

        if (data == null || data.Length < 16)
            return false;

        packet = new UdpPacket(
            BitConverter.ToUInt32(data, 0),
            BitConverter.ToUInt32(data, 4),
            BitConverter.ToUInt32(data, 8),
            BitConverter.ToUInt32(data, 12),
            endpoint);

        return true;
    }
}