using CommandLine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using WikiaClientLibrary;

namespace FetchAllCardBrowsingPages
{
    class Program
    {
        static ExtendedWebClient client;
        static Options options;

        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(opts => { options = opts;  Run(); });
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected Error");
                Console.Error.WriteLine(ex.ToString());
            }
#endif
        }

        private static void Run()
        {
            using (client = new ExtendedWebClient())
            {
                client.UserAgent = UserAgent();
                List<string> cardBrowseTitles = GetCardTitles(options.Category, options.Filter);
                if (options.List)
                    ListTitles(cardBrowseTitles);
                else
                    FetchPages(cardBrowseTitles);
            }
        }

        private static void ListTitles(List<string> titles)
        {
            foreach (string title in titles)
            {
                Console.Write("[[");
                Console.Write(title);
                //Console.Write('|');
                //Console.Write(title.Substring(6));
                Console.WriteLine("]]<br />");
            }
        }

        private static void FetchPages(List<string> titles)
        {
            int count = 0;
            Console.WriteLine($"Fetching {titles.Count} pages.");
            foreach (string title in titles)
            {
                Console.WriteLine($"{++count}: {title}");
                string url = options.Site + title.Replace(' ', '_');
                if (options.Verbose)
                    Console.WriteLine(url);
                string response = client.DownloadString(url);
                if (options.Verbose)
                    Console.WriteLine(response.Substring(0, 2048));
            }
        }

        private static List<string> GetCardTitles(string category, string filter)
        {
            Regex regex = string.IsNullOrEmpty(filter) ? null : new Regex(filter);
            XmlDocument members = FetchCategoryMembers(category);
            var pageTitles = new List<string>();
            foreach (XmlNode node in members.SelectNodes("/api/query/categorymembers/cm"))
            {
                string title = node.Attributes["title"].Value;
                if (regex == null || regex.IsMatch(title))
                    pageTitles.Add(title);
            }
            return pageTitles;
        }

        private static XmlDocument FetchCategoryMembers(string category)
        {
            string url = options.Site + "api.php?cb=foo&action=query&list=categorymembers&cmlimit=500&format=xml&cmtitle=Category:" + Uri.EscapeUriString(category);
            string response = client.DownloadString(url);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            return doc;
        }

        private static string UserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"FetchAllCardBrowsingPages/{version.Major}.{version.Minor}.{version.Build} (Contact admin at magicarena.fandom.com)";
        }
    }
}
