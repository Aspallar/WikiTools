using System.Text.RegularExpressions;

namespace WikiToolsShared
{
    public class RatingsEntry
    {
        static readonly Regex commentRegex = new Regex(@"Rating for (?:\[\[.*\|)?([^\]]+)(?:\]\])? \((\d)\)");

        public string DeckName { get; private set; }
        public string Vote { get; private set; }
        public bool IsValid => DeckName != null;
        public static bool IsEntry(string comment) => commentRegex.IsMatch(comment);

        public RatingsEntry(string comment)
        {
            Match match = commentRegex.Match(comment);
            if (match.Success)
            {
                DeckName = match.Groups[1].Value;
                Vote = match.Groups[2].Value;
            }
            else
            {
                DeckName = Vote = null;
            }
        }

    }
}
