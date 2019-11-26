using CommandLine;
using System.Collections.Generic;

namespace DuplicateDecks
{
    internal class Options
    {
        [Option("no-sideboard", HelpText = "Only use the main deck when comparing decks.")]
        public bool NoSideboard { get; set; }

        [Option(HelpText = "Output results in html.")]
        public bool Html { get; set; }

        [Option(HelpText = "Only check if the specified title is a duplicate.")]
        public string Title { get; set; }

        [Option(HelpText = "Merge the sideboard into the main deck before comparing decks.")]
        public bool Merged { get; set; }

        [Option(HelpText = "List of local deck list files to include in duplicate check.")]
        public IEnumerable<string> Files { get; set; }

        [Option(HelpText = "Also check deck in clipboard.")]
        public bool Clipboard { get; set; }
    }
}
