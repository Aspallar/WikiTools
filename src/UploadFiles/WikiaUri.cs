using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadFiles
{
    internal class WikiaUri
    {
        public Uri Api { get; private set; }
        public Uri Wiki { get; private set; }

        public WikiaUri(string wikiSite)
        {
            Api = new Uri(wikiSite + "/api.php");
            Wiki = new Uri(wikiSite + "/wiki");
        }

        public Uri ApiQuery(string parameters)
        {
            return new Uri(Api, "?action=query&format=xml&" + parameters);
        }

        public Uri Article(string title)
        {
            return new Uri(Wiki, "/" + title);
        }

        public Uri RawArticle(string title)
        {
            return new Uri(Wiki, "/" + title + "?action=raw");
        }
    }
}
