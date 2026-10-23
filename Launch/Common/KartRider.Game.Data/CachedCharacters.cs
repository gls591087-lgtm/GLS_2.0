using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO;

namespace KartRider.Game.Data;

public sealed class CachedCharacters : Dictionary<ushort, string>
{
    public CachedCharacters()
    {
        using (Log.Load("Characters"))
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "data.bml");

            Log.Inform(
                "Characters BML data path: {0}",
                path);

            if (!File.Exists(path))
            {
                Log.Error(
                    "Characters BML file not found: {0}",
                    path);

                return;
            }

            BmlObject obj = BmlObject.Create(path);

            BmlObject charactersObj = obj.GetObject("Characters");

            if (charactersObj == null || charactersObj.SubObjects.Count == 0)
            {
                Log.Error(
                    "BML object 'Characters' not found or empty.");

                return;
            }

            foreach (Tuple<string, BmlObject> subObj in charactersObj.SubObjects)
            {
                ushort id = subObj.Item2.GetUShort("Id");
                string name = subObj.Item2.GetString("Name");

                if (ContainsKey(id))
                {
                    Log.Inform(
                        "Duplicate Character ID: {0}",
                        id);

                    continue;
                }

                Add(id, name);

                Log.Inform(
                    "Loaded Character: GameID={0}, Name={1}",
                    id,
                    name);
            }

            Log.Inform(
                "CachedCharacters: Loaded {0} character(s).",
                Count);

            Log.Inform(
                "CachedCharacters: Character 11 exists = {0}.",
                ContainsKey(11));
        }
    }
}
