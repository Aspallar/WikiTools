using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using WikiaClientLibrary;

namespace WikiToolsShared
{
    public class RatingPage : WikiaPage
    {
        public RatingPage(WikiaClient client, string pageTitle) : base(client, pageTitle) { }

        public List<VoteTotal> Votes { get; internal set; }

        public new void Open()
        {
            base.Open();
            if (!Exists)
                throw new MissingRatingsPageException();
            Votes = JsonConvert.DeserializeObject<List<VoteTotal>>(Content);
            Content = null;
        }

        public new void Save(string summary)
        {
            // using this convoluted method because we want to indent by a single space
            // in order to keep to the same format as used by 
            // http://magicarena.fandom.com/wiki/MediaWiki:Ratings.js

            MemoryStream memoryStream = new MemoryStream();
            using (TextWriter textWriter = new StreamWriter(memoryStream))
            {
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Indentation = 1,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                };
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, Votes);
                textWriter.Flush();
                memoryStream.Position = 0;
                var reader = new StreamReader(memoryStream);
                Content = reader.ReadToEnd();
            }
            base.Save(summary);
        }
    }
}
