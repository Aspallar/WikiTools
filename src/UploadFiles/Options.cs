using CommandLine;

namespace UploadFiles
{
    internal class Options
    {
        [Value(0, HelpText = "Files to upload e.g. images\\*.png")]
        public string FilePattern { get; set; }

        // TODO: supply correct default htttp://magicarena.wikia.com
        [Option(Default = "http://aspallar.wikia.com", HelpText = "Wikia site")]
        public string Site { get; set; }

        [Option(HelpText = "Filename of a file containg a list of images to upload, 1 per line. Overrides the file pattern parameter.")]
        public string List { get; set; }

        [Option(HelpText = "File uploads that had a warning will be output to this file.")]
        public string Fails { get; set; }

        [Option(HelpText = "Always upload even if file already exists or is a duplicate")]
        public bool Force { get; set; }

        [Option(HelpText = "Prompt for upload if file already exist or is a duplicate")]
        public bool Prompt { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }
    }
}
