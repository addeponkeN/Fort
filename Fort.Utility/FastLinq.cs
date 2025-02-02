namespace Fort.Utility;

public static class FastExtensions
{
    public static T LastFast<T>(this IList<T> list)
    {
        return list[^1];
    }

    public static T LastFast<T>(this T[] arr)
    {
        return arr[^1];
    }

    public static T FirstFast<T>(this IList<T> list)
    {
        return list[0];
    }

    public static T FirstFast<T>(this T[] arr)
    {
        return arr[0];
    }

}
