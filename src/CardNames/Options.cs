using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardNames
{
    internal class Options
    {
        [Value(0, HelpText = "Input filesname. Reads input from console if omitted.")]
        public string InputFileName { get; set; }

        [Option(Default = ".", HelpText = "Words prefixed with this will not be expanded.")]
        public string Prefix { get; set; }
    }
}
