using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadFilesTestServer
{
    internal class Options
    {
        [Option(Default = 0)]
        public int Exists { get; set; }

        [Option(Default = 0)]
        public int Delay { get; set; }
    }
}
