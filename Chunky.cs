using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static billions.Consts;

namespace billions
{
    public static class Chunky
    {
        public static Stats Stats = new Stats();

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
            while (fs.ReadByte() != NewLine)
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

                while (seek != -1 && seek != NewLine)
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
                long lines = Chunk(start, end);
                Interlocked.Add(ref TotalLines, lines);
                DebugMsg("thread done {0} - {1} : {2}", start, end, lines);

            })
            {
                Priority = ThreadPriority.AboveNormal,
                //IsBackground = false,
            };
            thread.Start();
            return thread;
        }




        public static int ChunkyFilling(long fileStart, long fileEnd)
        {
            const long BUFFER_SIZE = 200_000;

            var stationList = Stats.NewThread();

            using var fs = File.OpenHandle(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.RandomAccess);

            //todo 
            //var buffer = new byte[BUFFER_SIZE];
            int lines = 0;
            long pending = fileEnd - fileStart;

            var lastBuffer = ArrayPool<byte>.Shared.Rent(Math.Min((int)BUFFER_SIZE, (int)(fileEnd - fileStart)));
            FileSegment lastSegment = new FileSegment(lastBuffer);
            var read = RandomAccess.Read(fs, lastBuffer, fileStart);
            pending -= read;

            var sequence = new ReadOnlySequence<byte>(lastBuffer);
            var reader = new SequenceReader<byte>(sequence);

            while (true)
            {
                while (reader.TryReadTo(out ReadOnlySpan<byte> line, NewLine, advancePastDelimiter: true))
                {

                    lines++;
                    NewStation(in line, in stationList);
                }

                if (pending <= 0)
                {
                    break;
                }

                // todo : optimise
                // if (pending < smallstacksize)
                byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min((int)BUFFER_SIZE, (int)pending));
                var sread = RandomAccess.Read(fs, buffer, fileEnd - pending);
                FileSegment newSegment;

                if (sread > pending)
                {
                    sread = (int)pending;
                    newSegment = new FileSegment(((Memory<byte>)buffer).Slice(0, (int)pending), lastSegment);
                }
                else
                {
                    newSegment = new FileSegment(buffer, lastSegment);
                }

                pending -= sread;

                // todo : free mem?
                sequence = new ReadOnlySequence<byte>(
                    lastSegment,
                    reader.CurrentSpanIndex,
                    newSegment,
                    sread);

                reader = new SequenceReader<byte>(sequence);
                ArrayPool<byte>.Shared.Return(lastBuffer);
                lastBuffer = buffer;
                lastSegment = newSegment;
            }


            //Debug.Assert(pending == 0);
            //Debug.Assert(reader.Length == lines);
            return lines;
        }

        public static int Chunk(long fileStart, long fileEnd)
        {
            try
            {
                return ChunkyFilling(fileStart, fileEnd);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error in Chunk: {fileStart} - {fileEnd}", ex);
            }
        }

        [Conditional("DEBUG")]
        private static void AddDebugInfo(long start, long end)
        {
            Console.WriteLine($"Chunk: {start}, End: {end}");
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewStation(in ReadOnlySpan<byte> line, in ThreadStats stats)
        {
            var seperatorIndex = line.IndexOf(Semicolon);

            var nameSpan = line.Slice(0, seperatorIndex);
            var valueSpan = line.Slice(seperatorIndex + 1);

            stats.Add(nameSpan,valueSpan);
        }
    }
}


public class FileSegment : ReadOnlySequenceSegment<byte>
{
    public FileSegment(ReadOnlyMemory<byte> memory, FileSegment? previous = null)
    {
        if (previous != null)
        {
            previous.Next = this;
            RunningIndex = previous.RunningIndex + previous.Memory.Length;
        }

        Memory = memory;
    }
}