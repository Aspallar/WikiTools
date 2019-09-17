using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompRules
{
    class Options
    {
        //[Option(HelpText = "Upload the rules to the specified site.")]
        //public bool Upload { get; set; }

        //[Option(Default = "https://aspallar.fandom.com", HelpText = "The wiki to upload the rues to")]
        //public string Site { get; set; }

        //[Option(HelpText = "Username")]
        //public string User { get; set; }

        //[Option(HelpText = "Password")]
        //public string Password { get; set; }

        //public void Validate()
        //{
        //    SetCredentials();
        //    if (string.IsNullOrEmpty(User))
        //        throw new OptionsException("No user name supplied.");
        //    if (string.IsNullOrEmpty(Password))
        //        throw new OptionsException("No password supplied.");
        //}

        //private void SetCredentials()
        //{
        //    var props = Properties.Settings.Default;
        //    if (string.IsNullOrEmpty(User))
        //    {
        //        User = props.username;
        //    }
        //    if (string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(props.password))
        //    {
        //        Password = Encryption.Decrypt(props.password);
        //    }
        //}

        //public void SaveCredentials()
        //{
        //    var props = Properties.Settings.Default;
        //    props.username = User ?? "";
        //    props.password = Encryption.Encrypt(Password ?? "");
        //    props.Save();
        //}



    }
}
