using System;

namespace WamData
{
    [Flags]
    internal enum ColumnFlags
    {
        WamDate = 1,
        Date = 2,
        Rank = 4,
        Score = 8
    }
}
