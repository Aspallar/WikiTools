using CommandLine;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using WikiToolsShared;

namespace PageContents
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Utils.InitializeTls();
                Console.OutputEncoding = Encoding.UTF8;
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (WebException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static void Run(Options options)
        {
            string content;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                content = client.DownloadString($"{options.Site}/{options.WikiPath}/{options.Page}?action=raw&cb={DateTime.Now.Ticks}");
            }
            Console.WriteLine(content);
        }

        private static void ShowUsage()
        {
            Console.WriteLine("PageContents <title>");
        }
    }
}
