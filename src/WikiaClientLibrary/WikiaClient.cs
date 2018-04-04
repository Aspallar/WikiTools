using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WikiaClientLibrary
{
    public class WikiaClient : IDisposable
    {
        private ExtendedWebClient _client;

        public string Site { get; private set; }

        public WikiaClient(string site, string userAgent)
        {
            Site = site;
            if (!Site.EndsWith("/"))
                Site += "/";
            _client = new ExtendedWebClient();
            _client.UserAgent = userAgent;
        }

        public void CancelAsync()
        {
            _client.CancelAsync();
        }

        public bool Login(string username, string password)
        {
            try
            {
                NameValueCollection userData = new NameValueCollection();
                userData.Add("lgname", username);
                LoginResponse response = AttemptLogin(userData);
                if (response.Result == ResponseResults.Success)
                    return true;
                if (response.Result != ResponseResults.NeedToken)
                    return false;
                userData.Add("lgpassword", password);
                userData.Add("lgtoken", response.Token);
                response = AttemptLogin(userData);
                return response.Result == ResponseResults.Success;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                NameValueCollection userData = new NameValueCollection();
                userData.Add("lgname", username);
                LoginResponse response = await AttemptLoginAsync(userData);
                if (response.Result == ResponseResults.Success)
                    return true;
                if (response.Result != ResponseResults.NeedToken)
                    return false;
                userData.Add("lgpassword", password);
                userData.Add("lgtoken", response.Token);
                response = await AttemptLoginAsync(userData);
                return response.Result == ResponseResults.Success;
            }
            catch (WebException)
            {
                return false;
            }
        }

        private LoginResponse AttemptLogin(NameValueCollection data)
        {
            string result = _client.UpLoadValuesStringResponse(ApiUrl("login"), data);
            return ParseLoginRespose(result);
        }

        private async Task<LoginResponse> AttemptLoginAsync(NameValueCollection data)
        {
            string result = await _client.UpLoadValuesStringResponseTaskAsync(ApiUrl("login"), data);
            return ParseLoginRespose(result);
        }

        private LoginResponse ParseLoginRespose(string result)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<LoginResponseRoot>(result);
                return response.Login;
            }
            catch (JsonReaderException)
            {
                return new LoginResponse()
                {
                    Result = "BadResponse",
                    Token = "",
                };
            }
        }

        public string GetPageContents(string title)
        {
            string contents = _client.DownloadString(IndexUrl("&title=" + title));
            return contents;
        }

        public PageInfo GetPageInfoWithContent(string title)
        {
            string result = _client.DownloadString(ApiPageInfoWithContentUrl(title));
            return ParsePageInfo(result);
        }

        public async Task<PageInfo> GetPageInfoWithContentAsync(string title)
        {
            string result = await _client.DownloadStringTaskAsync(ApiPageInfoWithContentUrl(title));
            return ParsePageInfo(result);
        }

        public EditInfo GetEditToken(string title)
        {
            string result = _client.DownloadString(ApiEditTokenUrl(title));
            var pageInfo = ParsePageInfo(result);
            return ExtractEditInfo(pageInfo);
        }

        public async Task<EditInfo> GetEditTokenAsync(string title)
        {
            string result = await _client.DownloadStringTaskAsync(ApiEditTokenUrl(title));
            var pageInfo = ParsePageInfo(result);
            return ExtractEditInfo(pageInfo);
        }

        private static EditInfo ExtractEditInfo(PageInfo pageInfo)
        {
            return new EditInfo()
            {
                EditToken = pageInfo.EditToken,
                StartTimeStamp = pageInfo.StartTimeStamp,
            };
        }

        public void SavePage(string title, PageInfo pageInfo, string summary)
        {
            NameValueCollection data = MakeSaveData(title, pageInfo, summary);
            string result = _client.UpLoadValuesStringResponse(ApiUrl("edit"), data);
            ThrowIfError(result);
        }

        public async Task SavePageAsync(string title, PageInfo pageInfo, string summary)
        {
            NameValueCollection data = MakeSaveData(title, pageInfo, summary);
            string result = await _client.UpLoadValuesStringResponseTaskAsync(ApiUrl("edit"), data);
            ThrowIfError(result);
        }

        public string Query(string query)
        {
            string url = ApiQuery(query);
            return _client.DownloadString(url);
        }

        public async Task<string> QueryAsync(string query)
        {
            string url = ApiQuery(query);
            return await _client.DownloadStringTaskAsync(url);
        }

        private void ThrowIfError(string result)
        {
            if (result.StartsWith("{\"edit\":{\"result\":\"Success\""))
                return;
            if (result.StartsWith("{\"edit\":{\"new\":\"\",\"result\":\"Success\""))
                return;
            int errorStart = result.IndexOf("{\"code\":");
            int errorEnd = result.IndexOf("}", errorStart + 1);
            if (errorStart != -1 && errorEnd != -1)
            {
                string errorText = result.Substring(errorStart, errorEnd - errorStart + 1);
                var error = JsonConvert.DeserializeObject<EditError>(errorText);
                if (error.Code == "editconflict")
                    throw new WikiaEditConflictException();
                throw new WikiaEditException(error);
            }
            else throw new WikiaUnknownResponseException("Unknown response from edit request");
        }

        private NameValueCollection MakeSaveData(string title, PageInfo pageInfo, string summary)
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("title", title);
            if (pageInfo.TimeStamp != null)
                data.Add("basetimestamp", pageInfo.TimeStamp);
            if (!string.IsNullOrEmpty(summary))
                data.Add("summary", summary);
            data.Add("text", pageInfo.Contents);
            data.Add("token", pageInfo.EditToken);
            return data;
        }

        private PageInfo ParsePageInfo(string result)
        {
            var root = JsonConvert.DeserializeObject<PageInfoRootResponse>(result);
            PageInfoResponse response = root.Query.Pages.First().Value;
            var pageInfo = new PageInfo()
            {
                Exists = response.PageExists,
                StartTimeStamp = response.StartTimeStamp,
                EditToken = response.EditToken,
            };
            if (response.PageExists && response.Revisions != null)
            {
                RevisionResponse revision = response.Revisions[0];
                pageInfo.TimeStamp = revision.TimeStamp;
                pageInfo.Contents = revision.Contents;
            }
            return pageInfo;
        }

        private string ApiEditTokenUrl(string title)
        {
            return ApiUrl("query&prop=info&intoken=edit&rvprop=timestamp&format=jsonfm&titles=" + title);
        }

        private string ApiPageInfoWithContentUrl(string title)
        {
            return ApiUrl("query&prop=info|revisions&intoken=edit&rvprop=content|timestamp&format=json&titles=" + title);
        }

        public async Task<string> GetPageContentsAsync(string title)
        {
            string contents = await _client.DownloadStringTaskAsync(IndexUrl("&title=" + title));
            return contents;
        }

        private string ApiUrl(string queryString)
        {
            return Site + "api.php?action=" + queryString + "&format=json";
        }

        private string IndexUrl(string queryString)
        {
            return Site + "index.php?action=raw" + queryString;
        }

        private string ApiQuery(string query)
        {
            return Site + "api.php?action=query" + query;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                CancelAsync();
                _client.Dispose();
                _client = null;
            }
        }
    }
}
