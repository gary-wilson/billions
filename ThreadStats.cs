using System.Text;

namespace billions
{

    public struct StationStats
    {
        public string Name;

        public int Count;

        public int Min;

        public int Max;

        public decimal Sum;

    }

    public sealed class ThreadStats
    {
        public readonly Dictionary<int, StationStats> Stats = new(10000);

        public void Add(in ReadOnlySpan<byte> nameSpan, in ReadOnlySpan<byte> valueSpan)
        {
            if (Stats.Count > 1000)
            {
                // todo .. 
            }

            System.Buffers.Text.Utf8Parser.TryParse(valueSpan, out decimal value, out _);
            var id = FastId(nameSpan);
            if (Stats.TryGetValue(id, out var stats))
            {
                stats.Count++;
                stats.Min = Math.Min(stats.Min, (int)value);
                stats.Max = Math.Max(stats.Max, (int)value);
                stats.Sum += value;
            }
            else
            {
                var name = Encoding.UTF8.GetString(nameSpan);
                Stats.Add(FastId(nameSpan), new StationStats
                {
                    Name = name,
                    Count = 1,
                    Min = (int)value,
                    Max = (int)value,
                    Sum = value
                });
            }
        }

        private static int FastId(in ReadOnlySpan<byte> span)
        {
            unchecked
            {
                // generate a unique id for the station name
                int accum = span.Length * 10000000;
                for (int i = 1; i < span.Length; i++)
                {
                    accum += span[i-1] * (i * 10);
                }

                return accum;
            }
        }
    }
}
