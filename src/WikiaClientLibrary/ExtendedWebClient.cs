using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WikiaClientLibrary
{
    public class ExtendedWebClient : WebClient
    {
        public string UserAgent { get; set; }
        public CookieContainer Cookies { get; set; }

        public ExtendedWebClient()
        {
            Cookies = new CookieContainer();
            UserAgent = "";
        }

        public string UpLoadValuesStringResponse(string address, NameValueCollection values)
        {
            byte[] result = UploadValues(address, values);
            return Encoding.UTF8.GetString(result);
        }

        public async Task<string> UpLoadValuesStringResponseTaskAsync(string address, NameValueCollection values)
        {
            byte[] result = await UploadValuesTaskAsync(address, values);
            return Encoding.UTF8.GetString(result);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = Cookies;
            request.UserAgent = UserAgent;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
        
        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            PreserveCookies(response);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            PreserveCookies(response);
            return response;
        }

        private void PreserveCookies(WebResponse webResponse)
        {
            var response = (HttpWebResponse)webResponse;
            Cookies.Add(response.Cookies);
        }
    }
}