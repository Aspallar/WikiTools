using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WikiActivity
{
    internal class Activity
    {
        public DateTime Timestamp { get; private set; }
        public string User { get; private set; }
        public string Comment { get; private set; }
        public string Title { get; private set; }

        public bool IsAnonymous { get; private set; }

        public Activity(XmlNode recentEntry)
        {
            IsAnonymous = recentEntry.Attributes["anon"] != null;
            User = recentEntry.Attributes["user"].Value;
            Title = recentEntry.Attributes["title"].Value;
            Comment = recentEntry.Attributes["comment"].Value;
            Timestamp = DateTime.Parse(recentEntry.Attributes["timestamp"].Value);
        }
    }
}
