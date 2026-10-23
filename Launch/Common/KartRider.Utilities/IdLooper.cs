using System;

namespace KartRider.Utilities;

public sealed class IdLooper
{
	public int Minimum { get; private set; }

	public int Maximum { get; private set; }

	public int Current { get; private set; }

	public IdLooper(int minimum = 1, int maximum = 100000)
	{
		if (maximum <= minimum)
		{
			throw new ArgumentException("Maximum must be greater than minimum.");
		}
		Minimum = minimum;
		Maximum = maximum;
		Current = minimum;
	}

	public int Next()
	{
		int ret = Current;
		if (Current == Maximum)
		{
			Reset();
		}
		else
		{
			Current++;
		}
		return ret;
	}

	public void Reset()
	{
		Current = Minimum;
	}
}
