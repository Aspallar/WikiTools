using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using WikiaClientLibrary;

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
                using (var client = new ExtendedWebClient())
                {
                    client.UserAgent = UserAgent();
                    var rulesUrl = FetchRulesUrl(client);
                    if (rulesUrl != null)
                    {
                        string rules = client.DownloadString(rulesUrl);
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

        private static string FetchRulesUrl(ExtendedWebClient client)
        {
            string contents = client.DownloadString("https://magic.wizards.com/en/game-info/gameplay/rules-and-formats/rules");
            var match = Regex.Match(contents, @"href=""(https://media.wizards.com.*?\.txt)");
            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }

        private static string UserAgent()
        {
            return $"CompRules/{VersionString()} (Contact admin at magicarena.fandom.com)";
        }

        private static string VersionString()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
