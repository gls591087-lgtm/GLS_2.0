using KartRider.Net;

namespace KartRider.IO.Packet
{
    public class UdpOutPacket : OutPacket
    {
        public UdpOutPacket(int size = 64)
            : base(size)
        {
        }

        public UdpOutPacket(
            int val1,
            int val2,
            RTTIValues rttiValue)
            : this()
        {
            WriteInt(val1);
            WriteInt(val2);
            WriteUInt((uint)rttiValue);
        }
    }
}