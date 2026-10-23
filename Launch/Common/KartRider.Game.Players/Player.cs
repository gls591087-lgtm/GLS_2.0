using KartRider.Data;
using KartRider.Game.Data;
using KartRider.IO;
using KartRider.Net;
using System;
using System.Data;

namespace KartRider.Game.Players;

public sealed class Player
{
    public KartClient Client { get; private set; }

    public PlayerItems Items { get; private set; }

    public PlayerGifts Gifts { get; private set; }

    public int Id { get; private set; }

    public int AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Lucci { get; set; }

    public int RP { get; set; }

    public int Cash { get; set; }

    public sbyte License { get; set; }

    public byte[] sessionData { get; set; }

    public Channel Channel { get; set; }

    public Garage Garage { get; set; }

    public Room Room { get; set; }

    public static Room Rooms { get; set; }

    public Statistics Statistics { get; private set; }

    public ushort NewbieStatus { get; set; }

    public ushort RookieStatus { get; set; }

    public ushort L3Status { get; set; }

    public ushort L2Status { get; set; }

    public ushort L1Status { get; set; }

    public ushort PROStatus { get; set; }

    public DateTime Creation { get; set; }

    public SessionData SessionData { get; set; }

    private bool Assigned { get; set; }

    // ============================================================
    // Constructor
    // ============================================================

    public Player(
        int accountId = 0,
        KartClient client = null)
    {
        AccountId = accountId;
        Client = client;

        Items = new PlayerItems(this);
        Gifts = new PlayerGifts(this);

        SessionData = new SessionData();

        Creation = DateTime.Now;
    }

    // ============================================================
    // Load
    // ============================================================

    public bool Load()
    {
        Log.Inform(
            "[Player.Load] START AccountID={0}",
            AccountId);

        // ========================================================
        // LOCAL MODE
        // ========================================================

        if (Database.LocalMode)
        {
            var player = Database.GetLocalPlayerByAccountId(AccountId);

            if (player == null)
            {
                Log.Error(
                    "[Player.Load] Player not found. AccountID={0}",
                    AccountId);

                return false;
            }

            try
            {
                Id = Convert.ToInt32(player["ID"]);
                AccountId = Convert.ToInt32(player["AccountID"]);
                Name = Convert.ToString(player["Name"]) ?? string.Empty;
                Lucci = Convert.ToInt32(player["Lucci"]);
                RP = Convert.ToInt32(player["RP"]);
                Cash = Convert.ToInt32(player["Cash"]);
                License = Convert.ToSByte(player["License"]);

                NewbieStatus = Convert.ToUInt16(player["NewbieStatus"]);
                RookieStatus = Convert.ToUInt16(player["RookieStatus"]);
                L3Status = Convert.ToUInt16(player["L3Status"]);
                L2Status = Convert.ToUInt16(player["L2Status"]);
                L1Status = Convert.ToUInt16(player["L1Status"]);
                PROStatus = Convert.ToUInt16(player["PROStatus"]);

                if (player["Creation"] is DateTime dateTime)
                {
                    Creation = dateTime;
                }

                Assigned = true;

                Log.Inform(
                    "[Player.Load] COMPLETE (LocalMode) " +
                    "AccountID={0}, ID={1}, Name={2}, License={3}, " +
                    "Lucci={4}, RP={5}",
                    AccountId,
                    Id,
                    Name,
                    License,
                    Lucci,
                    RP);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Player.Load] LocalMode data initialization failed. " +
                    "AccountID={0}: {1}",
                    AccountId,
                    ex);

                return false;
            }

            // ====================================================
            // LEVEL DATA (fallback added)
            // ====================================================
            if (DataProvider.Levels == null)
            {
                Log.Error(
                    "[Player.Load] DataProvider.Levels is NULL.");

                return false;
            }

            if (!DataProvider.Levels.TryGetValue(License, out Statistics statistics))
            {
                Log.Error(
                    "[Player.Load] Statistics not found. " +
                    "License={0}, Levels.Count={1}",
                    License,
                    DataProvider.Levels.Count);

                // Try a fallback: use level 0 first, then the first entry if none exists.
                if (DataProvider.Levels.TryGetValue((sbyte)0, out Statistics fallback))
                {
                    Log.Inform(
                        "[Player.Load] Falling back to level 0 for License={0}",
                        License);

                    Statistics = fallback;
                    License = (sbyte)0;
                }
                else if (DataProvider.Levels.Count > 0)
                {
                    foreach (var kvp in DataProvider.Levels)
                    {
                        Statistics = kvp.Value;
                        License = kvp.Key;
                        Log.Inform(
                            "[Player.Load] Falling back to first available level: Index={0}",
                            kvp.Key);
                        break;
                    }
                }
                else
                {
                    Log.Error("[Player.Load] No Levels available in DataProvider.Levels.");
                    return false;
                }
            }
            else
            {
                Statistics = statistics;
            }

            // ====================================================
            // ITEMS
            // ====================================================
            try
            {
                Items.Load();
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Player.Load] Items.Load failed. " +
                    "AccountID={0}: {1}",
                    AccountId,
                    ex);

                return false;
            }

            // ====================================================
            // GIFTS
            // ====================================================
            try
            {
                Gifts.Load();
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Player.Load] Gifts.Load failed. " +
                    "AccountID={0}: {1}",
                    AccountId,
                    ex);

                return false;
            }

            return true;
        }

        // ========================================================
        // DATABASE (MySQL) -- apply the same fallback logic below.
        // ========================================================
        dynamic datum = new Datum("players");

        try
        {
            datum.Populate(
                "AccountID = {0}",
                AccountId);
        }
        catch (RowNotInTableException)
        {
            Log.Error(
                "[Player.Load] Player not found. AccountID={0}",
                AccountId);

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(
                "[Player.Load] Database error. AccountID={0}: {1}",
                AccountId,
                ex);

            return false;
        }

        try
        {
            Id = Convert.ToInt32(datum.ID);
            AccountId = Convert.ToInt32(datum.AccountID);
            Name = datum.Name == null || datum.Name is DBNull ? string.Empty : Convert.ToString(datum.Name);
            Lucci = datum.Lucci == null || datum.Lucci is DBNull ? 0 : Convert.ToInt32(datum.Lucci);
            RP = datum.RP == null || datum.RP is DBNull ? 0 : Convert.ToInt32(datum.RP);
            Cash = datum.Cash == null || datum.Cash is DBNull ? 0 : Convert.ToInt32(datum.Cash);
            License = datum.License == null || datum.License is DBNull ? (sbyte)0 : Convert.ToSByte(datum.License);

            NewbieStatus = ushort.MaxValue;
            RookieStatus = ushort.MaxValue;
            L3Status = ushort.MaxValue;
            L2Status = ushort.MaxValue;
            L1Status = ushort.MaxValue;
            PROStatus = ushort.MaxValue;

            object creationValue = datum.Creation;
            if (creationValue is DateTime dateTime2)
            {
                Creation = dateTime2;
            }
            else if (creationValue != null && creationValue is not DBNull && DateTime.TryParse(Convert.ToString(creationValue), out DateTime parsedDate))
            {
                Creation = parsedDate;
            }
            else
            {
                Creation = DateTime.Now;
            }

            Assigned = true;
        }
        catch (Exception ex)
        {
            Log.Error(
                "[Player.Load] Basic data initialization failed. " +
                "AccountID={0}: {1}",
                AccountId,
                ex);

            return false;
        }

        Log.Inform("[Player.Load] ID={0}", Id);
        Log.Inform("[Player.Load] AccountID={0}", AccountId);
        Log.Inform("[Player.Load] Name={0}", Name);
        Log.Inform("[Player.Load] License={0}", License);
        Log.Inform("[Player.Load] Lucci={0}", Lucci);
        Log.Inform("[Player.Load] RP={0}", RP);

        if (DataProvider.Levels == null)
        {
            Log.Error("[Player.Load] DataProvider.Levels is NULL.");
            return false;
        }

        Log.Inform("[Player.Load] Levels.Count={0}", DataProvider.Levels.Count);
        Log.Inform("[Player.Load] Looking for License={0}", License);

        if (!DataProvider.Levels.TryGetValue(License, out Statistics statistics2))
        {
            Log.Error(
                "[Player.Load] Statistics not found. " +
                "License={0}, Levels.Count={1}",
                License,
                DataProvider.Levels.Count);

            if (DataProvider.Levels.TryGetValue((sbyte)0, out Statistics fallback2))
            {
                Log.Inform("[Player.Load] Falling back to level 0 for License={0}", License);
                Statistics = fallback2;
                License = (sbyte)0;
            }
            else if (DataProvider.Levels.Count > 0)
            {
                foreach (var kvp in DataProvider.Levels)
                {
                    Statistics = kvp.Value;
                    License = kvp.Key;
                    Log.Inform("[Player.Load] Falling back to first available level: Index={0}", kvp.Key);
                    break;
                }
            }
            else
            {
                Log.Error("[Player.Load] No Levels available in DataProvider.Levels.");
                return false;
            }
        }
        else
        {
            Statistics = statistics2;
        }

        Log.Inform("[Player.Load] Statistics assigned. License={0}", License);

        try
        {
            Items.Load();
        }
        catch (Exception ex)
        {
            Log.Error("[Player.Load] Items.Load failed. AccountID={0}: {1}", AccountId, ex);
            return false;
        }

        try
        {
            Gifts.Load();
        }
        catch (Exception ex)
        {
            Log.Error("[Player.Load] Gifts.Load failed. AccountID={0}: {1}", AccountId, ex);
            return false;
        }

        Log.Inform(
            "[Player.Load] COMPLETE " +
            "AccountID={0}, ID={1}, Name={2}, License={3}, " +
            "Lucci={4}, RP={5}",
            AccountId,
            Id,
            Name,
            License,
            Lucci,
            RP);

        return true;
    }

    // ============================================================
    // Save
    // ============================================================

    public void Save()
    {
        // ========================================================
        // LOCAL MODE
        // ========================================================

        if (Database.LocalMode)
        {
            if (!Assigned)
            {
                Log.Error(
                    "[Player.Save] LocalMode player is not assigned. AccountID={0}",
                    AccountId);

                return;
            }

            bool updated =
                Database.UpdateLocalPlayer(
                    Id,
                    AccountId,
                    Name,
                    RP,
                    Lucci,
                    Cash,
                    License,
                    NewbieStatus,
                    RookieStatus,
                    L3Status,
                    L2Status,
                    L1Status,
                    PROStatus,
                    Creation);

            if (!updated)
            {
                Log.Error(
                    "[Player.Save] Local player update failed. PlayerID={0}",
                    Id);

                return;
            }

            if (Items != null)
            {
                Items.Save();
            }

            if (Gifts != null)
            {
                Gifts.Save();
            }

            Log.Inform(
                "[LocalDB] Saved player '{0}'. " +
                "PlayerID={1}, AccountID={2}, License={3}, " +
                "RP={4}, Lucci={5}, Cash={6}",
                Name,
                Id,
                AccountId,
                License,
                RP,
                Lucci,
                Cash);

            return;
        }

        // ========================================================
        // MYSQL
        // ========================================================

        dynamic datum =
            new Datum("players");

        datum.ID = Id;
        datum.AccountID = AccountId;
        datum.Name = Name;
        datum.Lucci = Lucci;
        datum.RP = RP;
        datum.Cash = Cash;
        datum.License = License;

        datum.NewbieStatus = NewbieStatus;
        datum.RookieStatus = RookieStatus;
        datum.L3Status = L3Status;
        datum.L2Status = L2Status;
        datum.L1Status = L1Status;
        datum.PROStatus = PROStatus;

        datum.Creation =
            Assigned
                ? Creation
                : DateTime.Now;

        if (Assigned)
        {
            datum.Update(
                "ID = {0}",
                Id);
        }
        else
        {
            datum.Insert();

            Id =
                Convert.ToInt32(
                    Database.Fetch(
                        "players",
                        "ID",
                        "Name = {0}",
                        Database.Escape(Name)));

            Assigned = true;
        }

        if (Items != null)
        {
            Items.Save();
        }

        if (Gifts != null)
        {
            Gifts.Save();
        }

        Log.Inform(
            "Saved player '{0}'. " +
            "LicenseStatus: Newbie={1}, Rookie={2}, L3={3}, " +
            "L2={4}, L1={5}, PRO={6}",
            Name,
            NewbieStatus,
            RookieStatus,
            L3Status,
            L2Status,
            L1Status,
            PROStatus);
    }
    // ============================================================
    // Delete
    // ============================================================

    public void Delete()
    {
        if (!Assigned)
            return;

        Database.Delete(
            "players",
            "ID = {0}",
            Id);

        if (Items != null)
        {
            Items.Delete();
        }

        if (Gifts != null)
        {
            Gifts.Delete();
        }

        Assigned = false;

        Log.Inform(
            "Deleted player '{0}'.",
            Name);
    }
}