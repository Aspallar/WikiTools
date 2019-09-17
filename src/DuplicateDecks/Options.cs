using CommandLine;

namespace DuplicateDecks
{
    internal class Options
    {
        [Option("no-sideboard")]
        public bool NoSideboard { get; set; }

        [Option]
        public bool Html { get; set; }

        [Option]
        public string Title { get; set; }
    }
}
