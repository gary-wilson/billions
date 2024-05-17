using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using static billions.Consts;
namespace billions
{
    public class Stats
    {
        public List<ThreadStats> StationLists = new(Chunky.Threads);

        public ThreadStats NewThread()
        {
            var list = new ThreadStats();
            StationLists.Add(list);
            return list;
        }

        public string GenerateOutput()
        {
            DebugMsg("Starting GenerateOutput with {0} station lists", StationLists.Count);

            var dict =
                StationLists.SelectMany(x => x.Stats)
                .GroupBy(x => x.Value.Name)
                .ToImmutableSortedDictionary(
                    x => x.Key, 
                    x => (
                        min: x.Min(y => y.Value.Min), 
                        sum: x.Sum(y => y.Value.Sum), 
                        count: x.Sum(y => y.Value.Count), 
                        max: x.Max(y => y.Value.Max))
                    );

            DebugStation("Addis Ababa");


            var sb = new StringBuilder(50000);
            sb.Append("{");
            foreach (var kvp in dict)
            {
                sb.AppendFormat("{0}={1:0.0}/{2:0.0}/{3:0.0}, ", kvp.Key, kvp.Value.min, kvp.Value.sum / kvp.Value.count, kvp.Value.max);
            }
            sb.Append("}");
            DebugMsg("Done GenerateOutput");
            return sb.ToString();
        }

        [Conditional("DEBUG")]
        private void DebugStation(string stationName)
        {
            var lists = StationLists.SelectMany(x=>x.Stats.Values).Where(x=>x.Name == stationName).ToList();
            var min = lists.Min(x => x.Min);    
            var max = lists.Max(x => x.Max);
            var sum = lists.Sum(x => x.Sum);
            var count = lists.Sum(x => x.Count);
        }

        public void WriteNames(string file)
        {
            var sb = new StringBuilder(50000);
            foreach (var item in StationLists.SelectMany(x => x.Stats).Select(x => x.Value.Name).OrderBy(x => x.Length))
            {
                sb.AppendLine(item);
            }

            File.WriteAllText(file, sb.ToString());
        }
    }
}
