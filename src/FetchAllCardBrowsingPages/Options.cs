using CommandLine;

namespace FetchAllCardBrowsingPages
{
    internal class Options
    {
        private string _site;

        [Option(Default = "http://magicarena.wikia.com/", HelpText = "Url to site.")]
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

        [Option(HelpText = "Purge pages when fetching")]
        public bool Purge { get; set; }
    }
}