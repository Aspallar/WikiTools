using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WikiToolsShared;

namespace PageContents
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length != 1 || args[0] == "--help")
            {
                ShowUsage();
                return;
            }
            Utils.InitializeTls();
            string content;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                content = client.DownloadString($"https://magicarena.fandom.com/wiki/{args[0]}?action=raw");
            }

            Console.WriteLine(content);
        }

        private static void ShowUsage()
        {
            Console.WriteLine("PageContents <title>");
        }
    }
}
