namespace KartRider.Game;

public sealed class Stock
{
	public int Id { get; set; }

	public string Name { get; set; }

	public bool IsOnSale { get; set; }

	public bool IsLucciPrice { get; set; }

	public StockItem Item { get; set; }
}
