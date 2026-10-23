using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO;

namespace KartRider.Game.Data;

public sealed class CachedTracks : List<Track>
{
    public CachedTracks()
    {
        using (Log.Load("Tracks"))
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "data.bml");

            Console.WriteLine(
                $"[Info] Tracks BML data path: {path}");

            if (!File.Exists(path))
            {
                Console.WriteLine(
                    $"[Error] Tracks BML file not found: {path}");
                return;
            }

            BmlObject obj = BmlObject.Create(path);

            BmlObject tracksObj = obj.GetObject("Tracks");

            if (tracksObj == null)
            {
                Console.WriteLine(
                    "[Error] BML object 'Tracks' not found.");
                return;
            }

            foreach (Tuple<string, BmlObject> subObj in tracksObj.SubObjects)
            {
                uint id = subObj.Item2.GetUInt("Id");
                string name = subObj.Item2.GetString("Name");

                string ribbonString =
                    subObj.Item2.GetString("Ribbon", "");

                TrackRibbon ribbon;

                if (!Enum.TryParse(
                    ribbonString,
                    true,
                    out ribbon))
                {
                    ribbon = default;
                }

                Add(new Track(id, name, ribbon));

                Console.WriteLine(
                    $"[Info] Loaded Track: Id={id}, Name={name}, Ribbon={ribbon}");
            }

            Console.WriteLine(
                $"[Info] CachedTracks: Loaded {Count} track(s).");
        }
    }
    public Track GetAllRandom()
    {
        if (Count == 0)
            throw new InvalidOperationException("No tracks loaded.");

        Random r = new Random();
        return this[r.Next(Count)];
    }

    public Track GetHotRandom()
    {
        List<Track> hotTracks = new List<Track>();

        foreach (Track track in this)
        {
            if (track.Ribbon == TrackRibbon.Hot)
            {
                hotTracks.Add(track);
            }
        }

        if (hotTracks.Count == 0)
            return GetAllRandom();

        Random r = new Random();
        return hotTracks[r.Next(hotTracks.Count)];
    }
}