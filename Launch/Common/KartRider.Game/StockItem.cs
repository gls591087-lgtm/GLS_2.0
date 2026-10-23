namespace KartRider.Game;

public sealed class StockItem
{
    public int Id { get; set; }

    public ItemType Type { get; set; }

    public short Quantity { get; set; }

    public int Expiration { get; set; }
}