using System.Collections.Generic;

namespace DeckCards
{
    internal class Deck
    {
        public string Title { get; private set; }
        public List<string> Cards { get; private set; }

        public Deck(string title, List<string> cards)
        {
            Title = title;
            Cards = cards;
        }
    }
}