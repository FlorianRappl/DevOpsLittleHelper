using Newtonsoft.Json;
using System;
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
            public String Lower { get; set; }

            [JsonProperty("upper")]
            public String Upper { get; set; }

            [JsonProperty("count")]
            public Int32 Count { get; set; }
        }
    }
}
