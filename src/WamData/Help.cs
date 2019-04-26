namespace WamData
{
    internal static class Help
    {
        public const string HelpText = @"WamData more help...

Switches
--------
--start        Start date (defaults to start of current year)
--end          End date (defaults to today)

The start and end dates (inclusive) to fetch data for. The dates must be in
in one of the following formats.

     d/m/yyyy
     dd/mm/yyyy
     d.m.yyyy
     dd.mm.yyyy
     yyyy-mm-dd

Output Format
-------------
The output is four columns in CSV format

    Column 0 - WAM date, this is the date value WAM uses
    Column 1 - Human readable vesion of the date, always in dd/mm/yyyy format.
    Column 2 - The rank
    Column 3 - The score

If --type is ""all"" the the rank is the rank accross all wikis, if it is any
other type then the rank is the verical rank in that category.

Example:

    1548979200,01/02/2019,487,74.23
    1549065600,02/02/2019,429,77.32
    1549152000,03/02/2019,428,77.30

You can specify what columns to include in the output with the columns switch

e.g.

WamData --columns wamdate date rank score --start 2019-02-01
WamData --columns rank --start 2019-02-01
";
    }
}
