using System;
using KartRider.IO;

namespace KartRider.Game.Data;

public static class DataProvider
{
    public static CachedKarts Karts { get; private set; } = null!;

    public static CachedLevels Levels { get; private set; } = null!;

    public static CachedTracks Tracks { get; private set; } = null!;

    public static CachedStrings Strings { get; private set; } = null!;

    public static CachedShopStock ShopStock { get; private set; } = null!;

    public static CachedCharacters Characters { get; private set; } = null!;

    public static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        if (IsInitialized)
        {
            Log.Inform("Data Provider already initialized.");
            return;
        }

        DateTime startTime = DateTime.Now;

        Log.SkipLine();

        try
        {
            // ====================================================
            // Karts
            // ====================================================

            Karts = new CachedKarts();

            Log.Inform(
                "Loaded Karts: {0}",
                Karts.Count);

            // ====================================================
            // Levels
            // ====================================================

            Levels = new CachedLevels();

            Log.Inform(
                "Loaded Levels: {0}",
                Levels.Count);

            // ====================================================
            // Tracks
            // ====================================================

            Tracks = new CachedTracks();

            Log.Inform(
                "Loaded Tracks: {0}",
                Tracks.Count);

            // ====================================================
            // Strings
            // ====================================================

            Strings = new CachedStrings();

            Log.Inform(
                "Loaded Strings: {0}",
                Strings.Count);

            // ====================================================
            // Shop
            // ====================================================

            ShopStock = new CachedShopStock();

            Log.Inform(
                "Loaded ShopStock: {0}",
                ShopStock.Count);

            // ====================================================
            // Characters
            // ====================================================

            Characters = new CachedCharacters();

            Log.Inform(
                "Loaded Characters: {0}",
                Characters.Count);

            IsInitialized = true;

            Log.SkipLine();

            Log.Inform(
                "Data Provider initialized in {0}ms.",
                (DateTime.Now - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            IsInitialized = false;

            Log.Error(
                "Data Provider initialization failed: {0}",
                ex);

            throw;
        }
    }

    public static void Reset()
    {
        Karts = null!;
        Levels = null!;
        Tracks = null!;
        Strings = null!;
        ShopStock = null!;
        Characters = null!;

        IsInitialized = false;
    }
}