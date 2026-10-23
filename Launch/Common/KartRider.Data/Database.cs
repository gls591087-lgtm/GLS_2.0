using KartRider.Game;
using KartRider.Game.Data;
using KartRider.IO;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KartRider.Data;

public static class Database
{
    // ============================================================
    // MySQL configuration
    // ============================================================

    public static string Host { get; set; } = "127.0.0.1";

    public static string Schema { get; set; } = "kcdb";

    public static string Username { get; set; } = "root";

    public static string Password { get; set; } = "";

    // ============================================================
    // Local Mode
    // ============================================================

    public static bool LocalMode { get; private set; }

    public static string LocalUsername { get; private set; } = "player";

    public static string LocalPassword { get; private set; } = "player";

    private static Dictionary<string, List<Dictionary<string, object>>> LocalTables
    { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    private static int LocalIdCounter { get; set; } = 1;

    // ============================================================
    // Connection
    // ============================================================

    internal static string ConnectionString =>
        LocalMode
            ? string.Empty
            : $"server={Host}; database={Schema}; uid={Username}; password={Password}; convertzerodatetime=yes;";

    public static string DefaultSchema =>
        LocalMode
            ? Schema
            : Settings.GetString("Database/Schema");

    // ============================================================
    // Enable Local Mode
    // ============================================================

    public static void EnableLocalMode(int accountCount = 1)
    {
        if (LocalMode)
        {
            Log.Inform(
                "[LocalDB] Local mode already enabled.");

            return;
        }

        if (accountCount < 1)
            accountCount = 1;

        LocalMode = true;

        // Default login account
        LocalUsername = "player1";
        LocalPassword = "player1";

        LocalTables =
            new Dictionary<string, List<Dictionary<string, object>>>(
                StringComparer.OrdinalIgnoreCase);

        LocalIdCounter = 1;

        Log.SkipLine();

        Log.Inform(
            "[LocalDB] Enabling local database mode.");

        Log.Inform(
            "[LocalDB] Account count = {0}",
            accountCount);

        // ========================================================
        // DataProvider
        // ========================================================

        if (!DataProvider.IsInitialized)
        {
            DataProvider.Initialize();
        }

        // ========================================================
        // Tables
        // ========================================================

        EnsureLocalTable("accounts");
        EnsureLocalTable("players");
        EnsureLocalTable("items");

        // ========================================================
        // Accounts / Players / Items
        // ========================================================

        for (int i = 1; i <= accountCount; i++)
        {
            string username = $"player{i}";
            string password = $"player{i}";

            // Account
            EnsureLocalAccount(
                i,
                username,
                password);

            // Player
            EnsureLocalPlayer(
                i,
                i,
                username);

            // Items
            EnsureLocalItems(i);
        }

        Log.Inform(
            "[LocalDB] Local database mode enabled.");

        Log.Inform(
            "[LocalDB] Total Accounts = {0}",
            LocalTables["accounts"].Count);

        Log.Inform(
            "[LocalDB] Total Players = {0}",
            LocalTables["players"].Count);

        Log.Inform(
            "[LocalDB] Total Items = {0}",
            LocalTables["items"].Count);

        Log.SkipLine();
    }
    public static void SetLocalLoginAccount(int accountNumber)
    {
        if (accountNumber < 1)
            accountNumber = 1;

        string username = $"player{accountNumber}";
        string password = $"player{accountNumber}";

        // Check whether the account exists
        var account = GetLocalAccount(username);

        if (account == null)
        {
            Log.Inform(
                "[LocalDB] Login account does not exist: {0}",
                username);

            return;
        }

        LocalUsername = username;
        LocalPassword = password;

        Log.Inform(
            "[LocalDB] Login account selected: {0}",
            LocalUsername);
    }
    // ============================================================
    // Ensure Local Items
    // ============================================================

    private static void EnsureLocalItems(int playerId)
    {
        EnsureLocalTable("items");
        if (LocalTables["items"].Count != 0)
        {
            bool hasItems =
                LocalTables["items"].Exists(
                    row =>
                        row.TryGetValue(
                            "PlayerID",
                            out object? value) &&
                        Convert.ToInt32(value) == playerId);

            if (hasItems)
                return;
        }


        Log.Inform(
            "[LocalDB] Creating default items...");

        // ========================================================
        // Characters
        // ========================================================

        int characterCount = 0;

        if (DataProvider.Strings != null &&
     DataProvider.Strings.TryGetValue(
         ItemType.Character,
         out List<Tuple<short, string>> characters))
        {
            foreach (Tuple<short, string> character in characters)
            {
                if (character.Item1 < 0)
                    continue;

                if (LocalItemExists(
                    playerId,
                    character.Item1,
                    ItemType.Character))
                {
                    continue;
                }

                AddLocalItem(
                    playerId,
                    character.Item1,
                    ItemType.Character);

                characterCount++;

                Log.Inform(
                    "[LocalDB] Character added: PlayerID={0}, GameID={1}, Name={2}",
                    playerId,
                    character.Item1,
                    character.Item2);
            }
        }

        // ========================================================
        // Karts
        // ========================================================
        // ========================================================
        // Colors
        // ========================================================

        int colorCount = 0;

        if (DataProvider.Strings != null &&
            DataProvider.Strings.TryGetValue(
                ItemType.Color,
                out List<Tuple<short, string>> colors))
        {
            foreach (Tuple<short, string> color in colors)
            {
                if (color.Item1 < 0)
                    continue;

                if (LocalItemExists(
                    playerId,
                    color.Item1,
                    ItemType.Color))
                {
                    continue;
                }

                AddLocalItem(
                    playerId,
                    color.Item1,
                    ItemType.Color);

                colorCount++;

                Log.Inform(
                    "[LocalDB] Color added: GameID={0}, Name={1}",
                    color.Item1,
                    color.Item2);
            }
        }
        int kartCount = 0;

        ushort defaultKartId = 0;

        if (DataProvider.Karts != null)
        {
            foreach (
     KeyValuePair<ushort, Statistics> kart
     in DataProvider.Karts)
            {
                ushort kartId = kart.Key;

                if (LocalItemExists(
                    playerId,
                    kartId,
                    ItemType.Kart))
                {
                    continue;
                }

                AddLocalItem(
                    playerId,
                    kartId,
                    ItemType.Kart);

                if (defaultKartId == 0)
                    defaultKartId = kartId;

                kartCount++;

                Log.Inform(
                    "[LocalDB] Kart added: PlayerID={0}, GameID={1}",
                    playerId,
                    kartId);
            }
        }

        // ========================================================
        // Shop items
        // ========================================================

        int shopItemCount = 0;

        if (DataProvider.ShopStock != null)
        {
            foreach (Stock stock in DataProvider.ShopStock)
            {
                if (stock.Item == null)
                    continue;

                // For Kart items, check whether metadata exists; skip the item if it does not.
                if (stock.Item.Type == ItemType.Kart)
                {
                    int itemId = stock.Item.Id;
                    // Skip values outside the ushort range.
                    if (DataProvider.Karts == null || itemId < 0 || itemId > ushort.MaxValue || !DataProvider.Karts.ContainsKey((ushort)itemId))
                    {
                        Log.Warn(
                            "[LocalDB] Shop item skipped: Kart data missing or invalid GameID={0}", 
                            itemId);
                        continue;
                    }
                }

                if (LocalItemExists(
                    playerId,
                    stock.Item.Id,
                    stock.Item.Type))
                {
                    continue;
                }

                AddLocalItem(
                    playerId,
                    stock.Item.Id,
                    stock.Item.Type);

                shopItemCount++;

                Log.Inform(
                    "[LocalDB] Shop item added: GameID={0}, Type={1}",
                    stock.Item.Id,
                    stock.Item.Type);
            }
        }
        // ========================================================
        // Default Character
        // ========================================================

        ushort defaultCharacterId = 0;

        foreach (
            Dictionary<string, object> row
            in LocalTables["items"])
        {
            if (!row.TryGetValue(
                    "PlayerID",
                    out object? playerIdValue))
            {
                continue;
            }

            if (!row.TryGetValue(
                    "Type",
                    out object? typeValue))
            {
                continue;
            }

            if (Convert.ToInt32(playerIdValue) != playerId)
                continue;

            if (Convert.ToInt32(typeValue) !=
                (int)ItemType.Character)
            {
                continue;
            }

            if (!row.TryGetValue(
                    "GameID",
                    out object? gameIdValue))
            {
                continue;
            }

            defaultCharacterId =
                Convert.ToUInt16(gameIdValue);

            break;
        }

        if (defaultCharacterId != 0)
        {
            SetDefaultEquippedItem(
                playerId,
                defaultCharacterId,
                ItemType.Character);
        }

        // ========================================================
        // Default Kart
        // ========================================================

        if (defaultKartId != 0)
        {
            SetDefaultEquippedItem(
                playerId,
                defaultKartId,
                ItemType.Kart);
        }

        Log.Inform(
            "[LocalDB] Default items created.");

        Log.Inform(
            "[LocalDB] Characters = {0}",
            characterCount);

        Log.Inform(
            "[LocalDB] Karts = {0}",
            kartCount);

        Log.Inform(
            "[LocalDB] ShopItems = {0}",
            shopItemCount);

        Log.Inform(
            "[LocalDB] Total Items = {0}",
            LocalTables["items"].Count);
    }

    // ============================================================
    // Add Local Item
    // ============================================================

    private static void AddLocalItem(
    int playerId,
    int gameId,
    ItemType type)
    {
        EnsureLocalTable("items");

        LocalTables["items"].Add(
            new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = LocalIdCounter++,
                ["PlayerID"] = playerId,
                ["GameID"] = gameId,
                ["Type"] = (int)type,
                ["Quantity"] = 1,
                ["Expiration"] = -1L,
                ["IsEquipped"] = false,
                ["IsPermanent"] = true
            });
    }

    // ============================================================
    // Local Item Exists
    // ============================================================

    private static bool LocalItemExists(
    int playerId,
    int gameId,
    ItemType type)
    {
        EnsureLocalTable("items");

        foreach (Dictionary<string, object> row in LocalTables["items"])
        {
            if (!row.TryGetValue("PlayerID", out object? pid))
                continue;

            if (!row.TryGetValue("GameID", out object? gid))
                continue;

            if (!row.TryGetValue("Type", out object? itemType))
                continue;

            if (Convert.ToInt32(pid) != playerId)
                continue;

            if (Convert.ToInt32(gid) != gameId)
                continue;

            if (Convert.ToInt32(itemType) != (int)type)
                continue;

            return true;
        }

        return false;
    }
    // ============================================================
    // Default Equipped
    // ============================================================

    private static void SetDefaultEquippedItem(
    int playerId,
    int gameId,
    ItemType type)
    {
        EnsureLocalTable("items");

        bool found = false;

        foreach (
            Dictionary<string, object> row
            in LocalTables["items"])
        {
            if (!row.TryGetValue(
                    "PlayerID",
                    out object? playerValue))
            {
                continue;
            }

            if (!row.TryGetValue(
                    "Type",
                    out object? typeValue))
            {
                continue;
            }

            if (Convert.ToInt32(playerValue) != playerId)
                continue;

            if (Convert.ToInt32(typeValue) != (int)type)
                continue;

            row["IsEquipped"] = false;

            if (!row.TryGetValue(
                    "GameID",
                    out object? gameValue))
            {
                continue;
            }

            if (Convert.ToInt32(gameValue) == gameId)
            {
                row["IsEquipped"] = true;
                found = true;
            }
        }

        if (!found)
        {
            LocalTables["items"].Add(
                new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["ID"] = LocalIdCounter++,
                    ["PlayerID"] = playerId,
                    ["GameID"] = gameId,
                    ["Type"] = (int)type,
                    ["Quantity"] = 1,
                    ["Expiration"] = -1L,
                    ["IsEquipped"] = true,
                    ["IsPermanent"] = true
                });
        }

        Log.Inform(
            "[LocalDB] Default equipped: PlayerID={0}, GameID={1}, Type={2}",
            playerId,
            gameId,
            type);
    }

    // ============================================================
    // Execute
    // ============================================================

    internal static void Execute(
        string nonQuery,
        params object[] args)
    {
        string query =
            string.Format(nonQuery, args);

        if (LocalMode)
        {
            LocalExecute(query);
            return;
        }

        MySqlHelper.ExecuteNonQuery(
            ConnectionString,
            query);
    }

    // ============================================================
    // Correct Fields
    // ============================================================

    public static string CorrectFields(
        string fields)
    {
        string final = string.Empty;

        string[] tokens =
            fields
                .Replace(",", " ")
                .Replace(";", " ")
                .Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tokens.Length; i++)
        {
            if (i > 0)
                final += ", ";

            final += tokens[i];
        }

        return final;
    }

    // ============================================================
    // Test
    // ============================================================

    public static void Test()
    {
        if (LocalMode)
        {
            Log.Inform(
                "[LocalDB] Local database mode active.");

            return;
        }

        using MySqlConnection connection =
            new MySqlConnection(ConnectionString);

        connection.Open();

        Log.Inform(
            "Able to connect to database '{0}'.",
            connection.Database);

        connection.Close();
    }

    // ============================================================
    // Analyze
    // ============================================================

    public static void Analyze()
    {
        if (LocalMode)
            return;

        using (TemporarySchema("information_schema"))
        {
            Meta.Initialize();
        }
    }

    // ============================================================
    // DELETE
    // ============================================================

    public static void Delete(
        string table,
        string constraints,
        params object[] args)
    {
        string formatted =
            string.Format(
                constraints,
                args);

        if (LocalMode)
        {
            LocalDelete(
                table,
                formatted);

            return;
        }

        Execute(
            "DELETE FROM {0} WHERE {1}",
            table,
            formatted);
    }

    // ============================================================
    // EXISTS
    // ============================================================

    public static bool Exists(
        string table,
        string constraints,
        params object[] args)
    {
        string formatted =
            string.Format(
                constraints,
                args);

        if (LocalMode)
        {
            return LocalExists(
                table,
                formatted);
        }

        MySqlParameter[] parameters =
            new MySqlParameter[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            parameters[i] =
                new MySqlParameter(
                    "?param_" + i,
                    args[i]);
        }

        using MySqlDataReader reader =
            MySqlHelper.ExecuteReader(
                ConnectionString,
                $"SELECT * FROM {table} WHERE {constraints}",
                parameters);

        return reader.HasRows;
    }

    // ============================================================
    // FETCH
    // ============================================================

    public static dynamic Fetch(
        string table,
        string field,
        string constraints,
        params object[] args)
    {
        if (LocalMode)
        {
            Dictionary<string, object> row =
                new Datum(table)
                    .PopulateWith(
                        field,
                        constraints,
                        args)
                    .Dictionary;

            if (!row.TryGetValue(
                    field,
                    out object? value))
            {
                return null;
            }

            if (value is DBNull)
                return null;

            return value;
        }

        object value2 =
            new Datum(table)
                .PopulateWith(
                    field,
                    constraints,
                    args)
                .Dictionary[field];

        if (value2 is DBNull)
            return null;

        if (value2 is byte b &&
            Meta.IsBool(table, field))
        {
            return b > 0;
        }

        return value2;
    }

    // ============================================================
    // SELECT MANY
    // ============================================================

    public static List<Dictionary<string, object>> SelectMany(
        string table,
        string[] fields,
        string constraints,
        params object[] args)
    {
        string formattedConstraints =
            string.Format(
                constraints,
                args);

        if (LocalMode)
        {
            return LocalSelectMany(
                table,
                fields,
                formattedConstraints);
        }

        string selectedFields =
            fields == null ||
            fields.Length == 0
                ? "*"
                : CorrectFields(
                    string.Join(",", fields));

        string query =
            $"SELECT {selectedFields} FROM {table}";

        if (!string.IsNullOrWhiteSpace(
                formattedConstraints))
        {
            query +=
                $" WHERE {formattedConstraints}";
        }

        List<Dictionary<string, object>> result =
            new();

        using MySqlDataReader reader =
            MySqlHelper.ExecuteReader(
                ConnectionString,
                query);

        while (reader.Read())
        {
            Dictionary<string, object> row =
                new(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0;
                 i < reader.FieldCount;
                 i++)
            {
                object value =
                    reader.GetValue(i);

                if (value is DBNull)
                    value = null;

                row[reader.GetName(i)] =
                    value;
            }

            result.Add(row);
        }

        return result;
    }

    // ============================================================
    // Execute Script
    // ============================================================

    public static void ExecuteScript(
        string host,
        string username,
        string password,
        string query,
        params object[] args)
    {
        if (LocalMode)
            return;

        using MySqlConnection connection =
            new MySqlConnection(
                $"SERVER={host}; UID={username}; PASSWORD={password};");

        connection.Open();

        new MySqlScript(
            connection,
            string.Format(query, args))
            .Execute();

        connection.Close();
    }

    // ============================================================
    // Execute File
    // ============================================================

    public static void ExecuteFile(
        string host,
        string username,
        string password,
        string path)
    {
        if (LocalMode)
            return;

        using MySqlConnection connection =
            new MySqlConnection(
                $"SERVER={host}; UID={username}; PASSWORD={password};");

        connection.Open();

        using TextReader reader =
            new StreamReader(path);

        new MySqlScript(
            connection,
            reader.ReadToEnd())
            .Execute();

        connection.Close();
    }

    // ============================================================
    // Temporary Connection
    // ============================================================

    public static TemporaryConnection TemporaryConnection(
        string host,
        string schema,
        string username,
        string password)
    {
        return new TemporaryConnection(
            host,
            schema,
            username,
            password);
    }

    // ============================================================
    // Temporary Schema
    // ============================================================

    public static TemporarySchema TemporarySchema(
        string schema)
    {
        return new TemporarySchema(schema);
    }

    // ============================================================
    // Escape
    // ============================================================

    public static string Escape(
        object argument)
    {
        if (argument == null)
            return "NULL";

        if (argument is string)
        {
            return "'" +
                   MySqlHelper.EscapeString(
                       (string)argument) +
                   "'";
        }

        if (argument is byte[])
        {
            byte[] bytes =
                (byte[])argument;

            StringBuilder text =
                new StringBuilder();

            foreach (byte b in bytes)
            {
                text.Append(
                    b.ToString("X2"));
            }

            return "UNHEX('" +
                   text +
                   "')";
        }

        if (argument is DateTime)
        {
            return "'" +
                   ((DateTime)argument)
                       .ToString(
                           "yyyy-MM-dd HH:mm:ss") +
                   "'";
        }

        if (argument is bool)
        {
            return (bool)argument
                ? "1"
                : "0";
        }

        return argument.ToString()!;
    }

    // ============================================================
    // Password
    // ============================================================

    internal static string HashPassword(
        string value)
    {
        using SHA512 sha =
            SHA512.Create();

        byte[] hash =
            sha.ComputeHash(
                Encoding.Unicode.GetBytes(value));

        StringBuilder sb =
            new StringBuilder(128);

        foreach (byte b in hash)
        {
            sb.Append(
                b.ToString("X2"));
        }

        return sb.ToString();
    }

    // ============================================================
    // LOCAL DELETE
    // ============================================================

    internal static void LocalDelete(
        string table,
        string constraints)
    {
        EnsureLocalTable(table);

        LocalTables[table].RemoveAll(
            row =>
                LocalMatches(
                    row,
                    constraints));
    }

    // ============================================================
    // LOCAL EXISTS
    // ============================================================

    internal static bool LocalExists(
        string table,
        string constraints)
    {
        EnsureLocalTable(table);

        return LocalTables[table].Exists(
            row =>
                LocalMatches(
                    row,
                    constraints));
    }

    // ============================================================
    // LOCAL SELECT MANY
    // ============================================================

    internal static List<Dictionary<string, object>>
        LocalSelectMany(
            string table,
            string[] fields,
            string constraints)
    {
        EnsureLocalTable(table);

        List<Dictionary<string, object>> result =
            new();

        foreach (
            Dictionary<string, object> row
            in LocalTables[table])
        {
            if (!LocalMatches(
                    row,
                    constraints))
            {
                continue;
            }

            if (fields == null ||
                fields.Length == 0)
            {
                result.Add(
                    new Dictionary<string, object>(
                        row,
                        StringComparer.OrdinalIgnoreCase));

                continue;
            }

            Dictionary<string, object> selected =
                new(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string field in fields)
            {
                string key =
                    field.Trim();

                if (row.TryGetValue(
                        key,
                        out object? value))
                {
                    selected[key] = value;
                }
            }

            result.Add(selected);
        }

        return result;
    }

    // ============================================================
    // LOCAL SELECT SINGLE
    // ============================================================

    internal static Dictionary<string, object>
        LocalSelectSingle(
            string table,
            string[] fields,
            string constraints)
    {
        List<Dictionary<string, object>> list =
            LocalSelectMany(
                table,
                fields,
                constraints);

        if (list.Count == 0)
            throw new RowNotInTableException();

        if (list.Count > 1)
            throw new RowNotUniqueException();

        return list[0];
    }

    // ============================================================
    // LOCAL SELECT QUERY
    // ============================================================

    internal static List<Dictionary<string, object>>
        LocalSelectManyFromQuery(
            string query)
    {
        string table =
            ParseTableNameFromQuery(
                query,
                out string fields,
                out string constraints);

        string[] fieldArray =
            string.IsNullOrWhiteSpace(fields) ||
            fields.Trim() == "*"
                ? null
                : fields.Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries);

        return LocalSelectMany(
            table,
            fieldArray,
            constraints);
    }

    // ============================================================
    // LOCAL SELECT SINGLE QUERY
    // ============================================================

    internal static Dictionary<string, object>
        LocalSelectSingleFromQuery(
            string query,
            string expectedTable)
    {
        string table =
            ParseTableNameFromQuery(
                query,
                out string fields,
                out string constraints);

        if (!string.Equals(
                table,
                expectedTable,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new RowNotInTableException();
        }

        string[] fieldArray =
            string.IsNullOrWhiteSpace(fields) ||
            fields.Trim() == "*"
                ? null
                : fields.Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries);

        return LocalSelectSingle(
            table,
            fieldArray,
            constraints);
    }

    // ============================================================
    // QUERY PARSER
    // ============================================================

    private static string ParseTableNameFromQuery(
        string query,
        out string fields,
        out string constraints)
    {
        fields = null!;
        constraints = string.Empty;

        string normalized =
            query.Trim();

        int selectIndex =
            normalized.IndexOf(
                "SELECT ",
                StringComparison.OrdinalIgnoreCase);

        int fromIndex =
            normalized.IndexOf(
                " FROM ",
                StringComparison.OrdinalIgnoreCase);

        if (selectIndex < 0 ||
            fromIndex < 0)
        {
            throw new InvalidOperationException(
                "Unable to parse query.");
        }

        fields =
            normalized.Substring(
                selectIndex + 7,
                fromIndex -
                (selectIndex + 7))
            .Trim();

        int whereIndex =
            normalized.IndexOf(
                " WHERE ",
                StringComparison.OrdinalIgnoreCase);

        string table;

        if (whereIndex < 0)
        {
            table =
                normalized.Substring(
                    fromIndex + 6)
                .Trim();
        }
        else
        {
            table =
                normalized.Substring(
                    fromIndex + 6,
                    whereIndex -
                    (fromIndex + 6))
                .Trim();
        }

        if (whereIndex >= 0)
        {
            constraints =
                normalized.Substring(
                    whereIndex + 7)
                .Trim();
        }

        return table;
    }

    // ============================================================
    // LOCAL EXECUTE
    // ============================================================

    internal static void LocalExecute(
        string query)
    {
        query = query.Trim();

        // ========================================================
        // UPDATE
        // ========================================================

        if (query.StartsWith(
                "UPDATE ",
                StringComparison.OrdinalIgnoreCase))
        {
            int setIndex =
                query.IndexOf(
                    " SET ",
                    StringComparison.OrdinalIgnoreCase);

            if (setIndex < 0)
                return;

            string table =
                query.Substring(
                    7,
                    setIndex - 7)
                .Trim();

            int whereIndex =
                query.IndexOf(
                    " WHERE ",
                    StringComparison.OrdinalIgnoreCase);

            string assignmentPart;

            string wherePart;

            if (whereIndex < 0)
            {
                assignmentPart =
                    query.Substring(
                        setIndex + 5);

                wherePart =
                    string.Empty;
            }
            else
            {
                assignmentPart =
                    query.Substring(
                        setIndex + 5,
                        whereIndex -
                        (setIndex + 5));

                wherePart =
                    query.Substring(
                        whereIndex + 7);
            }

            Dictionary<string, object> values =
                ParseAssignments(
                    assignmentPart.Trim());

            EnsureLocalTable(table);

            foreach (
                Dictionary<string, object> row
                in LocalTables[table])
            {
                if (!LocalMatches(
                        row,
                        wherePart.Trim()))
                {
                    continue;
                }

                foreach (
                    KeyValuePair<string, object> kvp
                    in values)
                {
                    row[kvp.Key] =
                        kvp.Value;
                }
            }

            return;
        }

        // ========================================================
        // DELETE
        // ========================================================

        if (query.StartsWith(
                "DELETE FROM ",
                StringComparison.OrdinalIgnoreCase))
        {
            int whereIndex =
                query.IndexOf(
                    " WHERE ",
                    StringComparison.OrdinalIgnoreCase);

            string table =
                query.Substring(
                    12,
                    whereIndex < 0
                        ? query.Length - 12
                        : whereIndex - 12)
                .Trim();

            string where =
                whereIndex < 0
                    ? string.Empty
                    : query.Substring(
                        whereIndex + 7)
                      .Trim();

            LocalDelete(
                table,
                where);

            return;
        }

        // ========================================================
        // INSERT
        // ========================================================

        if (query.StartsWith(
                "INSERT INTO ",
                StringComparison.OrdinalIgnoreCase))
        {
            int startFields =
                query.IndexOf('(');

            int endFields =
                query.IndexOf(
                    ')',
                    startFields + 1);

            int valuesIndex =
                query.IndexOf(
                    "VALUES",
                    endFields,
                    StringComparison.OrdinalIgnoreCase);

            if (startFields < 0 ||
                endFields < 0 ||
                valuesIndex < 0)
            {
                return;
            }

            string table =
                query.Substring(
                    12,
                    startFields - 12)
                .Trim();

            string fieldsText =
                query.Substring(
                    startFields + 1,
                    endFields -
                    startFields - 1);

            int startValues =
                query.IndexOf(
                    '(',
                    valuesIndex);

            int endValues =
                query.IndexOf(
                    ')',
                    startValues + 1);

            if (startValues < 0 ||
                endValues < 0)
            {
                return;
            }

            string valuesText =
                query.Substring(
                    startValues + 1,
                    endValues -
                    startValues - 1);

            string[] fieldNames =
                fieldsText.Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries);

            string[] valueTexts =
                valuesText.Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, object> row =
                new(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0;
                 i < fieldNames.Length &&
                 i < valueTexts.Length;
                 i++)
            {
                string fieldName =
                    fieldNames[i].Trim();

                row[fieldName] =
                    ParseValue(
                        valueTexts[i].Trim());
            }

            EnsureLocalTable(table);

            if (!row.ContainsKey("ID"))
            {
                row["ID"] =
                    LocalIdCounter++;
            }

            LocalTables[table].Add(row);

            return;
        }
    }

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    private static Dictionary<string, object>
        ParseAssignments(
            string assignments)
    {
        Dictionary<string, object> result =
            new(
                StringComparer.OrdinalIgnoreCase);

        string[] parts =
            assignments.Split(
                new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            int index =
                part.IndexOf('=');

            if (index < 0)
                continue;

            string name =
                part.Substring(
                    0,
                    index)
                .Trim();

            string valueText =
                part.Substring(
                    index + 1)
                .Trim();

            result[name] =
                ParseValue(valueText);
        }

        return result;
    }

    // ============================================================
    // LOCAL MATCH
    // ============================================================

    internal static bool LocalMatches(
        Dictionary<string, object> row,
        string constraints)
    {
        if (string.IsNullOrWhiteSpace(
                constraints))
        {
            return true;
        }

        string[] conditions =
            constraints.Split(
                new[] { " AND " },
                StringSplitOptions.None);

        foreach (string rawCondition
                 in conditions)
        {
            string condition =
                rawCondition.Trim();

            int equalsIndex =
                condition.IndexOf('=');

            if (equalsIndex < 0)
                return false;

            string field =
                condition.Substring(
                    0,
                    equalsIndex)
                .Trim();

            string expectedValue =
                condition.Substring(
                    equalsIndex + 1)
                .Trim();

            object? actualValue =
                row.TryGetValue(
                    field,
                    out object? value)
                    ? value
                    : null;

            object? parsed =
                ParseValue(expectedValue);

            if (!CompareValues(
                    actualValue,
                    parsed))
            {
                return false;
            }
        }

        return true;
    }

    // ============================================================
    // COMPARE
    // ============================================================

    private static bool CompareValues(
        object? actual,
        object? expected)
    {
        if (actual == null &&
            expected == null)
        {
            return true;
        }

        if (actual == null ||
            expected == null)
        {
            return false;
        }

        if (actual is bool actualBool &&
            expected is bool expectedBool)
        {
            return actualBool ==
                   expectedBool;
        }

        // Numeric comparison
        if (IsNumber(actual) &&
            IsNumber(expected))
        {
            try
            {
                decimal a =
                    Convert.ToDecimal(actual);

                decimal b =
                    Convert.ToDecimal(expected);

                return a == b;
            }
            catch
            {
                // Continue with the string comparison below.
            }
        }

        return string.Equals(
            actual.ToString(),
            expected.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // NUMBER
    // ============================================================

    private static bool IsNumber(
        object value)
    {
        return value is byte ||
               value is sbyte ||
               value is short ||
               value is ushort ||
               value is int ||
               value is uint ||
               value is long ||
               value is ulong ||
               value is float ||
               value is double ||
               value is decimal;
    }

    // ============================================================
    // PARSE VALUE
    // ============================================================

    private static object? ParseValue(
        string value)
    {
        value =
            value.Trim();

        // String value
        if (value.Length >= 2 &&
            value.StartsWith("'") &&
            value.EndsWith("'"))
        {
            value =
                value.Substring(
                    1,
                    value.Length - 2)
                .Replace(
                    "''",
                    "'");

            return value;
        }

        // NULL
        if (value.Equals(
                "NULL",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // TRUE
        if (value.Equals(
                "TRUE",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // FALSE
        if (value.Equals(
                "FALSE",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 1
        if (value == "1")
            return (byte)1;

        // 0
        if (value == "0")
            return (byte)0;

        // Int
        if (int.TryParse(
                value,
                out int intValue))
        {
            return intValue;
        }

        // Long
        if (long.TryParse(
                value,
                out long longValue))
        {
            return longValue;
        }

        // Decimal
        if (decimal.TryParse(
                value,
                out decimal decimalValue))
        {
            return decimalValue;
        }

        return value;
    }

    // ============================================================
    // TABLE
    // ============================================================

    private static void EnsureLocalTable(
        string table)
    {
        if (!LocalTables.ContainsKey(table))
        {
            LocalTables.Add(
                table,
                new List<Dictionary<string, object>>());
        }
    }

    // ============================================================
    // LOCAL ACCOUNT
    // ============================================================

    private static void EnsureLocalAccount(
     int accountId,
     string username,
     string password)
    {
        EnsureLocalTable("accounts");

        if (GetLocalAccount(username) != null)
            return;

        LocalTables["accounts"].Add(
            new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = accountId,

                ["Username"] =
                    username,

                ["Password"] =
                    HashPassword(password),

                ["IsBanned"] =
                    (byte)0,

                // Only the first account is a master account.
                ["IsMaster"] =
                    accountId == 1
                        ? (byte)1
                        : (byte)0,

                ["BitSetValue"] =
                    0u,

                ["SessionIP"] =
                    string.Empty
            });

        if (LocalIdCounter <= accountId)
            LocalIdCounter = accountId + 1;

        Log.Inform(
            "[LocalDB] Account created: ID={0}, Username={1}",
            accountId,
            username);
    }

    // ============================================================
    // LOCAL PLAYER
    // ============================================================

    private static void EnsureLocalPlayer(
     int playerId,
     int accountId,
     string nickname)
    {
        EnsureLocalTable("players");

        if (GetLocalPlayer(playerId) != null)
            return;

        LocalTables["players"].Add(
            new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] =
                    playerId,

                ["AccountID"] =
                    accountId,

                ["Name"] =
                    nickname,

                ["RP"] =
                    1000000000,

                ["Lucci"] =
                    1000000000L,

                ["Cash"] =
                    1000000000L,

                ["License"] = 5,


                ["NewbieStatus"] =
                    0xFFFF,

                ["RookieStatus"] =
                    0xFFFF,

                ["L3Status"] =
                    0xFFFF,

                ["L2Status"] =
                    0xFFFF,

                ["L1Status"] =
                    0xFFFF,

                ["PROStatus"] =
                    0xFFFF,

                ["Creation"] =
                    DateTime.Now,

                ["Statistics"] =
                    (byte)0
            });

        if (LocalIdCounter <= playerId)
            LocalIdCounter = playerId + 1;

        Log.Inform(
            "[LocalDB] Player created: ID={0}, AccountID={1}, Name={2}",
            playerId,
            accountId,
            nickname);
    }

    // ============================================================
    // GET LOCAL ACCOUNT
    // ============================================================

    // ============================================================
    // LOCAL ACCOUNT GET BY USERNAME
    // ============================================================

    public static Dictionary<string, object>? GetLocalAccount(
        string username)
    {
        if (!LocalMode)
            return null;

        EnsureLocalTable("accounts");

        foreach (Dictionary<string, object> row
                 in LocalTables["accounts"])
        {
            if (!row.TryGetValue(
                    "Username",
                    out object? value))
            {
                continue;
            }

            string storedUsername =
                Convert.ToString(value)
                ?? string.Empty;

            if (string.Equals(
                    storedUsername,
                    username,
                    StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }

        return null;
    }

    // ============================================================
    // LOCAL ACCOUNT GET BY ID
    // ============================================================

    public static Dictionary<string, object>? GetLocalAccountById(
        int id)
    {
        if (!LocalMode)
            return null;

        EnsureLocalTable("accounts");

        foreach (Dictionary<string, object> row
                 in LocalTables["accounts"])
        {
            if (!row.TryGetValue(
                    "ID",
                    out object? value))
            {
                continue;
            }

            if (Convert.ToInt32(value) == id)
            {
                return row;
            }
        }

        return null;
    }

    // ============================================================
    // LOCAL ACCOUNT UPDATE
    // ============================================================

    public static bool UpdateLocalAccount(
        int id,
        string username,
        bool isBanned,
        bool isMaster,
        uint bitSetValue,
        string sessionIP)
    {
        if (!LocalMode)
            return false;

        EnsureLocalTable("accounts");

        foreach (Dictionary<string, object> row
                 in LocalTables["accounts"])
        {
            if (!row.TryGetValue(
                    "ID",
                    out object? value))
            {
                continue;
            }

            if (Convert.ToInt32(value) != id)
                continue;

            /*
             * The account ID and Username are login identifiers.
             *
             * When changing a nickname in userset,
             * do not call this method.
             */
            row["Username"] = username;
            row["IsBanned"] = isBanned ? (byte)1 : (byte)0;
            row["IsMaster"] = isMaster ? (byte)1 : (byte)0;
            row["BitSetValue"] = bitSetValue;
            row["SessionIP"] = sessionIP ?? string.Empty;

            return true;
        }

        return false;
    }
    // ============================================================
    // GET LOCAL PLAYER BY ACCOUNT ID
    // ============================================================

    public static Dictionary<string, object>?
        GetLocalPlayerByAccountId(
            int accountId)
    {
        if (!LocalMode)
            return null;

        EnsureLocalTable("players");

        foreach (
            Dictionary<string, object> row
            in LocalTables["players"])
        {
            if (!row.TryGetValue(
                    "AccountID",
                    out object? accountValue))
            {
                continue;
            }

            if (Convert.ToInt32(
                    accountValue) != accountId)
            {
                continue;
            }

            return row;
        }

        return null;
    }

    // ============================================================
    // GET LOCAL PLAYER BY ID
    // ============================================================

    public static Dictionary<string, object>? GetLocalPlayer()
    {
        return GetLocalPlayer(1);
    }

    public static Dictionary<string, object>? GetLocalPlayer(int playerId)
    {
        if (!LocalMode)
            return null;

        EnsureLocalTable("players");

        foreach (Dictionary<string, object> row
                 in LocalTables["players"])
        {
            if (!row.TryGetValue(
                    "ID",
                    out object? id))
            {
                continue;
            }

            if (Convert.ToInt32(id) == playerId)
            {
                return row;
            }
        }

        return null;
    }

    // ============================================================
    // UPDATE LOCAL PLAYER
    // ============================================================

    public static bool UpdateLocalPlayer(
      int playerId,
      int accountId,
      string nickname,
      int rp,
      long lucci,
      long cash,
      sbyte license,
      ushort newbieStatus,
      ushort rookieStatus,
      ushort l3Status,
      ushort l2Status,
      ushort l1Status,
      ushort proStatus,
      DateTime creation)
    {
        if (!LocalMode)
            return false;

        EnsureLocalTable("players");

        foreach (Dictionary<string, object> row
                 in LocalTables["players"])
        {
            if (!row.TryGetValue(
                    "ID",
                    out object? idValue))
            {
                continue;
            }

            if (Convert.ToInt32(idValue) != playerId)
                continue;

            row["AccountID"] = accountId;
            row["Name"] = nickname;
            row["RP"] = rp;
            row["Lucci"] = lucci;
            row["Cash"] = cash;
            row["License"] = license;

            row["NewbieStatus"] = newbieStatus;
            row["RookieStatus"] = rookieStatus;
            row["L3Status"] = l3Status;
            row["L2Status"] = l2Status;
            row["L1Status"] = l1Status;
            row["PROStatus"] = proStatus;

            row["Creation"] = creation;

            Log.Inform(
                "[LocalDB] Player updated: " +
                "ID={0}, AccountID={1}, Name={2}, License={3}, " +
                "RP={4}, Lucci={5}, Cash={6}",
                playerId,
                accountId,
                nickname,
                license,
                rp,
                lucci,
                cash);

            return true;
        }

        Log.Error(
            "[LocalDB] Player not found for update. PlayerID={0}",
            playerId);

        return false;
    }

    // ============================================================
    // UPDATE LOCAL PLAYER NAME ONLY
    // ============================================================
    public static bool UpdateLocalPlayer(
    string nickname,
    int rp,
    long lucci,
    long cash)
    {
        if (!LocalMode)
            return false;

        EnsureLocalTable("players");

        foreach (Dictionary<string, object> row
                 in LocalTables["players"])
        {
            if (!row.TryGetValue(
                    "ID",
                    out object? idValue))
            {
                continue;
            }

            int playerId = Convert.ToInt32(idValue);

            row["Name"] = nickname;
            row["RP"] = rp;
            row["Lucci"] = lucci;
            row["Cash"] = cash;

            Log.Inform(
                "[LocalDB] Player updated: " +
                "ID={0}, Name={1}, RP={2}, Lucci={3}, Cash={4}",
                playerId,
                nickname,
                rp,
                lucci,
                cash);

            return true;
        }

        Log.Error(
            "[LocalDB] Player not found for update.");

        return false;
    }
    public static bool UpdateLocalPlayerName(
       int playerId,
       string nickname)
    {
        if (!LocalMode)
            return false;

        Dictionary<string, object>? player =
            GetLocalPlayer(playerId);

        if (player == null)
            return false;

        player["Name"] =
            nickname;

        Log.Inform(
            "[LocalDB] Player nickname changed: PlayerID={0}, Name={1}",
            playerId,
            nickname);

        return true;
    }

    // ============================================================
    // UPDATE LOCAL PLAYER ACCOUNT NAME
    // ============================================================

    public static bool UpdateLocalPlayerNameByAccount(
        int accountId,
        string nickname)
    {
        if (!LocalMode)
            return false;

        Dictionary<string, object>? player =
            GetLocalPlayerByAccountId(
                accountId);

        if (player == null)
            return false;

        player["Name"] =
            nickname;

        Log.Inform(
            "[LocalDB] Player nickname changed: AccountID={0}, Name={1}",
            accountId,
            nickname);

        return true;
    }

    // ============================================================
    // DEBUG DUMP
    // ============================================================
    public static int GetNextLocalId()
    {
        if (!LocalMode)
            throw new InvalidOperationException(
                "GetNextLocalId() can only be used in LocalMode.");

        return LocalIdCounter++;
    }
    public static void DumpLocalDatabase()
    {
        if (!LocalMode)
        {
            Log.Inform(
                "[LocalDB] Local mode is not enabled.");

            return;
        }

        Log.SkipLine();

        Log.Inform(
            "[LocalDB] ================= DATABASE DUMP =================");

        foreach (
            KeyValuePair<
                string,
                List<Dictionary<string, object>>>
            table
            in LocalTables)
        {
            Log.Inform(
                "[LocalDB] TABLE: {0}, ROWS: {1}",
                table.Key,
                table.Value.Count);

            int index = 0;

            foreach (
                Dictionary<string, object> row
                in table.Value)
            {
                StringBuilder builder =
                    new StringBuilder();

                builder.Append(
                    $"[{index}] ");

                foreach (
                    KeyValuePair<string, object> field
                    in row)
                {
                    builder.Append(
                        $"{field.Key}={field.Value}; ");
                }

                Log.Inform(
                    "[LocalDB] {0}",
                    builder.ToString());

                index++;
            }
        }

        Log.Inform(
            "[LocalDB] ====================================================");

        Log.SkipLine();
    }
}