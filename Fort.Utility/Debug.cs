namespace Fort.Utility;

public static class Debug
{
    public static bool Enable = true;
    public static bool ShowDate = true;

    //  todo (?)
    // #if DEBUG
    // #else
    // public static bool Enable = false;
    // #endif

    static string GetDate() => DateTime.Now.ToString("yy/MM/dd H:mm");
    static string GetStamp => ShowDate ? $"[{GetDate()}] " : "[] ";

    /// <summary>
    /// ERROR
    /// </summary>
    public static void LogE(string msg)
    {
        if (!Enable) return;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{GetStamp} {msg}");
        Console.ResetColor();
    }

    /// <summary>
    /// WARNING
    /// </summary>
    public static void LogW(string msg)
    {
        if (!Enable) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{GetStamp} {msg}");
        Console.ResetColor();
    }

    /// <summary>
    /// DEBUG
    /// </summary>
    public static void Log(string msg)
    {
        if (!Enable) return;

        Console.WriteLine($"{GetStamp} {msg}");
    }

    public static void LogI(string msg)
    {
        if (!Enable) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{GetStamp} {msg}");
        Console.ResetColor();
    }
}