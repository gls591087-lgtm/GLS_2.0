using KartRider.IO.Packet;

namespace KartRider.Game.Players;

public class SessionData : ISerializable
{
	public int A { get; set; }

	public int B { get; set; }

	public int C { get; set; }


	public int D { get; set; }

	public int E { get; set; }

	public int F { get; set; }

	public void Serialize(OutPacket p)
	{
		p.WriteInt(A);
		p.WriteInt(B);
		p.WriteInt(C);
		p.WriteInt(D);
		p.WriteInt(E);
		p.WriteInt(F);
	}

	public void Deserialize(InPacket p)
	{
		A = p.ReadInt();
		B = p.ReadInt();
		C = p.ReadInt();
		D = p.ReadInt();
		E = p.ReadInt();
		F = p.ReadInt();
	}
}
