using KartRider.IO.Packet;

namespace KartRider.Net.Handler;

public static class RoomHandler
{
	[PacketHandler(RTTIValues.RmFirstRequestPacket)]
	public static void HandleFirst(KartClient c, InPacket p)
	{
		if (c.Player != null && c.Player.Garage != null)
		{
			c.Player.Garage.UpdateSlotData();
		}
	}

	[PacketHandler(RTTIValues.RmNotiMyRoomInfoPacket)]
	public static void HandleNotiMyRoomInfo(KartClient c, InPacket p)
	{
		if (c.Player == null || c.Player.Garage == null || c.Player.Garage.Owner != c.Player)
		{
			return;
		}
		ushort gameId = p.ReadUShort();
		bool isProtected = p.ReadBool();
		bool useGuestBookPassword = p.ReadBool();
		bool isItemProtected = p.ReadBool();
		string password = p.ReadString();
		p.ReadString();
		string itemPassword = p.ReadString();
		ushort kart1GameId = p.ReadUShort();
		ushort kart2GameId = p.ReadUShort();
		c.Player.Garage.GameId = gameId;
		c.Player.Garage.Password = password;
		c.Player.Garage.ItemPassword = itemPassword;
		c.Player.Garage.Kart1GameId = kart1GameId;
		c.Player.Garage.Kart2GameId = kart2GameId;
		c.Player.Garage.Save();
		using OutPacket outPacket = new OutPacket(RTTIValues.RmNotiMyRoomInfoPacket);
		c.Player.Garage.Serialize(outPacket);
		c.Player.Garage.Broadcast(outPacket);
	}

	[PacketHandler(RTTIValues.RmRiderTalkPacket)]
	public static void HandleRiderTalk(KartClient c, InPacket p)
	{
		if (c.Player == null || c.Player.Garage == null)
		{
			return;
		}
		string text = p.ReadString();
		int index = c.Player.Garage.GetIndex(c.Player);
		if (index == -1)
		{
			return;
		}
		using OutPacket outPacket = new OutPacket(RTTIValues.RmRiderEchoPacket);
		outPacket.WriteInt(index);
		outPacket.WriteString(text);
		c.Player.Garage.Broadcast(outPacket);
	}
}
