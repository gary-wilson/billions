using System.Diagnostics;

namespace billions;

internal sealed class Program
{
    static void Main(string[] args)
    {

        Stopwatch sw = Stopwatch.StartNew();
        Console.WriteLine("Started");

        FileChunker.Start("C:\\temp\\measurements-1000000000.txt", 20);
        sw.Stop();
        Console.WriteLine($"Finished in {sw.ElapsedMilliseconds}ms");
    }
}
