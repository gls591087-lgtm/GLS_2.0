using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;

namespace KartRider.Game.Data;

public class BmlObject
{
	public string Name { get; set; }

	public string Value { get; set; }

	public Dictionary<string, string> Values { get; private set; }

	public List<Tuple<string, BmlObject>> SubObjects { get; private set; }

	public BmlObject()
	{
		Name = string.Empty;
		Value = string.Empty;
		Values = new Dictionary<string, string>();
		SubObjects = new List<Tuple<string, BmlObject>>();
	}

	public BmlObject(InPacket p)
		: this()
	{
		Name = p.ReadString();
		Value = p.ReadString();
		int count = p.ReadInt();
		for (int i = 0; i < count; i++)
		{
			string name = p.ReadString();
			string value = p.ReadString();
			Values.Add(name, value);
		}
		int count2 = p.ReadInt();
		for (int j = 0; j < count2; j++)
		{
			BmlObject obj = new BmlObject(p);
			SubObjects.Add(new Tuple<string, BmlObject>(obj.Name, obj));
		}
	}

    public static BmlObject Create(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception("Unable to locate object file.");
        }

        byte[] contents = File.ReadAllBytes(path);
        InPacket packet = new InPacket(contents);

        return new BmlObject(packet);
    }

    public void Save(OutPacket p)
	{
		p.WriteString(Name);
		p.WriteString(Value);
		p.WriteInt(Values.Count);
		foreach (KeyValuePair<string, string> value in Values)
		{
			p.WriteString(value.Key);
			p.WriteString(value.Value);
		}
		p.WriteInt(SubObjects.Count);
		foreach (Tuple<string, BmlObject> subObject in SubObjects)
		{
			subObject.Item2.Save(p);
		}
	}

    public BmlObject GetObject(string name)
    {
        foreach (Tuple<string, BmlObject> subObject in SubObjects)
        {
            if (subObject.Item1 == name)
            {
                return subObject.Item2;
            }
        }

        return new BmlObject();
    }

    public string GetString(string key, string def = "")
	{
		if (Values.ContainsKey(key))
		{
			return Values[key];
		}
		return def;
	}

	public bool GetBool(string key, bool def = false)
	{
		return GetByte(key, (byte)(def ? 1u : 0u)) == 1;
	}

	public byte GetByte(string key, byte def = 0)
	{
		string value = GetString(key, def.ToString());
		return byte.Parse(value);
	}

	public short GetShort(string key, int def = 0)
	{
		string value = GetString(key, def.ToString());
		return short.Parse(value);
	}

	public ushort GetUShort(string key, int def = 0)
	{
		string value = GetString(key, def.ToString());
		return ushort.Parse(value);
	}

	public int GetInt(string key, int def = 0)
	{
		string value = GetString(key, def.ToString());
		return int.Parse(value);
	}

	public uint GetUInt(string key, int def = 0)
	{
		string value = GetString(key, def.ToString());
		return uint.Parse(value);
	}

	public float GetFloat(string key, float def = 0f)
	{
		string value = GetString(key, def.ToString());
		return float.Parse(value);
	}
}
