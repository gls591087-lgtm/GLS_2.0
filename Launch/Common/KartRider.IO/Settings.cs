using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using KartRider.Data;

namespace KartRider.IO;

public static class Settings
{
	private const string Comment = "#";

	private static Dictionary<string, string> Dictionary;

	public static string Path { get; private set; }

	public static void Initialize(string path = null)
	{
		if (path == null)
		{
			path = "Configuration.ini";
		}
		if (!System.IO.Path.IsPathRooted(path))
		{
			path = System.IO.Path.Combine(AppContext.BaseDirectory, path);
		}
		if (Dictionary != null)
		{
			Dictionary.Clear();
		}
		Path = path;
		Dictionary = new Dictionary<string, string>();
		string[] array = Path.Split('\\');
		string name = array[array.Length - 1];
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Unable to find configuration file '{name}'.");
		}
		string currentSection = string.Empty;
		using (StreamReader file = new StreamReader(path))
		{
			string line;
			while ((line = file.ReadLine()) != null)
			{
				if (!line.StartsWith("#"))
				{
					if (line.StartsWith("[") && line.EndsWith("]"))
					{
						currentSection = line.Trim('[', ']');
					}
					else if (line.Contains("="))
					{
						Dictionary.Add(string.Format("{0}{1}{2}", currentSection, (currentSection != string.Empty) ? "/" : string.Empty, line.Split('=')[0]), line.Split('=')[1].Split(';')[0]);
					}
				}
			}
		}
		Database.Host = GetString("Database/Host");
		Database.Schema = GetString("Database/Schema");
		Database.Username = GetString("Database/Nickname");
		Database.Password = GetString("Database/Password");
	}

	public static int GetInt(string key, params object[] args)
	{
		try
		{
			return int.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static uint GetUInt(string key, params object[] args)
	{
		try
		{
			return uint.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static short GetShort(string key, params object[] args)
	{
		try
		{
			return short.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static ushort GetUShort(string key, params object[] args)
	{
		try
		{
			return ushort.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static byte GetByte(string key, params object[] args)
	{
		try
		{
			return byte.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static sbyte GetSByte(string key, params object[] args)
	{
		try
		{
			return sbyte.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static bool GetBool(string key, params object[] args)
	{
		try
		{
			return bool.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static string GetString(string key, params object[] args)
	{
		try
		{
			return Dictionary[string.Format(key, args)];
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static IPAddress GetIPAddress(string key, params object[] args)
	{
		try
		{
			if (Dictionary[string.Format(key, args)] == "localhost")
			{
				return IPAddress.Loopback;
			}
			return IPAddress.Parse(Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}

	public static T GetEnum<T>(string key, params object[] args)
	{
		try
		{
			return (T)Enum.Parse(typeof(T), Dictionary[string.Format(key, args)]);
		}
		catch
		{
			throw new SettingReadException(key);
		}
	}
}
