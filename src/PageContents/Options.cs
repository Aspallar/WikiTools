using CommandLine;

namespace PageContents
{
    internal class Options
    {
        [Option(Default = "https://magicarena.fandom.com", HelpText = "The site to download from.")]
        public string Site { get; set; }

        [Option(Default = "wiki", HelpText = "The wiki path")]
        public string WikiPath { get; set; }

        [Value(0, Required = true, MetaName = "PageName")]
        public string Page { get; set; }
    }
}