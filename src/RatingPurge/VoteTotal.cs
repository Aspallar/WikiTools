using Newtonsoft.Json;

namespace RatingPurge
{
    public class VoteTotal
    {
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "total", Required = Required.Always)]
        public long Total { get; set; }

        [JsonProperty(PropertyName = "votes", Required = Required.Always)]
        public long Votes { get; set; }
    }
}
