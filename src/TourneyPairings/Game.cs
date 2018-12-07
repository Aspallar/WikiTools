using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TourneyPairings
{
    class Game
    {
        public Game(GroupCollection groups)
        {
            Number = groups[1].Value.Trim();
            Player1 = groups[2].Value.Trim();
            Player2 = groups[3].Value.Trim();
        }

        public String Number { get; set; }
        public String Player1 { get; set; }
        public String Player2 { get; set; }


    }
}
