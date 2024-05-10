using System.IO.MemoryMappedFiles;

namespace billions
{
    public static class FileChunker
    {
        public unsafe static void Start(string file, int threads)
        {
            using var fs = new FileStream(file,
                new FileStreamOptions()
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.SequentialScan,
                    Share = FileShare.ReadWrite,
                    BufferSize = 1,
                });

            long batchSize = fs.Length / threads;
            long totalLines = 0;
            long skip = 0;

            Thread[] threadArray = new Thread[threads];
            for (int t = 0; t < threads; t++)
            {
                fs.Position = (t * batchSize) + skip;
                int next = fs.ReadByte();
                while (next != -1 && next != 10)
                {
                    // read till eof or newline
                    skip++;
                    next = fs.ReadByte();
                }

                threadArray[t] = new Thread(() =>
                {
                    var lines = ProcessChunk(file, fs.Position, fs.Position + batchSize);
                    Interlocked.Add(ref totalLines, lines);
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

        private static unsafe int ProcessChunk(string file, long position, long end)
        {
            const long BUFFER_SIZE = 600_000;

            var fs = File.OpenHandle(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.SequentialScan);

            //todo 
            var buffer = new byte[BUFFER_SIZE];

            int lines = 0;
            while (position < end)
            {
                var read = RandomAccess.Read(fs, buffer, position);
                if (read == 0)
                {
                    //eof
                    break;
                }
                var slice = buffer.AsSpan(0, read);

                position += read;
                lines += slice.Count((byte)10);
            }

            return lines;
        }
    }
}
