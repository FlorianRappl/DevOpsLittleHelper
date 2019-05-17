using Newtonsoft.Json;
using System.Collections.Generic;

namespace DevOpsLittleHelper
{
    public class NuGetPackageInfo
    {
        [JsonProperty("items")]
        public List<NuGetPackageItem> Items { get; set; }

        public class NuGetPackageItem
        {
            [JsonProperty("lower")]
            public string Lower { get; set; }

            [JsonProperty("upper")]
            public string Upper { get; set; }

            [JsonProperty("count")]
            public int Count { get; set; }
        }
    }
}
