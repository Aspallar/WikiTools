using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WikiaClientLibrary;
using WikiToolsShared;

namespace WikiActivity
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            IEnumerable<Activity> changes = GetRecentActivity(options.Limit);
            foreach (Activity change in changes)
            {
                Console.WriteLine($"{change.Timestamp} {change.Title} {change.User}");
            }
            
            //var doc = new XmlDocument();

            //if (!string.IsNullOrEmpty(options.Ip))
            //{
            //    doc.Load("http://ip-api.com/xml/" + options.Ip);
            //    var details = new IpDetails(doc);
            //    Console.WriteLine($"{details.Ip} {details.Country}, {details.City}");
            //}
            //else
            //{
            //    var countries = new Dictionary<string, int>();
            //    var ips = new Dictionary<string, IpDetails>();
            //    IEnumerable<Activity> changes = GetRecentActivity(options.Limit);
            //    foreach (Activity change in changes.Where(x => x.IsAnonymous))
            //    {
            //        int coutryTotal;
            //        IpDetails details;

            //        if (!options.NoIp)
            //        { 
            //            if (!!ips.TryGetValue(change.User, out details))
            //            {
            //                doc.Load("http://ip-api.com/xml/" + change.User);
            //                details = new IpDetails(doc);
            //                ips.Add(change.User, details);
            //                if (options.IpOnly)
            //                    Console.WriteLine($"{details.Ip},{details.Country},{details.City}");
            //                Thread.Sleep(500);
            //            }
            //            countries.TryGetValue(details.Country, out coutryTotal);
            //            countries[details.Country] = coutryTotal + 1;
            //            if (!options.IpOnly && !options.Count)
            //                Console.WriteLine($"{change.Timestamp} {change.Title} {change.Comment} {change.User} {details.Country}, {details.City}");
            //        }
            //        else {
            //            if (options.NoIp)
            //                Console.WriteLine($"{change.Timestamp} {change.Title} {change.Comment} {change.User}");
            //    }
            //    if (options.Count)
            //    {
            //        foreach (var country in countries)
            //            Console.WriteLine($"{country.Value} {country.Key}");
            //        int usTotal = countries.Sum(x => x.Key.StartsWith("United States") ? x.Value : 0);
            //        Console.WriteLine($"{usTotal} United States");
            //    }
            //}
        }

        static IEnumerable<Activity> GetRecentActivity(int numItems)
        {
            const string urlBase = "https://magicarena.wikia.com/api.php?action=query&list=recentchanges&rcprop=title|user|timestamp|comment&format=xml&rclimit=";
            var doc = new XmlDocument();
            string rcStart = null;
            do
            {
                int limit = Math.Min(500, numItems);
                numItems -= limit;
                string url = urlBase + limit.ToString();
                if (!string.IsNullOrEmpty(rcStart))
                    url += "&rcstart=" + rcStart;
                doc.Load(url);
                XmlNodeList changes = doc.SelectNodes("/api/query/recentchanges/rc");
                foreach (XmlNode change in changes)
                    yield return new Activity(change);
                var cont = doc.SelectSingleNode("/api/query-continue/recentchanges");
                rcStart = cont?.Attributes["rcstart"].Value;
            } while (numItems > 0);
        }
    }
}
