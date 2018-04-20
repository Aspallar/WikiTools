using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace UploadFiles
{
    internal class FileUploader : IDisposable
    {
        private Uri _api;
        private string _editToken;
        private HttpClient _client;

        // TODO: implement custom text

        public FileUploader(string site)
        {
            _api = new Uri(site + "/api.php");
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            _client = new HttpClient(handler);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var loginParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("action", "login"),
                new KeyValuePair<string, string>("format", "xml"),
                new KeyValuePair<string, string>("lgname", username),
            };
            LoginResponse response = await AttemptLoginAsync(loginParams);
            if (response.Result == ResponseCodes.Success)
                return true;
            if (response.Result != ResponseCodes.NeedToken)
                return false;
            loginParams.Add(new KeyValuePair<string, string>("lgtoken", response.Token));
            loginParams.Add(new KeyValuePair<string, string>("lgpassword", password));
            response = await AttemptLoginAsync(loginParams);
            if (response.Result != ResponseCodes.Success)
                return false;

            _editToken = await GetEditTokenAsync();
            return !string.IsNullOrEmpty(_editToken);
        }


        public async Task<UploadResponse> UpLoadAsync(string file, bool force = false)
        {
            // TODO: implement force
            using (var actionContent = new StringContent("upload"))
            using (var fileNameContent = new StringContent(Path.GetFileName(file)))
            using (var editTokenContent = new StringContent(_editToken))
            using (var textContent = new StringContent("== Summary ==\n== Licensing ==\n{{Fairuse}}\n"))
            using (var formatContent = new StringContent("xml"))
            using (FileStream fs = File.OpenRead(file))
            using (var streamContent = new StreamContent(fs))
            {
                streamContent.Headers.Add("Content-Type", "application/octet-stream");
                streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + Path.GetFileName(file) + "\"");
                actionContent.Headers.Add("Content-Disposition", "form-data; name=\"action\"");
                fileNameContent.Headers.Add("Content-Disposition", "form-data; name=\"filename\"");
                editTokenContent.Headers.Add("Content-Disposition", "form-data; name=\"token\"");
                textContent.Headers.Add("Content-Disposition", "form-data; name=\"text\"");
                formatContent.Headers.Add("Content-Disposition", "form-data; name=\"format\"");
                using (var uploadParams = new MultipartFormDataContent())
                {
                    uploadParams.Add(actionContent, "action");
                    uploadParams.Add(fileNameContent, "filename");
                    uploadParams.Add(editTokenContent, "token");
                    uploadParams.Add(formatContent, "format");
                    uploadParams.Add(textContent, "text");
                    if (force)
                    {
                        var forceContent = new StringContent("1");
                        forceContent.Headers.Add("Content-Disposition", "form-data; name=\"ignorewarnings\"");
                        uploadParams.Add(forceContent, "ignorewarnings");
                    }
                    uploadParams.Add(streamContent, "file", Path.GetFileName(file));

                    using (HttpResponseMessage response = await _client.PostAsync(_api, uploadParams))
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        return new UploadResponse(responseContent);
                    }
                }
            }
        }

        private async Task<string> GetEditTokenAsync()
        {
            string url = _api.OriginalString + "?action=query&prop=info&intoken=edit&titles=Foo&format=xml&indexpageids=1";
            using (HttpResponseMessage response = await _client.GetAsync(url))
            {
                string content = await response.Content.ReadAsStringAsync();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
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
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(responseContent);
                    XmlNode login = doc.SelectSingleNode("/api/login");
                    return new LoginResponse
                    {
                        Result = login?.Attributes["result"]?.Value,
                        Token = login?.Attributes["token"]?.Value,
                    };
                }
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