using CommandLine;

namespace UploadFiles
{
    internal class Options
    {
        [Value(0, HelpText = "Files to upload e.g. images\\*.png")]
        public string FileNames { get; set; }

        // TODO: supply correct default htttp://magicarena.wikia.com
        [Option(Default = "http://aspallar.wikia.com", HelpText = "Wikia site")]
        public string Site { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }
    }
}
