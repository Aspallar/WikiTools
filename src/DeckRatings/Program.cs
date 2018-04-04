using CsvHelper;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using CommandLine;

namespace DeckRatings
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
            var votes = new List<Vote>();
            var invalidVotes = new List<InvalidVote>();
            var ratings = new XmlDocument();
            string rvstartid = null;

            do
            {
                ratings.Load(GetRatingsUrl(options.Site, rvstartid, options.Days));
                var revs = ratings.SelectNodes("/api/query/pages/page/revisions/rev");
                foreach (XmlNode rev in revs)
                    GetVote(votes, invalidVotes, rev);
                rvstartid = GetContinueId(ratings);
            } while (rvstartid != null);

            WriteResults(options, votes, invalidVotes);
        }

        private static string GetContinueId(XmlDocument ratings)
        {
            var queryContinue = ratings.SelectNodes("/api/query-continue/revisions");
            if (queryContinue.Count == 0)
                return null;
            return queryContinue[0].Attributes["rvstartid"].Value;
        }

        private static void GetVote(List<Vote> votes, List<InvalidVote> invalidVotes, XmlNode rev)
        {
            string comment = rev.Attributes["comment"].Value;
            Match commentMatch = commentRegex.Match(comment);
            if (commentMatch.Success)
            {
                var vote = new Vote
                {
                    Score = int.Parse(commentMatch.Groups[2].Value),
                    DeckName = commentMatch.Groups[1].Value,
                    User = rev.Attributes["user"].Value,
                    Timestamp = DateTimeOffset.Parse(rev.Attributes["timestamp"].Value),
                    RevId = rev.Attributes["revid"].Value
                };
                votes.Add(vote);
            }
            else
            {
                var invalidVote = new InvalidVote
                {
                    Comment = rev.Attributes["comment"].Value,
                    User = rev.Attributes["user"].Value,
                    Timestamp = DateTimeOffset.Parse(rev.Attributes["timestamp"].Value),
                    RevId = rev.Attributes["revid"].Value
                };
                invalidVotes.Add(invalidVote);
            }
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

        private static string GetRvend(int days)
        {
            if (days < 0)
                return "2018-03-26T19:10:23Z"; // when ratings went live

            DateTimeOffset startOfToday = new DateTimeOffset(DateTime.UtcNow.Date, new TimeSpan(0));
            DateTimeOffset wantedStart = startOfToday.AddDays(-days);
            return wantedStart.ToWikiTimestamp();
        }

        private static void WriteResults(Options options, List<Vote> votes, List<InvalidVote> invalidVotes)
        {
            if (options.Counts)
            {
                Console.WriteLine($"{votes.Count} {invalidVotes.Count}");
            }
            else if (options.Both)
            {
                WriteListToConsole(votes);
                Console.WriteLine();
                WriteListToConsole(invalidVotes);
            }
            else if (options.Invalid)
            {
                WriteListToConsole(invalidVotes);
            }
            else
            {
                WriteListToConsole(votes);
            }
        }

        private static void WriteListToConsole<T>(List<T> list)
        {
            using (CsvWriter csvWriter = new CsvWriter(Console.Out, true))
            {
                csvWriter.WriteHeader<T>();
                csvWriter.NextRecord();
                csvWriter.WriteRecords(list);
            }
        }
    }
}
