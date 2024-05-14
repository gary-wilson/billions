using System.Diagnostics;

namespace billions;

public static class Consts
{
    public static byte Semicolon = (byte)';';
    public static byte NewLine = (byte)'\n';

    public static Stopwatch Stopwatch = new();

    [Conditional("DEBUG")]
    public static void DebugMsg(string msg, params object?[]? param)
    {
        if (Stopwatch.IsRunning)
        {
            msg += $" [{Stopwatch.ElapsedMilliseconds}ms]";
        }
        Console.WriteLine(msg, param);
    }
}


