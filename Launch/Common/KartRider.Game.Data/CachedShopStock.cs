using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO;

namespace KartRider.Game.Data;

public sealed class CachedShopStock : List<Stock>
{
    public CachedShopStock()
    {
        using (Log.Load("Shop Stock"))
        {
            string dataPath = Path.Combine(
                AppContext.BaseDirectory,
                "data.bml");

            BmlObject obj = BmlObject.Create(dataPath);

            BmlObject objStocks = obj.GetObject("Shop Stock");

            foreach (Tuple<string, BmlObject> subObj in objStocks.SubObjects)
            {
                Stock stock = new Stock
                {
                    Id = subObj.Item2.GetInt("Id"),
                    Name = subObj.Item2.GetString("Name"),
                    IsOnSale = subObj.Item2.GetBool("Sale"),
                    IsLucciPrice = subObj.Item2.GetBool("Lucci")
                };

                foreach (Tuple<string, BmlObject> itemObj in subObj.Item2.SubObjects)
                {
                    stock.Item = new StockItem
                    {
                        Id = itemObj.Item2.GetInt("Id"),
                        Type = (ItemType)Enum.Parse(typeof(ItemType), itemObj.Item2.GetString("Type")),
                        Quantity = itemObj.Item2.GetShort("Quantity"),
                        Expiration = itemObj.Item2.GetInt("Expiration")
                    };
                }

                Add(stock);
            }
        }
    }
}