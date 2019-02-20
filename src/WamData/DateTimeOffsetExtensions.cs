using System;
using System.Globalization;

namespace WamData
{
    static class DateTimeOffsetExtensions
    {
        public static long ToWamTime(this DateTimeOffset dto)
        {
            return dto.ToUnixTimeMilliseconds() / 1000;
        }

        public static string ToWamHumanTime(this DateTimeOffset dto)
        {
            return dto.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        }
    }
}
