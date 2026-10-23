using System;

namespace KartRider.Net;

[AttributeUsage(
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
public sealed class PacketHandlerAttribute : Attribute
{
    public uint RTTIValue { get; }

    public uint Rtti => RTTIValue;

    public PacketHandlerAttribute(RTTIValues rttiValue)
    {
        RTTIValue = (uint)rttiValue;
    }

    public PacketHandlerAttribute(uint rttiValue)
    {
        RTTIValue = rttiValue;
    }
}