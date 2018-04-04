using Newtonsoft.Json;
using System;

namespace WikiaClientLibrary
{
    public class EditError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }
    }
}
