using KartRider.Collections;

namespace KartRider.Game;

public sealed class Garages : NumericalKeyedCollection<Garage>
{
	protected override int GetKeyForItem(Garage item)
	{
		return item.PlayerId;
	}

	public Garage TryGetGarage(int playerId)
	{
		Garage garage = new Garage(playerId);
		if (!garage.Load())
		{
			garage.GameId = 1;
			garage.Save();
		}
		return garage;
	}
}
