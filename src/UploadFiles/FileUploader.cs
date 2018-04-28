using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace UploadFiles
{
    internal class FileUploader : IDisposable
    {

        private static ILog log = LogManager.GetLogger(typeof(FileUploader));

        private ApiUri _api;
        private string _editToken;
        private string _defaultText;
        private HttpClient _client;
        private string _comment;
        private List<string> _permittedTypes;

        public FileUploader(string site, string defaultText, string category, string comment, int timeoutSeconds)
        {
            _defaultText = defaultText == null ? "" : defaultText;
            if (!string.IsNullOrEmpty(category))
                _defaultText += "\n[[Category:" + category + "]]";
            _comment = comment == null ? "" : comment;
            _api = new ApiUri(site);

            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (timeoutSeconds > 0)
                _client.Timeout = new TimeSpan(0, 0, timeoutSeconds);
        }

        public async Task<bool> LoginAsync(string username, string password, bool allFilesPermitted)
        {
            try
            {
                var loginParams = new RequestParameters();
                loginParams.Add("action", "login");
                loginParams.Add("format", "xml");
                loginParams.Add("lgname", username);
                LoginResponse response = await AttemptLoginAsync(loginParams);
                if (response.Result != ResponseCodes.Success)
                {
                    if (response.Result != ResponseCodes.NeedToken)
                        return false;
                    loginParams.Add("lgtoken", response.Token);
                    loginParams.Add("lgpassword", password);
                    response = await AttemptLoginAsync(loginParams);
                    if (response.Result != ResponseCodes.Success)
                        return false;
                }

                Task<bool> userConfirmed = IsUserConfirmedAsync(username);
                Task<bool> authorized = IsAuthorizedForUploadFilesAsync(username);
                if (allFilesPermitted)
                    Task.WaitAll(userConfirmed, authorized);
                else
                    Task.WaitAll(userConfirmed, authorized, GetPermittedTypes());

                if (!userConfirmed.Result || !authorized.Result)
                    return false;

                // must do edittoken last, otherwise it will be invalid
                _editToken = await GetEditTokenAsync();
                return !string.IsNullOrEmpty(_editToken);
            }
            catch (XmlException)
            {
                return false;
            }
            catch (AggregateException ex)
            {
                ex.Handle(x => x is XmlException);
                return false;
            }
        }

        public bool IsPermittedFile(string filePath)
        {
            if (_permittedTypes == null)
                return true;
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return _permittedTypes.Contains(extension);
        }

        public async Task<UploadResponse> UpLoadAsync(string file, bool force = false)
        {
            string filename = Path.GetFileName(file);
            using (var actionContent = new StringContent("upload"))
            using (var fileNameContent = new StringContent(filename))
            using (var editTokenContent = new StringContent(_editToken))
            using (var textContent = new StringContent(_defaultText))
            using (var commentContent = new StringContent(_comment))
            using (var formatContent = new StringContent("xml"))
            using (FileStream fs = File.OpenRead(file))
            using (var streamContent = new StreamContent(fs))
            {
                using (var uploadData = new MultipartFormDataContent())
                {
                    uploadData.Add(actionContent, "action");
                    uploadData.Add(fileNameContent, "filename");
                    uploadData.Add(editTokenContent, "token");
                    uploadData.Add(formatContent, "format");
                    uploadData.Add(textContent, "text");
                    uploadData.Add(commentContent, "comment");
                    if (force)
                    {
                        var forceContent = new StringContent("1");
                        uploadData.Add(forceContent, "ignorewarnings");
                    }
                    uploadData.Add(streamContent, "file", filename);

                    using (HttpResponseMessage response = await _client.PostAsync(_api, uploadData))
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        log.Debug(responseContent);
                        return new UploadResponse(responseContent);
                    }
                }
            }
        }

        private async Task<bool> IsUserConfirmedAsync(string username)
        {
            Uri uri = _api.ApiQuery("list=users&usprop=groups&ususers=" + username);
            using (HttpResponseMessage response = await _client.GetAsync(uri))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNode node = xml.SelectSingleNode("/api/query/users/user/groups/g[.=\"autoconfirmed\"]");
                return node != null;
            }
        }

        private async Task<string> GetEditTokenAsync()
        {
            Uri uri = _api.ApiQuery("prop=info&intoken=edit&titles=Foo&indexpageids=1");
            using (HttpResponseMessage response = await _client.GetAsync(uri))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNode node = xml.SelectSingleNode("/api/query/pages/page");
                return node?.Attributes["edittoken"]?.Value;
            }
        }

        private async Task GetPermittedTypes()
        {
            Uri uri = _api.ApiQuery("meta=siteinfo&siprop=fileextensions");
            using (HttpResponseMessage response = await _client.GetAsync(uri))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNodeList fileExtensions = xml.SelectNodes("/api/query/fileextensions/fe");
                if (fileExtensions.Count > 0)
                {
                    _permittedTypes = new List<string>();
                    foreach (XmlNode fe in fileExtensions)
                        _permittedTypes.Add("." + fe.Attributes["ext"].Value);
                }
            }
        }

        private async Task<bool> IsAuthorizedForUploadFilesAsync(string username)
        {
            Uri uri = _api.ApiQuery("prop=revisions&titles=MediaWiki:UploadFilesUsers.css&rvprop=content&rvlimit=1");
            using (HttpResponseMessage response = await _client.GetAsync(uri))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNode revision = xml.SelectSingleNode("/api/query/pages/page/revisions/rev");
                return revision != null && revision.InnerText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToUpperInvariant()).Contains(username.ToUpperInvariant()); ;
            }
        }

        private async Task<LoginResponse> AttemptLoginAsync(List<KeyValuePair<string, string>> loginParams)
        {
            using (var formParams = new FormUrlEncodedContent(loginParams))
            {
                using (HttpResponseMessage response = await _client.PostAsync(_api, formParams))
                {
                    XmlDocument xml = await GetXml(response.Content);
                    XmlNode login = xml.SelectSingleNode("/api/login");
                    return new LoginResponse
                    {
                        Result = login?.Attributes["result"]?.Value,
                        Token = login?.Attributes["token"]?.Value,
                    };
                }
            }
        }

        private static XmlDocument GetXmlDocument(string xmlString)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlString);
            return doc;
        }

        private static async Task<XmlDocument> GetXml(HttpContent content)
        {
            string response = await content.ReadAsStringAsync();
            log.Debug(response);
            return GetXmlDocument(response);
        }

        private string UserAgent
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                Version version = assembly.GetName().Version;
                AssemblyTitleAttribute title = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                    assembly, typeof(AssemblyTitleAttribute));
                string userAgent = $"{title.Title}/{version.Major}.{version.Minor}.{version.Build}";
                return userAgent;
            }
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}