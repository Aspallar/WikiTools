using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourneyPairings
{
    internal class NameMap : Dictionary<string, string>
    {
        public static NameMap LoadFromFile(string fileName, Encoding encoding)
        {
            NameMap map = new NameMap();
            if (File.Exists(fileName))
            {
                IEnumerable<string> lines = File.ReadAllLines(fileName, encoding)
                    .Select(line => line.Trim())
                    .Where(line => line.Length > 0);
                foreach (string line in lines)
                {
                    string[] split = line.Split('|');
                    if (split.Length != 2)
                        throw new InvalidNameMap(line);
                    map.Add(split[1], split[0]);
                }
            }
            return map;
        }

        public bool GetDeckName(string name, out string deckname)
        {
            bool found = TryGetValue(name, out deckname);
            if (!found)
                deckname = name;
            return found;
        }
    }
}
