using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace UploadFiles
{
    internal class FileUploader : IDisposable
    {
        private Uri _api;
        private string _editToken;
        private string _defaultText;
        private HttpClient _client;
        private string _comment;

        public FileUploader(string site, string defaultText, string category, string comment)
        {
            _defaultText = defaultText == null ? "" : defaultText;
            if (!string.IsNullOrEmpty(category))
                _defaultText += "\n[[Category:" + category + "]]";
            _comment = comment == null ? "" : comment;
            _api = new Uri(site + "/api.php");

            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public async Task<bool> LoginAsync(string username, string password)
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
                if (!await IsUserConfirmedAsync(username))
                    return false;
                _editToken = await GetEditTokenAsync();
                return !string.IsNullOrEmpty(_editToken);
            }
            catch (XmlException)
            {
                return false;
            }
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
                streamContent.Headers.Add("Content-Type", "application/octet-stream");
                streamContent.Headers.Add("Content-Disposition", $"form-data; name=\"file\"; filename=\"{filename}\"");
                actionContent.Headers.Add("Content-Disposition", "form-data; name=\"action\"");
                fileNameContent.Headers.Add("Content-Disposition", "form-data; name=\"filename\"");
                editTokenContent.Headers.Add("Content-Disposition", "form-data; name=\"token\"");
                textContent.Headers.Add("Content-Disposition", "form-data; name=\"text\"");
                commentContent.Headers.Add("Content-Disposition", "form-data; name=\"comment\"");
                formatContent.Headers.Add("Content-Disposition", "form-data; name=\"format\"");
                using (var uploadParams = new MultipartFormDataContent())
                {
                    uploadParams.Add(actionContent, "action");
                    uploadParams.Add(fileNameContent, "filename");
                    uploadParams.Add(editTokenContent, "token");
                    uploadParams.Add(formatContent, "format");
                    uploadParams.Add(textContent, "text");
                    uploadParams.Add(commentContent, "comment");
                    if (force)
                    {
                        var forceContent = new StringContent("1");
                        forceContent.Headers.Add("Content-Disposition", "form-data; name=\"ignorewarnings\"");
                        uploadParams.Add(forceContent, "ignorewarnings");
                    }
                    uploadParams.Add(streamContent, "file", filename);

                    using (HttpResponseMessage response = await _client.PostAsync(_api, uploadParams))
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        return new UploadResponse(responseContent);
                    }
                }
            }
        }

        private async Task<bool> IsUserConfirmedAsync(string username)
        {
            string url = _api.OriginalString + "?action=query&list=users&usprop=groups&format=xml&ususers=" + username;
            using (HttpResponseMessage response = await _client.GetAsync(url))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNode node = xml.SelectSingleNode("/api/query/users/user/groups/g[.=\"autoconfirmed\"]");
                return node != null;
            }
        }

        private async Task<string> GetEditTokenAsync()
        {
            string url = _api.OriginalString + "?action=query&prop=info&intoken=edit&titles=Foo&format=xml&indexpageids=1";
            using (HttpResponseMessage response = await _client.GetAsync(url))
            {
                XmlDocument xml = await GetXml(response.Content);
                XmlNode node = xml.SelectSingleNode("/api/query/pages/page");
                if (node == null)
                    return null;
                return node?.Attributes["edittoken"]?.Value;
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