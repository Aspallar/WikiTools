using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WikiActivity
{
    internal class IpDetails
    {
        public string Ip { get; set; }
        public string Country { get; set; }
        public string City { get; set; }

        public IpDetails(XmlDocument details)
        {
            var query = details.SelectSingleNode("/query");
            Ip = query.SelectSingleNode("query")?.InnerText;
            Country = query.SelectSingleNode("country")?.InnerText;
            if (Country == "United States")
            {
                XmlNode regionName = query.SelectSingleNode("regionName");
                Country += regionName != null ? $" ({regionName.InnerText})" : " (Unknown";
            }
            City = query.SelectSingleNode("city")?.InnerText;
        }
    }
}
