using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class NpmHelper : HelperBase
    {
        private readonly string _pat;

        public NpmHelper(string pat, TraceWriter log)
            : base(log)
        {
            _pat = pat;
        }

        public async Task<string> ReadPackageVersion(string packageName)
        {
            using (var http = new HttpClient())
            {
                var auth = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{_pat}"));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

                Log($"Set basic header requesting version URL.");
                return await GetPackageVersion(http, packageName).ConfigureAwait(false);
            }
        }

        private static async Task<string> GetPackageVersion(HttpClient http, string packageName)
        {
            var url = new Uri($"{Constants.NpmApiRoot}/{packageName}");
            var content = await http.GetStringAsync(url).ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<NpmPackageInfo>(content);
            return obj.DistTags?.Latest;
        }
    }
}
