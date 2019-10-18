using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanRatings
{
    internal class Options
    {
        [Option(Default = "https://magicarena.fandom.com", HelpText = "The site to query")]
        public string Site { get; set; }

        [Option(Default = "Ratings:DeckRatings", HelpText = "name of page containing ratings data.")]
        public string RatingsPage { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited).")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited).")]
        public string Password { get; set; }

        [Option(HelpText = "Update the ratings.")]
        public bool Update { get; set; }
    }
}
