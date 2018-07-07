using CommandLine;
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
        private static ExtendedWebClient client;
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
                using (client = new ExtendedWebClient())
                {
                    client.UserAgent = UserAgent();
                    var cardNames = ReadCardNames();
                    cards = CardsFromDecks(cardNames);
                }
                Console.Error.WriteLine(new string('=', 20));
                string markup = GetMarkup(cards);
                if (options.NoUpload)
                {
                    Console.WriteLine(FormatUpdated(runTime));
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
                + FormatUpdated(runTime)
                + target.Content.Substring(updatedEndPos, startPos - updatedEndPos)
                + markup 
                + target.Content.Substring(endPos);
            target.Save("Updating list via DeckCards utility.");
        }

        private static string FormatUpdated(DateTime dt)
        {
            return $"Updated on {dt.ToString("dddd, dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture)} UTC";
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

        private static string GetMarkup(Dictionary<string, List<string>> cards)
        {
            StringBuilder markup = new StringBuilder(350 * 1024);
            char currentFirstLetter = '*';
            List<string> sortedCards = cards.Keys.OrderBy(x => x).ToList();
            markup.AppendLine("<div style=\"margin-left:60px\">");
            foreach (var card in sortedCards)
            {
                var decks = cards[card];
                char firstLetter = char.ToLowerInvariant(card[0]);
                if (firstLetter != currentFirstLetter)
                {
                    AppendAnchorDiv(markup, firstLetter);
                    currentFirstLetter = firstLetter;
                }
                markup.AppendLine("<div class=\"mdw-collapse-row\">");
                AppendCardRow(markup, card, decks);
                markup.AppendLine("<div class=\"mdw-collapsable\">");
                foreach (var deck in decks)
                    AppendDeckRow(markup, deck);
                markup.AppendLine("</div></div>");
            }
            markup.AppendLine("</div>");
            return markup.ToString();
        }

        private static void AppendCardRow(StringBuilder markup, string card, List<string> decks)
        {
            markup.Append("<span class=\"mdw-arrow-collapse\"></span> '''{{Card|");
            markup.Append(card);
            markup.Append("}}''' (");
            markup.Append(decks.Count);
            markup.AppendLine(")");
        }

        private static void AppendDeckRow(StringBuilder markup, string deck)
        {
            markup.Append("*[[");
            markup.Append(deck);
            markup.Append('|');
            markup.Append(deck.Substring(6));
            markup.AppendLine("]]");
        }

        private static void AppendAnchorDiv(StringBuilder markup, char firstLetter)
        {
            markup.Append("<div id=\"mdw");
            markup.Append(firstLetter);
            markup.AppendLine("\"></div>");
        }

        private static Dictionary<string, List<string>> CardsFromDecks(Dictionary<string, string> cardNames)
        {
            var cards = new Dictionary<string, List<string>>();
            foreach (var deck in GetDecks())
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

        private static IEnumerable<Deck> GetDecks()
        {
            var apfrom = "";
            var decks = new XmlDocument();
            var rev = new XmlDocument();
            var url = ApiQuery(new Dictionary<string, string>
            {
                { "list", "allpages" },
                { "apprefix", "Decks/" },
                { "aplimit", "500" },
                { "apfrom", ""},
            });
            do
            {
                string deckxml = client.DownloadString(url + apfrom);
                decks.LoadXml(deckxml);
                var pages = decks.SelectNodes("/api/query/allpages/p");
                var cont = decks.SelectSingleNode("/api/query-continue/allpages");
                apfrom = cont == null ? "" : cont.Attributes["apfrom"].Value;
                foreach (XmlNode node in pages)
                {
                    var revisionUrl = ApiQuery(new Dictionary<string, string>
                    {
                        { "prop", "revisions" },
                        { "rvprop", "content" },
                        { "rvlimit", "1" },
                        { "pageids", node.Attributes["pageid"].Value },
                    });
                    string content = client.DownloadString(revisionUrl);
                    rev.LoadXml(content);
                    var revNode = rev.SelectSingleNode("/api/query/pages/page/revisions/rev");
                    List<string> cards = GetCards(revNode.InnerText);
                    yield return new Deck(node.Attributes["title"].Value, cards);
                }
            } while (!string.IsNullOrEmpty(apfrom));
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

        private static string ApiQuery(Dictionary <string, string> queryParameters)
        {
            const string baseUrl = "http://magicarena.wikia.com/";
            const string apiUrl = baseUrl + "api.php?action=query&format=xml";

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

        private static string UserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"DeckCards/{version.Major}.{version.Minor}.{version.Build} (Contact admin at magicarena.wikia.com)";
        }
    }
}
