using CommandLine;

namespace FetchAllCardBrowsingPages
{
    internal class Options
    {
        private string _site;

        [Option(Default = "http://magicarena.fandom.com/", HelpText = "Url to site.")]
        public string Site
        {
            get
            {
                return _site;
            }
            set
            {
                _site = value;
                if (!_site.EndsWith("/"))
                    _site += "/";
            }
        }

        [Option(HelpText = "Just list titles")]
        public bool List { get; set; }
        
        [Option(HelpText = "Category", Default = "Card Browsing")]
        public string Category { get; set; }

        [Option(HelpText = "Show start of response when fetching pages")]
        public bool Verbose { get; set; }

        [Option(HelpText = "Regex filter", Default = "^Cards/")]
        public string Filter { get; set; }
    }
}