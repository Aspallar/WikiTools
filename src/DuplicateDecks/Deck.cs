using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DuplicateDecks
{
    internal sealed class Deck
    {
        private List<Card> _main;
        private List<Card> _sideboard;
        
        public string Title { get; private set; }

        public IReadOnlyList<Card> Main => _main;

        public IReadOnlyList<Card> Sideboard => _sideboard;

        public bool IsWikiDeck { get; private set; }

        public Deck(string title, List<Card> cards, List<Card> sideboard, bool isWikiDeck)
        {
            Title = title;
            _main = cards;
            _sideboard = sideboard;
            IsWikiDeck = isWikiDeck;
        }

        public bool HasCards => _main.Count > 0 || _sideboard.Count > 0;

        public void MergeSideboardIntoMain()
        {
            if (_sideboard.Count != 0)
            {
                bool needsSort = false;
                foreach (var card in _sideboard)
                {
                    var mainCard = _main.Where(x => x.Name == card.Name).FirstOrDefault();
                    if (mainCard == null)
                    {
                        _main.Add(card);
                        needsSort = true;
                    }
                    else
                    {
                        mainCard.Amount += card.Amount;
                    }
                }
                _sideboard.Clear();
                if (needsSort)
                    _main = Sort(_main);
            }
        }

        public bool HasSameMain(Deck other) => Same(_main, other._main);

        public bool HasSameSideboard(Deck other) => Same(_sideboard, other._sideboard);

        private static bool Same(List<Card> list1, List<Card> list2)
        {
            if (list1.Count != list2.Count)
                return false;
            for (int k = 0; k < list1.Count; k++)
            {
                if (!list1[k].EqualTo(list2[k]))
                    return false;
            }
            return true;
        }

        private static readonly Regex cardRegex = new Regex(@"(\d+)\s+([^\(/]+)");

        public static Deck ParseWikiDeck(string title, string cardText)
        {
            char[] newline = { '\n' };
            var main = new List<Card>();
            var sideboard = new List<Card>();
            var active = main;

            var cardLines = cardText.Split(newline, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => x.Length > 0);

            foreach (string cardLine in cardLines)
            {
                if (!cardLine.StartsWith("--"))
                {
                    Match match = cardRegex.Match(cardLine);
                    if (match.Success)
                        AddCard(active, match);
                }
                else
                {
                    active = sideboard;
                }
            }
            main = Sort(main);
            sideboard = Sort(sideboard);
            return new Deck(title, main, sideboard, true);
        }

        public static Deck ParseDeckExport(string title, string cardText)
        {
            char[] newline = { '\n' };
            var main = new List<Card>();
            var sideboard = new List<Card>();
            var active = main;

            var cardLines = cardText.Trim().Split(newline).Select(x => x.Trim().ToLowerInvariant());
            if (cardLines.Any(x => x == "commander" || x == "deck" || x == "sideboard"))
                cardLines = cardLines.Where(x => x.Length > 0 && x != "commander" && x != "deck");

            foreach (string cardLine in cardLines)
            {
                if (cardLine.Length > 0)
                {
                    Match match = cardRegex.Match(cardLine);
                    if (match.Success)
                        AddCard(active, match);
                    else if (cardLine == "sideboard")
                        active = sideboard;
                }
                else
                {
                    active = sideboard;
                }
            }
            main = Sort(main);
            sideboard = Sort(sideboard);
            return new Deck(title, main, sideboard, false);
        }

        private static void AddCard(List<Card> active, Match match)
        {
            int amount = int.Parse(match.Groups[1].Value);
            string name = match.Groups[2].Value.Trim();
            var card = active.FirstOrDefault(x => x.Name == name);
            if (card != null)
                card.Amount += amount;
            else
                active.Add(new Card { Amount = amount, Name = name });
        }

        private static List<Card> Sort(List<Card> list)
        {
            return list.OrderBy(x => x.Name).ToList();
        }

    }
}