using KartRider.Data;
using KartRider.IO;
using KartRider.Net;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace KartRider.Game;

public sealed class Account
{
    // ============================================================
    // Properties
    // ============================================================

    public KartClient Client { get; private set; }

    public int Id { get; private set; }

    // Important:
    // Username = login account ID
    // The in-game nickname is managed by Player.Name.
    public string Username { get; private set; } = string.Empty;

    public bool IsBanned { get; private set; }

    public bool IsMaster { get; private set; }

    public uint BitSetValue { get; private set; }

    public string SessionIP { get; set; } = string.Empty;

    private bool Assigned { get; set; }

    // ============================================================
    // Constructor
    // ============================================================

    public Account(KartClient client)
    {
        Client = client;
    }

    // ============================================================
    // Load by ID
    // ============================================================

    public bool Load(int id)
    {
        Log.Inform(
            "[Account] Load(ID={0}), LocalMode={1}",
            id,
            Database.LocalMode);

        // ========================================================
        // LOCAL MODE
        // ========================================================

        if (Database.LocalMode)
        {
            var account =
                Database.GetLocalAccountById(id);

            if (account == null)
            {
                Log.Inform(
                    "[LocalDB] Account ID not found: {0}",
                    id);

                return false;
            }

            ApplyLocalAccount(account);

            return true;
        }

        // ========================================================
        // MYSQL
        // ========================================================

        dynamic datum =
            new Datum("accounts");

        try
        {
            datum.Populate(
                "ID = {0}",
                id);
        }
        catch (RowNotInTableException)
        {
            Log.Inform(
                "[MySQL] Account ID not found: {0}",
                id);

            return false;
        }

        try
        {
            Id =
                Convert.ToInt32(datum.ID);

            Assigned = true;

            Username =
                Convert.ToString(datum.Username)
                ?? string.Empty;

            IsBanned =
                Convert.ToBoolean(
                    Convert.ToInt32(datum.IsBanned));

            IsMaster =
                Convert.ToBoolean(
                    Convert.ToInt32(datum.IsMaster));

            BitSetValue =
                Convert.ToUInt32(datum.BitSetValue);

            SessionIP =
                Convert.ToString(datum.SessionIP)
                ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[Account] Failed to parse account: {0}",
                ex);

            return false;
        }

        Log.Inform(
            "[Account] Loaded ID={0}, Username='{1}'",
            Id,
            Username);

        return true;
    }

    // ============================================================
    // Load by Username + Password
    // ============================================================

    public bool Load(
        string username,
        string password)
    {
        username ??= string.Empty;
        password ??= string.Empty;

        Log.Inform(
            "[Account] Login request Username='{0}', PasswordLength={1}, LocalMode={2}",
            username,
            password.Length,
            Database.LocalMode);

        // ========================================================
        // LOCAL MODE
        // ========================================================

        // ========================================================
        // LOCAL MODE
        // ========================================================

        if (Database.LocalMode)
        {
            var account =
                Database.GetLocalAccount(username);

            if (account == null)
            {
                Log.Inform(
                    "[LocalDB] Account not found: '{0}'",
                    username);

                return false;
            }

            // ----------------------------------------------------
            // Password stored in LocalDB.
            // ----------------------------------------------------

            string localStoredPassword =
                Convert.ToString(
                    account["Password"])
                ?? string.Empty;

            // ----------------------------------------------------
            // SHA-512 hash of the entered password.
            // ----------------------------------------------------

            string localInputHash =
                Database.HashPassword(password);

            // ----------------------------------------------------
            // Verify the password.
            // ----------------------------------------------------

            bool passwordCorrect =
                string.Equals(
                    password,
                    localStoredPassword,
                    StringComparison.Ordinal)
                ||
                string.Equals(
                    localInputHash,
                    localStoredPassword,
                    StringComparison.OrdinalIgnoreCase);

            if (!passwordCorrect)
            {
                Log.Inform(
                    "[LocalDB] Invalid password for '{0}'",
                    username);

                return false;
            }

            // ----------------------------------------------------
            // Apply the account.
            // ----------------------------------------------------

            ApplyLocalAccount(account);

            Log.Inform(
                "[LocalDB] Login successful: ID={0}, Username='{1}'",
                Id,
                Username);

            return true;
        }

        // ========================================================
        // MYSQL
        // ========================================================

        dynamic datum =
            new Datum("accounts");

        try
        {
            datum.Populate(
                "Username = {0}",
                Database.Escape(username));
        }
        catch (RowNotInTableException)
        {
            Log.Inform(
                "[MySQL] Account not found: '{0}'",
                username);

            return false;
        }

        // ========================================================
        // Password
        // ========================================================

        string storedPassword =
            Convert.ToString(datum.Password)
            ?? string.Empty;

        /*
         * If the existing database stores a plain-text password
         * and its length is not 128, convert it to SHA-512.
         */
        if (storedPassword.Length != 128)
        {
            byte[] hashedPassword =
                SHA512.Create().ComputeHash(
                    Encoding.Unicode.GetBytes(
                        storedPassword));

            StringBuilder sb =
                new StringBuilder(128);

            foreach (byte b in hashedPassword)
            {
                sb.Append(
                    b.ToString("X2"));
            }

            datum.Password =
                sb.ToString();

            datum.Update(
                "ID = {0}",
                datum.ID);

            storedPassword =
                sb.ToString();

            Log.Inform(
                "[MySQL] Updated password hash for '{0}'",
                username);
        }

        /*
         * Keep the MySQL authentication method used by the existing project.
         */
        if (!password.Equals(
                storedPassword,
                StringComparison.Ordinal))
        {
            /*
             * Also accept cases where the input is already a SHA-512 hash.
             */
            string inputHash =
                Database.HashPassword(password);

            if (!inputHash.Equals(
                    storedPassword,
                    StringComparison.OrdinalIgnoreCase))
            {
                Log.Inform(
                    "[MySQL] Invalid password for '{0}'",
                    username);

                return false;
            }
        }

        // ========================================================
        // Apply
        // ========================================================

        try
        {
            Id =
                Convert.ToInt32(datum.ID);

            Assigned = true;

            Username =
                Convert.ToString(datum.Username)
                ?? string.Empty;

            IsBanned =
                Convert.ToBoolean(
                    Convert.ToInt32(datum.IsBanned));

            IsMaster =
                Convert.ToBoolean(
                    Convert.ToInt32(datum.IsMaster));

            BitSetValue =
                Convert.ToUInt32(datum.BitSetValue);

            SessionIP =
                Convert.ToString(datum.SessionIP)
                ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warn(
                "[Account] Failed to load MySQL account: {0}",
                ex);

            return false;
        }

        Log.Inform(
            "[MySQL] Account loaded: ID={0}, Username='{1}'",
            Id,
            Username);

        return true;
    }

    // ============================================================
    // Apply Local Account
    // ============================================================

    private void ApplyLocalAccount(
        System.Collections.Generic.Dictionary<string, object> account)
    {
        Id =
            Convert.ToInt32(
                account["ID"]);

        Assigned = true;

        /*
         * Here, Username refers to accounts.Username.
         *
         * Never use players.Name here.
         */
        Username =
            Convert.ToString(
                account["Username"])
            ?? string.Empty;

        IsBanned =
            Convert.ToBoolean(
                Convert.ToInt32(
                    account["IsBanned"]));

        IsMaster =
            Convert.ToBoolean(
                Convert.ToInt32(
                    account["IsMaster"]));

        BitSetValue =
            Convert.ToUInt32(
                account["BitSetValue"]);

        SessionIP =
            Convert.ToString(
                account["SessionIP"])
            ?? string.Empty;
    }

    // ============================================================
    // Save
    // ============================================================

    public void Save()
    {
        if (!Assigned)
        {
            Log.Warn(
                "[Account] Save skipped because account is not assigned.");

            return;
        }

        // ========================================================
        // LOCAL MODE
        // ========================================================

        if (Database.LocalMode)
        {
            bool updated =
                Database.UpdateLocalAccount(
                    Id,
                    Username,
                    IsBanned,
                    IsMaster,
                    BitSetValue,
                    SessionIP);

            if (updated)
            {
                Log.Inform(
                    "[LocalDB] Account saved: ID={0}, Username='{1}'",
                    Id,
                    Username);
            }
            else
            {
                Log.Warn(
                    "[LocalDB] Failed to save account: ID={0}",
                    Id);
            }

            return;
        }

        // ========================================================
        // MYSQL
        // ========================================================

        dynamic datum =
            new Datum("accounts");

        datum.ID =
            Id;

        datum.Username =
            Username;

        datum.IsBanned =
            IsBanned;

        datum.IsMaster =
            IsMaster;

        datum.BitSetValue =
            BitSetValue;

        datum.SessionIP =
            SessionIP;

        datum.Update(
            "ID = {0}",
            Id);

        Log.Inform(
            "[MySQL] Saved account '{0}'",
            Username);
    }
}