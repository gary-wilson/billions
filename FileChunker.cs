using System.IO.MemoryMappedFiles;

namespace billions
{
    public static class FileChunker
    {
        public unsafe static void Start(string file, int threads)
        {
            using var fs = File.OpenRead(file);
            var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            long batchSize = fs.Length / threads;
            long totalLines = 0;
            long totalBatches = (fs.Length / batchSize); // calculate total batches
            Console.WriteLine($"Total batches: {totalBatches}");
            Thread[] threadArray = new Thread[threads];
            for (int t = 0; t < threads; t++)
            {
                threadArray[t] = new Thread(() =>
                {
                    for (long batch = t; batch < totalBatches; batch += threads)
                    {
                        long offset = batch * batchSize;
                        long size = Math.Min(batchSize, fs.Length - offset);
                        int lines = 0;

                        using MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
                        byte* p = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref p);

                        try
                        {
                            for (int i = 0; i < size; i++)
                            {
                                if (p[i] == 10)
                                {
                                    lines++;
                                }
                            }
                        }
                        finally
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }

                        Interlocked.Add(ref totalLines, lines);
                    }
                })
                {
                    Priority = ThreadPriority.AboveNormal,
                    //IsBackground = false,
                };
                threadArray[t].Start();
            }

            for (int t = 0; t < threads; t++)
            {
                threadArray[t].Join();
            }

            Console.WriteLine($"Lines: {totalLines}");
        }
    }
}
