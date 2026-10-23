using System.Collections.ObjectModel;

namespace KartRider.Game;

public sealed class Channels : KeyedCollection<int, Channel>
{
	protected override int GetKeyForItem(Channel item)
	{
		return item.ID;
	}
}
