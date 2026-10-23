using System;

namespace KartRider.Net;

public sealed class PacketSendException : Exception
{
	public override string Message => "Disconnected while sending packet.";
}
