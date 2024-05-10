using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace billions
{
    public static class FileChunker
    {
        public static string Output = "";

        public static string FilePath;

        public static long TotalLines;

        public unsafe static void Start(int threads)
        {
            TotalLines = 0;
            using var fs = new FileStream(FilePath,
                new FileStreamOptions()
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.SequentialScan,
                    Share = FileShare.ReadWrite,
                    BufferSize = 1,
                });
            fs.Position = 0;

            long batchSize = fs.Length / threads;

            fs.Position = batchSize;
            while (fs.ReadByte() != 10)
            {
            }
            AddDebugInfo(0, fs.Position);
            List<Thread> threadArray = new List<Thread>(threads);
            long end = fs.Position;
            threadArray.Add(StartThread(0, end));

            for (int t = 1; t < threads - 1; t++)
            {
                long start = fs.Position;
                fs.Position = start + batchSize;
                int seek = fs.ReadByte();

                while (seek != -1 && seek != 10)
                {
                    // read till eof or newline
                    seek = fs.ReadByte();
                }

                AddDebugInfo(start, fs.Position);
                threadArray.Add(StartThread(start, fs.Position));
            }

            AddDebugInfo(fs.Position, fs.Length);
            threadArray.Add(StartThread(fs.Position, fs.Length));

            for (int t = 0; t < threads; t++)
            {
                threadArray[t].Join();
            }

            Console.WriteLine($"Lines: {TotalLines}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Thread StartThread(long start, long end)
        {
            var thread = new Thread(() =>
            {
                long lines = ProcessChunk(start, end);
                Interlocked.Add(ref TotalLines, lines);
            })
            {
                Priority = ThreadPriority.AboveNormal,
                //IsBackground = false,
            };
            thread.Start();
            return thread;
        }

        private static unsafe int ProcessChunk(long position, long end)
        {
            const long BUFFER_SIZE = 600_000;

            //using var fs = File.Open(FilePath, new FileStreamOptions
            //{
            //    Options = FileOptions.SequentialScan,
            //    Access = FileAccess.Read,
            //    Mode = FileMode.Open,
            //    Share = FileShare.Read,
            //    BufferSize = (int)BUFFER_SIZE,
            //});
            //fs.Position = position;

            using var fs = File.OpenHandle(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.RandomAccess);

            //todo 
            var buffer = new byte[BUFFER_SIZE];
            int lines = 0;
            fixed (byte* bufferPtr = buffer)
            {
                
                while (position < end)
                {
                    //var read = RandomAccess.Read(fs, buffer, position);
                    var read = RandomAccess.Read(fs, buffer.AsSpan(0, (int)Math.Min(BUFFER_SIZE, end - position)), position);
                    //var read = fs.Read(buffer.AsSpan(0, (int)Math.Min(BUFFER_SIZE, end - position)));
                    if (read == 0)
                    {
                        //eof
                        break;
                    }
                    

                    //var slice = buffer.AsSpan(0, read);

                    SequenceReader<byte> reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer,0,read));  

                    position += read;

                    while (reader.TryReadTo(out ReadOnlySpan<byte> line, (byte)10, advancePastDelimiter: true))
                    {
                        lines++;
                    }
                }
            }

            return lines;
        }

        [Conditional("DEBUG")]
        private static void AddDebugInfo(long start, long end)
        {
            Console.WriteLine($"Chunk: {start}, End: {end}");
        }
    }


    public struct Station
    {
        public LazyString Name;

        public decimal Value;
    }

    public struct LazyString : IEqualityComparer<LazyString>
    {
        public LazyString(byte[] value)
        {
            _value = value;
        }

        public string Value => Encoding.UTF8.GetString(_value);

        private readonly byte[] _value;

        public bool Equals(LazyString x, LazyString y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] LazyString obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
