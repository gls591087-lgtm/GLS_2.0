using System;
using System.Collections.Generic;
using KartRider.Data;
using KartRider.Game.Players;

namespace KartRider.Game;

public class Item
{
    public PlayerItems Parent { get; set; }

    public int Id { get; private set; }

    public ushort GameId { get; set; }

    public ItemType Type { get; set; }

    public short Quantity { get; set; }

    public long Expiration { get; set; }

    public bool IsEquipped { get; set; }

    private bool Assigned { get; set; }

    public Player Player =>
        Parent?.Parent;

    public Item(
        ushort gameId,
        ItemType type,
        short quantity,
        long expiration,
        bool isEquipped = false)
    {
        GameId = gameId;
        Type = type;
        Quantity = quantity;
        Expiration = expiration;
        IsEquipped = isEquipped;

        Assigned = false;
    }

    public Item(Dictionary<string, object> datum)
    {
        if (datum == null)
            throw new ArgumentNullException(nameof(datum));

        Id = Convert.ToInt32(datum["ID"]);
        GameId = Convert.ToUInt16(datum["GameID"]);
        Type = (ItemType)Convert.ToInt16(datum["Type"]);
        Quantity = Convert.ToInt16(datum["Quantity"]);
        Expiration = Convert.ToInt64(datum["Expiration"]);
        IsEquipped = Convert.ToBoolean(datum["IsEquipped"]);
        Assigned = true;
    }

    public Item(dynamic datum)
        : this(new Dictionary<string, object>
        {
            ["ID"] = (object)datum.ID,
            ["GameID"] = (object)datum.GameID,
            ["Type"] = (object)datum.Type,
            ["Quantity"] = (object)datum.Quantity,
            ["Expiration"] = (object)datum.Expiration,
            ["IsEquipped"] = (object)datum.IsEquipped
        })
    {
    }
    public bool IsPermanent =>
        Expiration == -1;

    public bool IsExpired
    {
        get
        {
            if (Expiration == -1)
                return false;

            return Expiration <=
                DateTime.Now.Ticks;
        }
    }

    public short GetExpiration()
    {
        if (Expiration == -1)
            return -1;

        long remaining =
            Expiration -
            DateTime.Now.Ticks;

        if (remaining <= 0)
            return 0;

        long days =
            remaining /
            TimeSpan.TicksPerDay;

        if (days > short.MaxValue)
            return short.MaxValue;

        return (short)days;
    }

    public void Save()
    {
        if (Player == null)
        {
            Console.WriteLine(
                $"[Item] Save skipped: Player is null. " +
                $"GameID={GameId}");

            return;
        }

        dynamic datum =
            new Datum("items");

        datum.PlayerID =
            Player.Id;

        datum.GameID =
            GameId;

        datum.Type =
            (short)Type;

        datum.Quantity =
            Quantity;

        datum.Expiration =
            Expiration;

        datum.IsEquipped =
            IsEquipped;

        datum.IsPermanent =
            IsPermanent;

        if (Assigned)
        {
            datum.Update(
                "ID = {0}",
                Id);

            return;
        }

        datum.Insert();

        dynamic id =
            Database.Fetch(
                "items",
                "ID",
                "PlayerID = '{0}' AND GameID = '{1}' AND Type = '{2}'",
                Player.Id,
                GameId,
                (short)Type);

        if (id == null)
        {
            throw new InvalidOperationException(
                $"Unable to retrieve inserted item ID. " +
                $"PlayerID={Player.Id}, " +
                $"GameID={GameId}, " +
                $"Type={Type}");
        }

        Id =
            Convert.ToInt32(id);

        Assigned = true;
    }

    public void Delete()
    {
        if (!Assigned)
            return;

        Database.Delete(
            "items",
            "ID = {0}",
            Id);

        Assigned = false;
    }

    public override string ToString()
    {
        return
            $"Item[" +
            $"ID={Id}, " +
            $"GameID={GameId}, " +
            $"Type={Type}, " +
            $"Quantity={Quantity}, " +
            $"Expiration={Expiration}, " +
            $"Equipped={IsEquipped}]";
    }
}