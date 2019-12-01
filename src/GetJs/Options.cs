using CommandLine;

namespace GetJs
{
    internal class Options
    {
        [Option(Default = "https://dev.fandom.com", HelpText = "Site to download javascript files from.")]
        public string Site { get; set; }

        [Option(Required = true, HelpText = "Folder to save javascript to.")]
        public string Folder { get; set; }

        [Option(Default = 1000, HelpText = "Delay in milliseconds between requests")]
        public int Delay { get; internal set; }
    }
}
