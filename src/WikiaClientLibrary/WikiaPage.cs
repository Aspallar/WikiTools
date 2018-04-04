using System.Net;
using System.Threading.Tasks;

namespace WikiaClientLibrary
{
    public class WikiaPage
    {
        private WikiaClient _client;
        private PageInfo _pageInfo;

        public string PageTitle { get; private set; }

        public string Content
        {
            get
            {
                return _pageInfo.Contents;
            }
            set
            {
                _pageInfo.Contents = value;
            }
        }

        public bool Exists => _pageInfo.Exists;

        public WikiaPage(WikiaClient client, string pageTitle)
        {
            _client = client;
            _pageInfo = new PageInfo();
            PageTitle = pageTitle;
        }

        public void Load()
        {
            try
            {
                Content = _client.GetPageContents(PageTitle);
                _pageInfo.Exists = true;
            }
            catch (WebException ex)
            {
                if (!HandleLoadError(ex.Message))
                    throw;
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                Content = await _client.GetPageContentsAsync(PageTitle);
                _pageInfo.Exists = true;
            }
            catch (WebException ex)
            {
                if (!HandleLoadError(ex.Message))
                    throw;
            }
        }

        public void Open()
        {
            _pageInfo = _client.GetPageInfoWithContent(PageTitle);
        }

        public async Task OpenAsync()
        {
            _pageInfo = await _client.GetPageInfoWithContentAsync(PageTitle);
        }

        private bool HandleLoadError(string message)
        {
            if (HttpErrors.Is404NotFoundError(message))
            {
                Content = "";
                _pageInfo.Exists = false;
                return true;
            }
            return false;
        }

        public void Save(string summary)
        {
            GetEditTokenIfNeeded();
            _client.SavePage(PageTitle, _pageInfo, summary);
        }

        public async Task SaveAsync(string summary)
        {
            GetEditTokenIfNeeded();
            await _client.SavePageAsync(PageTitle, _pageInfo, summary);
        }

        private void GetEditTokenIfNeeded()
        {
            if (string.IsNullOrEmpty(_pageInfo.EditToken))
            {
                EditInfo edit = _client.GetEditToken(PageTitle);
                _pageInfo.EditToken = edit.EditToken;
                _pageInfo.StartTimeStamp = edit.StartTimeStamp;
            }
        }


    }
}
