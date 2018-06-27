using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DeckCards
{
    class Program
    {
        static WebClient client;

        static void Main(string[] args)
        {
            using (client = new WebClient())
            {
                var cardNames = ReadCardNames();
                Dictionary<string, List<string>> cards = CardsFromDecks(cardNames);
                Console.Error.WriteLine(new string('=', 20));
                WriteLastUpdated();
                WriteCards(cards);
            }
        }

        private static void WriteLastUpdated()
        {
            DateTime now = DateTime.Now.ToUniversalTime();
            Console.WriteLine($"Updated on {now.ToString("dddd, dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture)} UTC\n");
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
                    string trimmed = line.Trim();
                    cardNames.Add(trimmed, trimmed);
                }
            }
            return cardNames;
        }

        private static void WriteCards(Dictionary<string, List<string>> cards)
        {
            List<string> sorted = cards.Keys.ToList();
            sorted.Sort();
            Console.WriteLine("<div style=\"margin-left:60px\">");
            foreach (var card in sorted)
            {
                var decks = cards[card];
                Console.WriteLine($"'''{{{{Card|{card}}}}}''' ({decks.Count})");
                foreach (var deck in decks)
                    Console.WriteLine($"*[[{deck}|{deck.Substring(6)}]]");
            }
            Console.WriteLine("</div>");
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
            var url = BuildApiUrl(new Dictionary<string, string>
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
                    var revisionUrl = BuildApiUrl(new Dictionary<string, string>
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

        private static List<string> GetCards(string pageText)
        {
            char[] newline = { '\n' };
            var cards = new List<string>();
            string cardText = ExtractCardText(pageText);
            foreach (string cardLine in cardText.Split(newline))
            {
                string trimmed = cardLine.Trim();
                if (trimmed.Length > 0) {
                    if (trimmed.StartsWith("--"))
                        break; // reached sideboard, ignore the rest
                    Match match = Regex.Match(trimmed, @"\d+\s+([^\(/]+)");
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

        private static string BuildApiUrl(Dictionary <string, string> queryParameters)
        {
            const string baseUrl = "http://magicarena.wikia.com/";
            const string apiUrl = baseUrl + "api.php";

            var url = new StringBuilder(apiUrl);
            url.Append("?action=query&format=xml");
            foreach (var entry in queryParameters)
            {
                url.Append('&');
                url.Append(entry.Key);
                url.Append('=');
                url.Append(entry.Value);
            }
            return url.ToString(); ;
        }

    }
}
