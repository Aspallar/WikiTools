using CommandLine;

namespace TourneyPairings
{
    internal class Options
    {
        [Value(0, Default = "round.txt")]
        public string InputFileName { get; internal set; }
    }
}