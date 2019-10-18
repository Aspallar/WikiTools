using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using WikiaClientLibrary;
using WikiToolsShared;

namespace CleanRatings
{
    class Program
    {
        static void Main(string[] args)
        {
            Utils.InitializeTls();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            HashSet<string> deckNames = GetDeckNames();
            using (WikiaClient client = new WikiaClient(options.Site, Utils.UserAgent()))
            {
                if (options.Update)
                {
                    string userName = GetUsername(options);
                    string password = GetPassword(options);
                    if (!client.Login(userName, password))
                    {
                        Console.WriteLine("Invalid credentials");
                        return;
                    }
                }
                RatingPage ratingPage = new RatingPage(client, options.RatingsPage);
                ratingPage.Open();
                var missingDecks = ratingPage.Votes.Where(x => !deckNames.Contains(x.Name)).ToList();
                if (missingDecks.Count > 0)
                {
                    foreach (var vote in missingDecks)
                        Console.WriteLine($"{vote.Name} {vote.Total}/{vote.Votes}");
                    if (options.Update)
                    {
                        ratingPage.Votes.RemoveAll(x => !deckNames.Contains(x.Name));
                        ratingPage.Save($"Removed entries for decks that no longer exist.");
                    }
                }
                else
                {
                    Console.WriteLine("No missing decks found.");
                }
            }
        }

        private static HashSet<string> GetDeckNames()
        {
            var deckNames = new HashSet<string>();
            var apfrom = "";
            var decks = new XmlDocument();
            var deckContents = new XmlDocument();
            var url = ApiQuery(new Dictionary<string, string>
            {
                { "list", "allpages" },
                { "apprefix", "Decks/" },
                { "aplimit", "500" },
                { "apfrom", ""},
            });
            do
            {
                GetXmlResponse(url + apfrom, decks);
                TerminateOnErrorOrWarning(decks, "Error while obtaining list of decks");
                apfrom = decks.SelectSingleNode("/api/query-continue/allpages")?.Attributes["apfrom"]?.Value;
                foreach (XmlNode node in decks.SelectNodes("/api/query/allpages/p"))
                    deckNames.Add(node.Attributes["title"].Value.Substring(6));
            } while (!string.IsNullOrEmpty(apfrom));
            return deckNames;
        }

        private static string ApiQuery(Dictionary<string, string> queryParameters = null)
        {
            var url = new StringBuilder("https://magicarena.fandom.com/").Append("api.php?action=query&format=xml");
            if (queryParameters != null)
            {
                foreach (var entry in queryParameters)
                    url.Append('&').Append(entry.Key).Append('=').Append(entry.Value);
            }
            return url.ToString(); ;
        }

        private static void GetXmlResponse(string url, XmlDocument response)
        {
            response.Load(url);
        }

        private static string GetPassword(Options options)
        {
            if (!string.IsNullOrEmpty(options.Password))
                return options.Password;
            Console.Write("Password: ");
            string password = Utils.ReadPasswordFromConsole();
            Console.WriteLine();
            return password;
        }

        private static string GetUsername(Options options)
        {
            if (!string.IsNullOrEmpty(options.User))
                return options.User;
            Console.Write("Username: ");
            string userName = Console.ReadLine();
            return userName;
        }

        private static void TerminateOnErrorOrWarning(XmlDocument response, string message)
        {
            XmlNode error = response.SelectSingleNode("/api/error");
            if (error != null)
            {
                Console.Error.WriteLine(message);
                Console.Error.WriteLine(error.Attributes["info"].Value);
                Environment.Exit(1);
            }
            XmlNode warnings = response.SelectSingleNode("/api/warnings");
            if (warnings != null)
            {
                Console.Error.WriteLine(message);
                foreach (XmlNode warning in warnings.ChildNodes)
                    Console.WriteLine(warning.InnerText);
                Environment.Exit(1);
            }
        }
    }
}
