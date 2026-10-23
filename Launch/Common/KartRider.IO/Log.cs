using System;
using System.Text;

namespace KartRider.IO;

public static class Log
{
	private const byte LabelWidth = 11;

	private static bool Entitled;

	public static bool Running { get; private set; }

	public static string Margin => new string(' ', 11);

	public static bool ShowStackTrace { get; set; }

	public static string MaskString(string input, char mask = '*')
	{
		return new string(mask, input.Length);
	}

	static Log()
	{
		Entitled = false;
		try
		{
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Clear();
			Running = true;
		}
		catch
		{
			Running = false;
		}
	}

	public static string Input(string label)
	{
		lock (typeof(Log))
		{
			WriteItem("Input", ConsoleColor.Cyan, string.Empty);
			Console.Write(label);
			return Console.ReadLine();
		}
	}

	public static string Input(string label, string defaultValue)
	{
		lock (typeof(Log))
		{
			WriteItem("Input", ConsoleColor.Cyan, string.Empty);
			Console.Write(label);
			string result = Console.ReadLine();
			if (result == string.Empty)
			{
				result = defaultValue;
				Console.CursorTop--;
				Console.CursorLeft = Margin.Length + label.Length;
				Console.WriteLine((defaultValue == string.Empty) ? "(None)" : result);
			}
			return result;
		}
	}

	private static void WriteItem(string label, ConsoleColor labelColor, string value, params object[] args)
	{
		lock (typeof(Log))
		{
			if (!Running)
			{
				return;
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(' ', 11 - label.Length - 3);
			sb.Append("[");
			sb.Append(label);
			sb.Append("]");
			sb.Append(" ");
			label = sb.ToString();
			value = string.Format(value, args);
			Console.ForegroundColor = labelColor;
			Console.Write(label);
			Console.ForegroundColor = ConsoleColor.Gray;
			bool first = true;
			string[] array = value.Split('\n');
			foreach (string s in array)
			{
				string[] lines = new string[(int)Math.Ceiling((float)s.Length / (float)(Console.BufferWidth - 11))];
				for (int j = 0; j < lines.Length; j++)
				{
					if (j == lines.Length - 1)
					{
						lines[j] = s.Substring((Console.BufferWidth - 11) * j);
					}
					else
					{
						lines[j] = s.Substring((Console.BufferWidth - 11) * j, Console.BufferWidth - 11);
					}
				}
				string[] array2 = lines;
				foreach (string line in array2)
				{
					if (!first)
					{
						Console.Write(Margin);
					}
					if (line.Length + 11 < Console.BufferWidth)
					{
						Console.WriteLine(line);
					}
					else if (line.Length + 11 == Console.BufferWidth)
					{
						Console.Write(line);
					}
					first = false;
				}
			}
		}
	}

	public static void SkipLine()
	{
		if (!Running) return;
		Console.WriteLine();
	}

	public static void Entitle(string value, params object[] args)
	{
		if (!Running) return;
		lock (typeof(Log))
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			StringBuilder sb = new StringBuilder();
			sb.Append("\n");
			sb.Append('\t');
			sb.Append((Entitled ? "" : "") + string.Format(value, args) + "\n");
			Console.WriteLine(sb.ToString());
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Title = string.Format(value, args);
			Entitled = true;
		}
	}

	public static void Inform(string value, params object[] args)
	{
		WriteItem("Info", ConsoleColor.White, value, args);
	}

	public static void Inform(object value)
	{
		Inform(value.ToString());
	}

	public static void Warn(string value, params object[] args)
	{
		WriteItem("Warning", ConsoleColor.Yellow, value, args);
	}

	public static void Warn(object value)
	{
		Warn(value.ToString());
	}

	public static void Error(string value, params object[] args)
	{
		WriteItem("Error", ConsoleColor.Red, value, args);
	}

	public static void Error(Exception exception)
	{
		WriteItem("Error", ConsoleColor.Red, ShowStackTrace ? exception.ToString() : exception.Message);
	}

	public static void Error(string label, Exception exception, params object[] args)
	{
		WriteItem("Error", ConsoleColor.Red, "{0}\n{1}", string.Format(label, args), ShowStackTrace ? exception.ToString() : exception.Message);
	}

	public static void Success(string value, params object[] args)
	{
		WriteItem("Success", ConsoleColor.Green, value, args);
	}

	public static void Sql(string value, params object[] args)
	{
		WriteItem("Sql", ConsoleColor.Magenta, value, args);
	}

	public static void Connection(string label, bool connection)
	{
	}

	public static void Hex(string label, byte[] value, params object[] args)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(string.Format(label, args));
		sb.Append('\n');
		if (value == null || value.Length == 0)
		{
			sb.Append("(Empty)");
		}
		else
		{
			int lineSeparation = 0;
			foreach (byte b in value)
			{
				if (lineSeparation == 16)
				{
					sb.Append('\n');
					lineSeparation = 0;
				}
				sb.AppendFormat("{0:X2} ", b);
				lineSeparation++;
			}
		}
		WriteItem("Hex", ConsoleColor.Magenta, sb.ToString());
	}

	public static void Hex(string label, byte b, params object[] args)
	{
		Hex(label, new byte[1] { b }, args);
	}

	public static void Quit()
	{
		Running = false;
		Inform("Press any key to quit . . .");
		Console.Read();
	}

	public static LoadingIndicator Load(string header)
	{
		return new LoadingIndicator(header);
	}
}
