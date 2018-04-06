using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace RatingPurge
{
    internal class Options
    {
        [Value(0, HelpText = "Username to purge from votes", Required = true)]
        public string PurgeUserName { get; set; }

        [Option(Default = "http://magicarena.wikia.com", HelpText = "Wikia site.")]
        public string Site { get; set; }

        [Option(Default = "Ratings:DeckRatings", HelpText = "name of page containing ratings data.")]
        public string RatingsPage { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }

        [Option(Default = -1, HelpText = "Number of days to go back (defaults to all vote history).")]
        public int Days { get; set; }

        [Option(Default = -1, HelpText = "Number of votes to undo. (defaults to all votes)")]
        public int Count { get; set; }
        
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example(
                    "Example 1 (purge votes by 101.82.34.5)",
                    new Options
                    {
                        PurgeUserName = "101.82.34.5"
                    }
                );
                yield return new Example(
                    "Example 2 (purge votes by HolyCrap WOTF, with credentials supplied)",
                    new Options
                    {
                        PurgeUserName = "HolyCrap WOTF",
                        User = "Aspallar",
                        Password = "mypassword"
                    }
                );
                yield return new Example(
                    "Example 3 (purge last 3 votes by Aspallar)",
                    new Options
                    {
                        PurgeUserName = "Aspallar",
                        Count = 3
                    }
                );
            }
        }
    }
}
