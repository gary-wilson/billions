using System.Diagnostics;
using System.Text;

namespace billions;

internal sealed class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Started");
        List<TimeSpan> times = new();
        for (var i = 0; i < 5; i++)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            FileChunker.Start("C:\\temp\\measurements-1000000000.txt", 20);
            ////Console.WriteLine(coordinator.Output);
            stopWatch.Stop();
            Console.WriteLine(stopWatch.ElapsedMilliseconds + "ms");
            times.Add(stopWatch.Elapsed);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1000);
        }
    }
}
