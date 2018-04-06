using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RatingPurge
{
    internal class RatingsHistory
    {
        private string _urlBase;

        public RatingsHistory(string site, string page, int days)
        {
            _urlBase = site +
                "/api.php?action=query&prop=revisions&titles=" +
                page +
                "&rvprop=ids|timestamp|user|comment&rvlimit=500&format=xml&rvend="
                + GetRvend(days);
        }

        public IEnumerable<XmlNode> Items
        {
            get
            {
                var ratings = new XmlDocument();
                string rvstartid = null;

                do
                {
                    ratings.Load(GetUrl(rvstartid));
                    var revisionHistory = ratings.SelectNodes("/api/query/pages/page/revisions/rev");
                    foreach (XmlNode revision in revisionHistory)
                        yield return revision;
                    rvstartid = GetContinueId(ratings);
                } while (rvstartid != null);
            }
        }

        private static string GetContinueId(XmlDocument ratings)
        {
            XmlNodeList queryContinue = ratings.SelectNodes("/api/query-continue/revisions");
            if (queryContinue.Count == 0)
                return null;
            return queryContinue[0].Attributes["rvstartid"].Value;
        }

        private string GetUrl(string rvstartid)
        {
            var url = new StringBuilder(_urlBase);
            if (rvstartid != null)
            {
                url.Append("&rvstartid=");
                url.Append(rvstartid);
            }
            url.Append("&random=");
            url.Append(DateTime.Now.Ticks.ToString());
            return url.ToString(); ;
        }

        private static string GetRvend(int days)
        {
            if (days < 0)
                return "2018-03-26T19:10:23Z"; // when ratings went live

            DateTimeOffset startOfToday = new DateTimeOffset(DateTime.UtcNow.Date, new TimeSpan(0));
            DateTimeOffset wantedStart = startOfToday.AddDays(-days);
            return wantedStart.ToWikiTimestamp();
        }

    }
}
