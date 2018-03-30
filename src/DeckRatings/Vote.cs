using System;

namespace DeckRatings
{
    internal class Vote
    {
        public string RevId { get; internal set; }
        public int Score { get; set; }
        public string DeckName { get; set; }
        public string User { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
