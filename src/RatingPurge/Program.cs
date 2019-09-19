using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using WikiaClientLibrary;
using WikiToolsShared;

namespace RatingPurge
{
    class Program
    {
        //static readonly Regex commentRegex = new Regex(@"Rating for (?:\[\[.*\|)?([^\]]+)(?:\]\])? \((\d)\)");
        
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
                Utils.WriteError($"An unexpected error occurred. You should report this at {issuesUrl}");
                Console.Error.WriteLine($"Holy gobbledygook Batman! Include this in the issue.\n{ex.ToString()}\n");
                Utils.Pause("Press any key to raise issue.");
                Process.Start(issuesUrl);
            }
#endif
        }

        private static void Run(Options options)
        {
            try
            {
                Utils.InitializeTls();
                options.Validate();
                if (options.Show)
                    ShowPurges(options);
                else
                    LoginAndPurge(options);
            }
            catch (OptionsException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private static void LoginAndPurge(Options options)
        {
            string userName = GetUsername(options);
            string password = GetPassword(options);
            using (WikiaClient client = new WikiaClient(options.Site, UserAgent))
            {
                if (client.Login(userName, password))
                    Purge(options, client);
                else
                    Utils.WriteError("That is an invalid logon.");
            }
        }

        private static void ShowPurges(Options options)
        {
            var ratings = new RatingsHistory(options.Site, options.RatingsPage, options.Days);
            foreach (XmlNode revision in ratings.Items)
            {
                string comment = revision.Attributes["comment"].Value;
                if (!RatingsEntry.IsEntry(comment))
                    Console.WriteLine($"{revision.Attributes["timestamp"].Value} {comment}");
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
                Utils.WriteError($"I cannot find the ratings page [{options.RatingsPage}]");
            }
            catch (WikiaEditConflictException)
            {
                Utils.WriteError("The ratings page was edited during purge. Purge aborted.");
            }
            catch (WikiaEditException ex)
            {
                Utils.WriteError("The purge was aborted due to wiki edit error: " + ex.Message);
            }
            catch (WikiaUnknownResponseException)
            {
                Utils.WriteError("Something went wrong. The wiki sent an unknown respose to the edit request. Please check the ratings page on the wiki.");
                Utils.Pause("Press any key to review ratings page");
                Process.Start(options.Site + "/wiki/" + options.RatingsPage);
            }
            catch (BadVoteTotalException ex)
            {
                Utils.WriteError(ex.Message);
            }
            catch (JsonException ex)
            {
                Utils.WriteError($"The ratings pages does not contain valid JSON.\nUnable to purge.\nYou will have to manually correct this.\n{ex.Message}");
            }
            catch (WebException ex)
            {
                Utils.WriteError($"There has bee a network error\n{ex.Message}");
            }
        }

        private static void SaveRatings(Options options, RatingPage ratingsPage)
        {
            ratingsPage.Save(EditSummary(options));
        }

        private static string EditSummary(Options options)
        {
            string summary = $"Purged ratings by {UserWikiText(options.PurgeUserName)}";
            if (options.Count >= 0)
                summary += $" Count={options.Count}";
            if (options.Days >= 0)
                summary += $" Days={options.Days}";
            if (!string.IsNullOrEmpty(options.Comment))
                summary += $" ({options.Comment})";
            return summary;
        }

        private static string UserWikiText(string usernameOrIp)
        {
            string text;
            if (Uri.CheckHostName(usernameOrIp) == UriHostNameType.IPv6 ||
                    Regex.IsMatch(usernameOrIp, @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                text = $"[[Special:Contributions/{usernameOrIp}|{usernameOrIp}]]";
            else
                text = $"[[User:{usernameOrIp}|{usernameOrIp}]]";
            return text;
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
                    UndoVote(voteTotals, vote, options.Force);
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
        
        private static void UndoVote(List<VoteTotal> voteTotals, Vote vote, bool force)
        {
            Console.WriteLine($"Undoing [{vote.Score}] [{vote.DeckName}] [{vote.Timestamp.ToWikiTimestamp()}]");
            VoteTotal deckTotals = voteTotals.Where(x => x.Name == vote.DeckName).FirstOrDefault();
            if (deckTotals != null)
            {
                deckTotals.Total -= vote.Score;
                if (--deckTotals.Votes == 0)
                {
                    if (deckTotals.Votes != 0)
                    {
                        string msg = $"Error: Votes reached 0 but total was {deckTotals.Total} for deck [{deckTotals.Name}]";
                        if (!force)
                            throw new BadVoteTotalException(msg);
                        Console.WriteLine(msg);
                    }
                    voteTotals.Remove(deckTotals);
                    Console.WriteLine($"Info: entry for [{deckTotals.Name}] removed because votes reduced to 0.");
                }
                else
                {
                    if (deckTotals.Total <= 0)
                    {
                        string msg = $"Error: Total reached {deckTotals.Total} but votes was {deckTotals.Votes} for deck [{deckTotals.Name}]";
                        if (!force)
                            throw new BadVoteTotalException(msg);
                        Console.WriteLine($"Error: entry for [{deckTotals.Name}] removed because votes reduced to less than 0.");
                        voteTotals.Remove(deckTotals);
                    }
                    else if (deckTotals.Total < deckTotals.Votes || deckTotals.Total > deckTotals.Votes * 5)
                    {
                        string msg = $"Error: Invalid totals for deck [{deckTotals.Name}] total={deckTotals.Total} votes={deckTotals.Votes}";
                        if (!force)
                            throw new BadVoteTotalException(msg);
                        Console.WriteLine(msg);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Warning: no deck entry found for {vote.DeckName}");
            }
        }

        private static Vote GetVote(XmlNode rev)
        {
            var rating = new RatingsEntry(rev.Attributes["comment"].Value);
            if (rating.IsValid)
            {
                return new Vote
                {
                    Score = int.Parse(rating.Vote),
                    DeckName = rating.DeckName,
                    User = rev.Attributes["user"].Value,
                    Timestamp = DateTimeOffset.Parse(rev.Attributes["timestamp"].Value),
                    RevId = rev.Attributes["revid"].Value
                };
            }
            else return null;
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

        private static string UserAgent
        {
            get
            {
                Version ver = Assembly.GetExecutingAssembly().GetName().Version;
                return $"RatingsPurge/{ver.Major}.{ver.Minor}.{ver.Build} (Contact: Admin at http://magicarena.fandom.com/wiki/Special:ListUsers/sysop)";
            }
        }
    }
}
