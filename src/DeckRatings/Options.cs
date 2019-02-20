using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeckRatings
{
    internal class Options
    {
        [Option(HelpText = "List invalid log entries")]
        public bool Invalid { get; set; }

        [Option(HelpText = "Show votes and invalid entries")]
        public bool Both { get; set; }

        [Option(HelpText = "Show counts")]
        public bool Counts { get; set; }

        [Option(Default = "http://magicarena.fandom.com", HelpText = "The site to query")]
        public string Site { get; set; }

        [Option(Default = -1, HelpText = "Number of days to look back, 0 for today only.")]
        public int Days { get; set; }
    }
}
