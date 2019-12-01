namespace GetJs
{
    internal sealed class Page
    {
        public string Title { get; private set; }

        public string Content{ get; private set; }

        public string Name => Title.Substring("MediaWiki:".Length);

        public Page(string title, string content)
        {
            Title = title;
            Content = content;
        }
    }
}
