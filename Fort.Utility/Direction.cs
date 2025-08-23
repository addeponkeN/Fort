namespace Fort.Utility;

[Flags]
public enum Direction
{
	NONE = 0,
	UP = 1 << 0,
	DOWN = 1 << 1,
	LEFT = 1 << 2,
	RIGHT = 1 << 3,
	UP_LEFT = UP | LEFT,
	UP_RIGHT = UP | RIGHT,
	DOWN_LEFT = DOWN | LEFT,
	DOWN_RIGHT = DOWN | RIGHT,
}

public static class DirectionHelper
{
	public static Direction Opposite(this Direction d)
	{
		switch (d)
		{
			case Direction.UP: return Direction.DOWN;
			case Direction.DOWN: return Direction.UP;
			case Direction.LEFT: return Direction.RIGHT;
			case Direction.RIGHT: return Direction.LEFT;
		}

		return Direction.NONE;
	}

	public static bool IsOppositeDirectionOf(this Direction d, Direction check)
	{
		return d.Opposite() == check;
	}

	public static bool IsHorizontal(this Direction d) => d == Direction.LEFT || d == Direction.RIGHT;
	public static bool IsVertical(this Direction d) => d == Direction.UP || d == Direction.DOWN;
}