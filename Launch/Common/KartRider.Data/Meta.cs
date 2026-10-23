using System.Collections.Generic;

namespace KartRider.Data;

internal static class Meta
{
	public static Dictionary<string, Dictionary<string, Column>> Tables = new Dictionary<string, Dictionary<string, Column>>();

	public static void Initialize()
	{
		Tables.Clear();
		foreach (dynamic datum in new Datums("COLUMNS").Populate("TABLE_SCHEMA = '{0}'", Database.DefaultSchema))
		{
			Meta.Add(datum);
		}
	}

	private static void Add(dynamic datum)
	{
		string tableName = datum.TABLE_NAME;
		Dictionary<string, Column> table;
		if (Tables.ContainsKey(tableName))
		{
			table = Tables[tableName];
		}
		else
		{
			table = new Dictionary<string, Column>();
			Tables.Add(tableName, table);
		}
		table.Add(datum.COLUMN_NAME, new Column(datum));
	}

	public static bool IsBool(string tableName, string fieldName)
	{
		return Tables[tableName][fieldName].ColumnType == "tinyint unsigned";
	}

	public static bool IsDate(string tableName, string fieldName)
	{
		return Tables[tableName][fieldName].ColumnType == "date";
	}

	public static bool IsDateTime(string tableName, string fieldName)
	{
		return Tables[tableName][fieldName].ColumnType == "datetime";
	}
}
