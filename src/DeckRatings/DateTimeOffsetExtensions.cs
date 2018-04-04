using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeckRatings
{
    internal static class DateTimeOffsetExtensions
    {
        public static string ToWikiTimestamp(this DateTimeOffset dt)
        {
            return dt.ToString("s", CultureInfo.InvariantCulture) + "Z";
        }
    }
}
