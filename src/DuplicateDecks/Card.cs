namespace DuplicateDecks
{
    class Card
    {
        public int Amount { get; set; }
        public string Name { get; set; }

        public override int GetHashCode()
        {
            return CombineHashCodes(Amount.GetHashCode(), Name.GetHashCode());
        }

        public bool EqualTo(Card other)
        {
            return Amount == other.Amount && Name == other.Name;
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
}
