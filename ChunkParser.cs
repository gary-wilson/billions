using System.Text;

namespace billions
{
    public class StationStats
    {
        public List<List<Station>> StationLists = new();

        public List<Station> NewThread()
        {
            var list = new List<Station>();
            StationLists.Add(list);
            return list;
        }

        public string GenerateOutput()
        {
            var dict = new Dictionary<string, (int count, decimal min, decimal max, decimal sum)>();

            //dict = StationLists.SelectMany(x => x).Aggregate(dict, (acc, station) =>
            //{
            //    if (!acc.ContainsKey(station.Name.Value))
            //    {
            //        acc[station.Name.Value] = (1, station.Value.Value, station.Value.Value, station.Value.Value);
            //    }
            //    else
            //    {
            //        var (count, min, max, sum) = acc[station.Name.Value];
            //        acc[station.Name.Value] = (count + 1, Math.Min(min, station.Value.Value), Math.Max(max, station.Value.Value), sum + station.Value.Value);
            //    }

            //    return acc;
            //});


            var sb = new StringBuilder(50000);
            foreach (var kvp in dict)
            {
                sb.AppendFormat("{0}={1:0.0}/{2:0.0}/{3:0.0},", kvp.Key, kvp.Value.min, kvp.Value.sum / kvp.Value.count, kvp.Value.max);
            }

            return sb.ToString();
        }
    }
}
