using KartRider.Collections;

namespace KartRider.Game;

public sealed class Rooms : NumericalKeyedCollection<Room>
{
	protected override int GetKeyForItem(Room item)
	{
		return item.Id;
	}
}
