using System;
using System.Globalization;

namespace RatingPurge
{
    internal static class DateTimeOffsetExtensions
    {
        public static string ToWikiTimestamp(this DateTimeOffset dt)
        {
            return dt.ToString("s", CultureInfo.InvariantCulture) + "Z";
        }
    }
}
