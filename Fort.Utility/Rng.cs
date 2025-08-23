namespace Fort.Utility;

public static class Rng
{
	private static FortRandom _rnd;
	private static FortRandom Rnd => _rnd ??= new FortRandom();

	public static int Next() => Rnd.Next();
	public static int Next(int max) => Rnd.Next(max);
	public static int Next(int min, int max) => Rnd.Next(min, max);

	public static double NextDouble() => Rnd.NextDouble();
	public static double NextDouble(double min, double max) => Rnd.NextDouble(min, max);
	public static double NextDouble(double max) => Rnd.NextDouble(max);

	public static float NextFloat() => Rnd.NextFloat();
	public static float NextFloat(float max) => Rnd.NextFloat(max);
	public static float NextFloat(float min, float max) => Rnd.NextFloat(min, max);

	public static string Choose(params string[] entries) => Rnd.Choose(entries);
	public static int Choose(params int[] entries) => Rnd.Choose(entries);
	public static float Choose(params float[] entries) => Rnd.Choose(entries);
	public static T Choose<T>(params T[] entries) => Rnd.Choose(entries);

	public static int Range(int range) => Rnd.Range(range);
	public static float Range(float range) => Rnd.Range(range);

	public static bool Roll(int chance) => Rnd.Roll(chance);

	public static bool NextBool => Rnd.NextBool;
	public static int RandomSeed => Rnd.RandomSeed;
	public static int RandomAlpha => Rnd.RandomAlpha;

	public static T Random<T>(this T[] items) => Rnd.Random(items);
	public static T Random<T>(this IList<T> items) => Rnd.Random(items);
	public static T RandomPop<T>(this IList<T> items) => Rnd.RandomPopItem(items);
}

public class FortRandom
{
	public int Seed { get; private set; }

	public Random BaseRandom;

	public FortRandom() : this(System.Random.Shared.Next()) { }
	public FortRandom(int seed)
	{
		Seed = seed;
		BaseRandom = new Random(seed);
	}

	public int Next() => BaseRandom.Next();
	public int Next(int max) => Next(0, max);
	public int Next(int min, int max) => BaseRandom.Next(min, max + 1);

	public double NextDouble() => BaseRandom.NextDouble();
	public double NextDouble(double min, double max) => BaseRandom.NextDouble() + BaseRandom.Next((int)min, (int)max + 1);
	public double NextDouble(double max) => NextDouble(0, max);

	public float NextFloat() => BaseRandom.NextSingle();
	public float NextFloat(float max) => NextFloat(0f, max);
	public float NextFloat(float min, float max) => BaseRandom.NextSingle() * (max - min) + min;

	public int Range(int range) => BaseRandom.Next(-range, range);
	public float Range(float range) => NextFloat(-range, range);

	public string Choose(params string[] entries) => entries[Next(entries.Length - 1)];
	public int Choose(params int[] entries) => entries[Next(entries.Length - 1)];
	public float Choose(params float[] entries) => entries[Next(entries.Length - 1)];
	public T Choose<T>(params T[] entries) => entries[Next(entries.Length - 1)];

	public bool Roll(float chance) => chance >= NextFloat(100);

	public bool NextBool => Next(1) != 1;
	public int RandomSeed => Next(int.MaxValue - 1);
	public int RandomAlpha => BaseRandom.Next(255);

	public T Random<T>(T[] items)
	{
		int length = items.Length;
		if (length < 1)
			return default!;
		return items[Next(length - 1)];
	}

	public T Random<T>(IList<T> items)
	{
		int length = items.Count;
		if (length < 1)
			return default!;
		return items[Next(length - 1)];
	}

	public T RandomPopItem<T>(IList<T> list)
	{
		var c = list.Count;
		if (c == 0)
			return default!;

		int i = Next(c - 1);
		var item = list[i];
		list.RemoveAt(i);
		return item;
	}
}