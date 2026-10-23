using System.Text;

namespace KartRider.IO.Packet;

public abstract class PacketBase
{
	public abstract int Length { get; }

	public abstract int Position { get; }

	public abstract byte[] ToArray();

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		byte[] array = ToArray();
		foreach (byte b in array)
		{
			sb.AppendFormat("{0:X2} ", b);
		}
		return sb.ToString();
	}
}
