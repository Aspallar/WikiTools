//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace WamData
//{
//    class WamDataFetcher
//    {
//        string userAgent;
//        int cancelCount = 0;
//        string urlBase;
//        string siteSearchTerm;
//        DaysRange range;
//        DateTimeOffset endDate;
//        List<WamItem> results = new List<WamItem>();
//        List<WamError> errors = new List<WamError>();

//        public WamDataFetcher(string name, int verticalType, DateTimeOffset firstDate, DateTimeOffset lastDate, string userAgent)
//        {
//            urlBase = UrlBase(name, verticalType);
//            siteSearchTerm = "https://" + name;
//            range = new DaysRange(firstDate);
//            endDate = lastDate;
//            RunFetchWamDataTasks(options.FirePower).GetAwaiter().GetResult();
//        }

//        private static string UrlBase(string name, int verticalId)
//        {
//            string url = Properties.Settings.Default.UrlFormat;
//            return string.Format(url, name, verticalId);
//        }



//    }
//}
