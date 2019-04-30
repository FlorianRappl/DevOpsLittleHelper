using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsLittleHelper
{
    internal static class Extensions
    {
        public static async Task<WebhookRequestEntity> GetRequestData(this Stream body)
        {
            var reader = new StreamReader(body);
            var requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<WebhookRequestEntity>(requestBody);
        }

        public static async Task<String> GetContent(this Stream item)
        {
            using (var ms = new MemoryStream())
            {
                await item.CopyToAsync(ms).ConfigureAwait(false);
                var raw = ms.ToArray();
                return Encoding.UTF8.GetString(raw);
            }
        }
    }
}
