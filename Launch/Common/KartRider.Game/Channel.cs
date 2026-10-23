using KartRider.Collections;
using KartRider.Game.Players;
using KartRider.Utilities;

namespace KartRider.Game;

public sealed class Channel : NumericalKeyedCollection<Player>
{
	public int ID { get; private set; }

	public string Name { get; private set; }

	public int Capacity { get; private set; }

	public Rooms Rooms { get; private set; }

	private IdLooper IdLooper { get; set; }	

	public Channel(int id, string name, int capacity)
	{
		ID = id;
		Name = name;
		Capacity = capacity;
		Rooms = new Rooms();
		IdLooper = new IdLooper();
	}

	public ushort AssignRoomId()
	{
		return (ushort)IdLooper.Next();
	}

	protected override int GetKeyForItem(Player item)
	{
		return item.Id;
	}

	protected override void InsertItem(int index, Player item)
	{
		item.Channel = this;
		base.InsertItem(index, item);
	}

	protected override void RemoveItem(int index)
	{
		Player item = GetAtIndex(index);
		if (item != null)
		{
			item.Channel = null;
		}
		base.RemoveItem(index);
	}
}
