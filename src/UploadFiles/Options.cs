using CommandLine;

namespace UploadFiles
{
    internal class Options
    {
        [Value(0, HelpText = "Files to upload e.g. images\\*.png")]
        public string FilePattern { get; set; }

        [Option(HelpText = "Wikia site. Defaults to setting in config file.")]
        public string Site { get; set; }

        [Option(HelpText = "Filename of file containing text to use for initial page contents")]
        public string Content { get; set; }

        [Option(HelpText = "Comment for upload log")]
        public string Comment { get; set; }

        [Option(HelpText = "Don't include any page content at all, overrides --content")]
        public bool NoContent { get; set; }

        [Option(HelpText = "Category to add to each upload")]
        public string Category { get; set; }

        [Option(HelpText = "Filename of a file containing a list of images to upload, 1 per line. Overrides the file pattern parameter.")]
        public string List { get; set; }

        [Option(HelpText = "File uploads that had a warning will be output to this file.")]
        public string Fails { get; set; }
        
        [Option(HelpText = "Always upload even if file already exists or is a duplicate")]
        public bool Force { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }
    }
}
