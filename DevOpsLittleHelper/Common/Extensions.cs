using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
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

        public static async Task<string> GetContent(this Stream item)
        {
            using (var ms = new MemoryStream())
            {
                await item.CopyToAsync(ms).ConfigureAwait(false);
                var raw = ms.ToArray();
                var offset = 0;
                var bom = new Byte[] { 0xEF, 0xBB, 0xBF };

                while (raw.StartsWith(bom, offset))
                {
                    offset++;
                }

                return Encoding.UTF8.GetString(raw, offset, raw.Length - offset);
            }
        }

        public static bool StartsWith(this byte[] content, byte[] values, int offset) => 
            content.Length > offset && values.Any(m => m == content[offset]);

        public static string ReplaceFromStartToQuote(this string content, string start, string replacement)
        {
            var index = content.IndexOf(start);

            if (index != -1)
            {
                var end = index + start.Length;
                var head = content.Substring(0, end);
                var tail = content.Substring(content.IndexOf('"', end));
                return $"{head}{replacement}{tail}";
            }

            return content;
        }
    }
}
