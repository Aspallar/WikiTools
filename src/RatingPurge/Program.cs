using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using WikiaClientLibrary;

namespace RatingPurge
{
    class Program
    {
        static readonly Regex commentRegex = new Regex(@"Rating for (.*) \((\d)\)");
        
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            string userName = GetUsername(options);
            string password = GetPassword(options);
            using (WikiaClient client = new WikiaClient(options.Site, UserAgent))
            {
                if (!client.Login(userName, password))
                {
                    WriteError("That is an invalid logon.");
                    return;
                }
                try
                {
                    var ratingsPage = new RatingPage(client, options.RatingsPage);
                    ratingsPage.Open();
                    int purgeCount = PurgeUsersVotes(options, ratingsPage.Votes, options.UserName);
                    if (purgeCount > 0)
                        ratingsPage.Save($"Purged ratings by [{options.UserName}]");
                    else
                        Console.WriteLine($"No votes found for user {options.UserName}");
                }
                catch (WikiaEditConflictException)
                {
                    WriteError("The ratings page was edited during purge. Purge aborted.");
                }
                catch (WikiaEditException ex)
                {
                    WriteError("The purge was aborted due to wiki edit error: " + ex.Message);
                }
                catch (WikiaUnknownResponseException)
                {
                    WriteError("Something went wrong. The wiki sent an unknown respose to the edit request. Please check the ratings page on the wiki.");
                }
            }
        }

        private static int PurgeUsersVotes(Options options, List<VoteTotal> voteTotals, string userName)
        {
            var ratings = new XmlDocument();
            string rvstartid = null;
            int purgeCount = 0;

            do
            {
                ratings.Load(GetRatingsUrl(options.Site, rvstartid, options.Days));
                var revs = ratings.SelectNodes("/api/query/pages/page/revisions/rev");
                foreach (XmlNode rev in revs)
                {
                    Vote vote = GetVote(rev);
                    if (vote != null && vote.User == options.UserName)
                    {
                        Console.WriteLine($"Undoing [{vote.Score}] [{vote.DeckName}] [{vote.Timestamp.ToWikiTimestamp()}]");
                        VoteTotal deckTotals = voteTotals.Where(x => x.Name == vote.DeckName).FirstOrDefault();
                        if (deckTotals != null)
                        {
                            deckTotals.Total -= vote.Score;
                            --deckTotals.Votes;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: no deck entry found for {vote.DeckName}");
                        }
                        ++purgeCount;
                    }
                }
                rvstartid = GetContinueId(ratings);
            } while (rvstartid != null);
            return purgeCount;
        }

        private static Vote GetVote(XmlNode rev)
        {
            string comment = rev.Attributes["comment"].Value;
            Match commentMatch = commentRegex.Match(comment);
            if (!commentMatch.Success)
                return null;
            return new Vote
            {
                Score = int.Parse(commentMatch.Groups[2].Value),
                DeckName = commentMatch.Groups[1].Value,
                User = rev.Attributes["user"].Value,
                Timestamp = DateTimeOffset.Parse(rev.Attributes["timestamp"].Value),
                RevId = rev.Attributes["revid"].Value
            };
        }

        private static string GetRatingsUrl(string site, string rvstartid, int days)
        {
            string ratingsUrl = site + "/api.php?action=query&prop=revisions&titles=Ratings:DeckRatings&rvprop=ids|timestamp|user|comment&rvlimit=500&format=xml&&rvend=";
            ratingsUrl += GetRvend(days);
            if (!string.IsNullOrEmpty(rvstartid))
                ratingsUrl += "&rvstartid=" + rvstartid;
            ratingsUrl += "&random=" + DateTime.Now.Ticks.ToString();
            return ratingsUrl;
        }

        private static string GetContinueId(XmlDocument ratings)
        {
            var queryContinue = ratings.SelectNodes("/api/query-continue/revisions");
            if (queryContinue.Count == 0)
                return null;
            return queryContinue[0].Attributes["rvstartid"].Value;
        }

        private static string GetRvend(int days)
        {
            if (days < 0)
                return "2018-03-26T19:10:23Z"; // when ratings went live

            DateTimeOffset startOfToday = new DateTimeOffset(DateTime.UtcNow.Date, new TimeSpan(0));
            DateTimeOffset wantedStart = startOfToday.AddDays(-days);
            return wantedStart.ToWikiTimestamp();
        }

        private static string GetPassword(Options options)
        {
            if (!string.IsNullOrEmpty(options.Password))
                return options.Password;
            Console.Write("Password: ");
            string password = ReadPasswordFromConsole();
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

        public static string ReadPasswordFromConsole()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
            }  while (key.Key != ConsoleKey.Enter);
            return password.ToString();
        }

        private static void WriteError(string errorMessage)
        {
            string[] robin = {
                "bouncing bunnies", "pulsating penguins", "kinky kangaroo",
                "rampaging ferocidon", "kippers", "deviant dinos"
            };
            Random rand = new Random();
            string batman = $"Holy {robin[rand.Next(robin.Length)]} Batman!";
            Console.Error.WriteLine(batman);
            Console.Error.WriteLine(errorMessage);
        }

        private static string UserAgent
        {
            get
            {
                Version ver = Assembly.GetExecutingAssembly().GetName().Version;
                return $"RatingsPurge/{ver.Major}.{ver.Minor}.{ver.Build} (Contact: Admin at http://magicarena.wikia.com/wiki/Special:ListUsers/sysop)";
            }
        }
    }
}
