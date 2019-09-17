using CommandLine;
using System.IO;
using System;
using WikiToolsShared;

namespace UploadFiles
{
    internal class Options
    {
        const string help = "(use --help to see available parameters)";

        private string _site;
        private int? _waitFiles;
        private int? _waitTime;
        private int? _timeout;
        private int? _delay;
        private string _content;

        [Value(0, HelpText = "File pattern for files to upload e.g. images\\*.png")]
        public string FilePattern { get; set; }

        [Option(HelpText = "Fandom site (e.g. http://mywiki.fandom.com). Defaults to setting in config file.")]
        public string Site
        {
            get { return _site ?? Properties.Settings.Default.DefaultSite; }
            set { _site = value; }
        }

        [Option(HelpText = "Filename of file containing text to use for initial page contents")]
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (Path.IsPathRooted(value) || File.Exists(value))
                {
                    _content = value;
                    return;
                }
                string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "content", value);
                if (File.Exists(appPath))
                {
                    _content = appPath;
                    return;
                }
                string userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "UploadFilesContent", value);
                if (File.Exists(userPath))
                {
                    _content = userPath;
                    return;
                }
                _content = value;
            }
        }

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

        [Option(HelpText = "Username (you will be prompted for this if omitted)")]
        public string User { get; set; }

        [Option(HelpText = "Password (you will be prompted for this if omitted)")]
        public string Password { get; set; }

        [Option("wait-files", HelpText = "Number of files to upload between waits, see also --wait-time.")]
        public int WaitFiles
        {
            get { return _waitFiles ?? Properties.Settings.Default.WaitFiles; }
            set { _waitFiles = value; }
        }

        [Option("wait-time", HelpText = "Number of seconds to wait after every --wait-files are uploaded.")]
        public int WaitTime
        {
            get { return _waitTime ?? Properties.Settings.Default.WaitTime; }
            set { _waitTime = value; }
        }

        [Option(HelpText = "Log file")]
        public string Log { get; set; }

        [Option(HelpText = "Don't color console output")]
        public bool NoColor { get; set; }

        [Option(HelpText = "Timeout, in seconds, for web requests.")]
        public int Timeout
        {
            get { return _timeout ?? Properties.Settings.Default.TimeOut; }
            set { _timeout = value; }
        }

        [Option("no-filetype-check", HelpText = "If specified UploadFiles will not check permitted types before sending file.")]
        public bool NoFileTypeCheck { get; set; }

        [Option(Hidden = true)]
        public int Delay
        {
            get { return Math.Max(_delay ?? Properties.Settings.Default.Delay, 0); }
            set { _delay = value; }
        }

        [Option(Hidden = true)]
        public bool Debug { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(FilePattern) && string.IsNullOrEmpty(List))
                throw new OptionsException($"No files to upload. Specify a file pattern or use the --list switch. {help}");

            if (!string.IsNullOrEmpty(FilePattern) && !string.IsNullOrEmpty(List))
                throw new OptionsException($"Specify a file pattern OR --list, not both. {help}");

            ValidateSite();

            if (!string.IsNullOrEmpty(Content) && NoContent)
                throw new OptionsException($"Cannot specify both --nocontent and --content. {help}");

            if (!string.IsNullOrEmpty(List) && !File.Exists(List))
                throw new OptionsException($"--list file not found. {help}");

            if (!string.IsNullOrEmpty(Content) && !File.Exists(Content))
                throw new OptionsException($"--content file not found. {help}");

            if (!string.IsNullOrEmpty(List) && !string.IsNullOrEmpty(Fails))
            {
                string listPath = Path.GetFullPath(List);
                string failsPath = Path.GetFullPath(Fails);
                if (listPath == failsPath)
                    throw new OptionsException($"--fails and --list should not specify the same file. {help}");
            }
        }

        private void ValidateSite()
        {
            if (string.IsNullOrEmpty(Site))
                throw new OptionsException($"No site specified. Use --site or edit the .config file to configure a default site. {help}");

            if (!Uri.IsWellFormedUriString(Site, UriKind.Absolute))
                throw new OptionsException($"Site \"{Site}\" is not a valid url. An example of a valid site url is http://mywiki.fandom.com");

            if (Site.EndsWith("/"))
                throw new OptionsException($"Invalid site {Site}. Don't end the site name with a '/'");

            if (!Site.ToUpperInvariant().StartsWith("HTTP"))
                throw new OptionsException($"Site {Site} is invalid. Only http is allowed. e.g http://mywiki.fandom.com");
        }
    }
}
