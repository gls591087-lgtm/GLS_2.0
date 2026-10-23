using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO;

namespace KartRider.Game.Data;

public sealed class CachedStrings : Dictionary<ItemType, List<Tuple<short, string>>>
{
    public CachedStrings()
    {
        using (Log.Load("Strings"))
        {
            ItemType[] types = (ItemType[])Enum.GetValues(typeof(ItemType));

            foreach (ItemType type in types)
            {
                Add(type, new List<Tuple<short, string>>());
            }

            string dataPath = Path.Combine(
                AppContext.BaseDirectory,
                "data.bml"
            );

            BmlObject root = BmlObject.Create(dataPath);

            BmlObject stringsObj = root.GetObject("Strings");

            if (stringsObj == null)
            {
                throw new Exception(
                    $"Strings object not found in BML: {dataPath}");
            }

            foreach (Tuple<string, BmlObject> subObject in stringsObj.SubObjects)
            {
                string typeString = subObject.Item2.GetString("Type");
                string idString = subObject.Item2.GetString("Id");
                string name = subObject.Item2.GetString("Name");

                Log.Inform(
                    "[CachedStrings] Type='{0}', Id='{1}', Name='{2}'",
                    typeString,
                    idString,
                    name
                );

                if (!Enum.TryParse(typeString, true, out ItemType type))
                {
                    Log.Warn(
                        "[CachedStrings] Unknown ItemType: '{0}'",
                        typeString
                    );
                    continue;
                }

                if (!short.TryParse(idString, out short id))
                {
                    Log.Warn(
                        "[CachedStrings] Invalid ID: Type='{0}', Id='{1}'",
                        typeString,
                        idString
                    );
                    continue;
                }

                if (!ContainsKey(type))
                    this[type] = new List<Tuple<short, string>>();

                this[type].Add(
                    new Tuple<short, string>(id, name)
                );
            }
        }
    }

    public string GetNameById(ItemType type, short id)
    {
        if (!TryGetValue(type, out List<Tuple<short, string>> list))
            return string.Empty;

        foreach (Tuple<short, string> item in list)
        {
            if (item.Item1 == id)
                return item.Item2;
        }

        return string.Empty;
    }

    public short GetIdByName(ItemType type, string name)
    {
        if (!TryGetValue(type, out List<Tuple<short, string>> list))
            return -1;

        foreach (Tuple<short, string> item in list)
        {
            if (item.Item2 == name)
                return item.Item1;
        }

        return -1;
    }
}