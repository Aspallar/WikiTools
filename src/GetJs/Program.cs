using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using WikiToolsShared;

namespace GetJs
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Utils.InitializeTls();
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static void Run(Options options)
        {
            foreach (var jsPage in JsPages(options.Site, options.Delay))
                Save(jsPage, options.Folder);
        }

        private static IEnumerable<Page> JsPages(string site, int delay)
        {
            bool more;
            string urlBase = site + "/api.php?action=query&format=xml&generator=allpages&gapnamespace=8&prop=revisions&rvprop=content&gaplimit=50&gapfrom=";
            var doc = new XmlDocument();
            string gapfrom = "";
            do
            {
                doc.Load(urlBase + gapfrom);
                gapfrom = doc.SelectSingleNode("/api/query-continue/allpages")?.Attributes["gapfrom"]?.Value;
                var pages = doc.SelectNodes("/api/query/pages/page");
                foreach (XmlNode page in pages)
                {
                    if (page.Attributes["title"].Value.EndsWith(".js"))
                        yield return new Page(page.Attributes["title"].Value, page.SelectSingleNode("revisions/rev").InnerText);
                }
                more = !string.IsNullOrEmpty(gapfrom);
                if (more && delay > 0) Thread.Sleep(delay);
            } while (more);
        }

        private static void Save(Page page, string folder)
        {
            string fileName = $"{folder}\\{ReplaceInvalidChars(page.Name.Replace('/', '_'))}";
            Console.WriteLine($"{page.Title} => {fileName}");
            using (var sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine($"// {page.Title}\n//");
                sw.WriteLine(page.Content);
            }
        }

        private static string ReplaceInvalidChars(string filename)
        {
            return string.Join("__", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
