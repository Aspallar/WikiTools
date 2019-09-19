using CommandLine;
using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using WikiToolsShared;

namespace GetRatings
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Utils.InitializeTls();
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (WebException ex)
            {
                Console.Error.WriteLine("There was a problem fetching the ratings page.");
                Console.Error.WriteLine(ex.Message);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine("The ratings page does not contain valid JSON.");
                Console.Error.WriteLine(ex.Message);
            }
            catch (ArithmeticException ex)
            {
                Console.Error.WriteLine("The ratings page contains invalid figures.");
                Console.Error.WriteLine(ex.Message);
            }
#if !DEBUG
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error.");
                Console.Error.WriteLine(ex.ToString());
            }
#endif
        }

        private static void Run(Options options)
        {
            string contents;
            using (var client = new WebClient())
                contents = client.DownloadString(GetUrl(options.Site, options.RatingsPage));

            if (options.Raw)
                Console.WriteLine(contents);
            else
            {
                List<DeckTotal> deckTotals = JsonConvert.DeserializeObject<List<DeckTotal>>(contents);
                if (options.Json)
                    WriteListToConsoleAsJson(deckTotals);
                else
                    WriteListAsCsvToConsole(deckTotals);
            }
        }

        private static string GetUrl(string site, string ratingsPage)
        {
            return $"{site}/wiki/{ratingsPage}?action=raw";
        }

        private static void WriteListToConsoleAsJson(List<DeckTotal> deckTotals)
        {
            string output = JsonConvert.SerializeObject(deckTotals, Formatting.Indented);
            Console.WriteLine(output);
        }

        private static void WriteListAsCsvToConsole<T>(List<T> list)
        {
            using (CsvWriter csvWriter = new CsvWriter(Console.Out, true))
            {
                csvWriter.WriteHeader<T>();
                csvWriter.NextRecord();
                csvWriter.WriteRecords(list);
            }
        }
    }
}
