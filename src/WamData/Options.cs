using CommandLine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WamData
{
    internal sealed class Options
    {
        [Option(HelpText = "Start date (defaults to start of current year)")]
        public string Start { get; set; }

        [Option(HelpText = "End date (defaults to today)")]
        public string End { get; set; }

        [Option(HelpText = "Show output on both stdout and stderr if stdout is redirected.")]
        public bool Verbose { get; set; }

        [Option(Default = "magicarena.fandom.com", HelpText = "Name of the wiki, must be exact i.e. magicarena.fandom.com not just magicarena")]
        public string Name { get; set; }

        [Option(Default = "games", HelpText = "all, tv, games, books, comics, lifestyle, music or films")]
        public string Type { get; set; }

        [Option(Default = 10, HelpText = "The max number of simultaneous requests (1 to 200)")]
        public int FirePower { get; set; }

        [Option(HelpText = "Display additional help.")]
        public bool MoreHelp { get; set; }

        [Option(HelpText = "List of columns to include in output. wamdate, date, rank, score.")]
        public IEnumerable<string> Columns { get; set; }

        private int _verticalType;
        public int VerticalType => _verticalType;

        private DateTimeOffset _startDate;
        public DateTimeOffset StartDate => _startDate;

        private DateTimeOffset _endDate;
        public DateTimeOffset EndDate => _endDate;

        private ColumnFlags _columnFlags;
        public ColumnFlags ColumnFlags => _columnFlags;

        private static string[] dateFormats = { "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };

        public void Validate()
        {
            if (string.IsNullOrEmpty(Start))
                _startDate = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, new TimeSpan(0));
            else if (!DateTimeOffset.TryParseExact(Start, dateFormats, CultureInfo.InvariantCulture,  DateTimeStyles.AssumeUniversal, out _startDate))
                throw new OptionsException($"Invalid Start date format.\n{DateFormatsHelp}");

            if (string.IsNullOrEmpty(End))
                _endDate = new DateTimeOffset(DateTime.Today.Date, new TimeSpan(0));
            else if (!DateTimeOffset.TryParseExact(End, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _endDate))
                throw new OptionsException($"Invalid end date format.\n{DateFormatsHelp}");

            if (_startDate > _endDate)
                throw new OptionsException("End date is before start date and that's just silly.");

            _verticalType = GetVeticalType(Type);
            if (_verticalType == -1)
                throw new OptionsException("Invalid WikiType");

            if (FirePower < 1 || FirePower > 200)
                throw new OptionsException("Invalid firepower");

            _columnFlags = SetColumns(Columns);
        }

        private ColumnFlags SetColumns(IEnumerable<string> columns)
        {
            ColumnFlags flags = 0;
            foreach (var column in columns)
            {
                switch (column.ToLowerInvariant())
                {
                    case "wamdate": flags |= ColumnFlags.WamDate; break;
                    case "date": flags |= ColumnFlags.Date; break;
                    case "rank": flags |= ColumnFlags.Rank; break;
                    case "score": flags |= ColumnFlags.Score; break;
                    default:
                        throw new OptionsException($"Unknown column name {column}");
                }
            }
            if (flags == 0)
                flags = ColumnFlags.WamDate | ColumnFlags.Date | ColumnFlags.Rank | ColumnFlags.Score;
            return flags;
        }

        private static int GetVeticalType(string code)
        {
            code = code.ToLowerInvariant();
            var types = new string[] { "all", "tv", "games", "books", "comics", "lifestyle", "music", "films" };
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].StartsWith(code))
                    return i;
            }
            return -1;
        }

        private static string DateFormatsHelp
        {
            get
            {
                var s = new StringBuilder("Must be one of ");
                for (int i = 0; i < dateFormats.Length - 1; i++)
                {
                    s.Append(dateFormats[i]);
                    s.Append(' ');
                }
                s.Append("or ");
                s.Append(dateFormats[dateFormats.Length - 1]);
                return s.ToString();
            }
        }

    }
}