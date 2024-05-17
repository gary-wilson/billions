using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace billions
{

    public class StationStats
    {
        public string Name;

        public int Count;

        public decimal Min;

        public decimal Max;

        public decimal Sum;

    }

    public sealed class ThreadStats
    {
        public readonly Dictionary<int, StationStats> Stats = new(600);

        public void Add(in ReadOnlySpan<byte> nameSpan, in ReadOnlySpan<byte> valueSpan)
        {
            if (!System.Buffers.Text.Utf8Parser.TryParse(valueSpan, out decimal value, out _))
            {
                throw new ApplicationException();
            }

            var id = FastId(nameSpan);

            if (Stats.TryGetValue(id, out var stats))
            {
                stats.Count++;
                stats.Min = Math.Min(stats.Min, value);
                stats.Max = Math.Max(stats.Max, value);
                stats.Sum += value;
            }
            else
            {
                var name = Encoding.UTF8.GetString(nameSpan);
                Stats.Add(id, new StationStats
                {
                    Name = name,
                    Count = 1,
                    Min = value,
                    Max = value,
                    Sum = value,
                });
            }
        }

        public static int FastId(in ReadOnlySpan<byte> span)
        {
            HashCode h = default;
            h.AddBytes(span);
            return h.ToHashCode();
        }
    }
}
