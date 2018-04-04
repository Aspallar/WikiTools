using Newtonsoft.Json;
using System.Collections.Generic;

namespace WikiaClientLibrary
{
    public class PageInfoRootResponse
    {
        [JsonProperty("query")]
        public QueryResponse Query { get; set; }
    }

    public class QueryResponse
    {
        [JsonProperty("pages")]
        public Dictionary<int, PageInfoResponse> Pages { get; set; }
    }

    public class PageInfoResponse
    {
        [JsonProperty("pageid")]
        public int PageId { get; set; }

        [JsonProperty("starttimestamp")]
        public string StartTimeStamp { get; set; }

        [JsonProperty("missing")]
        public string MissingMarker { get; set; }

        [JsonProperty("edittoken")]
        public string EditToken { get; set; }

        [JsonProperty("revisions")]
        public List<RevisionResponse> Revisions { get; set; }

        public bool PageExists => MissingMarker == null;
    }

    public class RevisionResponse
    {
        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("*")]
        public string Contents { get; set; }
    }

}
