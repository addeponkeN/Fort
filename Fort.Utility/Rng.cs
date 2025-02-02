//using Microsoft.Xna.Framework;

namespace Fort.Utility;

public static class Rng
{
    private static Random _rnd;
    private static Random rnd => _rnd ??= new Random();

    public static int Next(int min, int max) => rnd.Next(min, max + 1);
    public static int Next(int max) => rnd.Next(max + 1);

    public static double NextDouble()
    {
        return rnd.NextDouble();
    }

    public static double NextDouble(double min, double max)
    {
        return rnd.NextDouble() + rnd.Next((int)min, (int)max + 1);
    }

    public static double NextDouble(double max)
    {
        return rnd.NextDouble() + rnd.Next(0, (int)max + 1);
    }

    public static float NextFloat()
    {
        return (float)rnd.NextDouble();
    }

    public static float NextFloat(float max)
    {
        return NextFloat(0f, max);
    }

    public static float NextFloat(float min, float max)
    {
        return (float)rnd.NextDouble() * (max - min) + min;
    }

    public static string Choose(params string[] entries) => entries[Next(entries.Length - 1)];
    public static int Choose(params int[] entries) => entries[Next(entries.Length - 1)];
    public static float Choose(params float[] entries) => entries[Next(entries.Length - 1)];
    public static T Choose<T>(params T[] entries) => entries[Next(entries.Length - 1)];

    public static int Range(int range) => Next(-range, range);
    public static float Range(float range) => NextFloat(-range, range);

    public static bool NextBool => Next(1) == 1 ? false : true;

    public static bool Roll(int chance) => chance >= Next(100);

    public static int RandomSeed => Rng.Next(int.MaxValue - 1);

    public static T Random<T>(this T[] items)
    {
        int length = items.Length;
        if (length < 1)
            return default!;
        return items.ElementAt(Next(length - 1));
    }

    public static T Random<T>(this IList<T> items)
    {
        int length = items.Count;
        if (length < 1)
            return default!;
        return items.ElementAt(Next(length - 1));
    }

    public static int RandomAlpha => Rng.Next(255);


	//public static Vector2 RngDirection => Vector2.Normalize(new Vector2(Rng.NextFloat(-1f, 1f), Rng.NextFloat(-1, 1f)));

	//public static Color RandomColor()
	//{
	//    return new Color(Rng.Next(255), Rng.Next(255), Rng.Next(255));
	//}

	//public static Vector2 PositionInRadius(float radius, Vector2 position)
	//{
	//    return new Vector2(
	//        position.X + MathF.Sin(Rng.NextFloat(MathF.PI * 2f)) * radius,
	//        position.Y + MathF.Cos(Rng.NextFloat(MathF.PI * 2f)) * radius);
	//}

	//public static Vector2 PositionOnCircle(float radius, Vector2 position)
	//{
	//    return new Vector2(
	//        position.X + MathF.Sin(Rng.NextFloat(MathF.PI * 2)) * radius,
	//        position.Y + MathF.Cos(Rng.NextFloat(MathF.PI * 2)) * radius);
	//}
}

public class OboRandom
{
    public int Seed { get; set; }

    public Random rnd;

    public OboRandom() : this(Rng.RandomSeed) { }
    public OboRandom(int seed)
    {
        Seed = seed;
        rnd = new Random(seed);
    }

    public int Next(int max) => Next(0, max);

    public int Next(int min, int max)
    {
        return rnd.Next(min, max + 1);
    }

    public int Choose(params int[] n) => n[Next(0, n.Length - 1)];

    public float NextFloat(float min, float max)
    {
        return (float)rnd.NextDouble() * (max - min) + min;
    }

    public float NextFloat(float max)
    {
        return NextFloat(0f, max);
    }
    public float NextFloat()
    {
        return NextFloat(0f, 1f);
    }

    public bool Roll(float chance) => chance >= NextFloat(100);

    public bool NextBool => Next(1) == 1 ? false : true;

    public int RandomSeed => Next(int.MaxValue - 1);

    public T RandomItem<T>(IEnumerable<T> list)
    {
        var c = list.Count();
        if (c == 0)
            return default!;
        return list.ElementAt(Next(c - 1));
    }

    public T RandomPopItem<T>(List<T> list)
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



