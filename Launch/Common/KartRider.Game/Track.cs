namespace KartRider.Game;

public sealed class Track
{
	public uint Id { get; private set; }

	public string Name { get; private set; }

	public TrackRibbon Ribbon { get; private set; }

	public Track(uint id, string name, TrackRibbon ribbon)
	{
		Id = id;
		Name = name;
		Ribbon = ribbon;
	}
}
