using System;

namespace GetRatings
{
    internal class DeckTotal
    {
        public string Name { get; set; }
        public int Total { get; set; }
        public int Votes { get; set; }

        public double Average
        {
            get
            {
                return Math.Round((double)Total / (double)Votes, 2);
            }
        }

        public int Score
        {
            get
            {
                return (int)Math.Round((double)Total / (double)Votes, 0);
            }
        }
    }
}