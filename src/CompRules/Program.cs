using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WikiaClientLibrary;
using WikiToolsShared;

namespace CompRules
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Console.OutputEncoding = Encoding.UTF8;
                using (var client = new ExtendedWebClient())
                {
                    client.UserAgent = Utils.UserAgent();
                    var rulesUrl = FetchRulesUrl(client);
                    if (rulesUrl != null)
                    {
                        string rules = client.DownloadString(rulesUrl);
                        rules = FixNewlines(rules);
                        Console.WriteLine(rules);
                    }
                    else
                    {
                        Console.Error.WriteLine("Unable to find link to text rules.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static string FixNewlines(string rules)
        {
            // sometimes the posted file uses \r for newline, sometimes \n, sometimes \r\n
            // \n and \r\n are fine for us but \r needs to be replaced
            return Regex.Replace(rules, @"\r(?!\n)", "\n", RegexOptions.Singleline);
        }

        private static string FetchRulesUrl(ExtendedWebClient client)
        {
            string contents = client.DownloadString("https://magic.wizards.com/en/game-info/gameplay/rules-and-formats/rules");
            var match = Regex.Match(contents, @"href=""(https://media.wizards.com.*?\.txt)");
            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }
    }
}
