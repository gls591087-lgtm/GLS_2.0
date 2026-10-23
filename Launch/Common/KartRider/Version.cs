namespace KartRider;

public struct Version
{
    public short Major { get; set; }

    public short Minor { get; set; }

    public Localisation Localisation { get; set; }

    public bool TestServer { get; set; }
}