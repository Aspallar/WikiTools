using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadFiles
{
    internal class WikiaUri : Uri
    {
        public WikiaUri(string wikiSite) : base(wikiSite) { }

        public Uri ApiQuery(string parameters)
        {
            return new Uri(this, "api.php?action=query&format=xml&" + parameters);
        }

        public Uri Article(string title)
        {
            return new Uri(this, "wiki/" + title);
        }

        public Uri RawArticle(string title)
        {
            return new Uri(this, "wiki/" + title + "?action=raw");
        }
    }
}
