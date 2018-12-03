using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.IO;
using System.Text.RegularExpressions;

namespace TourneyPairings
{
    class Program
    {
        static readonly Encoding encoding = Encoding.UTF8;

        static void Main(string[] args)
        {
            Console.OutputEncoding = encoding;
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            var errors = new List<string>();
            var pairingRegex = new Regex(@"^\[\s*(\d+)\](.*) - (.+)", RegexOptions.Singleline);
            Dictionary<string, string> nameMap = GetNameMap();
            IEnumerable<string> lines = File.ReadAllLines(options.InputFileName, encoding).Skip(1);
            foreach (string line in lines)
            {
                Match match = pairingRegex.Match(line);
                if (match.Success)
                {
                    string number = match.Groups[1].Value.Trim();
                    string player1 = match.Groups[2].Value.Trim();
                    string player2 = match.Groups[3].Value.Trim();
                    string deckname1, deckname2;
                    if (!nameMap.TryGetValue(player1, out deckname1))
                    {
                        errors.Add($"No name map for {player1}, player name used for deck name");
                        deckname1 = player1;
                    }
                    if (!nameMap.TryGetValue(player2, out deckname2))
                    {
                        errors.Add($"No name map for {player2}, player name used for deck name");
                        deckname2 = player2;
                    }
                    Console.WriteLine("|-");
                    Console.WriteLine($"| {number} || {Link(player1, deckname1)} || - || {Link(player2, deckname2)}");
                }
                else
                {
                    errors.Add($"Invalid format: {line}");
                }
            }
            foreach (string error in errors)
                Console.Error.WriteLine(error);
        }

        private static Dictionary<string, string> GetNameMap()
        {
            var map = new Dictionary<string, string>();
            string filename = AppDomain.CurrentDomain.BaseDirectory + "namemap.txt";
            if (File.Exists(filename))
            {
                string[] lines = File.ReadAllLines(filename, encoding);
                foreach (string line in lines)
                {
                    string[] split = line.Split('|');
                    map.Add(split[1], split[0]);
                }
            }
            return map;
        }

        private static string Link(string name, string deckname) => $"[[Undercity Coliseum {deckname}|{name}]]";

    }
}
