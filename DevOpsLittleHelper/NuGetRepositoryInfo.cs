using Newtonsoft.Json;
using System;
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
            public String Id { get; set; }

            [JsonProperty("@type")]
            public String Type { get; set; }

            [JsonProperty("comment")]
            public String Comment { get; set; }
        }
    }
}
