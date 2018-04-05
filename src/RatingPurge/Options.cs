using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace RatingPurge
{
    internal class Options
    {
        [Value(0, HelpText = "Username to purge from votes", Required = true)]
        public string UserName { get; set; }

        [Option(Default = "http://magicarena.wikia.com", HelpText = "Wikia site.")]
        public string Site { get; set; }

        [Option(Default = "Ratings:DeckRatings", HelpText = "name of page containing ratings data.")]
        public string RatingsPage { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }

        [Option(Default = -1, HelpText = "Number of days to go back.")]
        public int Days { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example(
                    "Example 1 (purge votes by 101.82.34.5)",
                    new Options
                    {
                        UserName = "101.82.34.5"
                    }
                );
                yield return new Example(
                    "Example 2 (purge votes by HolyCrap WOTF)",
                    new Options
                    {
                        UserName = "HolyCrap WOTF",
                        User = "Aspallar",
                        Password = "mypassword"
                    }
                );
            }
        }
    }
}
