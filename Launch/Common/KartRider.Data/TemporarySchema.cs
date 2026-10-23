using System;

namespace KartRider.Data;

public class TemporarySchema : IDisposable
{
	private string oldSchema;

	internal TemporarySchema(string schema)
	{
		oldSchema = Database.Schema;
		Database.Schema = schema;
	}

	public void Dispose()
	{
		Database.Schema = oldSchema;
	}
}
