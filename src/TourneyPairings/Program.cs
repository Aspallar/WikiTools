using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            try
            {
                Console.OutputEncoding = encoding;
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (Exception ex)
            when (ex is FileNotFoundException 
                    || ex is InvalidInputFileFormat
                    || ex is InvalidConfig
                    || ex is InvalidEncoding
                    || ex is InvalidNameMap)
            {
                Console.Error.WriteLine(ex.Message);
            }
#if !DEBUG
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
#endif
        }

        private static void Run(Options options)
        {
            if (options.Scores)
                Scores(options);
            else
                Pairings(options);
        }

        private static void Scores(Options options)
        {
            IEnumerable<string> lines = File.ReadAllLines(options.InputFileName, options.InputEncoding).Skip(1);
            Config config = Config.LoadFromFile(options.Config, options.InputFileName);
            NameMap nameMap = NameMap.LoadFromFile(AppDomain.CurrentDomain.BaseDirectory + "namemap.txt", encoding);
            var warnings = new List<string>();
            var gameRegex = new Regex(@"^\[\s*(\d+)\]\s+(.*)", RegexOptions.Singleline);
            string[] scoreSpliter = { ") - " };
            foreach (string line in lines)
            {
                Match gameMatch = gameRegex.Match(line);
                if (!gameMatch.Success)
                    throw new InvalidInputFileFormat(line);
                string gameNumber = gameMatch.Groups[1].Value;
                string rest = gameMatch.Groups[2].Value;
                Console.WriteLine(gameNumber);
                Console.WriteLine(rest);
                string[] players = rest.Split(scoreSpliter, StringSplitOptions.None);
                players[0] += ")";
                Console.WriteLine(players[0]);
                Console.WriteLine(players[1]);
                int pos = players[0].LastIndexOf(' ');
                string name = players[0].Substring(0, pos).Trim();
                string score = players[0].Substring(pos + 1);
                Console.WriteLine(name);
                Console.WriteLine(score);
            }
        }

        private static void Pairings(Options options)
        {
            IEnumerable<string> lines = File.ReadAllLines(options.InputFileName, options.InputEncoding).Skip(1);
            Config config = Config.LoadFromFile(options.Config, options.InputFileName);
            NameMap nameMap = NameMap.LoadFromFile(AppDomain.CurrentDomain.BaseDirectory + "namemap.txt", encoding);
            var warnings = new List<string>();
            var pairingRegex = new Regex(@"^\[\s*(\d+)\](.*) - (.+)", RegexOptions.Singleline);
            foreach (string line in lines)
            {
                Match match = pairingRegex.Match(line);
                if (!match.Success)
                    throw new InvalidInputFileFormat(line);
                Game game = new Game(match.Groups);

                string deckname1, deckname2;
                if (!nameMap.GetDeckName(game.Player1, out deckname1))
                    warnings.Add($"No name map for {game.Player1}.");
                if (!nameMap.GetDeckName(game.Player2, out deckname2))
                    warnings.Add($"No name map for {game.Player2}.");

                if (!string.IsNullOrEmpty(config.SeparatorLine))
                    Console.WriteLine(config.SeparatorLine);
                Console.WriteLine(config.LineFormat
                    .Replace("%n", game.Number)
                    .Replace("%1", Link(game.Player1, deckname1))
                    .Replace("%2", Link(game.Player2, deckname2))
                );
            }
            if (!options.NoWarnings)
                WriteWarnings(warnings);
        }

        private static void WriteWarnings(List<string> errors)
        {
            foreach (string error in errors)
                Console.Error.WriteLine($"WARNING {error}");
        }

        private static string Link(string name, string deckname) => $"[[Undercity Coliseum {deckname}|{name}]]";

    }
}
