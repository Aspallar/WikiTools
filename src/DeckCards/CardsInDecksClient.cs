using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using WikiaClientLibrary;

namespace DeckCards
{
    internal class CardsInDecksClient : WikiaClient
    {
        private int _batchSize;
        private Dictionary<string, string> _cardNames;

        public CardsInDecksClient(
            string site,
            string userAgent,
            Dictionary<string, string> cardNames,
            int batchSize) : base(site, userAgent)
        {
            _cardNames = cardNames;
            _batchSize = batchSize;
        }

        public Dictionary<string, List<string>> GetCardsInDecks(HashSet<string> ignoredDecks, HashSet<string> removedCards)
        {
            var cards = new Dictionary<string, List<string>>();
            foreach (var deck in GetDecks(ignoredDecks, removedCards))
            {
                Console.Error.WriteLine($"{deck.Title} {deck.Cards.Count}");
                foreach (var card in deck.Cards)
                {
                    string correctCase;
                    if (_cardNames.TryGetValue(card, out correctCase))
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

        private IEnumerable<Deck> GetDecks(HashSet<string> ignoredDecks, HashSet<string> removedCards)
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
                GetXmlResponse(url + apfrom, decks);
                TerminateOnErrorOrWarning(decks, "Error while obtaining list of decks");
                var deckPages = decks.SelectNodes("/api/query/allpages/p");
                var continueNode = decks.SelectSingleNode("/api/query-continue/allpages");
                apfrom = continueNode == null ? "" : continueNode.Attributes["apfrom"].Value;
                var deckNodes = deckPages.GetEnumerator();
                while (deckNodes.MoveNext())
                {
                    var pageids = new List<string>();
                    int k = _batchSize;
                    do
                    {
                        XmlNode deck = (XmlNode)deckNodes.Current;
                        if (!ignoredDecks.Contains(deck.Attributes["title"].Value))
                            pageids.Add(deck.Attributes["pageid"].Value);
                    } while (--k != 0 && deckNodes.MoveNext());
                    var revisionUrl = ApiQuery(new Dictionary<string, string>
                    {
                        { "prop", "revisions" },
                        { "rvprop", "content" },
                        { "pageids", string.Join("|", pageids) },
                    });
                    GetXmlResponse(revisionUrl, deckContents);
                    TerminateOnErrorOrWarning(deckContents, "Error while obtaining deck contents.");
                    foreach (XmlNode deckPage in deckContents.SelectNodes("/api/query/pages/page"))
                    {
                        List<string> cards = GetCards(deckPage.SelectSingleNode("revisions/rev").InnerText);
                        if (!cards.Any(x => removedCards.Contains(x)))
                        {
                            yield return new Deck(deckPage.Attributes["title"].Value, cards);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(deckPage.Attributes["title"].Value);
                        }
                    }
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
                if (trimmedCardLine.Length > 0)
                {
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

        private string ApiQuery(Dictionary<string, string> queryParameters = null)
        {
            var url = new StringBuilder(Site).Append("api.php?action=query&format=xml");
            if (queryParameters != null)
            {
                foreach (var entry in queryParameters)
                    url.Append('&').Append(entry.Key).Append('=').Append(entry.Value);
            }
            return url.ToString(); ;
        }

        private void GetXmlResponse(string url, XmlDocument response)
        {
            string responseContent = _client.DownloadString(url);
            response.Load(url);
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
    }
}
