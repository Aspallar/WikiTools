using CommandLine;

namespace DeckCards
{
    internal class Options
    {
        [Option(HelpText = "Don't upload, just write results to console")]
        public bool NoUpload { get; set; }

        [Option(HelpText = "Username")]
        public string UserName { get; set; }

        [Option(HelpText = "Password")]
        public string Password { get; set; }

        [Option(Default = "https://magicarena.wikia.com", HelpText = "Site to update")]
        public string Site { get; set; }

        [Option(HelpText = "Save username and password")]
        public bool Save { get; set; }

        [Option(Default = "Cards In Decks", HelpText = "Title of page to update")]
        public string Target { get; set; }

        [Option(Default = 20, HelpText = "Number of decks to fetch in each batch")]
        public int Batch { get; set; }

        public void SetDefaults()
        {
            var props = Properties.Settings.Default;
            if (string.IsNullOrEmpty(UserName))
            {
                UserName = props.username;
            }
            if (string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(props.password))
            {
                Password = Encryption.Decrypt(props.password);
            }
        }

        public void SaveDefaults()
        {
            var props = Properties.Settings.Default;
            props.username = UserName ?? "";
            props.password = Encryption.Encrypt(Password ?? "");
            props.Save();
        }
    }
}
