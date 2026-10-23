using System.Collections;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace KartRider.Data;

public class Datums : IEnumerable<Datum>, IEnumerable
{
	private List<Datum> Values { get; set; }

	private string Table { get; set; }

	public Datums(string table = null)
	{
		Table = table;
	}

	internal void PopulateInternal(string fields, string constraints)
	{
		Values = new List<Datum>();
		if (Database.LocalMode)
		{
			string[] fieldList = (fields == null) ? null : fields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (Dictionary<string, object> row in Database.LocalSelectMany(Table, fieldList, constraints))
			{
				Values.Add(new Datum(Table, row));
			}
			return;
		}
		string query = string.Format("SELECT {0} FROM {1}{2}", (fields == null) ? "*" : Database.CorrectFields(fields), Table, (constraints != null) ? (" WHERE " + constraints) : string.Empty);
		using MySqlDataReader reader = MySqlHelper.ExecuteReader(Database.ConnectionString, query);
		while (reader.Read())
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			for (int i = 0; i < reader.FieldCount; i++)
			{
				dictionary.Add(reader.GetName(i), reader.GetValue(i));
			}
			Values.Add(new Datum(Table, dictionary));
		}
	}

	internal void PopulateInternalFree(string query)
	{
		Values = new List<Datum>();
		if (Database.LocalMode)
		{
			foreach (Dictionary<string, object> row in Database.LocalSelectManyFromQuery(query))
			{
				Values.Add(new Datum(Table, row));
			}
			return;
		}
		using MySqlDataReader reader = MySqlHelper.ExecuteReader(Database.ConnectionString, query);
		while (reader.Read())
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			for (int i = 0; i < reader.FieldCount; i++)
			{
				dictionary.Add(reader.GetName(i), reader.GetValue(i));
			}
			Values.Add(new Datum(Table, dictionary));
		}
	}

	public dynamic Populate()
	{
		PopulateInternal(null, null);
		return this;
	}

	public dynamic Populate(string constraints, params object[] args)
	{
		PopulateInternal(null, string.Format(constraints, args));
		return this;
	}

	public dynamic PopulateWith(string fields)
	{
		PopulateInternal(fields, null);
		return this;
	}

	public dynamic PopulateWith(string fields, string constraints, params object[] args)
	{
		PopulateInternal(fields, string.Format(constraints, args));
		return this;
	}

	public dynamic PopulateFree(string query)
	{
		PopulateInternalFree(query);
		return this;
	}

	public IEnumerator<Datum> GetEnumerator()
	{
		foreach (Datum value in Values)
		{
			yield return value;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
