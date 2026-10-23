using System.Collections.Generic;

namespace KartRider.Game.Players;

public class PlayerGifts : List<Gift>
{
	public Player Parent { get; private set; }

	public PlayerGifts(Player parent)
	{
		Parent = parent;
	}

	public void Load()
	{
	}

	public void Save()
	{
	}

	public void Delete()
	{
	}
}
