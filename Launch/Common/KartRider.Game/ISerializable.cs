using KartRider.IO.Packet;

namespace KartRider.Game;

public interface ISerializable
{
	void Serialize(OutPacket p);

	void Deserialize(InPacket p);
}
