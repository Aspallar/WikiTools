using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using WikiaClientLibrary;
using WikiToolsShared;

namespace DeckCards
{
    class Program
    {
        private static CardsInDecksClient wiki;

        private static void Main(string[] args)
        {
            try
            {
                Utils.InitialiseTls();
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (OptionsException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
#if !DEBUG
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected Error");
                Console.Error.WriteLine(ex.ToString());
            }
#endif
        }

        private static void Run(Options options)
        {
            options.Validate();
            if (options.Save)
                options.SaveCredentials();
            else
                GenerateMarkup(options);
        }


        private static void GenerateMarkup(Options options)
        {
            DateTime runTime = DateTime.Now.ToUniversalTime();

            using (wiki = new CardsInDecksClient(options.Site, Utils.UserAgent(), ReadCardNames(), options.Batch))
            {
                if (!wiki.Login(options.User, options.Password))
                {
                    Console.Error.WriteLine("Unable to log in.");
                    return;
                }
                Dictionary<string, List<string>> cards = wiki.GetCardsInDecks(ReadIgnoredDecks(), ReadRemovedCards());
                string markup = Markup.GetMarkup(cards);
                if (options.NoUpload)
                {
                    Console.WriteLine(Markup.UpdatedOn(runTime));
                    Console.WriteLine(markup);
                }
                else
                {
                    try
                    {
                        Upload(options, markup, runTime);
                    }
                    catch (UploadException ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
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
            if (!target.Exists)
                throw new UploadException("Target page does not exist.");
            int startPos = target.Content.IndexOf(startMarker);
            if (startPos == -1)
                throw new UploadException($"Unable to find insertion point: {startMarker}");
            startPos += startMarker.Length;
            int endPos = target.Content.IndexOf(endMarker);
            if (endPos == -1)
                throw new UploadException($"Unable to find insertion point: {endMarker}");
            int updatedStartPos = target.Content.IndexOf(updatedMarkerStart);
            if (updatedStartPos == -1)
                throw new UploadException($"Unable to find insertion point: {updatedMarkerStart}");
            updatedStartPos += updatedMarkerStart.Length;
            int updatedEndPos = target.Content.IndexOf(updatedMarkerEnd);
            if (updatedEndPos == -1)
                throw new UploadException($"Unable to find insertion point: {updatedMarkerEnd}");
            target.Content = target.Content.Substring(0, updatedStartPos)
                + Markup.UpdatedOn(runTime)
                + target.Content.Substring(updatedEndPos, startPos - updatedEndPos)
                + markup
                + target.Content.Substring(endPos);
            target.Save($"Updating via DeckCards {Utils.VersionString()}");
        }

        private static Dictionary<string, string> ReadCardNames()
        {
            string line;
            var cardNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string filename = FullPath("cardnames.txt");
            Console.Error.WriteLine($"Reading card names from {filename}");
            using (var sr = new StreamReader(filename))
            {
                while ((line = sr.ReadConfigLine()) != null)
                    cardNames.Add(line, line);
            }
            return cardNames;
        }

        private static HashSet<string> ReadRemovedCards()
        {
            string line;
            var removedCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string filename = FullPath("removedcards.txt");
            if (File.Exists(filename))
            {
                Console.Error.WriteLine($"Reading removed cards from {filename}");
                using (var sr = new StreamReader(filename))
                {
                    while ((line = sr.ReadConfigLine()) != null)
                        removedCards.Add(line);
                }
            }
            return removedCards;
        }


        private static List<string> ReadIgnoredDecks()
        {
            string line;
            var ignoredDecks = new List<string>();
            string filename = FullPath("ignoreddecks.txt");
            if (File.Exists(filename))
            {
                Console.Error.WriteLine($"Reading ignored decks from {filename}");
                using (var sr = new StreamReader(filename))
                {
                    while ((line = sr.ReadConfigLine()) != null)
                        ignoredDecks.Add(line);
                }
            }
            return ignoredDecks;
        }

        private static string FullPath(string fileName)
        {
            return AppContext.BaseDirectory + fileName;
        }
    }
}
