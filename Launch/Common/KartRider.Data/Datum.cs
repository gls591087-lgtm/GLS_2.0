using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using MySql.Data.MySqlClient;

namespace KartRider.Data;

public class Datum : DynamicObject
{
	public string Table { get; private set; }

	internal Dictionary<string, object> Dictionary { get; set; }

	public Datum(string table)
	{
		Table = table;
		Dictionary = new Dictionary<string, object>();
	}

	internal Datum(string table, Dictionary<string, object> values)
	{
		Table = table;
		Dictionary = values;
	}

	internal void Populate(string query)
	{
		if (Database.LocalMode)
		{
			Dictionary<string, object> row = Database.LocalSelectSingleFromQuery(query.Trim(), Table);
			Dictionary = row;
			return;
		}
		using MySqlDataReader reader = MySqlHelper.ExecuteReader(Database.ConnectionString, query);
		if (reader.RecordsAffected > 1)
		{
			throw new RowNotUniqueException();
		}
		if (!reader.HasRows)
		{
			throw new RowNotInTableException();
		}
		reader.Read();
		for (int i = 0; i < reader.FieldCount; i++)
		{
			string name = reader.GetName(i);
			object value = reader.GetValue(i);
			if (Dictionary.ContainsKey(name))
			{
				Dictionary[name] = value;
			}
			else
			{
				Dictionary.Add(name, value);
			}
		}
	}

	public dynamic Populate(string constraints, params object[] args)
	{
		Populate($"SELECT * FROM {Table} WHERE {string.Format(constraints, args)}");
		return this;
	}

	public dynamic PopulateWith(string fields, string constraints, params object[] args)
	{
		Populate($"SELECT {Database.CorrectFields(fields)} FROM {Table} WHERE {string.Format(constraints, args)}");
		return this;
	}

	public void Insert()
	{
		string fields = "( ";
		int processed = 0;
		foreach (KeyValuePair<string, object> item in Dictionary)
		{
			fields += item.Key;
			processed++;
			if (processed < Dictionary.Count)
			{
				fields += ", ";
			}
		}
		fields += " ) VALUES ( ";
		processed = 0;
		foreach (KeyValuePair<string, object> item2 in Dictionary)
		{
			fields += Database.Escape(item2.Value);
			processed++;
			if (processed < Dictionary.Count)
			{
				fields += ", ";
			}
		}
		fields += " )";
		Database.Execute("INSERT INTO {0} {1}", Table, fields);
	}

	public void Update(string constraints, params object[] args)
	{
		int processed = 0;
		string fields = string.Empty;
		foreach (KeyValuePair<string, object> loopPair in Dictionary)
		{
			fields += $"{loopPair.Key} = {Database.Escape(loopPair.Value)}";
			processed++;
			if (processed < Dictionary.Count)
			{
				fields += ", ";
			}
		}
		Database.Execute("UPDATE {0} SET {1} WHERE {2}", Table, fields, string.Format(constraints, args));
	}

	public override bool TryGetMember(GetMemberBinder binder, out object result)
	{
		if (Dictionary.ContainsKey(binder.Name))
		{
			if (Dictionary[binder.Name] is DBNull)
			{
				result = null;
			}
			else if (Dictionary[binder.Name] is byte && !Database.LocalMode && Meta.IsBool(Table, binder.Name))
			{
				result = (byte)Dictionary[binder.Name] > 0;
			}
			else
			{
				result = Dictionary[binder.Name];
			}
			return true;
		}
		result = null;
		return false;
	}

	public override bool TrySetMember(SetMemberBinder binder, object value)
	{
		if (value is DateTime)
		{
			if (!Database.LocalMode && Meta.IsDate(Table, binder.Name))
			{
				value = ((DateTime)value).ToString("yyyy-MM-dd");
			}
			else if (!Database.LocalMode && Meta.IsDateTime(Table, binder.Name))
			{
				value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
			}
		}
		if (Dictionary.ContainsKey(binder.Name))
		{
			Dictionary[binder.Name] = value;
		}
		else
		{
			Dictionary.Add(binder.Name, value);
		}
		return true;
	}

	public override string ToString()
	{
		string result = Table + " [ ";
		int processed = 0;
		foreach (KeyValuePair<string, object> item in Dictionary)
		{
			result += item.Key;
			processed++;
			if (processed < Dictionary.Count)
			{
				result += ", ";
			}
		}
		return result + " ]";
	}
}
