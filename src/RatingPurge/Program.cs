using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WikiaClientLibrary;

namespace RatingPurge
{
    class Program
    {
        static readonly Regex commentRegex = new Regex(@"Rating for (.*) \((\d)\)");
        
        static void Main(string[] args)
        {
#if !DEBUG
            const string issuesUrl = "https://github.com/Aspallar/WikiTools/issues";
            try
            {
#endif
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
#if !DEBUG
            }
            catch(Exception ex)
            {
                WriteError($"An unexpected error occurred. You should report this at {issuesUrl}");
                Console.Error.WriteLine($"Holy gobbledygook Batman! Include this in the issue.\n{ex.ToString()}\n");
                Pause("Press any key to raise issue.");
                Process.Start(issuesUrl);
            }
#endif
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
                Purge(options, client);
            }
        }

        private static void Purge(Options options, WikiaClient client)
        {
            try
            {
                var ratingsPage = new RatingPage(client, options.RatingsPage);
                ratingsPage.Open();
                int purgeCount = PurgeUsersVotes(options, ratingsPage.Votes);
                if (purgeCount > 0)
                    SaveRatings(options, ratingsPage);
                else
                    Console.WriteLine($"No votes found for user {options.PurgeUserName}");
            }
            catch (MissingRatingsPageException)
            {
                WriteError($"I cannot find the ratings page [{options.RatingsPage}]");
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
                Pause("Press any key to review ratings page");
                Process.Start(options.Site + "/wiki/" + options.RatingsPage);
            }
            catch (BadVoteTotalException ex)
            {
                WriteError(ex.Message);
            }
            catch (JsonException ex)
            {
                WriteError($"The ratings pages does not contain valid JSON.\nUnable to purge.\nYou will have to manually correct this.\n{ex.Message}");
            }
        }

        private static void SaveRatings(Options options, RatingPage ratingsPage)
        {
            string summary = $"Purged ratings by [{options.PurgeUserName}]";
            if (options.Count >= 0)
                summary += $" Count={options.Count}";
            if (options.Days >= 0)
                summary += $" Days={options.Days}";
            ratingsPage.Save(summary);
        }

        private static int PurgeUsersVotes(Options options, List<VoteTotal> voteTotals)
        {
            var ratings = new RatingsHistory(options.Site, options.RatingsPage, options.Days);
            int purgeCount = 0;

            foreach (XmlNode revision in ratings.Items)
            {
                Vote vote = GetVote(revision);
                if (vote != null && vote.User == options.PurgeUserName)
                {
                    UndoVote(voteTotals, vote);
                    ++purgeCount;
                    if (options.Count != -1 && purgeCount >= options.Count)
                    {
                        Console.WriteLine("Info: vote count limit reached.");
                        break; // foreach
                    }
                }
            }
            return purgeCount;
        }
        
        private static void UndoVote(List<VoteTotal> voteTotals, Vote vote)
        {
            Console.WriteLine($"Undoing [{vote.Score}] [{vote.DeckName}] [{vote.Timestamp.ToWikiTimestamp()}]");
            VoteTotal deckTotals = voteTotals.Where(x => x.Name == vote.DeckName).FirstOrDefault();
            if (deckTotals != null)
            {
                // Consider: add a force switch to control aborts here
                deckTotals.Total -= vote.Score;
                if (--deckTotals.Votes == 0)
                {
                    if (deckTotals.Votes != 0)
                        throw new BadVoteTotalException($"Error: Votes reached 0 but total was {deckTotals.Total} for deck [{deckTotals.Name}]");
                    voteTotals.Remove(deckTotals);
                    Console.WriteLine($"Info: entry for [{deckTotals.Name}] removed because votes reduced to 0.");
                }
                else
                {
                    if (deckTotals.Total <= 0)
                        throw new BadVoteTotalException($"Error: Total reached {deckTotals.Total} but votes was {deckTotals.Votes} for deck [{deckTotals.Name}]");
                    if (deckTotals.Total < deckTotals.Votes || deckTotals.Total > deckTotals.Votes * 5)
                        throw new BadVoteTotalException($"Error: Invalid totals for deck [{deckTotals.Name}] total={deckTotals.Total} votes={deckTotals.Votes}");
                }
            }
            else
            {
                Console.WriteLine($"Warning: no deck entry found for {vote.DeckName}");
            }
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

        private static void Pause(string message)
        {
            Console.Error.WriteLine(message);
            while (Console.KeyAvailable)
                Console.ReadKey(true);
            Console.ReadKey(true);
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
