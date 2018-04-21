using CommandLine;
using System.IO;
using System;

namespace UploadFiles
{
    internal class Options
    {
        const string help = "(use --help to see available parameters)";

        private string _site;

        [Value(0, HelpText = "File pattern for files to upload e.g. images\\*.png")]
        public string FilePattern { get; set; }

        [Option(HelpText = "Wikia site. Defaults to setting in config file.")]
        public string Site
        {
            get { return _site ?? Properties.Settings.Default.DefaultSite; }
            set { _site = value; }
        }

        [Option(HelpText = "Filename of file containing text to use for initial page contents")]
        public string Content { get; set; }

        [Option(HelpText = "Comment for upload log")]
        public string Comment { get; set; }

        [Option(HelpText = "Don't use default content specified in config file.")]
        public bool NoContent { get; set; }

        [Option(HelpText = "Category to add to each upload")]
        public string Category { get; set; }

        [Option(HelpText = "Filename of a file containing a list of images to upload, 1 per line.")]
        public string List { get; set; }

        [Option(HelpText = "File uploads that had a warning will be output to this file.")]
        public string Fails { get; set; }
        
        [Option(HelpText = "Always upload even if file already exists or is a duplicate")]
        public bool Force { get; set; }

        [Option(HelpText = "Username (you will be prompted for this if ommited)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if ommited)")]
        public string Password { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(FilePattern) && string.IsNullOrEmpty(List))
                throw new UploadFilesFatalException($"No files to upload. Specify a file pattern or use the --list switch. {help}");

            if (!string.IsNullOrEmpty(FilePattern) && !string.IsNullOrEmpty(List))
                throw new UploadFilesFatalException($"Specify a file pattern OR --list, not both. {help}");

            ValidateSite();

            if (!string.IsNullOrEmpty(Content) && NoContent)
                throw new UploadFilesFatalException($"Cannot specify both --nocontent and --content. {help}");

            if (!string.IsNullOrEmpty(List) && !File.Exists(List))
                throw new UploadFilesFatalException($"--list file not found. {help}");

            if (!string.IsNullOrEmpty(Content) && !File.Exists(Content))
                throw new UploadFilesFatalException($"--content file not found. {help}");

            if (!string.IsNullOrEmpty(List) && !string.IsNullOrEmpty(Fails))
            {
                string listPath = Path.GetFullPath(List);
                string failsPath = Path.GetFullPath(Fails);
                if (listPath == failsPath)
                    throw new UploadFilesFatalException($"--fails and --list should not specify the same file. {help}");
            }
        }

        private void ValidateSite()
        {
            if (string.IsNullOrEmpty(Site))
                throw new UploadFilesFatalException($"No site specified. Use --site or edit the .config file to configure a default site. {help}");

            if (!Uri.IsWellFormedUriString(Site, UriKind.Absolute))
                throw new UploadFilesFatalException($"Site \"{Site}\" is not a valid url. An example of a valid site url is http://mywiki.wikia.com");

            if (Site.EndsWith("/"))
                throw new UploadFilesFatalException($"Invalid site {Site}. Don't end the site name with a '/'");

            if (!Site.ToUpperInvariant().StartsWith("HTTP"))
                throw new UploadFilesFatalException($"Site {Site} is invalid. Only http is allowed. e.g http://mywiki.wikia.com");
        }
    }
}
