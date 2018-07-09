using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WikiaClientLibrary;

namespace DeckCards
{
    class Program
    {
        private static CardsInDecksClient wiki;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            if (options.Save)
            {
                options.SaveDefaults();
                Console.WriteLine("Username and password saved.");
                return;
            }
            options.SetDefaults();
            DateTime runTime = DateTime.Now.ToUniversalTime();

            using (wiki = new CardsInDecksClient(options.Site, UserAgent(), ReadCardNames(), options.Batch))
            {
                if (!wiki.Login(options.UserName, options.Password))
                {
                    Console.Error.WriteLine("Unable to log in.");
                    return;
                }
                Dictionary<string, List<string>> cards = wiki.GetCardsInDecks();
                Console.Error.WriteLine(new string('=', 20));
                string markup = Markup.GetMarkup(cards);
                if (options.NoUpload)
                {
                    Console.WriteLine(Markup.UpdatedOn(runTime));
                    Console.WriteLine(markup);
                }
                else
                {
                    Console.WriteLine("Uploading...");
                    Upload(options, markup, runTime);
                    Console.WriteLine("Uploaded");
                }
            }
        }

        private static void Upload(Options options, string markup, DateTime runTime)
        {
            const string updatedMarkerStart = "<!-- updated start -->";
            const string updatedMarkerEnd = "<!-- updated end -->";
            const string startMarker = "<!-- start -->";
            const string endMarker = "<!-- end -->";

            WikiaPage target = new WikiaPage(wiki, options.Target);
            target.Open();
            int startPos = target.Content.IndexOf(startMarker);
            if (startPos == -1)
            {
                Console.Error.WriteLine($"Unable to find insertion point: {startMarker}");
                return;
            }
            startPos += startMarker.Length;
            int endPos = target.Content.IndexOf(endMarker);
            if (endPos == -1)
            {
                Console.Error.WriteLine($"Unable to find insertion point: {endMarker}");
                return;
            }
            int updatedStartPos = target.Content.IndexOf(updatedMarkerStart);
            if (updatedStartPos == -1)
            {
                Console.Error.WriteLine($"Unable to find insertion point: {updatedMarkerStart}");
                return;
            }
            updatedStartPos += updatedMarkerStart.Length;
            int updatedEndPos = target.Content.IndexOf(updatedMarkerEnd);
            if (updatedEndPos == -1)
            {
                Console.Error.WriteLine($"Unable to find insertion point: {updatedMarkerEnd}");
                return;
            }
            target.Content = target.Content.Substring(0, updatedStartPos)
                + Markup.UpdatedOn(runTime)
                + target.Content.Substring(updatedEndPos, startPos - updatedEndPos)
                + markup 
                + target.Content.Substring(endPos);
            target.Save("Updating list via DeckCards utility.");
        }

        private static Dictionary<string, string> ReadCardNames()
        {
            string line;
            var cardNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\cardnames.txt";
            Console.Error.WriteLine($"Reading card names from {filename}");
            using (var sr = new StreamReader(filename))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    string trimmedLine = line.Trim();
                    cardNames.Add(trimmedLine, trimmedLine);
                }
            }
            return cardNames;
        }

        private static string UserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"DeckCards/{version.Major}.{version.Minor}.{version.Build} (Contact admin at magicarena.wikia.com)";
        }
    }
}
