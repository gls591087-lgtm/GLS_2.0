using System;

namespace KartRider.IO;

public class SettingReadException : Exception
{
	public SettingReadException(string key)
		: base($"Failed to read setting '{key}'.")
	{
	}
}
