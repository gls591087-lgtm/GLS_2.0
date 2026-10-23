using System;

namespace KartRider.IO;

public class LoadingIndicator : IDisposable
{
	public static bool ShowTime { get; set; }

	private DateTime LoadStartTime { get; set; }

	internal LoadingIndicator(string header)
	{
		lock (typeof(Log))
		{
			Console.Write("{0}  {1}... ", Log.Margin, header);
		}
	}

	public void Dispose()
	{
		lock (typeof(Log))
		{
			if (ShowTime)
			{
				Console.WriteLine("({0}ms)", (DateTime.Now - LoadStartTime).Milliseconds);
			}
			else
			{
				Console.WriteLine();
			}
		}
	}
}
