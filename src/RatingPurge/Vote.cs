using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatingPurge
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
