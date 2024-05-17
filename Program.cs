using System.Diagnostics;

namespace billions;

internal sealed class Program
{
    const int runs = 5;
    static void Main(string[] args)
    {
        Console.WriteLine("Started");
        List<TimeSpan> times = new();
        for (var i = 0; i < runs; i++)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Chunky.FilePath = "C:\\temp\\measurements-1000000000.txt";
            Consts.Stopwatch.Start();
            Chunky.Threads = 20;
            Chunky.Start();
            var result = Chunky.Stats.GenerateOutput();
            stopWatch.Stop();
            if (i==0)
            {
                if (false)
                {
                    Chunky.Stats.WriteNames("C:\\temp\\measurements-1000000000.names");
                }

                File.WriteAllText("C:\\temp\\measurements-1000000000.res", result);
            }

            Console.WriteLine(stopWatch.ElapsedMilliseconds + "ms");
            times.Add(stopWatch.Elapsed);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1000);
        }

        var sorted = times.OrderBy(x => x.Ticks).ToList();
        var avg = sorted.Skip(1).Take(runs - 2).Average(x => x.TotalSeconds);
        Console.WriteLine($"Score: {avg:F} Seconds");
    }
}
