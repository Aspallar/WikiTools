using CommandLine;

namespace GetRatings
{
    internal class Options
    {
        [Option(HelpText = "Output the raw contents of ratings page")]
        public bool Raw { get; set; }

        [Option(HelpText = "Output JSON instead of Csv")]
        public bool Json { get; set; }

        [Option(Default = "https://magicarena.fandom.com", HelpText = "Fandom site")]
        public string Site { get; set; }

        [Option(Default = "Ratings:DeckRatings", HelpText = "name of page containing ratings data")]
        public string RatingsPage { get; set; }
    }
}