using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
                List<string> cardBrowseTitles = GetCardBrowsingTitles();
                FetchPages(cardBrowseTitles);
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
                if (options.Purge)
                    url += "&action=purge";
                client.DownloadString(url);
            }
        }

        private static List<string> GetCardBrowsingTitles()
        {
            XmlDocument members = GetCategoryMembers();
            var pageTitles = new List<string>();
            foreach (XmlNode node in members.SelectNodes("/api/query/categorymembers/cm"))
                pageTitles.Add(node.Attributes["title"].Value);
            return pageTitles;
        }

        private static XmlDocument GetCategoryMembers()
        {
            string response = client.DownloadString(options.Site + "api.php?action=query&list=categorymembers&cmtitle=Category:Card%20Browsing&cmlimit=500&format=xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            return doc;
        }

        private static string UserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"FetchAllCardBrowsingPages/{version.Major}.{version.Minor}.{version.Build} (Contact admin at magicarena.wikia.com)";
        }
    }
}
