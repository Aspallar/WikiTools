using System;

namespace DuplicateDecks
{
    internal class PlainResultWriter : IResultWriter
    {
        public void Footer() { }

        public void Group(int count)
        {
            Console.WriteLine();
            Console.WriteLine(count);
        }

        public void Header() { }

        public void Item(Deck deck) => Console.WriteLine(deck.Title);
    }
}
