using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static billions.Consts;

namespace billions
{
    public static class Chunky
    {
        public static StationStats Stats = new StationStats();

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
                long lines = Chunk(start, end);
                Interlocked.Add(ref TotalLines, lines);
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

            List<Station> stationList = new(100000);
            var stations = CollectionsMarshal.AsSpan(stationList);

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
                    NewStation(in line,in stations);
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
        public static void NewStation(in ReadOnlySpan<byte> line, in Span<Station> stations)
        {
            var seperatorIndex = line.IndexOf(Semicolon);

            //var nameStr = Encoding.UTF8.GetString(nameSpan);
            //var dec = decimal.Parse(valueSpan);

            var nameSpan = line.Slice(0, seperatorIndex);
            var valueSpan = line.Slice(seperatorIndex + 1);

            stations.Fill(new Station()
            {
                NameBytes = nameSpan.ToArray(),
                ValueBytes = valueSpan.ToArray(),
            });
        }
    }


    public struct Station
    {
        //public LazyString Name;

        //public LazyDecimal Value;

        public Memory<byte> NameBytes { get; set; }

        public Memory<byte> ValueBytes { get; set; }


    }

    public unsafe struct LazyString : IEqualityComparer<LazyString>
    {
        public readonly unsafe void* Pointer;
        public readonly int Length;

        public LazyString(void* pointer, int length)
        {
            Pointer = pointer;
            Length = length;
        }

        public string Value => Encoding.UTF8.GetString(AsSpan());

        public Span<byte> AsSpan() => new Span<byte>(Pointer, Length);

        public bool Equals(LazyString x, LazyString y)
        {
            return MemoryExtensions.SequenceEqual<byte>(x.AsSpan(), y.AsSpan());
            //return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] LazyString obj)
        {
            return obj.Value.GetHashCode();
        }
    }

    public unsafe struct LazyDecimal : IEqualityComparer<LazyDecimal>
    {
        public readonly unsafe void* Pointer;
        public readonly int Length;

        public LazyDecimal(void* pointer, int length)
        {
            Pointer = pointer;
            Length = length;
        }

        public decimal Value => decimal.Parse(Encoding.UTF8.GetString(AsSpan()));

        public Span<byte> AsSpan() => new Span<byte>(Pointer, Length);

        public bool Equals(LazyDecimal x, LazyDecimal y)
        {
            return MemoryExtensions.SequenceEqual<byte>(x.AsSpan(), y.AsSpan());

            //return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] LazyDecimal obj)
        {
            return obj.Value.GetHashCode();
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

    //public FileSegment Append(Memory<byte> memory)
    //{
    //    var nextSegment = new FileSegment(memory)
    //    {
    //        RunningIndex = RunningIndex + Memory.Length
    //    };

    //    Next = nextSegment;

    //    return nextSegment;
    //}
}