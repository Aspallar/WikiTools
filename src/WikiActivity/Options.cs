using CommandLine;

namespace WikiActivity
{
    internal class Options
    {
        [Value(0, Required = false)]
        public string Ip { get; set; }

        [Option]
        public bool IpOnly { get; internal set; }

        [Option(Default = 50)]
        public int Limit { get; internal set; }

        [Option]
        public bool Count { get; internal set; }

        [Option]
        public bool NoIp { get; internal set; }
    }
}