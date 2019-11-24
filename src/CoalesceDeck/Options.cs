using CommandLine;

namespace CoalesceDeck
{
    class Options
    {
        [Value(0, MetaName = "Deck Title")]
        public string Title { get; set; }

        [Option]
        public string User { get; set; }

        [Option]
        public string Password { get; set; }
    }
}
