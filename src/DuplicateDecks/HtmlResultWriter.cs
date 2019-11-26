using System;

namespace DuplicateDecks
{
    internal class HtmlResultWriter : IResultWriter
    {
        public void Footer()
        {
            Console.WriteLine("</body>");
            Console.WriteLine("</html>");
        }

        public void Group(int count)
        {
            Console.WriteLine("<br />");
            Console.Write(count);
            Console.WriteLine("<br />");
        }

        public void Header()
        {
            Console.WriteLine("<html>");
            Console.WriteLine("<head>");
            Console.WriteLine("</head>");
            Console.WriteLine("<body>");
        }

        public void Item(Deck deck)
        {
            if (deck.IsWikiDeck)
                Console.WriteLine($"<a href=\"https://magicarena.fandom.com/wiki/{deck.Title}\" target=\"_blank\">{deck.Title}</a><br />");
            else
                Console.WriteLine($"{deck.Title}<br />");
        }
    }
}
