using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DeckCards
{
    internal static class Markup
    {
        public static string UpdatedOn(DateTime dt)
        {
            return $"Updated on {dt.ToString("dddd, dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture)} UTC";
        }

        public static string GetMarkup(Dictionary<string, List<string>> cards)
        {
            List<string> deckNames = new List<string>();
            StringBuilder markup = new StringBuilder(300 * cards.Count);
            char currentFirstLetter = '*';
            List<string> sortedCards = cards.Keys.OrderBy(x => x).ToList();
            markup.AppendLine("<div id=\"mdw-cardsindecks-container\" style=\"margin-left:60px\">");
            foreach (var card in sortedCards)
            {
                var deckIds = new List<int>();
                var decks = cards[card].OrderBy(x => x).ToList();
                char firstLetter = char.ToLowerInvariant(card[0]);
                if (firstLetter != currentFirstLetter)
                {
                    AppendAnchorDivAndTitle(markup, firstLetter);
                    currentFirstLetter = firstLetter;
                }
                AppendCardRow(markup, card, decks);
                foreach (var deck in decks)
                {
                    string noPrefix = deck.Substring(6);
                    int index = deckNames.FindIndex(x => x == noPrefix);
                    if (index == -1)
                    {
                        deckNames.Add(noPrefix);
                        index = deckNames.Count - 1;
                    }
                    deckIds.Add(index);
                }
                AppendDecksRow(markup, deckIds);
            }
            markup.AppendLine("</div>");
            markup.Insert(0, DeckDataPreTag(deckNames));
            return markup.ToString();
        }

        private static StringBuilder DeckDataPreTag(List<string> deckNames)
        {
            StringBuilder sb = new StringBuilder(16 * deckNames.Count);
            sb.Append("<pre id=\"mdw-deck-data\" style=\"display:none;\">[");
            foreach (string deckName in deckNames)
            {
                sb.Append('"');
                if (deckName.IndexOf('"') == -1)
                    sb.Append(deckName);
                else
                    sb.Append(deckName.Replace("\"","\\\""));
                sb.Append("\",");
            }
            sb[sb.Length - 1] = ']';
            sb.AppendLine("</pre>");
            return sb;
        }

        private static void AppendDecksRow(StringBuilder markup, List<int> deckIds)
        {
            markup.Append("<div style=\"display:none\" data-decks=\"[");
            foreach (int id in deckIds)
            {
                markup.Append(id);
                markup.Append(',');
            }
            markup[markup.Length - 1] = ']';
            markup.AppendLine("\"></div>");
        }

        private static void AppendCardRow(StringBuilder markup, string card, List<string> decks)
        {
            markup.Append("<div><span class=\"mdw-cardsindecks-arrow\"></span> '''{{Card|");
            markup.Append(card);
            markup.Append("}}''' (");
            markup.Append(decks.Count);
            markup.AppendLine(")</div>");
        }

        private static void AppendAnchorDivAndTitle(StringBuilder markup, char firstLetter)
        {
            markup.Append("<div id=\"mdw");
            markup.Append(firstLetter);
            markup.Append("\"></div>\n== ");
            markup.Append(char.ToUpperInvariant(firstLetter));
            markup.AppendLine(" ==");
        }

    }
}
