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
            StringBuilder markup = new StringBuilder(350 * 1024);
            char currentFirstLetter = '*';
            List<string> sortedCards = cards.Keys.OrderBy(x => x).ToList();
            markup.AppendLine("<div style=\"margin-left:60px\">");
            foreach (var card in sortedCards)
            {
                var decks = cards[card].OrderBy(x => x).ToList();
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

    }
}
