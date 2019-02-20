using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using CommandLine;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using AngleSharp.Dom;

namespace WamData
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

        static string urlBase;
        static string siteSearchTerm;
        static DateTimeOffset next;
        static object nextLock = new object();
        static DateTimeOffset endDate;
        static List<WamItem> results = new List<WamItem>();
        static List<WamError> errors = new List<WamError>();

        private static void Run(Options options)
        {
            try
            {
                options.Validate();
                ShowDateRange(options.Name, options.StartDate, options.EndDate);
                urlBase = $"https://community.wikia.com/wiki/WAM?verticalId={options.VerticalType}&langCode=&searchPhrase={options.Name}&date=";
                siteSearchTerm = "https://" + options.Name;
                next = options.StartDate;
                endDate = options.EndDate;
                RunFetchWamDataTasks(options.FirePower).GetAwaiter().GetResult();
                WriteResults(options.Verbose);
                WriteErrors();
            }
            catch (OptionsException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private static void ShowDateRange(string name, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            const string format = "MMMM d, yyyy";
            int days = (int)(endDate - startDate).TotalDays + 1;
            Console.Error.WriteLine($"Commencing to probe WAM for {name} data.");
            Console.Error.WriteLine($"From {startDate.ToString(format)} to {endDate.ToString(format)} ({days} days)");
        }

        private static void WriteErrors()
        {
            if (errors.Count > 0)
            {
                Console.Error.WriteLine("There were errors:");
                foreach (var err in errors.OrderBy(x => x.Date))
                    Console.Error.WriteLine($"{err.Date.ToWamTime()} {FormatDate(err.Date)} {err.Reason}");
            }
        }

        private static void WriteResults(bool verbose)
        {
            foreach (var item in results.OrderBy(x => x.Date))
            {
                string output = $"{item.Date.ToWamTime()},{FormatDate(item.Date)},{item.Rank},{item.Score}";
                Console.WriteLine(output);
                if (verbose && Console.IsOutputRedirected)
                    Console.Error.WriteLine(output);
            }
        }

        private static async Task RunFetchWamDataTasks(int numTasks)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numTasks; i++)
                tasks.Add(GetWamData());
            var allTasks = Task.WhenAll(tasks);
            await allTasks;
        }

        private static DateTimeOffset NextDate()
        {
            DateTimeOffset nextDate;
            lock (nextLock)
            {
                nextDate = next;
                next = next.AddDays(1);
            }
            return nextDate;
        }

        private static async Task GetWamData()
        {
            HtmlParser parser = new HtmlParser();
            using (var client = CreateClient())
            {
                while (true)
                {
                    DateTimeOffset date = NextDate();
                    if (date > endDate)
                        break; // while true
                    try
                    {
                        var doc = await GetDocument(parser, client, date);
                        ProcessDocument(date, doc);
                    }
                    catch (HttpRequestException ex)
                    {
                        AddError(date, ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                    }
                }
            }
        }

        private static void ProcessDocument(DateTimeOffset date, IHtmlDocument doc)
        {
            var rows = doc.QuerySelectorAll(".wam-index-table-wrapper table tr")
                .Skip(1)
                .Where(x => x.QuerySelector("td:nth-child(4) a")?.GetAttribute("href") == siteSearchTerm)
                .ToList();
            if (rows.Count > 0)
            {
                var input = doc.QuerySelector("#WamFilterHumanDate");
                if (input != null)
                {
                    var responseDate = input.GetAttribute("value");
                    AddResult(date, rows);
                    if (responseDate != date.ToWamHumanTime())
                        AddError(date, $"Returned date {responseDate} did not match expected {date.ToWamHumanTime()}");
                }
                else
                {
                    AddResult(date, rows);
                    AddError(date, "No date input found");
                }
            }
            else
            {
                AddResult(date, "", "");
                AddError(date, "No entry for specified wiki");
            }
        }

        private static async Task<IHtmlDocument> GetDocument(HtmlParser parser, HttpClient client, DateTimeOffset date)
        {
            string url = urlBase + date.ToWamTime().ToString();
            string content = await client.GetStringAsync(urlBase + date.ToWamTime().ToString())
                .ConfigureAwait(false);
            var doc = parser.Parse(content);
            return doc;
        }

        private static void AddResult(DateTimeOffset date, string rank, string score)
        {
            var result = new WamItem
            {
                Date = date,
                Rank = rank,
                Score = score,
            };
            lock (results) results.Add(result);
        }

        private static void AddResult(DateTimeOffset date, IList<IElement> rows)
        {
            var cols = rows[0].QuerySelectorAll("td");
            AddResult(date, cols[0].TextContent.Trim(), cols[1].TextContent.Trim());
        }

        private static void AddError(DateTimeOffset date, string reason)
        {
            var wamError = new WamError
            {
                Date = date,
                Reason = reason
            };
            lock (errors) errors.Add(wamError);
        }

        private static string FormatDate(DateTimeOffset date)
        {
            return date.ToString("dd/MM/yyyy");
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "WamData/1.0");
            return client;
        }
    }
}
