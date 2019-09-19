using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using CommandLine;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using AngleSharp.Dom;
using System.Threading;
using WikiToolsShared;
using System.Reflection;
using System.Net;

namespace WamData
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        static string userAgent;
        static int cancelCount = 0;
        static string urlBase;
        static string siteSearchTerm;
        static DaysRange range;
        static DateTimeOffset endDate;
        static readonly List<WamItem> results = new List<WamItem>();
        static readonly List<WamError> errors = new List<WamError>();

        private static void Run(Options options)
        {
            Utils.InitializeTls();
            SetUserAgent();
            try
            {
                if (options.MoreHelp)
                {
                    Console.WriteLine(Help.HelpText);
                    return;
                }
                options.Validate();
                if (options.Latest)
                    FetchLatestDate(options);
                else
                    FetchWamData(options);
            }
            catch (OptionsException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private static void FetchLatestDate(Options options)
        {
            FetchLatestDateAsync(options).GetAwaiter().GetResult();
        }

        private static async Task FetchLatestDateAsync(Options options)
        {
            string url = UrlBase(options.Name, options.VerticalType).Replace("&date=", "");
            using (var client = CreateClient())
            {
                string contents = await client.GetStringAsync(url);
                var parser = new HtmlParser();
                var doc = parser.Parse(contents);
                var input = doc.QuerySelector("#WamFilterHumanDate");
                if (input != null)
                {
                    var latestDate = input.GetAttribute("value");
                    Console.WriteLine(latestDate);
                }
                else
                {
                    Console.WriteLine("Unable to find latest date on returned page.");
                }
            }
        }

        private static void FetchWamData(Options options)
        {
            ShowInitialMessage(options.Name, options.StartDate, options.EndDate);
            urlBase = UrlBase(options.Name, options.VerticalType);
            siteSearchTerm = "https://" + options.Name;
            range = new DaysRange(options.StartDate);
            endDate = options.EndDate;
            int totalTasks = Math.Min(options.FirePower, options.StartDate.InclusiveDaysUntil(endDate));
            RunFetchWamDataTasks(totalTasks).GetAwaiter().GetResult();
            if (cancelCount == 0)
            {
                WriteResults(options.Verbose, options.ColumnFlags);
                WriteErrors();
            }
        }

        private static void SetUserAgent()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            userAgent = $"WamData/{v.Major}.{v.Minor}.{v.Build}";
        }

        private static void ShowInitialMessage(string name, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            const string format = "MMMM d, yyyy";
            int days = startDate.InclusiveDaysUntil(endDate);
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

        private static void WriteResults(bool verbose, ColumnFlags flags)
        {
            string format = MakeFormatString(flags);
            foreach (var item in results.OrderBy(x => x.Date))
            {
                var output = string.Format(format, item.Date.ToWamTime(), FormatDate(item.Date), item.Rank, item.Score);
                Console.WriteLine(output);
                if (verbose && Console.IsOutputRedirected)
                    Console.Error.WriteLine(output);
            }
        }

        private static string MakeFormatString(ColumnFlags flags)
        {
            string format = "";
            if ((flags & ColumnFlags.WamDate) != 0)
                format += "{0},";
            if ((flags & ColumnFlags.Date) != 0)
                format += "{1},";
            if ((flags & ColumnFlags.Rank) != 0)
                format += "{2},";
            if ((flags & ColumnFlags.Score) != 0)
                format += "{3},";
            format = format.Substring(0, format.Length - 1);
            return format;
        }

        private static async Task RunFetchWamDataTasks(int numTasks)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numTasks; i++)
                tasks.Add(GetWamData());
            var allTasks = Task.WhenAll(tasks);
            await allTasks;
        }

        private static async Task GetWamData()
        {
            HtmlParser parser = new HtmlParser();
            using (var client = CreateClient())
            {
                while (cancelCount == 0)
                {
                    DateTimeOffset date = range.Next();
                    if (date > endDate)
                        break; // while
                    try
                    {
                        var doc = await GetDocument(parser, client, date).ConfigureAwait(false);
                        TerminateIfMoreThanOnePage(doc, date);
                        ProcessDocument(date, doc);
                    }
                    catch (HttpRequestException ex)
                    {
                        AddError(date, ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                        AddResult(date, "", "");
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
            string content = await client.GetStringAsync(url).ConfigureAwait(false);
            var doc = parser.Parse(content);
            return doc;
        }

        private static void TerminateIfMoreThanOnePage(IHtmlDocument doc, DateTimeOffset date)
        {
            var pageDiv = doc.QuerySelector("div.wikia-paginator");
            if (pageDiv != null)
            {
                int count = Interlocked.Increment(ref cancelCount);
                if (count == 1)
                {
                    var pages = pageDiv.QuerySelectorAll("a.paginator-page")?.Last()?.GetAttribute("data-page") ?? "More than one";
                    Console.Error.WriteLine($"Holy {Utils.RobinSays()} Batman!");
                    Console.Error.WriteLine($"{pages} pages of results were returned for {date.ToWamHumanTime()}.");
                    Console.Error.WriteLine("This probably means that your --name was not specific enough.");
                    Environment.Exit(1);
                }
            }
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
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("pragma", "no-cache");
            return client;
        }

        private static string UrlBase(string name, int verticalId)
        {
            string url = Properties.Settings.Default.UrlFormat;
            return string.Format(url, name, verticalId, DateTime.Now.Ticks);
        }
    }
}
