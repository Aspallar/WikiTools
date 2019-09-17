using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiToolsShared
{
    public class CredentialsOptions : SiteOptions
    {
        [Option(HelpText = "Username")]
        public string User { get; set; }

        [Option(HelpText = "Password")]
        public string Password { get; set; }

        private void SetCredentials(string defaultUser, string defaultPassword)
        {
            if (string.IsNullOrEmpty(User))
            {
                User = defaultUser;
            }
            if (string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(defaultPassword))
            {
                Password = Encryption.Decrypt(defaultPassword);
            }
        }

        public void ValidateCredentialsExist(string defaultUser, string defaultPassword)
        {
            SetCredentials(defaultUser, defaultPassword);
            if (string.IsNullOrEmpty(User))
                throw new OptionsException("No user name supplied.");
            if (string.IsNullOrEmpty(Password))
                throw new OptionsException("No password supplied.");
        }

        public string EncryptedPassword()
        {
            return Encryption.Encrypt(Password ?? "");
        }

    }
}
