using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using WikiToolsShared;

namespace DuplicateDecks
{
    class Program
    {
        const int _batchSize = 50;

        [STAThread]
        static void Main(string[] args)
        {
            Utils.InitializeTls();
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed((options) => Run(options));
            }
            catch (DuplicateDecksException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            catch (IOException ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Console.Error.WriteLine(ex.Message);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static void Run(Options options)
        {
            List<Deck> decks = GetAllDecks(options.Files, options.Clipboard);
            if (options.Merged)
                MergeSideboards(decks);
            IResultWriter rw = options.Html ? (IResultWriter)new HtmlResultWriter() : (IResultWriter)new PlainResultWriter();
            if (string.IsNullOrEmpty(options.Title))
                AllDecks(decks, !options.NoSideboard, rw);
            else
                OneDeck(decks, options.Title, !options.NoSideboard, rw);
        }

        private static List<Deck> GetAllDecks(IEnumerable<string> fileNames, bool clipboard)
        {
            var localDecks = ReadLocalDecks(fileNames);
            if (clipboard)
            {
                var text = Clipboard.GetText();
                if (text == "")
                    throw new DuplicateDecksException("The clipboard does not contain any text.");
                localDecks.Add(Deck.ParseDeckExport("(Clipboard)", text));
            }
            var decks = GetDecks().ToList();
            decks.AddRange(localDecks);
            return decks;
        }

        private static List<Deck> ReadLocalDecks(IEnumerable<string> files)
        {
            var localDecks = new List<Deck>();
            if (files != null)
            {
                foreach (var fileName in files)
                {
                    var contents = File.ReadAllText(fileName);
                    localDecks.Add(Deck.ParseDeckExport("Local: " + fileName, contents));
                }
            }
            return localDecks;
        }

        private static void MergeSideboards(List<Deck> decks)
        {
            foreach (var deck in decks)
                deck.MergeSideboardIntoMain();
        }

        private static void AllDecks(List<Deck> decks, bool useSideboard, IResultWriter rw)
        {
            int count = 0;
            rw.Header();
            while (decks.Count > 1)
            {
                var wanted = decks[0];
                var dups = DuplicatesQuery(decks, wanted, useSideboard).ToList();
                if (dups.Count > 1)
                {
                    rw.Group(++count);
                    foreach (var deck in dups)
                        rw.Item(deck);
                    decks = decks.Except(dups).ToList();
                }
                else
                {
                    decks.RemoveAt(0);
                }
            }
            rw.Footer();
        }

        private static void OneDeck(List<Deck> decks, string title, bool useSideboard, IResultWriter rw)
        {
            var wanted = decks.Where(x => x.Title == title).FirstOrDefault();
            if (wanted != null)
            {
                var dups = DuplicatesQuery(decks, wanted, useSideboard).ToList();
                if (dups.Count > 1)
                {
                    rw.Header();
                    foreach (var deck in dups)
                        rw.Item(deck);
                    rw.Footer();
                }
            }
            else
            {
                throw new DuplicateDecksException($"{title} not found.");
            }
        }

        private static IEnumerable<Deck> GetDecks()
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
                apfrom = decks.SelectSingleNode("/api/query-continue/allpages")?.Attributes["apfrom"]?.Value;
                var deckNodes = deckPages.GetEnumerator();
                while (deckNodes.MoveNext())
                {
                    var revisionUrl = ApiQuery(new Dictionary<string, string>
                    {
                        { "prop", "revisions" },
                        { "rvprop", "content" },
                        { "pageids", PageIdBatch(deckNodes) },
                    });
                    GetXmlResponse(revisionUrl, deckContents);
                    TerminateOnErrorOrWarning(deckContents, "Error while obtaining deck contents.");
                    foreach (XmlNode deckPage in deckContents.SelectNodes("/api/query/pages/page"))
                    {
                        Deck deck = Deck.ParseWikiDeck(deckPage.Attributes["title"].Value, CardText(deckPage.SelectSingleNode("revisions/rev").InnerText));
                        if (deck.HasCards)
                            yield return deck;
                    }
                }
            } while (!string.IsNullOrEmpty(apfrom));
        }

        private static string PageIdBatch(System.Collections.IEnumerator deckNodes)
        {
            var pageids = new List<string>();
            int k = _batchSize;
            do
            {
                XmlNode deck = (XmlNode)deckNodes.Current;
                pageids.Add(deck.Attributes["pageid"].Value);
            } while (--k != 0 && deckNodes.MoveNext());
            return string.Join("|", pageids);
        }

        private static string CardText(string content)
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

        private static string ApiQuery(Dictionary<string, string> queryParameters = null)
        {
            var url = new StringBuilder("https://magicarena.fandom.com/").Append("api.php?action=query&format=xml");
            if (queryParameters != null)
            {
                foreach (var entry in queryParameters)
                    url.Append('&').Append(entry.Key).Append('=').Append(entry.Value);
            }
            return url.ToString(); ;
        }

        private static void GetXmlResponse(string url, XmlDocument response)
        {
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

        private static IEnumerable<Deck> DuplicatesQuery(List<Deck> decks, Deck wanted, bool useSideboard)
        {
            var query = decks.Where(x => x.HasSameMain(wanted));
            if (useSideboard)
                query = query.Where(x => x.HasSameSideboard(wanted));
            return query;
        }

    }
}
