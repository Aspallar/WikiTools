using System;
using CommandLine;

namespace DeckCards
{
    internal class Options
    {
        private const int maxBatch = 50;

        [Option(HelpText = "Don't upload, just write results to console")]
        public bool NoUpload { get; set; }

        [Option(HelpText = "Username")]
        public string User { get; set; }

        [Option(HelpText = "Password")]
        public string Password { get; set; }

        [Option(Default = "https://magicarena.fandom.com", HelpText = "Site to update")]
        public string Site { get; set; }

        [Option(HelpText = "Save username and password")]
        public bool Save { get; set; }

        [Option(Default = "Cards In Decks", HelpText = "Title of page to update")]
        public string Target { get; set; }

        [Option(Default = 50, HelpText = "Number of decks to fetch in each batch.")]
        public int Batch { get; set; }

        private void SetCredentials()
        {
            var props = Properties.Settings.Default;
            if (string.IsNullOrEmpty(User))
            {
                User = props.username;
            }
            if (string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(props.password))
            {
                Password = Encryption.Decrypt(props.password);
            }
        }

        public void Validate()
        {
            if (Batch <= 0 || Batch > maxBatch)
                throw new OptionsException($"Value for --batch should be 1 to {maxBatch} inclusive.");
            SetCredentials();
            if (string.IsNullOrEmpty(User))
                throw new OptionsException("No user name supplied.");
            if (string.IsNullOrEmpty(Password))
                throw new OptionsException("No password supplied.");
        }

        public void SaveCredentials()
        {
            var props = Properties.Settings.Default;
            props.username = User ?? "";
            props.password = Encryption.Encrypt(Password ?? "");
            props.Save();
        }
    }
}
