using Newtonsoft.Json;
using System.Collections.Generic;

namespace DevOpsLittleHelper
{
    public class NuGetRepositoryInfo
    {
        [JsonProperty("resources")]
        public List<NuGetResource> Resources { get; set; }

        public class NuGetResource
        {
            [JsonProperty("@id")]
            public string Id { get; set; }

            [JsonProperty("@type")]
            public string Type { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }
        }
    }
}
