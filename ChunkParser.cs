using System.Text;
using static billions.Consts;
namespace billions
{
    public class Stats
    {
        public List<ThreadStats> StationLists = new();

        public ThreadStats NewThread()
        {
            var list = new ThreadStats();
            StationLists.Add(list);
            return list;
        }

        public string GenerateOutput()
        {
            DebugMsg("Starting GenerateOutput with {0} station lists", StationLists.Count);
            var dict = StationLists.SelectMany(x=>x.Stats.Values)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (min: x.Min(y => y.Min), sum: x.Sum(y => y.Sum), count: x.Sum(y => y.Count), max: x.Max(y => y.Max)));

            var sb = new StringBuilder(50000);
            foreach (var kvp in dict)
            {
                sb.AppendFormat("{0}={1:0.0}/{2:0.0}/{3:0.0},", kvp.Key, kvp.Value.min, kvp.Value.sum / kvp.Value.count, kvp.Value.max);
            }
            DebugMsg("Done GenerateOutput");
            return sb.ToString();
        }
    }
}
