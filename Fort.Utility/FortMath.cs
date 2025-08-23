namespace Fort.Utility;

public static class FortMath
{
	// Conversion constants for degrees <-> radians
	public const float Deg2Rad = MathF.PI / 180f;
	public const float Rad2Deg = 180f / MathF.PI;

	public static float ToDegrees(float radians) => radians * Rad2Deg;

	public static float ToRadians(float degrees) => degrees * Deg2Rad;

	public static float NormalizeDegrees(float degrees)
	{
		degrees %= 360f;
		if (degrees < 0)
			degrees += 360f;
		return degrees;
	}

	public static float Lerp(float a, float b, float t) => a + (b - a) * t;

	public static float InverseLerp(float a, float b, float value)
	{
		return (value - a) / (b - a);
	}

	public static float Clamp(float value, float min, float max)
	{
		return MathF.Min(MathF.Max(value, min), max);
	}

	public static float Clamp01(float value) => Clamp(value, 0f, 1f);

	public static float Repeat(float value, float length)
	{
		return value - MathF.Floor(value / length) * length;
	}

	public static float PingPong(float t, float length)
	{
		t = Repeat(t, length * 2f);
		return length - MathF.Abs(t - length);
	}

	public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (MathF.Abs(target - current) <= maxDelta)
			return target;
		return current + MathF.Sign(target - current) * maxDelta;
	}

	public static float DeltaAngle(float current, float target)
	{
		float delta = Repeat((target - current), 360f);
		if (delta > 180f) delta -= 360f;
		return delta;
	}

	public static float MoveTowardsAngle(float current, float target, float maxDelta)
	{
		float delta = DeltaAngle(current, target);
		if (-maxDelta < delta && delta < maxDelta)
			return target;
		target = current + delta;
		return MoveTowards(current, target, maxDelta);
	}

	public static float LerpAngle(float a, float b, float t)
	{
		a = (a % 360 + 360) % 360;
		b = (b % 360 + 360) % 360;

		float diff = b - a;
		if (diff > 180)
			diff -= 360;
		else if (diff < -180)
			diff += 360;

		return a + diff * t;
	}
}
