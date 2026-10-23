using System;

namespace KartRider.Data;

public class TemporaryConnection : IDisposable
{
	private string oldHost;

	private string oldSchema;

	private string oldUsername;

	private string oldPassword;

	internal TemporaryConnection(string host, string schema, string username, string password)
	{
		oldHost = Database.Host;
		oldSchema = Database.Schema;
		oldUsername = Database.Username;
		oldPassword = Database.Password;
		Database.Host = host;
		Database.Schema = schema;
		Database.Username = username;
		Database.Password = password;
	}

	public void Dispose()
	{
		Database.Host = oldHost;
		Database.Schema = oldSchema;
		Database.Username = oldUsername;
		Database.Password = oldPassword;
	}
}
