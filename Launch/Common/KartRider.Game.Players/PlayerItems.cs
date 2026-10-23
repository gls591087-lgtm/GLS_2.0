using System;
using System.Collections;
using System.Collections.Generic;
using KartRider.Data;
using KartRider.IO.Packet;
using KartRider.IO;
using KartRider.Game.Data;

namespace KartRider.Game.Players;

public class PlayerItems : IEnumerable<Item>, IEnumerable, ISerializable
{
    public Player Parent { get; private set; }

    private List<Item> Items { get; set; }

    public int Count => Items.Count;

    public Item this[ItemType type, bool equipped = false]
    {
        get
        {
            using (IEnumerator<Item> enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Item item = enumerator.Current;
                    // Fix: Use comparison (==). The previous assignment (=) caused a bug.
                    if (item.Type == type && item.IsEquipped == equipped)
                    {
                        return item;
                    }
                }
            }
            return null;
        }
    }

    public PlayerItems(Player parent)
    {
        Parent = parent;
        Items = new List<Item>();
    }

    public void Load()
    {
        foreach (dynamic datum in new Datums("items").Populate("PlayerID = '{0}'", Parent.Id))
        {
            ItemType type = (ItemType)datum.Type;

            try
            {
                if (type == ItemType.Kart)
                {
                    // Safely attempt to extract GameID.
                    ushort gid = 0;
                    try
                    {
                        gid = Convert.ToUInt16(datum.GameID);
                    }
                    catch
                    {
                        Log.Warn("[PlayerItems] Invalid GameID in datum, falling back to generic Item.");
                        Add(new Item(datum));
                        continue;
                    }

                    // Create a Kart when Kart metadata exists; otherwise warn and fall back to a generic Item.
                    if (DataProvider.Karts != null && DataProvider.Karts.ContainsKey(gid))
                    {
                        Add(new Kart(datum));
                    }
                    else
                    {
                        Log.Warn("[PlayerItems] Kart data not found. GameID={0}. Falling back to Item.", gid);
                        Add(new Item(datum));
                    }
                }
                else
                {
                    Add(new Item(datum));
                }
            }
            catch (Exception ex)
            {
                // Use the correct Error overload: pass the exception separately instead of putting a format string in the label.
                Log.Error("[PlayerItems] Failed to load item.", ex);

                // Try adding a generic Item as a last resort.
                try
                {
                    Add(new Item(datum));
                }
                catch (Exception ex2)
                {
                    Log.Error("[PlayerItems] Fallback to generic Item failed.", ex2);
                }
            }
        }
    }

    public void Save()
    {
        using IEnumerator<Item> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            Item item = enumerator.Current;
            item.Save();
        }
    }

    public void Delete()
    {
        using IEnumerator<Item> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            Item item = enumerator.Current;
            item.Delete();
        }
    }

    public void Add(Item item)
    {
        item.Parent = this;

        // Call CalculateAdditions only when the runtime type is Kart.
        if (item is Kart kart)
        {
            kart.CalculateAdditions();
        }

        Items.Add(item);
    }

    public IEnumerable<Item> GetEquipped()
    {
        for (int i = 1; i <= 9; i++)
        {
            ItemType type = ItemConstants.GetItemType(i);
            yield return this[type, true];
        }
    }

    public IEnumerator<Item> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Items).GetEnumerator();
    }

    public bool AddItem(ushort gameId, ItemType type, short quantity, long expiration, bool equipped)
    {
        try
        {
            Item item;
            if (type == ItemType.Kart)
            {
                if (DataProvider.Karts != null && DataProvider.Karts.ContainsKey(gameId))
                {
                    item = new Kart(gameId, expiration, equipped);
                }
                else
                {
                    Log.Warn("[PlayerItems] AddItem: Kart data not found. GameID={0}. Creating generic Item.", gameId);
                    item = new Item(gameId, type, quantity, expiration, equipped);
                }
            }
            else
            {
                item = new Item(gameId, type, quantity, expiration, equipped);
            }

            Add(item);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("[PlayerItems] AddItem failed. GameID={0}, Type={1}", gameId, type, ex);
            return false;
        }
    }

    public void Serialize(OutPacket p)
    {
        // Slots 1-9
        // Slots 1-9
        for (int i = 1; i <= 9; i++)
        {
            ItemType type = ItemConstants.GetItemType(i);
            Item equipped = this[type, true];
            p.WriteUShort(equipped != null ? equipped.GameId : (ushort)0);
        }
        // Slots 10-16
        for (int i = 10; i <= 16; i++)
        {
            p.WriteUShort(0);
        }
    }
   

    public void Deserialize(InPacket p)
    {
    }
}
