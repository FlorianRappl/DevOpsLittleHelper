using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevOpsLittleHelper
{
    public class NpmPackageInfo
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_rev")]
        public string Revision { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("dist-tags")]
        public NpmDistTags DistTags { get; set; }

        [JsonProperty("versions")]
        public JObject Versions { get; set; }

        public class NpmDistTags
        {
            [JsonProperty("latest")]
            public string Latest { get; set; }
        }
    }
}
