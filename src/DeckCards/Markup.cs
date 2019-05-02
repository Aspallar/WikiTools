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
            markup.AppendLine("<div id=\"mdw-cardsindecks-container\">");
            foreach (var card in sortedCards)
            {
                var decks = cards[card].OrderBy(x => x).ToList();
                char firstLetter = char.ToLowerInvariant(card[0]);
                if (firstLetter != currentFirstLetter)
                {
                    if (currentFirstLetter != '*')
                        markup.Append("</div>");
                    AppendAnchorDivAndTitle(markup, firstLetter);
                    currentFirstLetter = firstLetter;
                }
                AppendCardRow(markup, card, decks.Count);
                List<int> deckIndexes = DeckIndexes(deckNames, decks);
                AppendDecksRow(markup, deckIndexes);
            }
            markup.AppendLine("</div>");
            markup.Insert(0, DeckDataPreTag(deckNames));
            return markup.ToString();
        }

        private static List<int> DeckIndexes(List<string> deckNames, List<string> decks)
        {
            var deckIndexes = new List<int>();
            foreach (var deck in decks)
            {
                string noPrefix = deck.Substring(6);
                int index = deckNames.FindIndex(x => x == noPrefix);
                if (index == -1)
                {
                    deckNames.Add(noPrefix);
                    index = deckNames.Count - 1;
                }
                deckIndexes.Add(index);
            }

            return deckIndexes;
        }

        private static StringBuilder DeckDataPreTag(List<string> deckNames)
        {
            StringBuilder sb = new StringBuilder(16 * deckNames.Count);
            sb.Append("<pre id=\"mdw-deck-data\" style=\"display:none;\">[");
            foreach (string deckName in deckNames)
            {
                sb.Append('"');
                sb.Append(deckName);
                if (deckName.IndexOf('"') != -1)
                    sb.Replace("\"", "\\\"", sb.Length - deckName.Length, deckName.Length);
                sb.Append("\",");
            }
            sb[sb.Length - 1] = ']';
            sb.AppendLine("</pre>");
            return sb;
        }

        private static void AppendDecksRow(StringBuilder markup, List<int> deckIndexes)
        {
            markup.Append("<div style=\"display:none\" data-decks=\"[");
            foreach (int id in deckIndexes)
                markup.Append(id).Append(',');
            markup[markup.Length - 1] = ']';
            markup.AppendLine("\"></div>");
        }

        private static void AppendCardRow(StringBuilder markup, string card, int numDecks)
        {
            markup.Append("<div><span class=\"mdw-cardsindecks-arrow\"></span> '''{{Card|")
                .Append(card)
                .Append("}}''' (")
                .Append(numDecks)
                .AppendLine(")</div>");
        }

        private static void AppendAnchorDivAndTitle(StringBuilder markup, char firstLetter)
        {

            markup.Append("<div id=\"mdw")
                .Append(firstLetter)
                .Append("\"></div>\n== ")
                .Append(char.ToUpperInvariant(firstLetter))
                .AppendLine(" ==\n<div style=\"margin-left: 60px\">");
        }
    }
}
