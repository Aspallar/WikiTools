using System;

namespace DeckRatings
{
    internal class InvalidVote
    {
        public string RevId { get; internal set; }
        public string Comment { get; set; }
        public string User { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}