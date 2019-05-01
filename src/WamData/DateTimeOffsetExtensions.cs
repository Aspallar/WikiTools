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

        public static int InclusiveDaysUntil(this DateTimeOffset start, DateTimeOffset end)
        {
            return (int)(end - start).TotalDays + 1;
        }
    }
}
