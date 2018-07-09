﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WikiaClientLibrary;

namespace DeckCards
{
    class Program
    {
        private static WikiaClient wiki;

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
            Dictionary<string, List<string>> cards;

            using (wiki = new WikiaClient(options.Site, UserAgent()))
            {
                if (!wiki.Login(options.UserName, options.Password))
                {
                    Console.Error.WriteLine("Unable to log in.");
                    return;
                }
                var cardNames = ReadCardNames();
                cards = CardsFromDecks(cardNames, options.Batch, wiki.Client);
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


        private static Dictionary<string, List<string>> CardsFromDecks(Dictionary<string, string> cardNames, int batchSize, ExtendedWebClient client)
        {
            var cards = new Dictionary<string, List<string>>();
            foreach (var deck in GetDecks(batchSize, client))
            {
                Console.Error.WriteLine($"{deck.Title} {deck.Cards.Count}");
                foreach (var card in deck.Cards)
                {
                    string correctCase;
                    if (cardNames.TryGetValue(card, out correctCase))
                    {
                        if (cards.ContainsKey(correctCase))
                        {
                            cards[correctCase].Add(deck.Title);
                        }
                        else
                        {
                            var decks = new List<string>();
                            decks.Add(deck.Title);
                            cards.Add(correctCase, decks);
                        }
                    }
                }
            }
            return cards;
        }

        private static IEnumerable<Deck> GetDecks(int batchSize, ExtendedWebClient client)
        {
            var apfrom = "";
            var decks = new XmlDocument();
            var deckContents = new XmlDocument();
            var url = ApiQuery(new Dictionary<string, string>
            {
                { "list", "allpages" },
                { "apprefix", "Decks/" },
                { "aplimit", "500" },
                { "apfrom", ""},
            });
            do
            {
                string response = client.DownloadString(url + apfrom);
                decks.LoadXml(response);
                TerminateOnErrorOrWarning(decks, "Error while obtaining list of decks");
                var deckPages = decks.SelectNodes("/api/query/allpages/p");
                var continueNode = decks.SelectSingleNode("/api/query-continue/allpages");
                apfrom = continueNode == null ? "" : continueNode.Attributes["apfrom"].Value;
                var deckNodes = deckPages.GetEnumerator();
                while (deckNodes.MoveNext())
                {
                    var pageids = new List<string>();
                    int k = batchSize;
                    do
                    {
                        pageids.Add(((XmlNode)deckNodes.Current).Attributes["pageid"].Value);
                    } while (--k != 0 && deckNodes.MoveNext());
                    var revisionUrl = ApiQuery(new Dictionary<string, string>
                    {
                        { "prop", "revisions" },
                        { "rvprop", "content" },
                        { "pageids", string.Join("|", pageids) },
                    });
                    response = client.DownloadString(revisionUrl);
                    deckContents.LoadXml(response);
                    TerminateOnErrorOrWarning(deckContents, "Error while obtaining deck contents.");
                    foreach (XmlNode deckPage in deckContents.SelectNodes("/api/query/pages/page"))
                    {
                        List<string> cards = GetCards(deckPage.SelectSingleNode("revisions/rev").InnerText);
                        yield return new Deck(deckPage.Attributes["title"].Value, cards);
                    }
                }
            } while (!string.IsNullOrEmpty(apfrom));
        }

        private static void TerminateOnErrorOrWarning(XmlDocument response, string message)
        {
            XmlNode error = response.SelectSingleNode("/api/error");
            if (error != null)
            {
                Console.Error.WriteLine(message);
                Console.Error.WriteLine(error.Attributes["info"].Value);
                Environment.Exit(1);
            }
            XmlNode warnings = response.SelectSingleNode("/api/warnings");
            if (warnings != null)
            {
                Console.Error.WriteLine(message);
                foreach (XmlNode warning in warnings.ChildNodes)
                    Console.WriteLine(warning.InnerText);
                Environment.Exit(1);
            }
        }

        private static Regex cardRegex = new Regex(@"\d+\s+([^\(/]+)");
        private static List<string> GetCards(string pageText)
        {
            char[] newline = { '\n' };
            var cards = new List<string>();
            string cardText = ExtractCardText(pageText);
            foreach (string cardLine in cardText.Split(newline))
            {
                string trimmedCardLine = cardLine.Trim();
                if (trimmedCardLine.Length > 0) {
                    if (trimmedCardLine.StartsWith("--"))
                        break; // reached sideboard, ignore the rest
                    Match match = cardRegex.Match(trimmedCardLine);
                    if (match.Success)
                    {
                        string name = match.Groups[1].Value.Trim();
                        cards.Add(name);
                    }
                }
            }
            return cards;
        }

        private static string ExtractCardText(string content)
        {
            int startPos = content.IndexOf("|Deck=", StringComparison.InvariantCultureIgnoreCase);
            if (startPos == -1)
                return string.Empty;
            startPos += 6;
            int endPos = content.IndexOf("}}", startPos);
            if (endPos == -1)
                return string.Empty;
            string cardText = content.Substring(startPos, endPos - startPos);
            return cardText;
        }

        private static string ApiQuery(Dictionary <string, string> queryParameters = null)
        {
            const string baseUrl = "http://magicarena.wikia.com/";
            const string apiUrl = baseUrl + "api.php?action=query&format=xml";

            if (queryParameters != null)
            {
                var url = new StringBuilder(apiUrl);
                foreach (var entry in queryParameters)
                {
                    url.Append('&');
                    url.Append(entry.Key);
                    url.Append('=');
                    url.Append(entry.Value);
                }
                return url.ToString(); ;
            }
            else
            {
                return apiUrl;
            }
        }

        private static string UserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"DeckCards/{version.Major}.{version.Minor}.{version.Build} (Contact admin at magicarena.wikia.com)";
        }
    }
}
