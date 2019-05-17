using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal class NugetHelper : HelperBase
    {
        private static readonly string PackageVersionType = "RegistrationsBaseUrl/Versioned";

        private readonly string _pat;

        public NugetHelper(string pat, TraceWriter log)
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
                var versionUrl = await GetPackageVersionUrl(http).ConfigureAwait(false);
                Log($"Received version URL '${versionUrl}'.");
                return await GetPackageVersion(http, versionUrl, packageName).ConfigureAwait(false);
            }
        }

        private static async Task<string> GetPackageVersion(HttpClient http, string versionUrl, string packageName)
        {
            var url = new Uri($"{versionUrl}/{packageName}");
            var content = await http.GetStringAsync(url).ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<NuGetPackageInfo>(content);
            return obj.Items.FirstOrDefault().Upper;
        }

        private static async Task<string> GetPackageVersionUrl(HttpClient http)
        {
            var url = new Uri($"{Constants.NuGetApiRoot}/v3/index.json");
            var content = await http.GetStringAsync(url).ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<NuGetRepositoryInfo>(content);
            return obj.Resources.FirstOrDefault(m => m.Type == PackageVersionType).Id.TrimEnd('/');
        }
    }
}
